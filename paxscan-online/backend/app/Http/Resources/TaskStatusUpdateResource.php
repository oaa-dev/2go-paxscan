<?php

declare(strict_types=1);

namespace App\Http\Resources;

use App\Models\TaskStatusUpdate;
use Illuminate\Http\Request;
use Illuminate\Http\Resources\Json\JsonResource;

/** @mixin TaskStatusUpdate */
final class TaskStatusUpdateResource extends JsonResource
{
    /** @return array<string, mixed> */
    public function toArray(Request $request): array
    {
        return [
            'id' => $this->id,
            'task_id' => $this->task_id,
            'from_status' => $this->from_status->value,
            'to_status' => $this->to_status->value,
            'base_version' => $this->base_version,
            'device_id' => $this->device_id,
            'device_seq' => $this->device_seq,
            'device_created_at' => $this->device_created_at->toIso8601ZuluString('microsecond'),
            'hub_id' => $this->hub_id,
            'hub_received_at' => $this->hub_received_at->toIso8601ZuluString('microsecond'),
            'result' => $this->result->value,
            'reject_reason' => $this->reject_reason,
            'created_at' => $this->created_at->toIso8601ZuluString('microsecond'),
        ];
    }
}
