<?php

declare(strict_types=1);

use App\Models\User;

it('logs an admin in and returns a token and the user', function () {
    User::factory()->create(['email' => 'admin@example.com', 'password' => 'secret-pass']);

    $response = $this->postJson('/api/v1/auth/login', ['email' => 'admin@example.com', 'password' => 'secret-pass']);

    $response->assertOk()
        ->assertJsonPath('success', true)
        ->assertJsonPath('data.user.email', 'admin@example.com')
        ->assertJsonPath('data.user.role', 'admin');
    expect($response->json('data.token'))->toBeString()->not->toBeEmpty();
});

it('rejects a wrong password with a validation envelope', function () {
    User::factory()->create(['email' => 'admin@example.com', 'password' => 'secret-pass']);

    $this->postJson('/api/v1/auth/login', ['email' => 'admin@example.com', 'password' => 'nope'])
        ->assertStatus(422)
        ->assertJsonPath('success', false)
        ->assertJsonStructure(['message', 'errors' => ['email']]);
});

it('does not let a hub account sign in to the portal', function () {
    User::factory()->hub()->create(['email' => 'hub@example.com', 'password' => 'secret-pass']);

    $this->postJson('/api/v1/auth/login', ['email' => 'hub@example.com', 'password' => 'secret-pass'])
        ->assertForbidden()
        ->assertJsonPath('success', false);
});

it('throttles repeated login attempts', function () {
    foreach (range(1, 5) as $_) {
        $this->postJson('/api/v1/auth/login', ['email' => 'x@example.com', 'password' => 'nope'])->assertStatus(422);
    }

    $this->postJson('/api/v1/auth/login', ['email' => 'x@example.com', 'password' => 'nope'])
        ->assertStatus(429)
        ->assertJsonPath('message', 'Too many requests.');
});

it('returns the current user and revokes the token on logout', function () {
    User::factory()->create(['email' => 'admin@example.com', 'password' => 'secret-pass']);
    $token = $this->postJson('/api/v1/auth/login', ['email' => 'admin@example.com', 'password' => 'secret-pass'])
        ->json('data.token');

    $this->withToken($token)->getJson('/api/v1/auth/me')->assertOk()->assertJsonPath('data.email', 'admin@example.com');
    $this->withToken($token)->postJson('/api/v1/auth/logout')->assertOk();

    $this->app['auth']->forgetGuards();
    $this->withToken($token)->getJson('/api/v1/auth/me')->assertUnauthorized()->assertJsonPath('success', false);
});
