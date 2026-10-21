<?php

declare(strict_types=1);

namespace App\Repositories;

use App\Models\User;
use App\Repositories\Contracts\UserRepositoryInterface;
use Laravel\Sanctum\NewAccessToken;
use Laravel\Sanctum\PersonalAccessToken;

final class UserRepository implements UserRepositoryInterface
{
    public function findByEmail(string $email): ?User
    {
        return User::query()->where('email', $email)->first();
    }

    public function createToken(User $user, string $name, array $abilities, ?\DateTimeInterface $expiresAt = null): NewAccessToken
    {
        return $user->createToken($name, $abilities, $expiresAt);
    }

    public function deleteTokensNamed(User $user, string $name): void
    {
        $user->tokens()->where('name', $name)->delete();
    }

    public function deleteCurrentToken(User $user): void
    {
        $token = $user->currentAccessToken();
        // A TransientToken (cookie session, Sanctum::actingAs) has nothing to delete.
        // @phpstan-ignore instanceof.alwaysTrue
        if ($token instanceof PersonalAccessToken) {
            $token->delete();
        }
    }
}
