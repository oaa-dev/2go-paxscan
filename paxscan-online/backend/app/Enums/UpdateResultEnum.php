<?php

declare(strict_types=1);

namespace App\Enums;

/** Per-item result of a hub upload. `AlreadyReceived` is never stored, only returned. */
enum UpdateResultEnum: string
{
    case Accepted = 'accepted';
    case AlreadyReceived = 'already_received';
    case Conflict = 'conflict';
    case Rejected = 'rejected';
}
