<?php

declare(strict_types=1);

namespace App\Http\Resources;

use App\Models\Task;
use Illuminate\Http\Request;
use Illuminate\Http\Resources\Json\JsonResource;
use Illuminate\Support\Collection;

/** @property array{dataset_version: ?string, checksum: string, tasks: Collection<int, Task>} $resource */
final class TaskSnapshotResource extends JsonResource
{
    /** @return array<string, mixed> */
    public function toArray(Request $request): array
    {
        return [
            'dataset_version' => $this->resource['dataset_version'],
            'checksum' => $this->resource['checksum'],
            'tasks' => TaskResource::collection($this->resource['tasks'])->resolve($request),
        ];
    }
}
