<?php

declare(strict_types=1);

namespace App\Models;

use App\Enums\TaskStatusEnum;
use App\Enums\UpdateResultEnum;
use Illuminate\Database\Eloquent\Attributes\DateFormat;
use Illuminate\Database\Eloquent\Attributes\Fillable;
use Illuminate\Database\Eloquent\Attributes\WithoutIncrementing;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Support\Carbon;

/**
 * Append-only record of a device status update uploaded by a hub. The id is generated on the device.
 *
 * @property string $id
 * @property string $task_id
 * @property TaskStatusEnum $from_status
 * @property TaskStatusEnum $to_status
 * @property int $base_version
 * @property string $device_id
 * @property int $device_seq
 * @property Carbon $device_created_at
 * @property string $hub_id
 * @property Carbon $hub_received_at
 * @property UpdateResultEnum $result
 * @property string|null $reject_reason
 * @property Carbon $created_at
 */
// Microseconds + offset: hub_received_at orders updates, so it must not be truncated to seconds.
#[DateFormat('Y-m-d H:i:s.uP')]
#[WithoutIncrementing]
#[Fillable([
    'id', 'task_id', 'from_status', 'to_status', 'base_version', 'device_id', 'device_seq',
    'device_created_at', 'hub_id', 'hub_received_at', 'result', 'reject_reason',
])]
class TaskStatusUpdate extends Model
{
    public const UPDATED_AT = null;

    protected $keyType = 'string';

    protected function casts(): array
    {
        return [
            'from_status' => TaskStatusEnum::class,
            'to_status' => TaskStatusEnum::class,
            'result' => UpdateResultEnum::class,
            'base_version' => 'integer',
            'device_seq' => 'integer',
            'device_created_at' => 'immutable_datetime',
            'hub_received_at' => 'immutable_datetime',
        ];
    }

    /** @return BelongsTo<Task, $this> */
    public function task(): BelongsTo
    {
        return $this->belongsTo(Task::class)->withTrashed();
    }
}
