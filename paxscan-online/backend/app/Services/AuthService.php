<?php

declare(strict_types=1);

namespace App\Services;

use App\Enums\TokenAbilityEnum;
use App\Enums\UserRoleEnum;
use App\Models\User;
use App\Repositories\Contracts\UserRepositoryInterface;
use Illuminate\Auth\Access\AuthorizationException;
use Illuminate\Support\Facades\Hash;
use Illuminate\Validation\ValidationException;

final class AuthService
{
    private const ADMIN_TOKEN_HOURS = 8;

    public function __construct(private readonly UserRepositoryInterface $users) {}

    /**
     * Issues an admin-portal token. Hub accounts cannot sign in here.
     *
     * @return array{token: string, user: User}
     */
    public function login(string $email, string $password, string $deviceName): array
    {
        $user = $this->users->findByEmail($email);
        if ($user === null || ! Hash::check($password, $user->password)) {
            throw ValidationException::withMessages(['email' => __('auth.failed')]);
        }
        if ($user->role !== UserRoleEnum::Admin) {
            throw new AuthorizationException('This account cannot sign in to the admin portal.');
        }

        $token = $this->users->createToken(
            $user,
            $deviceName,
            [TokenAbilityEnum::TasksManage->value],
            now()->addHours(self::ADMIN_TOKEN_HOURS),
        );

        return ['token' => $token->plainTextToken, 'user' => $user];
    }

    public function logout(User $user): void
    {
        $this->users->deleteCurrentToken($user);
    }

    /** Creates (or rotates) the token a desktop hub uses for /api/v1/hub/*. */
    public function issueHubToken(User $hub, string $name): string
    {
        $this->users->deleteTokensNamed($hub, $name);

        return $this->users->createToken($hub, $name, [TokenAbilityEnum::HubSync->value])->plainTextToken;
    }
}
