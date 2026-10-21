<?php

declare(strict_types=1);

namespace App\Repositories\Contracts;

use App\Models\Task;
use Illuminate\Contracts\Pagination\LengthAwarePaginator;
use Illuminate\Database\Eloquent\ModelNotFoundException;
use Illuminate\Support\Collection;

interface TaskRepositoryInterface
{
    /** @return LengthAwarePaginator<int, Task> */
    public function paginate(int $perPage): LengthAwarePaginator;

    /** @throws ModelNotFoundException */
    public function findById(string $id): Task;

    /** @param array<string, mixed> $attributes */
    public function create(array $attributes): Task;

    /** @param array<string, mixed> $attributes */
    public function update(Task $task, array $attributes): Task;

    public function delete(Task $task): void;

    public function loadStatusUpdates(Task $task, int $limit): Task;

    /** Row-locked, including soft-deleted rows; call inside a transaction. */
    public function findForUpdateWithTrashed(string $id): ?Task;

    /** @return Collection<int, Task> Non-deleted tasks ordered by id. */
    public function snapshot(): Collection;
}
