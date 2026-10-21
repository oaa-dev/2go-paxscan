# Task Module

Stack: Laravel 13 (`paxscan-online/backend/`). All paths below are relative to `paxscan-online/backend/`.

Admin-managed tasks (chef/guard) that hubs download and devices update. `version` bumps on every change; devices send it back as `base_version` for conflict detection.

## Model
- **Path**: app/Models/Task.php
- **Table**: tasks (UUIDv7 PK, soft deletes)
- **Fillable** (`#[Fillable]`): name, type, description, status, version
- **Casts**: type → TaskTypeEnum, status → TaskStatusEnum, version → integer
- **Defaults**: status `pending`, version `1`
- **Relationships**: statusUpdates() hasMany TaskStatusUpdate
- **Traits**: HasFactory, HasUuids, SoftDeletes

## Connected Files
| Category | File | Notes |
|----------|------|-------|
| Controller | app/Http/Controllers/Api/V1/TaskController.php | index, store, show (`?include=statusUpdates`), update, destroy |
| Controller | app/Http/Controllers/Api/V1/Hub/TaskSnapshotController.php | hub snapshot (invokable) |
| Controller | app/Http/Controllers/Api/V1/Hub/TaskUpdateController.php | hub push of device updates |
| Service | app/Services/TaskService.php | create (v1), update (submitted fields only; bump version if dirty), delete, history (200 max) |
| Service | app/Services/TaskSyncService.php | snapshot + checksum; push: per-item transaction, already_received / duplicate_seq / task_not_found / invalid_transition / conflict / accepted |
| Repository | app/Repositories/TaskRepository.php | Spatie QueryBuilder: filters `type`, `status`, `q` (ilike name); sorts name, updated_at; `findForUpdateWithTrashed` (row lock) |
| Repository (contract) | app/Repositories/Contracts/TaskRepositoryInterface.php | bound in AppServiceProvider::$bindings |
| DTO | app/Data/TaskData.php | all fields `Optional` (PATCH semantics; null description clears) |
| Enum | app/Enums/TaskTypeEnum.php | Chef=chef, Guard=guard |
| Enum | app/Enums/TaskStatusEnum.php | Pending, InProgress=in_progress, Done |
| FormRequest | app/Http/Requests/Task/IndexTaskRequest.php | filter.*, per_page (≤100) |
| FormRequest | app/Http/Requests/Task/StoreTaskRequest.php | name ≤150, type, description ≤2000, status |
| FormRequest | app/Http/Requests/Task/UpdateTaskRequest.php | same, all `sometimes` |
| Resource | app/Http/Resources/TaskResource.php | `status_updates` via callback `whenLoaded` |
| Resource | app/Http/Resources/TaskSnapshotResource.php | {dataset_version, checksum, tasks} |
| Trait | app/Traits/ApiResponse.php | {success, message, data}; paginator spread |
| Provider | app/Providers/AppServiceProvider.php | gate `tasks.manage` (admin role + token ability), rate limiter `hub` |
| Contract | ../../contracts/openapi.yaml | `/api/v1/tasks*`, `/api/v1/hub/tasks` |

## Routes (routes/api.php)
| Method | URI | Action | Middleware |
|--------|-----|--------|------------|
| GET | /api/v1/tasks | TaskController@index | auth:sanctum, can:tasks.manage |
| POST | /api/v1/tasks | TaskController@store | auth:sanctum, can:tasks.manage |
| GET | /api/v1/tasks/{task} | TaskController@show | auth:sanctum, can:tasks.manage |
| PUT/PATCH | /api/v1/tasks/{task} | TaskController@update | auth:sanctum, can:tasks.manage |
| DELETE | /api/v1/tasks/{task} | TaskController@destroy | auth:sanctum, can:tasks.manage |
| GET | /api/v1/hub/tasks | TaskSnapshotController | auth:sanctum, can:hub.sync, throttle:hub |

## Database
| Type | File |
|------|------|
| Migration | database/migrations/2026_10_15_020100_create_tasks_table.php |
| Factory | database/factories/TaskFactory.php (states: chef, guard) |
| Seeder | database/seeders/DatabaseSeeder.php (6 demo tasks, firstOrCreate) |

## Tests
| Type | File |
|------|------|
| Feature | tests/Feature/Tasks/TaskCrudTest.php |
| Feature | tests/Feature/Hub/HubSnapshotTest.php |
| Feature | tests/Feature/Hub/HubPushTest.php |
| Helpers | tests/Pest.php (actingAsAdmin, actingAsHub, hubItem) |

## Gotchas
- QueryBuilder v7: `allowedFilters(...$filters)` / `allowedSorts(...)` are variadic.
- Checksum = sha256 of sorted `id:version` lines joined by `\n`; must match the hub's `TaskChecksum` (desktop).
