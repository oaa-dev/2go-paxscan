<?php

declare(strict_types=1);

use App\Models\Task;

it('forbids admin tokens from hub endpoints', function () {
    actingAsAdmin();

    $this->getJson('/api/v1/hub/tasks')->assertForbidden();
});

it('returns every non-deleted task with a verifiable checksum', function () {
    actingAsHub();
    $kept = Task::factory()->count(2)->create();
    Task::factory()->create()->delete();

    $data = $this->getJson('/api/v1/hub/tasks')->assertOk()->assertJsonPath('success', true)->json('data');

    $expected = $kept->sortBy('id')->map(fn (Task $t) => strtolower($t->id).':'.$t->version)->sort()->implode("\n");
    expect($data['tasks'])->toHaveCount(2)
        ->and(collect($data['tasks'])->pluck('id')->sort()->values()->all())->toBe($kept->pluck('id')->sort()->values()->all())
        ->and($data['checksum'])->toBe(hash('sha256', $expected))
        ->and($data['dataset_version'])->toBeString();
});

it('returns an empty snapshot with a null dataset version', function () {
    actingAsHub();

    $data = $this->getJson('/api/v1/hub/tasks')->assertOk()->json('data');

    expect($data['tasks'])->toBe([])
        ->and($data['dataset_version'])->toBeNull()
        ->and($data['checksum'])->toBe(hash('sha256', ''));
});
