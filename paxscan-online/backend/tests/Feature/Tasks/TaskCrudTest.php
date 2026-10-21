<?php

declare(strict_types=1);

use App\Models\Task;

it('requires authentication', function () {
    $this->getJson('/api/v1/tasks')->assertUnauthorized()->assertJson(['success' => false, 'message' => 'Unauthenticated.']);
});

it('forbids hub tokens from managing tasks', function () {
    actingAsHub();

    $this->getJson('/api/v1/tasks')->assertForbidden();
});

it('lists tasks with the paginator spread into the envelope', function () {
    actingAsAdmin();
    Task::factory()->count(3)->create();

    $response = $this->getJson('/api/v1/tasks?per_page=2')->assertOk();

    expect($response->json('success'))->toBeTrue()
        ->and($response->json('data'))->toHaveCount(2)
        ->and($response->json('meta.total'))->toBe(3)
        ->and($response->json('meta.last_page'))->toBe(2)
        ->and($response->json())->toHaveKey('links')
        ->and($response->json('data.0'))->toHaveKeys(['id', 'name', 'type', 'description', 'status', 'version', 'created_at', 'updated_at'])
        ->and(array_key_exists('status_updates', $response->json('data.0')))->toBeFalse();
});

it('filters by type, status and name', function (string $query, array $expected) {
    actingAsAdmin();
    Task::factory()->chef()->create(['name' => 'Prep breakfast', 'status' => 'pending']);
    Task::factory()->chef()->create(['name' => 'Bake bread', 'status' => 'done']);
    Task::factory()->guard()->create(['name' => 'Patrol deck 5', 'status' => 'pending']);

    $names = collect($this->getJson('/api/v1/tasks?'.$query)->assertOk()->json('data'))->pluck('name')->sort()->values()->all();

    expect($names)->toBe($expected);
})->with([
    'type' => ['filter[type]=guard', ['Patrol deck 5']],
    'status' => ['filter[status]=done', ['Bake bread']],
    'name, case-insensitive' => ['filter[q]=BREAK', ['Prep breakfast']],
    'combined' => ['filter[type]=chef&filter[status]=pending', ['Prep breakfast']],
]);

it('rejects unknown filter values', function () {
    actingAsAdmin();

    $this->getJson('/api/v1/tasks?filter[type]=pilot')->assertStatus(422)->assertJsonStructure(['errors' => ['filter.type']]);
});

it('creates a task at version 1, pending by default', function () {
    actingAsAdmin();

    $response = $this->postJson('/api/v1/tasks', ['name' => 'Prep breakfast', 'type' => 'chef'])->assertCreated();

    $response->assertJsonPath('data.status', 'pending')
        ->assertJsonPath('data.version', 1)
        ->assertJsonPath('data.description', null);
    expect(Task::query()->count())->toBe(1);
});

it('validates task input', function () {
    actingAsAdmin();

    $this->postJson('/api/v1/tasks', ['name' => str_repeat('x', 151), 'type' => 'pilot', 'status' => 'cancelled'])
        ->assertStatus(422)
        ->assertJsonPath('success', false)
        ->assertJsonStructure(['errors' => ['name', 'type', 'status']]);
});

it('updates only submitted fields and bumps the version', function () {
    actingAsAdmin();
    $task = Task::factory()->chef()->create(['name' => 'Old', 'description' => 'Keep me']);

    $this->putJson("/api/v1/tasks/{$task->id}", ['name' => 'New'])
        ->assertOk()
        ->assertJsonPath('data.name', 'New')
        ->assertJsonPath('data.description', 'Keep me')
        ->assertJsonPath('data.type', 'chef')
        ->assertJsonPath('data.version', 2);
});

it('clears the description when null is sent', function () {
    actingAsAdmin();
    $task = Task::factory()->create(['description' => 'Something']);

    $this->putJson("/api/v1/tasks/{$task->id}", ['description' => null])->assertOk()->assertJsonPath('data.description', null);
});

it('does not bump the version when nothing changed', function () {
    actingAsAdmin();
    $task = Task::factory()->create(['name' => 'Same']);

    $this->putJson("/api/v1/tasks/{$task->id}", ['name' => 'Same'])->assertOk()->assertJsonPath('data.version', 1);
});

it('soft deletes a task', function () {
    actingAsAdmin();
    $task = Task::factory()->create();

    $this->deleteJson("/api/v1/tasks/{$task->id}")->assertOk()->assertJsonPath('success', true);

    expect(Task::query()->count())->toBe(0)->and(Task::withTrashed()->count())->toBe(1);
    $this->getJson("/api/v1/tasks/{$task->id}")->assertNotFound()->assertJsonPath('message', 'Not found.');
});

it('includes the status history only when asked', function () {
    actingAsAdmin();
    $task = Task::factory()->create();

    $plain = $this->getJson("/api/v1/tasks/{$task->id}")->assertOk()->json('data');
    $withHistory = $this->getJson("/api/v1/tasks/{$task->id}?include=statusUpdates")->assertOk()->json('data');

    expect(array_key_exists('status_updates', $plain))->toBeFalse()
        ->and($withHistory['status_updates'])->toBe([]);
});
