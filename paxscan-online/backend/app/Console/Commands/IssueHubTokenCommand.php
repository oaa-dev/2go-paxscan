<?php

declare(strict_types=1);

namespace App\Console\Commands;

use App\Enums\UserRoleEnum;
use App\Repositories\Contracts\UserRepositoryInterface;
use App\Services\AuthService;
use Illuminate\Console\Attributes\Description;
use Illuminate\Console\Attributes\Signature;
use Illuminate\Console\Command;

#[Signature('paxscan:hub-token {email=hub@paxscan.test} {--name=desktop-hub : Token name; an existing token with this name is revoked}')]
#[Description('Create (or rotate) the API token a desktop hub uses to sync with Online')]
final class IssueHubTokenCommand extends Command
{
    public function handle(UserRepositoryInterface $users, AuthService $auth): int
    {
        $hub = $users->findByEmail((string) $this->argument('email'));
        if ($hub === null || $hub->role !== UserRoleEnum::Hub) {
            $this->error('No hub user with that email. Run `php artisan db:seed` first.');

            return self::FAILURE;
        }

        $token = $auth->issueHubToken($hub, (string) $this->option('name'));
        $this->info('Hub token (shown once — paste it into the desktop Settings):');
        $this->line($token);

        return self::SUCCESS;
    }
}
