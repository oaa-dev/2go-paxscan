<?php

declare(strict_types=1);

namespace App\Http\Resources;

use App\Models\Task;
use Illuminate\Http\Request;
use Illuminate\Http\Resources\Json\JsonResource;

/** @mixin Task */
final class TaskResource extends JsonResource
{
    /** @return array<string, mixed> */
    public function toArray(Request $request): array
    {
        return [
            'id' => $this->id,
            'name' => $this->name,
            'type' => $this->type->value,
            'description' => $this->description,
            'status' => $this->status->value,
            'version' => $this->version,
            'created_at' => $this->created_at->toIso8601ZuluString(),
            'updated_at' => $this->updated_at->toIso8601ZuluString(),
            // Key absent = not requested; [] = requested and none.
            'status_updates' => $this->whenLoaded(
                'statusUpdates',
                fn () => TaskStatusUpdateResource::collection($this->statusUpdates),
            ),
        ];
    }
}
