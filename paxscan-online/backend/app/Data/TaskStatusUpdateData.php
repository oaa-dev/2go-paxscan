<?php

declare(strict_types=1);

namespace App\Data;

use App\Data\Casts\IsoDateTimeCast;
use App\Enums\TaskStatusEnum;
use Carbon\CarbonImmutable;
use Spatie\LaravelData\Attributes\MapInputName;
use Spatie\LaravelData\Attributes\WithCast;
use Spatie\LaravelData\Data;
use Spatie\LaravelData\Mappers\SnakeCaseMapper;

/** One device status update as uploaded by a hub. */
#[MapInputName(SnakeCaseMapper::class)]
final class TaskStatusUpdateData extends Data
{
    public function __construct(
        public string $id,
        public string $taskId,
        public TaskStatusEnum $fromStatus,
        public TaskStatusEnum $toStatus,
        /** Online task version the hub last downloaded. */
        public int $baseVersion,
        public string $deviceId,
        public int $deviceSeq,
        #[WithCast(IsoDateTimeCast::class)]
        public CarbonImmutable $deviceCreatedAt,
        #[WithCast(IsoDateTimeCast::class)]
        public CarbonImmutable $hubReceivedAt,
    ) {}
}
