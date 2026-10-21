---
module: "System"
date: "2026-10-15"
problem_type: "config_error"
component: "docker"
symptoms:
  - "`SQLSTATE[08006] [7] connection to server at \"127.0.0.1\", port 5432 failed: Connection refused` on HTTP requests"
  - "`docker compose run --rm backend php artisan migrate` worked fine"
root_cause: "env_config"
framework_version: "Laravel 13.36, PHP 8.5 (Docker), Docker Compose"
resolution_type: "config_change"
severity: "medium"
tags:
  - "docker"
  - "artisan-serve"
  - "env"
  - "db_host"
  - "php-built-in-server"
---

# Troubleshooting: `php artisan serve` in Docker ignores DB_HOST from docker-compose environment

## Problem
The backend container set `DB_HOST=postgres` in compose, but HTTP requests still connected to `127.0.0.1` (from `.env`) and failed. Artisan commands in the same container worked.

## Environment
- Module: System-wide
- Framework Version: Laravel 13.36, PHP 8.5 (Docker), Docker Compose
- Affected Component: docker
- Date: 2026-10-15

## Symptoms
- `SQLSTATE[08006] [7] connection to server at "127.0.0.1", port 5432 failed: Connection refused` on HTTP requests
- `docker compose run --rm backend php artisan migrate` worked fine

## What Didn't Work

**Attempted Solution 1:** `php -S 0.0.0.0:8000 -t public vendor/.../server.php`
- **Why it failed:** Laravel's `server.php` requires `getcwd()/index.php`, so it must run from `public/` (`Failed opening required '/var/www/backend/index.php'`).

**Attempted Solution 2:** `working_dir: /var/www/backend/public` on the service
- **Why it failed:** it also changed the cwd of every `docker compose run backend …`, breaking `./vendor/bin/pest` and `php artisan`.

## Solution

Run PHP's built-in server directly (so it inherits the container env) and scope the `cd public` to that command only.

```
# docker-compose.yml (service: backend)
command: sh -c "cd public && exec php -S 0.0.0.0:8000 ../vendor/laravel/framework/src/Illuminate/Foundation/resources/server.php"
```

## Why This Works

`artisan serve` spawns a child `php -S` process and passes only an allowlist of environment variables to it, so `DB_HOST` never reached the app and `.env` won. Running `php -S` as the container's main process inherits the full environment. Real env vars override `.env` in Laravel.

## Prevention

- In containers, don't rely on `artisan serve` for env-driven config; use `php -S` with `server.php`, FrankenPHP/Octane, or nginx+fpm.
- Never set `working_dir` on a service that is also used for `docker compose run` commands.

## Related Issues

No related issues documented yet.
