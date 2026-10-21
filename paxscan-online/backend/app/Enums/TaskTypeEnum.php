<?php

declare(strict_types=1);

namespace App\Enums;

enum TaskTypeEnum: string
{
    case Chef = 'chef';
    case Guard = 'guard';

    /** @return list<string> */
    public static function values(): array
    {
        return array_column(self::cases(), 'value');
    }
}
