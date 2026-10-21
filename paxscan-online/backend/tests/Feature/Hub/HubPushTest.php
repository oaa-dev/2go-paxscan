<?php

declare(strict_types=1);

use App\Models\Task;
use App\Models\TaskStatusUpdate;

function push(array $items)
{
    return test()->postJson('/api/v1/hub/task-updates', ['hub_id' => HUB_ID, 'items' => $items]);
}

it('applies an update based on the current version', function () {
    actingAsHub();
    $task = Task::factory()->create(['status' => 'pending', 'version' => 3]);

    push([hubItem($task, ['to_status' => 'in_progress'])])
        ->assertOk()
        ->assertJsonPath('data.results.0.result', 'accepted')
        ->assertJsonPath('data.results.0.task_version', 4);

    expect($task->fresh())->status->value->toBe('in_progress')->version->toBe(4);
    $stored = TaskStatusUpdate::query()->sole();
    expect($stored->hub_id)->toBe(HUB_ID)
        ->and($stored->hub_received_at->format('Y-m-d\TH:i:s.u'))->toBe('2026-10-15T08:00:00.765432');
});

it('is idempotent: resending a batch stores nothing new', function () {
    actingAsHub();
    $task = Task::factory()->create();
    $items = [hubItem($task, ['to_status' => 'in_progress']), hubItem($task, ['from_status' => 'in_progress', 'to_status' => 'done', 'base_version' => 2, 'device_seq' => 2])];

    push($items)->assertOk();
    $again = push($items)->assertOk();

    expect(collect($again->json('data.results'))->pluck('result')->all())->toBe(['already_received', 'already_received'])
        ->and(TaskStatusUpdate::query()->count())->toBe(2)
        ->and($task->fresh()->version)->toBe(3);
});

it('applies a stale update but flags it as a conflict', function () {
    actingAsHub();
    $task = Task::factory()->create(['status' => 'pending', 'version' => 1]);
    $this->app['auth']->forgetGuards();
    actingAsAdmin();
    $this->putJson("/api/v1/tasks/{$task->id}", ['status' => 'in_progress'])->assertOk();
    actingAsHub();

    push([hubItem($task, ['from_status' => 'pending', 'to_status' => 'done', 'base_version' => 1])])
        ->assertOk()
        ->assertJsonPath('data.results.0.result', 'conflict');

    expect($task->fresh()->status->value)->toBe('done')
        ->and(TaskStatusUpdate::query()->sole()->result->value)->toBe('conflict');
});

it('stores updates for deleted or unknown tasks as rejected', function () {
    actingAsHub();
    $deleted = Task::factory()->create();
    $deleted->delete();
    $unknown = Task::factory()->make(['id' => (string) Str::uuid7()]);

    $response = push([hubItem($deleted), hubItem($unknown, ['device_seq' => 2])])->assertOk();

    expect(collect($response->json('data.results'))->pluck('reason')->all())->toBe(['task_not_found', 'task_not_found'])
        ->and(collect($response->json('data.results'))->pluck('task_version')->all())->toBe([null, null])
        ->and(TaskStatusUpdate::query()->where('result', 'rejected')->count())->toBe(2);
});

it('rejects a no-op transition', function () {
    actingAsHub();
    $task = Task::factory()->create(['status' => 'done']);

    push([hubItem($task, ['to_status' => 'done'])])->assertOk()->assertJsonPath('data.results.0.reason', 'invalid_transition');

    expect($task->fresh()->version)->toBe(1);
});

it('refuses a different update reusing a device sequence number', function () {
    actingAsHub();
    $task = Task::factory()->create();
    push([hubItem($task)])->assertOk();

    push([hubItem($task, ['from_status' => 'done', 'to_status' => 'pending'])])
        ->assertOk()
        ->assertJsonPath('data.results.0.result', 'rejected')
        ->assertJsonPath('data.results.0.reason', 'duplicate_seq');

    expect(TaskStatusUpdate::query()->count())->toBe(1);
});

it('processes each item independently and answers in request order', function () {
    actingAsHub();
    $good = Task::factory()->create();
    $gone = Task::factory()->create();
    $gone->delete();
    $items = [hubItem($gone, ['device_seq' => 1]), hubItem($good, ['device_seq' => 2])];

    $results = push($items)->assertOk()->json('data.results');

    expect(collect($results)->pluck('id')->all())->toBe([$items[0]['id'], $items[1]['id']])
        ->and(collect($results)->pluck('result')->all())->toBe(['rejected', 'accepted']);
});

it('validates the batch', function () {
    actingAsHub();
    $task = Task::factory()->create();

    push([hubItem($task, ['to_status' => 'cancelled', 'device_seq' => 0])])
        ->assertStatus(422)
        ->assertJsonStructure(['errors' => ['items.0.to_status', 'items.0.device_seq']]);

    push(array_map(fn ($i) => hubItem($task, ['device_seq' => $i]), range(1, 201)))
        ->assertStatus(422)
        ->assertJsonStructure(['errors' => ['items']]);
});
