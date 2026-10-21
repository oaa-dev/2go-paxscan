<?php

declare(strict_types=1);

namespace App\Http\Controllers\Api\V1;

use App\Data\TaskData;
use App\Http\Controllers\Controller;
use App\Http\Requests\Task\IndexTaskRequest;
use App\Http\Requests\Task\StoreTaskRequest;
use App\Http\Requests\Task\UpdateTaskRequest;
use App\Http\Resources\TaskResource;
use App\Models\Task;
use App\Services\TaskService;
use App\Traits\ApiResponse;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

final class TaskController extends Controller
{
    use ApiResponse;

    public function __construct(private readonly TaskService $tasks) {}

    public function index(IndexTaskRequest $request): JsonResponse
    {
        return $this->paginatedResponse(TaskResource::collection($this->tasks->paginate($request->perPage())), 'Tasks retrieved');
    }

    public function store(StoreTaskRequest $request): JsonResponse
    {
        $task = $this->tasks->create(TaskData::from($request->validated()));

        return $this->successResponse(new TaskResource($task), 'Task created', 201);
    }

    public function show(Request $request, Task $task): JsonResponse
    {
        $task = $this->tasks->show($task, $request->query('include') === 'statusUpdates');

        return $this->successResponse(new TaskResource($task), 'Task retrieved');
    }

    public function update(UpdateTaskRequest $request, Task $task): JsonResponse
    {
        $task = $this->tasks->update($task, TaskData::from($request->validated()));

        return $this->successResponse(new TaskResource($task), 'Task updated');
    }

    public function destroy(Task $task): JsonResponse
    {
        $this->tasks->delete($task);

        return $this->successResponse(message: 'Task deleted');
    }
}
