<?php

declare(strict_types=1);

namespace App\Http\Requests\Hub;

use App\Data\TaskStatusUpdateData;
use App\Enums\TaskStatusEnum;
use Illuminate\Foundation\Http\FormRequest;
use Illuminate\Support\Collection;
use Illuminate\Validation\Rule;

final class PushTaskUpdatesRequest extends FormRequest
{
    public const MAX_ITEMS = 200;

    /** @return array<string, mixed> */
    public function rules(): array
    {
        return [
            'hub_id' => ['required', 'uuid'],
            'items' => ['required', 'array', 'min:1', 'max:'.self::MAX_ITEMS],
            'items.*.id' => ['required', 'uuid', 'distinct'],
            'items.*.task_id' => ['required', 'uuid'],
            'items.*.from_status' => ['required', Rule::enum(TaskStatusEnum::class)],
            'items.*.to_status' => ['required', Rule::enum(TaskStatusEnum::class)],
            'items.*.base_version' => ['required', 'integer', 'min:1'],
            'items.*.device_id' => ['required', 'uuid'],
            'items.*.device_seq' => ['required', 'integer', 'min:1'],
            'items.*.device_created_at' => ['required', 'date'],
            'items.*.hub_received_at' => ['required', 'date'],
        ];
    }

    /** @return Collection<int, TaskStatusUpdateData> */
    public function items(): Collection
    {
        /** @var list<array<string, mixed>> $items */
        $items = $this->validated('items');

        return collect($items)->map(fn (array $item) => TaskStatusUpdateData::from($item));
    }
}
