<?php

declare(strict_types=1);

namespace App\Repositories\Contracts;

use App\Models\User;
use Laravel\Sanctum\NewAccessToken;

interface UserRepositoryInterface
{
    public function findByEmail(string $email): ?User;

    /** @param list<string> $abilities */
    public function createToken(User $user, string $name, array $abilities, ?\DateTimeInterface $expiresAt = null): NewAccessToken;

    public function deleteTokensNamed(User $user, string $name): void;

    public function deleteCurrentToken(User $user): void;
}
