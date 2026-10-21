<?php

declare(strict_types=1);

namespace App\Services;

use App\Data\TaskData;
use App\Enums\TaskStatusEnum;
use App\Models\Task;
use App\Repositories\Contracts\TaskRepositoryInterface;
use Illuminate\Contracts\Pagination\LengthAwarePaginator;
use Spatie\LaravelData\Optional;

final class TaskService
{
    private const HISTORY_LIMIT = 200;

    public function __construct(private readonly TaskRepositoryInterface $tasks) {}

    /** @return LengthAwarePaginator<int, Task> */
    public function paginate(int $perPage): LengthAwarePaginator
    {
        return $this->tasks->paginate($perPage);
    }

    public function show(Task $task, bool $withHistory): Task
    {
        return $withHistory ? $this->tasks->loadStatusUpdates($task, self::HISTORY_LIMIT) : $task;
    }

    public function create(TaskData $data): Task
    {
        $attributes = $this->submitted($data);
        $attributes['status'] ??= TaskStatusEnum::Pending;
        $attributes['version'] = 1;

        return $this->tasks->create($attributes);
    }

    /** Applies only submitted fields; any real change bumps the version hubs and devices compare against. */
    public function update(Task $task, TaskData $data): Task
    {
        $task->fill($this->submitted($data));
        if (! $task->isDirty()) {
            return $task;
        }

        return $this->tasks->update($task, ['version' => $task->version + 1]);
    }

    public function delete(Task $task): void
    {
        $this->tasks->delete($task);
    }

    /** @return array<string, mixed> */
    private function submitted(TaskData $data): array
    {
        return collect($data->all())->reject(fn ($value) => $value instanceof Optional)->all();
    }
}
