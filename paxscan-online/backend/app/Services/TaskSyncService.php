<?php

declare(strict_types=1);

namespace App\Services;

use App\Data\ItemResultData;
use App\Data\TaskStatusUpdateData;
use App\Enums\UpdateResultEnum;
use App\Models\Task;
use App\Repositories\Contracts\TaskRepositoryInterface;
use App\Repositories\Contracts\TaskStatusUpdateRepositoryInterface;
use Illuminate\Database\UniqueConstraintViolationException;
use Illuminate\Support\Collection;
use Illuminate\Support\Facades\DB;

/**
 * Hub ↔ Online sync. Uploads are idempotent by update id; each item is applied in its own
 * transaction so one bad item cannot fail a batch. Device clocks never decide anything.
 */
final class TaskSyncService
{
    public const REASON_TASK_NOT_FOUND = 'task_not_found';

    public const REASON_INVALID_TRANSITION = 'invalid_transition';

    public const REASON_DUPLICATE_SEQ = 'duplicate_seq';

    public function __construct(
        private readonly TaskRepositoryInterface $tasks,
        private readonly TaskStatusUpdateRepositoryInterface $updates,
    ) {}

    /** @return array{dataset_version: ?string, checksum: string, tasks: Collection<int, Task>} */
    public function snapshot(): array
    {
        $tasks = $this->tasks->snapshot();

        return [
            'dataset_version' => $tasks->max('updated_at')?->toIso8601String(),
            'checksum' => self::checksum($tasks),
            'tasks' => $tasks,
        ];
    }

    /**
     * sha256 of `id:version` lines sorted by id, joined with "\n" (see contracts/openapi.yaml).
     *
     * @param  Collection<int, Task>  $tasks
     */
    public static function checksum(Collection $tasks): string
    {
        $lines = $tasks->map(fn (Task $task) => strtolower($task->id).':'.$task->version)->sort()->values();

        return hash('sha256', $lines->implode("\n"));
    }

    /**
     * @param  Collection<int, TaskStatusUpdateData>  $items
     * @return Collection<int, ItemResultData> in request order
     */
    public function push(string $hubId, Collection $items): Collection
    {
        $results = $items
            ->sortBy([['hubReceivedAt', 'asc'], ['deviceSeq', 'asc']])
            ->mapWithKeys(fn (TaskStatusUpdateData $item) => [$item->id => $this->apply($hubId, $item)]);

        return $items->map(fn (TaskStatusUpdateData $item) => $results[$item->id])->values();
    }

    private function apply(string $hubId, TaskStatusUpdateData $item): ItemResultData
    {
        try {
            return DB::transaction(fn () => $this->applyInTransaction($hubId, $item));
        } catch (UniqueConstraintViolationException) {
            // A concurrent request stored the same id first.
            return new ItemResultData($item->id, UpdateResultEnum::AlreadyReceived);
        }
    }

    private function applyInTransaction(string $hubId, TaskStatusUpdateData $item): ItemResultData
    {
        if ($this->updates->find($item->id) !== null) {
            return new ItemResultData($item->id, UpdateResultEnum::AlreadyReceived);
        }

        if ($this->updates->seqTakenByOther($item->deviceId, $item->deviceSeq, $item->id)) {
            return new ItemResultData($item->id, UpdateResultEnum::Rejected, self::REASON_DUPLICATE_SEQ);
        }

        $task = $this->tasks->findForUpdateWithTrashed($item->taskId);

        [$result, $reason] = match (true) {
            $task === null || $task->trashed() => [UpdateResultEnum::Rejected, self::REASON_TASK_NOT_FOUND],
            $item->fromStatus === $item->toStatus => [UpdateResultEnum::Rejected, self::REASON_INVALID_TRANSITION],
            // Last received wins; a stale base is applied but flagged for review.
            $item->baseVersion !== $task->version || $item->fromStatus !== $task->status => [UpdateResultEnum::Conflict, null],
            default => [UpdateResultEnum::Accepted, null],
        };

        if ($result !== UpdateResultEnum::Rejected) {
            $this->tasks->update($task, ['status' => $item->toStatus, 'version' => $task->version + 1]);
        }

        $this->updates->create([
            'id' => $item->id,
            'task_id' => $item->taskId,
            'from_status' => $item->fromStatus,
            'to_status' => $item->toStatus,
            'base_version' => $item->baseVersion,
            'device_id' => $item->deviceId,
            'device_seq' => $item->deviceSeq,
            'device_created_at' => $item->deviceCreatedAt,
            'hub_id' => $hubId,
            'hub_received_at' => $item->hubReceivedAt,
            'result' => $result,
            'reject_reason' => $reason,
        ]);

        return new ItemResultData(
            $item->id,
            $result,
            $reason,
            $result === UpdateResultEnum::Rejected ? null : $task->version,
        );
    }
}
