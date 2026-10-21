<?php

declare(strict_types=1);

namespace Database\Seeders;

use App\Enums\TaskStatusEnum;
use App\Enums\TaskTypeEnum;
use App\Enums\UserRoleEnum;
use App\Models\Task;
use App\Models\User;
use Illuminate\Database\Seeder;
use Illuminate\Support\Str;

/** Demo data. Idempotent: safe to run again. Create the hub token with `php artisan paxscan:hub-token`. */
class DatabaseSeeder extends Seeder
{
    public function run(): void
    {
        User::query()->firstOrCreate(
            ['email' => 'admin@paxscan.test'],
            ['name' => 'Demo Admin', 'password' => 'password', 'role' => UserRoleEnum::Admin],
        );

        // Hub accounts never sign in with a password.
        User::query()->firstOrCreate(
            ['email' => 'hub@paxscan.test'],
            ['name' => 'Demo Hub', 'password' => Str::random(40), 'role' => UserRoleEnum::Hub],
        );

        $tasks = [
            ['Prepare breakfast buffet', TaskTypeEnum::Chef, 'Tier A dining room, opens 06:30.'],
            ['Restock pantry station 2', TaskTypeEnum::Chef, 'Check dry goods and beverages.'],
            ['Lunch service — tier B', TaskTypeEnum::Chef, null],
            ['Patrol deck 5', TaskTypeEnum::Guard, 'Sections A–C, every 2 hours.'],
            ['Cabin inspection deck 3', TaskTypeEnum::Guard, 'Verify occupancy against the manifest.'],
            ['Night watch bridge access', TaskTypeEnum::Guard, null],
        ];

        foreach ($tasks as [$name, $type, $description]) {
            Task::query()->firstOrCreate(
                ['name' => $name],
                ['type' => $type, 'description' => $description, 'status' => TaskStatusEnum::Pending],
            );
        }
    }
}
