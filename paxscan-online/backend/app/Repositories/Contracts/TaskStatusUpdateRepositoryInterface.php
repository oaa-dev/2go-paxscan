<?php

declare(strict_types=1);

namespace App\Repositories\Contracts;

use App\Models\TaskStatusUpdate;

interface TaskStatusUpdateRepositoryInterface
{
    public function find(string $id): ?TaskStatusUpdate;

    public function seqTakenByOther(string $deviceId, int $deviceSeq, string $exceptId): bool;

    /** @param array<string, mixed> $attributes */
    public function create(array $attributes): TaskStatusUpdate;
}
