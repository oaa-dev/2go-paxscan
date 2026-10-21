<?php

declare(strict_types=1);

namespace App\Data;

use App\Enums\TaskStatusEnum;
use App\Enums\TaskTypeEnum;
use Spatie\LaravelData\Data;
use Spatie\LaravelData\Optional;

/**
 * Admin create/update input. `Optional` = not submitted; an explicit null description clears it.
 */
final class TaskData extends Data
{
    public function __construct(
        public string|Optional $name = new Optional,
        public TaskTypeEnum|Optional $type = new Optional,
        public string|null|Optional $description = new Optional,
        public TaskStatusEnum|Optional $status = new Optional,
    ) {}
}
