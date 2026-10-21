<?php

declare(strict_types=1);

namespace App\Http\Controllers\Api\V1\Hub;

use App\Http\Controllers\Controller;
use App\Http\Resources\TaskSnapshotResource;
use App\Services\TaskSyncService;
use App\Traits\ApiResponse;
use Illuminate\Http\JsonResponse;

final class TaskSnapshotController extends Controller
{
    use ApiResponse;

    public function __construct(private readonly TaskSyncService $sync) {}

    public function __invoke(): JsonResponse
    {
        return $this->successResponse(new TaskSnapshotResource($this->sync->snapshot()), 'Task snapshot');
    }
}
