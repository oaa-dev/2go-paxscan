<?php

declare(strict_types=1);

namespace App\Enums;

enum TokenAbilityEnum: string
{
    case TasksManage = 'tasks.manage';
    case HubSync = 'hub.sync';
}
