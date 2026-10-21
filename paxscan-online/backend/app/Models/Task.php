<?php

declare(strict_types=1);

namespace App\Models;

use App\Enums\TaskStatusEnum;
use App\Enums\TaskTypeEnum;
use Database\Factories\TaskFactory;
use Illuminate\Database\Eloquent\Attributes\Fillable;
use Illuminate\Database\Eloquent\Concerns\HasUuids;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\SoftDeletes;
use Illuminate\Support\Carbon;

/**
 * @property string $id
 * @property string $name
 * @property TaskTypeEnum $type
 * @property string|null $description
 * @property TaskStatusEnum $status
 * @property int $version
 * @property Carbon $created_at
 * @property Carbon $updated_at
 * @property Carbon|null $deleted_at
 */
#[Fillable(['name', 'type', 'description', 'status', 'version'])]
class Task extends Model
{
    /** @use HasFactory<TaskFactory> */
    use HasFactory;

    use HasUuids;
    use SoftDeletes;

    protected $attributes = [
        'status' => 'pending',
        'version' => 1,
    ];

    protected function casts(): array
    {
        return [
            'type' => TaskTypeEnum::class,
            'status' => TaskStatusEnum::class,
            'version' => 'integer',
        ];
    }

    /** @return HasMany<TaskStatusUpdate, $this> */
    public function statusUpdates(): HasMany
    {
        return $this->hasMany(TaskStatusUpdate::class);
    }
}
