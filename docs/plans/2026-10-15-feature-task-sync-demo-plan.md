# Plan: Task Sync Demo (Web ↔ Desktop Hub ↔ Mobile)

**Date:** 2026-10-15 (rev. 2: platform best practices applied)
**Type:** feature
**Status:** Implemented (2026-10-15) — see Implementation Notes
**Based on:** [docs/brainstorms/2026-10-14-paxscan-sync-architecture-and-schema.md](../brainstorms/2026-10-14-paxscan-sync-architecture-and-schema.md)

## Knowledge Context

### Relevant Learnings
- None. The knowledge base is empty (0 solutions, 0 modules), so **every area is a blind spot**. Run `/kg:compound` after anything non-obvious, especially cert pinning, Kestrel inside WPF, the Drift outbox and SQLite encryption.

### Rules applied (kg plugin)
- `rules/laravel.md`:
  - request flow `Request → Controller → Data → Service → Repository(+interface) → Model → Resource + ApiResponse`
  - Spatie QueryBuilder for list endpoints
  - Pest tests that assert on the response body
  - idempotent seeders
  - Pint for formatting
- `rules/nextjs.md`:
  - data flow `services → hooks (React Query) → pages`
  - Server Components by default
  - forms with react-hook-form + zod
  - shadcn/ui + Tailwind v4
  - no `any`
  - `filter[...]` query params
  - permission gating in the UI is cosmetic only

### Known Gotchas
- **Device clocks never decide outcomes.** Ordering uses `hub_received_at`, then `device_seq`.
- **Mark acked only on an explicit per-item result.** A timeout means resend, which is safe because every write is idempotent by `id`.
- **Spatie QueryBuilder silently ignores undeclared filters.** Add each filter on the backend and the frontend in the same change (rules/laravel.md §4).
- **Paginated responses spread the paginator,** so rows are at `data`, not `data.data` (rules/nextjs.md §4).
- **Next.js 16 renamed `middleware.ts` to `proxy.ts`.** Read `node_modules/next/dist/docs/` before writing App Router code (rules/nextjs.md).
- **`sqlcipher_flutter_libs` and `sqlite3_flutter_libs` are EOL.** Use `package:sqlite3` 3.x build hooks with SQLite3MultipleCiphers instead (Step 8).
- **EF Core `DbContext` is not thread-safe.** Long-lived WPF view models must use `IDbContextFactory`, while API requests use a scoped context.
- **Libraries now under commercial licenses:** MediatR, AutoMapper and FluentAssertions v8+. Use plain handler classes, manual mapping and Shouldly.
- **`knowledge/schema.yaml` says the frontend is "React (Vite)",** which is stale. Fix it in Step 0.

### Critical Patterns Applied
- **Immutable transactions:** a status change is a new `task_status_updates` row. `tasks.status` is a projection of those rows.
- **Idempotent re-send:** every row has a UUIDv7 `id` created on the device, plus unique `(device_id, device_seq)`.
- **Rules outside the UI:**
  - Laravel: Services
  - Desktop: `Domain`/`Application`
  - Mobile: `paxscan_core`
- **The API is the security boundary** on every tier.

## Overview

This is a thin vertical slice through all three platforms that proves the brainstorm's sync loop with a single `tasks` table:

1. **Web:** admins log in and create, edit and delete tasks (`name`, `type` chef|guard, `description`, `status`).
2. **Desktop hub:**
   - **Sync with web:** uploads pending status updates, then downloads the tasks.
   - **Devices:** pairs phones by QR with a pinned TLS cert, monitors the connected devices, and asks them to sync.
3. **Mobile:**
   - Pairs by QR and receives **all tasks** (the real app will filter by role).
   - Changes a task's status offline.
   - Syncs with the hub.

**Round trip:** Mobile → Desktop → Web.

### Decisions

| Topic | Decision |
|---|---|
| Upload direction | Full round trip |
| Pairing | QR + pinned self-signed TLS |
| Partitioning | All tasks to all devices (demo only) |
| Status values | `pending`, `in_progress`, `done` |
| Web admin auth | Real login. Laravel issues a Sanctum token, and Next.js keeps it in an **httpOnly cookie** and proxies API calls (BFF). The token never reaches browser JS, and it works on any domain. |
| Conflict rule | Each update carries the `base_version` it was made against. If that matches the current version, the result is `accepted`. If it doesn't, the update is still applied (last received wins) and marked `conflict`, which shows on the desktop and the web. |

### Versions (latest stable as of 2026-10-15)

| Area | Versions |
|---|---|
| Web backend | PHP 8.5 · Laravel 13.x · Sanctum 4.3 · spatie/laravel-data 4.23 · spatie/laravel-query-builder 7.3 · Pest 5.3 · Larastan · Pint · PostgreSQL 17 |
| Web frontend | Next.js 16.4 · React 19.3 · TanStack Query 5 · axios 1.20 · zod 4.6 · react-hook-form 7.90 · Tailwind 4.3 · shadcn/ui · openapi-typescript 7.13 |
| Desktop | .NET 10 (LTS) · EF Core 10 (SQLite) · CommunityToolkit.Mvvm 8.4 · QRCoder 1.8 · Microsoft.Extensions.Http.Resilience · Serilog · xUnit v3 · Shouldly |
| Mobile | Flutter 3.47 · Drift 2.35 + `sqlite3` 3.7 (SQLite3MultipleCiphers) · Riverpod 3.4 · go_router 18 · dio 5.11 · freezed 4 · mobile_scanner 7.4 · flutter_secure_storage 11 · uuid 4.6 |

## Data Model

### Web (PostgreSQL)

**`users`**: the Laravel default, plus `role` enum `admin | hub`.

**`tasks`**

| Column | Type / notes |
|---|---|
| `id` | uuid v7 (`HasUuids`) |
| `name` | string(150) |
| `type` | enum `chef \| guard` |
| `description` | text, null allowed |
| `status` | enum `pending \| in_progress \| done` |
| `version` | unsigned int, default 1 |
| timestamps, `deleted_at` | standard |

Index on `(type, status)`.

**`task_status_updates`** — append-only, no `updated_at`

| Column | Type / notes |
|---|---|
| `id` | uuid PK, created on the device |
| `task_id` | FK, no cascade |
| `from_status`, `to_status` | status values |
| `base_version` | int |
| `device_id` | uuid |
| `device_seq` | int |
| `device_created_at` | timestamp |
| `hub_id` | uuid |
| `hub_received_at` | timestamp |
| `result` | `accepted \| conflict \| rejected` |
| `reject_reason` | null allowed |
| `created_at` | timestamp |

Unique `(device_id, device_seq)`, and an index on `(task_id, hub_received_at)`.

### Desktop (SQLite, EF Core migrations)

- **`tasks`:** mirrors the web fields, plus:
  - `version`: incremented locally by the hub on each accepted update
  - `web_version`
  - `has_pending_upload`
- **`task_status_updates`:** same shape as the web table, plus:
  - `business_status`
  - `upload_status`: `pending | uploading | acked | rejected`
  - `web_base_version`
  - `online_acked_at`
  - `upload_error`
- **`devices`:**

  | Column | Type / notes |
  |---|---|
  | `id` | uuid |
  | `name` | string |
  | `platform` | string |
  | `app_version` | string |
  | `secret_hash` | string |
  | `status` | `active \| revoked` |
  | `paired_at`, `last_seen_at`, `last_sync_at` | timestamps |
  | `last_seq` | int |
  | `pending_count` | int |
  | `seq_gap_detected` | bool |
  | `sync_requested` | bool |

- **`pairing_tokens`:** `token_hash`, `expires_at`, `used_at`.
- **`hub_settings`:** a single row with:
  - `hub_id`
  - `web_base_url`
  - `hub_token_protected`: bytes encrypted with DPAPI
  - `advertised_host`, `port`
  - `cert_thumbprint`
  - `last_web_sync_at`, `dataset_version`

### Mobile (Drift on an encrypted SQLite file)

- `tasks`: the snapshot fields plus `version`.
- `outbox`: the update shape plus `state` (`saved | sending | acked`), `attempts` and `last_error`.
- `meta`: a key/value table holding `device_id`, `hub_id`, `hub_url`, `cert_sha256`, `last_seq` and `dataset_version`.
- The device secret and the DB key are kept **only** in `flutter_secure_storage`.

## API Surface (`contracts/openapi.yaml`, OpenAPI 3.1)

The web API is versioned under `/api/v1`. Every web response uses the envelope `{success, message, data}` (rules/laravel.md §5).

**Web auth:**
- `POST /api/v1/auth/login` returns `{token, user}`.
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me`

**Web admin** (Sanctum token with role `admin`, route middleware `can:tasks.manage`):
- `GET /api/v1/tasks?filter[type]=&filter[status]=&filter[q]=&sort=-updated_at&page=` (paginated)
- `POST /api/v1/tasks`
- `GET /api/v1/tasks/{task}?include=statusUpdates`
- `PUT /api/v1/tasks/{task}`
- `DELETE /api/v1/tasks/{task}`

**Web hub** (Sanctum token with ability `hub.sync`, throttled):
- `GET /api/v1/hub/tasks` returns `{dataset_version, checksum, tasks[]}`. This is a full snapshot that excludes soft-deleted tasks.
- `POST /api/v1/hub/task-updates` takes up to 200 items and returns `{results: [{id, result: accepted|already_received|conflict|rejected, reason?, task_version}]}`.

**Hub** (Desktop's Kestrel, HTTPS with the pinned cert, errors as `ProblemDetails`). Every endpoint except `/pair` requires the header `Authorization: Device <device_id>:<secret>`.

| Endpoint | Request → response |
|---|---|
| `POST /v1/pair` | `{pairing_token, device_name, platform, app_version}` → `{device_id, device_secret, hub_id}` |
| `POST /v1/ping` | `{pending_count, last_seq}` → `{sync_requested, server_time}` |
| `POST /v1/sync/hello` | `{last_seq, pending_count, device_time}` → `{expected_from_seq}` |
| `POST /v1/sync/push` | `{items[≤200]}` → per-item results |
| `GET /v1/sync/snapshot` | `{dataset_version, tasks[]}` |

## Best Practices by Platform

### Web backend — Laravel 13

- **Structure:** follow rules/laravel.md exactly. Controllers are thin, data crosses into services as `Data` objects, and all Eloquent access is in repositories behind interfaces bound in `AppServiceProvider::$bindings`.
- **Hub sync:**
  - Each item in a batch is processed **in its own DB transaction**, so one bad item can't fail the whole batch.
  - The repository method `findForUpdate()` does the `lockForUpdate`.
  - The insert-or-detect-duplicate check relies on the PK and the unique `(device_id, device_seq)`.
- **Auth:**
  - Sanctum personal access tokens with abilities: `hub.sync` for the hub, `*` for admins.
  - `Gate::define('tasks.manage', fn ($u) => $u->role === UserRoleEnum::Admin)`, checked with `can:` middleware on the routes.
  - Throttle `auth/login` (5 per minute) and the hub routes.
  - Return `401` / `403` as envelope errors, configured in `bootstrap/app.php` `withExceptions`.
- **Quality:**
  - Enums are backed and cast on the models; `$fillable` is explicit.
  - `declare(strict_types=1)`.
  - Larastan level 6 and Pint.
  - `Model::shouldBeStrict()` outside production, which catches lazy loading and missing attributes.
- **Config:** everything comes from `.env`. Commit `.env.example`, and use a separate `.env.testing` with its own Postgres database (use Postgres for tests, not SQLite, because enum and lock behaviour differ).

### Web frontend — Next.js 16

- **Auth as a BFF (backend-for-frontend):**
  - The login Server Action calls Laravel, then stores the token in an `httpOnly; Secure; SameSite=Lax` cookie.
  - `proxy.ts` redirects to `/login` when the cookie is missing.
  - `app/api/backend/[...path]/route.ts` forwards requests to Laravel and adds `Authorization: Bearer`. It **checks the `Origin` header on mutations** to stop CSRF.
  - Browser code only ever calls `/api/backend/*`.
- **Data flow (rules/nextjs.md §1):**
  - `services/task-service.ts` makes the axios calls to `/api/backend/v1/...`.
  - `hooks/use-tasks.ts` wraps them in React Query; mutations show a toast and invalidate both `["tasks"]` and `["task", id]`.
  - Pages only compose the screen.
- **Components:**
  - Server Components by default.
  - The list, filters and dialogs are client components placed low in the tree.
  - The list screen uses the shadcn data-table shape: `page.tsx` + `columns.tsx` + `actions.tsx` + `task-form-dialog.tsx`.
- **Types:**
  - Generate `types/api.d.ts` from the contract with openapi-typescript.
  - Wire-shape aliases live in `types/task.ts`.
  - `tsc --noEmit` must stay clean, with no `any`.
- **Forms:**
  - react-hook-form + zodResolver.
  - The zod schema mirrors the Laravel validation (name required, max 150; type and status as enums).
  - Call `reset()` on `[open, task?.id]` so a reused dialog doesn't carry the previous task's values.

### Desktop — .NET 10 / WPF

- **Solution hygiene:**
  - `Directory.Build.props` sets `net10.0-windows` for the UI project and `net10.0` for the others, with `Nullable=enable`, `TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended` and `ImplicitUsings`.
  - `Directory.Packages.props` holds the package versions (central package management).
  - Add an `.editorconfig`.
- **One host, one DI container:**
  - `App.xaml.cs` builds a single `WebApplication` (Kestrel + Hub endpoints) and also registers the WPF windows and view models in it.
  - It calls `StartAsync()` on startup and `StopAsync()` on exit.
  - The UI and the API share the same services, so the rules are written once.
- **Layering:**
  - `Domain` has no dependencies.
  - `Application` holds the use-case handler classes. It has no MediatR, and it returns a `Result<T>` for expected failures.
  - `Infrastructure` implements `IHubDbContext`, `IWebApiClient` and `ISecretProtector`.
  - Use the built-in `TimeProvider` instead of a custom clock, which also makes tests deterministic.
- **EF Core:**
  - `AddDbContextFactory` for WPF and a scoped context for API requests.
  - Migrations run at startup (`Database.MigrateAsync`).
  - Run `PRAGMA journal_mode=WAL; synchronous=FULL; busy_timeout=5000` from a connection interceptor.
  - A `SqliteWriteGate` (a `SemaphoreSlim`) serializes write transactions.
- **Minimal APIs:**
  - `MapGroup("/v1")` with `TypedResults`.
  - Device authentication is a custom scheme (`AuthenticationHandler`) that compares the secret's hash in **constant time**.
  - Errors use `AddProblemDetails`.
  - Rate limiting on `/v1/pair`.
- **HTTP to the web:**
  - A typed client generated by NSwag, registered with `AddHttpClient` and `AddStandardResilienceHandler()`.
  - Retrying POSTs is safe here because the endpoints are idempotent; say so in a code comment.
- **MVVM:**
  - CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]` with async and cancellation).
  - The UI is never blocked on I/O.
  - The device monitor refreshes with a `PeriodicTimer`.
- **Secrets and logs:**
  - DPAPI (`ProtectedData`, CurrentUser scope) protects the hub token and the PFX password.
  - Serilog writes rolling files to `%LOCALAPPDATA%\PaxScan\logs`, and never logs tokens or secrets.
  - `IOptions<HubOptions>` provides the port and the paths.
- **Publish:** `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true`. The installer (Velopack or MSIX) is out of scope for the demo.

### Mobile — Flutter 3.47

- **Architecture:** follows the official Flutter app-architecture guide (MVVM).
  - **UI layer:** Views (widgets) plus ViewModels (Riverpod `Notifier` / `AsyncNotifier`).
  - **Data layer:** Repositories plus Services. The Services are `HubApiClient`, `SecureStore` and the Drift DB.
  - The Repositories live in `paxscan_core`, so they can be tested without Flutter.
  - Repositories expose Drift `watch()` streams, so the UI updates when the outbox or the snapshot changes.
- **Models:** `freezed` + `json_serializable` immutable models. Errors are modelled as sealed `Result` / `SyncFailure` classes rather than thrown across layers.
- **Navigation:** `go_router` with a `redirect` that sends unpaired devices to `/pair`.
- **Workspace:** root `pubspec.yaml` with `workspace: [packages/paxscan_core, app]` (Dart pub workspaces), and `very_good_analysis` lints.
- **Security:**
  - SQLite encryption: `package:sqlite3` 3.x with the SQLite3MultipleCiphers build-hook option, `PRAGMA key` on open, and a random 32-byte key in `flutter_secure_storage`.
  - Pinning: dio with an `IOHttpClientAdapter` whose `HttpClient` uses `SecurityContext(withTrustedRoots: false)` and a `badCertificateCallback` that accepts only a matching SHA-256 of `cert.der`.
- **Lifecycle:** `AppLifecycleListener` pauses the heartbeat in the background and runs a sync on resume when the device is paired.
- **Platform:**
  - Android `CAMERA` permission.
  - iOS keys `NSCameraUsageDescription` and `NSLocalNetworkUsageDescription`, added now even though iOS testing is deferred.
  - No cleartext traffic.

### Cross-cutting

- **Contract-first:**
  - `contracts/openapi.yaml` is linted with Spectral.
  - The TypeScript and C# clients are generated from it.
  - The Dart client is hand-written, with contract tests against a running HubApi.
- **Git:** monorepo, Conventional Commits, one commit per step.
- **CI (GitHub Actions with path filters):**
  - `backend`: pint --test, larastan, pest against a Postgres service
  - `frontend`: tsc, lint, build
  - `desktop` (windows-latest): build, test
  - `mobile`: `flutter analyze`, `flutter test`, `dart test` in core

## Implementation Steps

### Step 0: Prerequisites & repo
- **Files:** `.gitignore`, `.editorconfig`, `docker-compose.yml` (Postgres 17 and a test database), `README.md`, `docs/knowledge/schema.yaml` (fix the frontend note to Next.js)
- **Details:**
  - Run `git init`.
  - **Toolchain gaps found on this machine:**
    - The .NET 10 SDK and Flutter 3.47 are not installed. Install both on **Windows**, along with Android Studio or a physical Android phone.
    - Build `paxscan-desktop` and `paxscan-mobile` from a Windows-side path, because WPF can't build or run in WSL.
    - The web part builds in WSL, where PHP 8.5, Composer, Node 24 and Docker are present.

### Step 1: Contract v0
- **Files:** `contracts/openapi.yaml`, `contracts/.spectral.yaml`
- **Details:**
  - Define the endpoints, the envelope, `Task`, `TaskStatusUpdate`, `ItemResult`, `ProblemDetails` and the enums.
  - Add the codegen scripts: `frontend/package.json` `gen:api` and an NSwag config in the desktop Infrastructure project.

### Step 2: Laravel backend
- **Files:** `paxscan-online/backend/app/`
  - `Enums/{TaskTypeEnum,TaskStatusEnum,UserRoleEnum,UpdateResultEnum}.php`
  - `Models/{Task,TaskStatusUpdate,User}.php`
  - `Http/Requests/{Auth/LoginRequest,Task/IndexTaskRequest,Task/StoreTaskRequest,Task/UpdateTaskRequest,Hub/PushTaskUpdatesRequest}.php`
  - `Http/Controllers/Api/V1/{AuthController,TaskController}.php`, `Http/Controllers/Api/V1/Hub/{TaskSnapshotController,TaskUpdateController}.php`
  - `Data/{TaskData,TaskStatusUpdateData}.php`
  - `Services/{AuthService,TaskService,TaskSyncService}.php`
  - `Repositories/{TaskRepository,TaskStatusUpdateRepository}.php` and `Repositories/Contracts/*`
  - `Http/Resources/{TaskResource,TaskStatusUpdateResource,TaskSnapshotResource}.php`
  - `Traits/ApiResponse.php`
  - migrations, `database/seeders/DemoSeeder.php` (idempotent: admin, hub user, 6 tasks; prints the hub token)
- **Details:**
  - `TaskService::update` increments `version` and only writes fields that were actually submitted (the `Optional` rule).
  - `TaskSyncService::push` handles each item as follows:
    - id already exists → `already_received`
    - task missing or soft-deleted → `rejected(task_not_found)`, and the row is still stored
    - otherwise apply the status and bump `version`; the result is `accepted` when `base_version` matches, else `conflict`
  - The snapshot checksum is the sha256 of the sorted `id:version` pairs.
  - The repository filters are `type`, `status` and `q` (on name), with sorts on `name` and `updated_at`.
- **Tests (Pest, Feature, asserting on the body):**
  - login, including the throttle
  - CRUD and validation
  - each filter actually filters
  - the envelope shape
  - snapshot excludes deleted tasks and the checksum is stable
  - push: a repeated batch returns `already_received`, conflict, rejected, and the per-item transaction isolation
  - a hub token on admin routes gets `403`; an admin token on hub routes gets `403`

### Step 3: Next.js admin
- **Files:** `paxscan-online/frontend/`
  - `proxy.ts`
  - `app/api/backend/[...path]/route.ts`
  - `app/login/{page.tsx,actions.ts}`
  - `app/(protected)/layout.tsx`
  - `app/(protected)/tasks/{page.tsx,columns.tsx,actions.tsx,task-form-dialog.tsx}`
  - `app/(protected)/tasks/[id]/page.tsx` (detail and status history with conflict badges)
  - `services/task-service.ts`, `hooks/use-tasks.ts`
  - `lib/{axios.ts,query-params.ts}`
  - `types/{api.d.ts,task.ts}`
  - `components/ui/*` (added with the shadcn CLI)
  - `components/task-status-badge.tsx`
- **Details:**
  - Read `node_modules/next/dist/docs/` first.
  - Filters are synced to URL search params and sent as `filter[...]`.
  - Logout clears the cookie and calls Laravel `logout`.
  - Shape the history view clearly: it is what makes the demo's round trip visible.

### Step 4: Desktop scaffold + Domain + Application
- **Files:**
  - `paxscan-desktop/{PaxScan.sln,Directory.Build.props,Directory.Packages.props,.editorconfig}`
  - `src/PaxScan.Domain/{Tasks/TaskItem.cs,Tasks/TaskStatus.cs,Tasks/TaskType.cs,Sync/TaskStatusUpdate.cs,Sync/StatusUpdatePolicy.cs,Devices/Device.cs}`
  - `src/PaxScan.Application/{Abstractions/*,Pairing/PairDeviceHandler.cs,Devices/{RecordHeartbeatHandler,RequestDeviceSyncHandler,RevokeDeviceHandler}.cs,Sync/{HelloHandler,PushUpdatesHandler,BuildSnapshotHandler,SyncWithWebHandler}.cs,Common/Result.cs}`
  - `tests/PaxScan.Domain.Tests`, `tests/PaxScan.Application.Tests`
- **Details:**
  - `PushUpdatesHandler` is idempotent by id, sets `hub_received_at` from `TimeProvider`, and flags a `device_seq` gap.
  - `SyncWithWebHandler` uploads pending updates first, then downloads. A downloaded task that still has pending local updates keeps its local status.

### Step 5: Desktop Infrastructure
- **Files:** `src/PaxScan.Infrastructure/`
  - `Persistence/{HubDbContext.cs,SqlitePragmaInterceptor.cs,SqliteWriteGate.cs,Configurations/*,Migrations/*}`
  - `Web/{WebApiClient.g.cs,WebApiClientAdapter.cs}`
  - `Security/{CertificateService.cs,DpapiSecretProtector.cs,SecretHasher.cs}`
  - `Pairing/QrPayloadFactory.cs`
  - `DependencyInjection.cs`
- **Details:**
  - **Certificate:** self-signed ECDSA P-256 with SANs for the LAN IPs and `192.168.137.1`, valid 5 years. The PFX lives in `%LOCALAPPDATA%\PaxScan`, protected with DPAPI.
  - **QR payload:** `{v:1, hub_url, cert_sha256, pairing_token, hub_id}`. The token is single-use, expires in 5 minutes, and only its hash is stored.

### Step 6: Desktop HubApi
- **Files:** `src/PaxScan.HubApi/{HubEndpoints.cs,DeviceAuthenticationHandler.cs,HubApiExtensions.cs}`
- **Details:**
  - `AddHubApi()` / `MapHubApi()` extension methods are called from the WPF host.
  - Kestrel listens on `0.0.0.0:5443` using the certificate from `CertificateService`.
  - Settings shows a one-time `netsh advfirewall` command for an elevated prompt.
- **Tests:** `tests/PaxScan.HubApi.Tests` uses `Microsoft.AspNetCore.TestHost` against the same `MapHubApi()`.

### Step 7: Desktop WPF UI
- **Files:** `src/PaxScan.Desktop/`
  - `App.xaml(.cs)` (host build and start)
  - `Views/{MainWindow,TasksView,DevicesView,PairDeviceDialog,UpdatesView,SettingsView}.xaml`
  - `ViewModels/*ViewModel.cs`
  - `Services/{NavigationService,DialogService}.cs`
- **Details:**
  - **Tasks:** a grid with **Sync with web** and a result summary showing uploaded, conflicts and downloaded counts.
  - **Devices (monitor):**
    - Online (`last_seen_at` < 60 s) or Offline
    - last sync, pending count, last seq, seq-gap warning
    - actions: Revoke, Request sync
    - toolbar: Pair new device (QR with countdown and a test-connection check), Request sync on all
    - refreshes every 5 s
  - **Updates log:** status updates with conflicts highlighted and upload errors shown.
  - **Settings:** web URL, hub token, a picker for which LAN IP to advertise, and the firewall helper.
  - "Sync to mobile" works by request: the desktop sets `sync_requested`, and the phone's next `/v1/ping` triggers its sync.

### Step 8: Mobile `paxscan_core`
- **Files:** `paxscan-mobile/{pubspec.yaml,analysis_options.yaml}` and `packages/paxscan_core/lib/src/`
  - `data/db/{app_database.dart,tables.dart,encrypted_executor.dart}`
  - `data/hub/{hub_api_client.dart,pinned_http_adapter.dart,dto/*.dart}`
  - `data/secure/secure_store.dart`
  - `domain/{task.dart,status_update.dart,result.dart}`
  - `repositories/{pairing_repository,task_repository,sync_repository}.dart`
  - `sync/{sync_engine.dart,heartbeat.dart}`
  - `ids/uuid_v7.dart`
- **Details:**
  - **Saving a status change** is one Drift transaction:
    - increment `last_seq`
    - insert the outbox row (`saved`, UUIDv7, `base_version`)
    - update the local task status
  - **Sync steps:**
    1. `hello`
    2. `push` in seq-ordered batches of ≤ 200; mark acked only on an explicit result
    3. `snapshot` replaces the local tasks
    4. re-apply unacked outbox items on top
  - Retries use exponential backoff with jitter.
  - **Encryption setup:** confirm the exact `package:sqlite3` 3.x hook option and the Drift `setup` callback against the current docs before writing it, because this API changed recently.
- **Tests (`dart test`, headless):**
  - Use the in-memory Drift DB with a fake `HubApiClient`.
  - outbox survives a DB reopen
  - timeout → resend → `already_received` marks the item acked
  - snapshot merge keeps unacked status changes
  - pinning rejects a different certificate
  - the seq increments without gaps

### Step 9: Mobile app
- **Files:** `paxscan-mobile/app/lib/`
  - `main.dart`, `app.dart`
  - `routing/router.dart`
  - `ui/pairing/{pair_view.dart,pair_view_model.dart}`
  - `ui/tasks/{task_list_view.dart,task_list_view_model.dart,task_detail_view.dart,task_detail_view_model.dart,widgets/*}`
  - `ui/settings/*`
  - `ui/core/{theme.dart,widgets/unsynced_badge.dart}`
- **Details:**
  - **Task list:** type chips, a status filter, the **unsynced count** badge and **Sync now**.
  - **Detail:** a status picker.
  - **Settings:** hub info and unpair (unpair is blocked while the outbox has unacked items).
- **Tests:** widget tests for each view with fake repositories, using Riverpod overrides.

### Step 10: CI + end-to-end demo
- **Files:** `.github/workflows/{backend,frontend,desktop,mobile}.yml`, `docs/demo-script.md`
- **Demo script:**
  1. Log in on the web and create tasks.
  2. On the desktop, sync with web.
  3. Pair 2 phones, which then show Online.
  4. Turn off Wi-Fi on the phones and edit statuses, including the same task on both phones.
  5. Reconnect → Request sync on all → the desktop shows the updates and the conflict.
  6. On the desktop, sync with web → the web history shows both updates, the conflict and the final status.
  7. Kill the app mid-push, then reopen → no duplicates and no lost updates.

## Risks and Mitigations

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| .NET and Flutter not installed; WPF can't run in WSL | high | Step 0: install on Windows and build from a Windows path |
| Phone can't reach the hub (firewall, AP isolation, wrong advertised IP) | high | Settings IP picker, the firewall helper, and a test-connection check in the QR dialog |
| Encryption API changes in `sqlite3` 3.x / Drift | medium | Check against the docs in Step 8. Test that opening the DB file without the key fails. |
| Dart pinning done wrong (hostname check instead of fingerprint, or the system trust store still used) | medium | `withTrustedRoots: false` plus a fingerprint-only callback, with a unit test |
| Next.js 16 API drift vs recalled conventions | medium | Read the installed `next/dist/docs` first, and `tsc` and `build` in CI |
| SQLite `database is locked` under concurrent pushes | medium | WAL, `busy_timeout`, a single write gate and short transactions |
| BFF proxy becomes an open relay or allows CSRF | medium | Allowlist `/v1/*` only, check `Origin` on mutations, use a SameSite cookie |
| Demo shortcuts reaching production (all tasks to all devices) | medium | Listed in Open Questions; partition by role before real use |

## Testing Strategy

- [ ] Laravel feature tests (Pest, Postgres): auth, CRUD, filters, envelope, snapshot, push idempotency, conflict, rejected, abilities
- [ ] Next.js: `tsc --noEmit`, lint, build; a Vitest unit test for `query-params` and the zod schema
- [ ] Desktop: xUnit v3 + Shouldly for the Domain policy and the Application handlers (using a fake `TimeProvider`); HubApi TestHost flow pair → hello → push → snapshot; revoked device → 401; expired or reused pairing token
- [ ] Mobile: `dart test` in core (outbox, resend, pinning, merge); `flutter test` widget tests
- [ ] Manual end-to-end demo script on a real Android phone plus an emulator

## Open Questions

- **Repo location:** a single Windows-side clone for everything, or WSL for the web and a Windows clone for the desktop and mobile?
- **Web `status` edits:** should the admin be able to change `status` directly? The plan allows it, and the conflict rule covers it.
- **Hotspot:** Windows Mobile Hotspot automation is deferred. Should it be included in the demo?
- **Desktop installer** (Velopack vs MSIX) is deferred until after the demo.

## Implementation Notes (2026-10-15)

Everything in Steps 0–10 is built and verified in WSL. The WPF window and the phone app still need a manual run on
Windows and a device.

**Verification**
- Laravel: 31 Pest tests (134 assertions), Larastan level 6 clean, Pint clean.
- Next.js: tsc, ESLint, 6 Vitest tests, production build; BFF checked against live Laravel (session redirects, CSRF origin check, path allowlist, 401 handling).
- Desktop: 37 xUnit tests (Domain, Application on real SQLite, HubApi via TestHost); whole solution incl. WPF compiles; real HTTPS smoke test of the hub (ECDSA cert, fingerprint matches QR).
- Mobile: 25 headless core tests (encryption with SQLite3MultipleCiphers, TLS pinning against a local HTTPS server, outbox/resend/merge) + 5 widget tests.
- End-to-end on real services: Laravel → dev hub download → Dart client pair/push (idempotent resend) → hub upload → Laravel history shows the device update.
- Mutation checks: pending-status protection, upload loop, snapshot re-apply, cert pinning, conflict rule — each test fails when the rule is removed.

**Deviations from the plan**
- **Laravel runs in Docker** (`docker compose` service `backend`, PHP 8.5) because local PHP lacks mbstring/pdo_pgsql and sudo was unavailable.
- **Hand-written C# web client** instead of NSwag: the contract is OpenAPI 3.1, which NSwag does not fully support; the surface is two endpoints.
- **No `freezed` on mobile**: Drift already generates immutable row classes; wire DTOs are small hand-written `final class`es.
- **`task_status_updates.task_id` has no FK** (web and hub): updates for unknown tasks are stored as `rejected(task_not_found)`.
- **Headless dev hub added** (`paxscan-desktop/tools/PaxScan.HubDevHost`): same hub API without WPF, for development and E2E on Linux.
- **Hub-rejected updates are not uploaded** (`upload_status = skipped`).
- **Mobile bumps the task version locally** on each change, so consecutive offline changes on one device are not flagged as conflicts.

**Gotchas found (worth `/kg:compound`)**
- .NET serializes `DateTimeOffset` with 7 fractional digits; PHP `createFromFormat('…u…')` (and laravel-data's default date cast) rejects it → custom `IsoDateTimeCast` using `Carbon::parse`.
- Eloquent's default date format drops microseconds when saving to `timestampTz(6)` → `#[DateFormat('Y-m-d H:i:s.uP')]` on `TaskStatusUpdate`.
- `php artisan serve` passes only allowlisted env vars to its child server → compose runs `php -S` with Laravel's `server.php` from `public/`.
- spatie/laravel-query-builder v7: `allowedFilters()`/`allowedSorts()` are variadic.
- xUnit v3/v4 on .NET 10 needs `"test": {"runner": "Microsoft.Testing.Platform"}` in `global.json`.
- Next.js 16 with `cacheComponents`: session/params/searchParams reads must sit inside `<Suspense>`; a streamed `redirect()` returns HTTP 200 with a client redirect.
- `~/.npmrc` `allow-scripts=…` breaks npm installs spawned by `npx` tools (create-next-app, shadcn) → run them with `NPM_CONFIG_USERCONFIG=/dev/null`.
- Dart pub workspaces share one resolution: core's `test` must match the `test_api` Flutter pins.
- `sqlcipher_flutter_libs` is EOL → `hooks.user_defines.sqlite3.source: sqlite3mc` in the workspace root pubspec.

**Still open**
- Web `status` edits are allowed (they bump the version, so a later device update is flagged as a conflict).
- Windows Mobile Hotspot automation and the installer (Velopack/MSIX) are deferred.
- Laravel's skeleton `CLAUDE.md` suggests installing Laravel Boost — not installed (not requested).
