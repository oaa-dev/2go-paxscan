<?php

use App\Enums\TokenAbilityEnum;
use App\Models\Task;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Str;
use Laravel\Sanctum\Sanctum;
use Tests\TestCase;

/*
|--------------------------------------------------------------------------
| Test Case
|--------------------------------------------------------------------------
|
| The closure you provide to your test functions is always bound to a specific PHPUnit test
| case class. By default, that class is "PHPUnit\Framework\TestCase". Of course, you may
| need to change it using the "pest()" function to bind different classes or traits.
|
*/

pest()->extend(TestCase::class)
    ->use(RefreshDatabase::class)
    ->in('Feature');

/*
|--------------------------------------------------------------------------
| Expectations
|--------------------------------------------------------------------------
|
| When you're writing tests, you often need to check that values meet certain conditions. The
| "expect()" function gives you access to a set of "expectations" methods that you can use
| to assert different things. Of course, you may extend the Expectation API at any time.
|
*/

expect()->extend('toBeOne', function () {
    return $this->toBe(1);
});

/*
|--------------------------------------------------------------------------
| Functions
|--------------------------------------------------------------------------
|
| While Pest is very powerful out-of-the-box, you may have some testing code specific to your
| project that you don't want to repeat in every file. Here you can also expose helpers as
| global functions to help you to reduce the number of lines of code in your test files.
|
*/

function something()
{
    // ..
}

function actingAsAdmin(): User
{
    $user = User::factory()->create();
    Sanctum::actingAs($user, [TokenAbilityEnum::TasksManage->value]);

    return $user;
}

function actingAsHub(): User
{
    $user = User::factory()->hub()->create();
    Sanctum::actingAs($user, [TokenAbilityEnum::HubSync->value]);

    return $user;
}

/** A status update as the hub uploads it. */
function hubItem(Task $task, array $overrides = []): array
{
    return array_merge([
        'id' => (string) Str::uuid7(),
        'task_id' => $task->id,
        'from_status' => $task->status->value,
        'to_status' => 'done',
        'base_version' => $task->version,
        'device_id' => '01a13d32-340c-71a5-9808-9aa1faf9a974',
        'device_seq' => 1,
        // .NET DateTimeOffset format (7 fractional digits).
        'device_created_at' => '2026-10-15T07:59:00.1234567+00:00',
        'hub_received_at' => '2026-10-15T08:00:00.7654321+00:00',
    ], $overrides);
}

const HUB_ID = '01a13d26-679f-7140-ac7c-c16d75000000';
