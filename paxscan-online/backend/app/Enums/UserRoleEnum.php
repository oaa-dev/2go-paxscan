<?php

declare(strict_types=1);

namespace App\Enums;

enum UserRoleEnum: string
{
    /** Signs in to the admin portal. */
    case Admin = 'admin';

    /** A desktop hub; authenticates with a `hub.sync` token only. */
    case Hub = 'hub';
}
