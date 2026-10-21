<?php

declare(strict_types=1);

namespace App\Traits;

use Illuminate\Http\JsonResponse;
use Illuminate\Http\Resources\Json\JsonResource;
use Illuminate\Http\Resources\Json\ResourceCollection;

/** The `{success, message, data}` envelope every API response uses (see contracts/openapi.yaml). */
trait ApiResponse
{
    protected function successResponse(mixed $data = null, string $message = 'OK', int $status = 200): JsonResponse
    {
        if ($data instanceof JsonResource) {
            $data = $data->resolve(request());
        }

        return response()->json(['success' => true, 'message' => $message, 'data' => $data], $status);
    }

    /** @param  array<string, list<string>>|null  $errors */
    protected function errorResponse(string $message, int $status, ?array $errors = null): JsonResponse
    {
        return response()->json(array_filter(
            ['success' => false, 'message' => $message, 'errors' => $errors],
            fn ($value) => $value !== null,
        ), $status);
    }

    /** Spreads the paginator: rows at `data`, plus `links` and `meta` (not `data.data`). */
    protected function paginatedResponse(ResourceCollection $collection, string $message = 'OK'): JsonResponse
    {
        $payload = $collection->response(request())->getData(true);

        return response()->json(['success' => true, 'message' => $message, ...$payload]);
    }
}
