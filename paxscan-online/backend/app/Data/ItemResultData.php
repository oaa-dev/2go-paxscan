<?php

declare(strict_types=1);

namespace App\Data;

use App\Enums\UpdateResultEnum;
use Spatie\LaravelData\Attributes\MapOutputName;
use Spatie\LaravelData\Data;
use Spatie\LaravelData\Mappers\SnakeCaseMapper;

#[MapOutputName(SnakeCaseMapper::class)]
final class ItemResultData extends Data
{
    public function __construct(
        public string $id,
        public UpdateResultEnum $result,
        public ?string $reason = null,
        public ?int $taskVersion = null,
    ) {}
}
