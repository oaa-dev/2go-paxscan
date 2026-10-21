<?php

declare(strict_types=1);

namespace App\Repositories;

use App\Models\TaskStatusUpdate;
use App\Repositories\Contracts\TaskStatusUpdateRepositoryInterface;

final class TaskStatusUpdateRepository implements TaskStatusUpdateRepositoryInterface
{
    public function find(string $id): ?TaskStatusUpdate
    {
        return TaskStatusUpdate::query()->find($id);
    }

    public function seqTakenByOther(string $deviceId, int $deviceSeq, string $exceptId): bool
    {
        return TaskStatusUpdate::query()
            ->where('device_id', $deviceId)
            ->where('device_seq', $deviceSeq)
            ->whereKeyNot($exceptId)
            ->exists();
    }

    public function create(array $attributes): TaskStatusUpdate
    {
        return TaskStatusUpdate::query()->create($attributes);
    }
}
