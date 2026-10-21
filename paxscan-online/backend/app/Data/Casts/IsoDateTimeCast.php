<?php

declare(strict_types=1);

namespace App\Data\Casts;

use Carbon\CarbonImmutable;
use Spatie\LaravelData\Casts\Cast;
use Spatie\LaravelData\Casts\Uncastable;
use Spatie\LaravelData\Data;
use Spatie\LaravelData\Support\Creation\CreationContext;
use Spatie\LaravelData\Support\DataProperty;

/**
 * Parses any ISO 8601 timestamp to UTC. The hub (.NET) sends 7 fractional digits, which the
 * default format-based cast (`Y-m-d\TH:i:s.uP`, max 6 digits) rejects.
 */
final class IsoDateTimeCast implements Cast
{
    /**
     * @param  array<string, mixed>  $properties
     * @param  CreationContext<Data>  $context
     */
    public function cast(DataProperty $property, mixed $value, array $properties, CreationContext $context): CarbonImmutable|Uncastable
    {
        return is_string($value) ? CarbonImmutable::parse($value)->utc() : Uncastable::create();
    }
}
