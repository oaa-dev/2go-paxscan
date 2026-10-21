<?php

declare(strict_types=1);

namespace App\Providers;

use App\Enums\TokenAbilityEnum;
use App\Enums\UserRoleEnum;
use App\Models\User;
use App\Repositories\Contracts\TaskRepositoryInterface;
use App\Repositories\Contracts\TaskStatusUpdateRepositoryInterface;
use App\Repositories\Contracts\UserRepositoryInterface;
use App\Repositories\TaskRepository;
use App\Repositories\TaskStatusUpdateRepository;
use App\Repositories\UserRepository;
use Illuminate\Cache\RateLimiting\Limit;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Gate;
use Illuminate\Support\Facades\RateLimiter;
use Illuminate\Support\ServiceProvider;

class AppServiceProvider extends ServiceProvider
{
    /** @var array<class-string, class-string> */
    public array $bindings = [
        TaskRepositoryInterface::class => TaskRepository::class,
        TaskStatusUpdateRepositoryInterface::class => TaskStatusUpdateRepository::class,
        UserRepositoryInterface::class => UserRepository::class,
    ];

    public function boot(): void
    {
        Model::shouldBeStrict(! $this->app->isProduction());

        // Role AND token ability: an admin token cannot sync, a hub token cannot manage tasks.
        Gate::define('tasks.manage', fn (User $user): bool => $user->role === UserRoleEnum::Admin
            && $user->tokenCan(TokenAbilityEnum::TasksManage->value));
        Gate::define('hub.sync', fn (User $user): bool => $user->role === UserRoleEnum::Hub
            && $user->tokenCan(TokenAbilityEnum::HubSync->value));

        RateLimiter::for('login', fn (Request $request) => Limit::perMinute(5)
            ->by(strtolower((string) $request->input('email')).'|'.$request->ip()));
        RateLimiter::for('hub', fn (Request $request) => Limit::perMinute(120)
            ->by((string) ($request->user()?->getAuthIdentifier() ?? $request->ip())));
    }
}
