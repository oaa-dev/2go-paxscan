<?php

declare(strict_types=1);

namespace App\Repositories;

use App\Models\Task;
use App\Repositories\Contracts\TaskRepositoryInterface;
use Illuminate\Contracts\Database\Eloquent\Builder;
use Illuminate\Contracts\Pagination\LengthAwarePaginator;
use Illuminate\Support\Collection;
use Spatie\QueryBuilder\AllowedFilter;
use Spatie\QueryBuilder\AllowedSort;
use Spatie\QueryBuilder\QueryBuilder;

final class TaskRepository implements TaskRepositoryInterface
{
    public function paginate(int $perPage): LengthAwarePaginator
    {
        return $this->query()->paginate($perPage)->withQueryString();
    }

    public function findById(string $id): Task
    {
        return Task::query()->findOrFail($id);
    }

    public function create(array $attributes): Task
    {
        return Task::query()->create($attributes);
    }

    public function update(Task $task, array $attributes): Task
    {
        $task->fill($attributes)->save();

        return $task;
    }

    public function delete(Task $task): void
    {
        $task->delete();
    }

    public function loadStatusUpdates(Task $task, int $limit): Task
    {
        return $task->load(['statusUpdates' => fn ($query) => $query
            ->orderByDesc('hub_received_at')
            ->orderByDesc('device_seq')
            ->limit($limit)]);
    }

    public function findForUpdateWithTrashed(string $id): ?Task
    {
        return Task::withTrashed()->lockForUpdate()->find($id);
    }

    public function snapshot(): Collection
    {
        return Task::query()->orderBy('id')->get();
    }

    /** @return QueryBuilder<Task> */
    protected function query(): QueryBuilder
    {
        return QueryBuilder::for(Task::class)
            ->allowedFilters(...$this->allowedFilters())
            ->allowedSorts(...$this->allowedSorts())
            ->defaultSort('-updated_at');
    }

    /**
     * Keep in sync with the frontend's buildTaskQuery(): undeclared filters are silently ignored.
     *
     * @return list<AllowedFilter>
     */
    protected function allowedFilters(): array
    {
        return [
            AllowedFilter::exact('type'),
            AllowedFilter::exact('status'),
            AllowedFilter::callback('q', fn (Builder $query, string $value) => $query->whereLike('name', '%'.$value.'%')),
        ];
    }

    /** @return list<AllowedSort> */
    protected function allowedSorts(): array
    {
        return [AllowedSort::field('name'), AllowedSort::field('updated_at')];
    }
}
