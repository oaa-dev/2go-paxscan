<?php

declare(strict_types=1);

namespace App\Http\Controllers\Api\V1\Hub;

use App\Http\Controllers\Controller;
use App\Http\Requests\Hub\PushTaskUpdatesRequest;
use App\Services\TaskSyncService;
use App\Traits\ApiResponse;
use Illuminate\Http\JsonResponse;

final class TaskUpdateController extends Controller
{
    use ApiResponse;

    public function __construct(private readonly TaskSyncService $sync) {}

    public function store(PushTaskUpdatesRequest $request): JsonResponse
    {
        $results = $this->sync->push($request->string('hub_id')->toString(), $request->items());

        return $this->successResponse(['results' => $results->toArray()], 'Updates processed');
    }
}
