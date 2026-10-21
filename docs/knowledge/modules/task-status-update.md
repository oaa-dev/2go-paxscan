# TaskStatusUpdate Module

Stack: Laravel 13 (`paxscan-online/backend/`). All paths below are relative to `paxscan-online/backend/`.

Append-only record of a device status change, uploaded by a hub. The id (UUIDv7) is generated on the device; uploads are idempotent by id.

## Model
- **Path**: app/Models/TaskStatusUpdate.php
- **Table**: task_status_updates (no `updated_at`; no FK on task_id — updates for unknown tasks are stored as rejected)
- **Fillable** (`#[Fillable]`): id, task_id, from_status, to_status, base_version, device_id, device_seq, device_created_at, hub_id, hub_received_at, result, reject_reason
- **Casts**: from/to_status → TaskStatusEnum, result → UpdateResultEnum, *_at → immutable_datetime
- **Attributes**: `#[WithoutIncrementing]`, `#[DateFormat('Y-m-d H:i:s.uP')]` (keeps microseconds)
- **Relationships**: task() belongsTo Task (withTrashed)

## Connected Files
| Category | File | Notes |
|----------|------|-------|
| Service | app/Services/TaskSyncService.php | push(): creates rows, decides result |
| Repository | app/Repositories/TaskStatusUpdateRepository.php | find, seqTakenByOther, create |
| Repository (contract) | app/Repositories/Contracts/TaskStatusUpdateRepositoryInterface.php | |
| DTO | app/Data/TaskStatusUpdateData.php | snake_case input mapping; dates via IsoDateTimeCast |
| DTO | app/Data/ItemResultData.php | per-item result {id, result, reason, task_version} |
| Cast | app/Data/Casts/IsoDateTimeCast.php | `Carbon::parse` — .NET sends 7 fractional digits |
| Enum | app/Enums/UpdateResultEnum.php | accepted, already_received (response only), conflict, rejected |
| FormRequest | app/Http/Requests/Hub/PushTaskUpdatesRequest.php | hub_id, items 1–200, distinct ids |
| Controller | app/Http/Controllers/Api/V1/Hub/TaskUpdateController.php | store |
| Resource | app/Http/Resources/TaskStatusUpdateResource.php | ISO Zulu timestamps with microseconds |

## Routes
| Method | URI | Action | Middleware |
|--------|-----|--------|------------|
| POST | /api/v1/hub/task-updates | TaskUpdateController@store | auth:sanctum, can:hub.sync, throttle:hub |

## Database
| Type | File |
|------|------|
| Migration | database/migrations/2026_10_15_020200_create_task_status_updates_table.php (unique device_id+device_seq; index task_id+hub_received_at) |

## Tests
| Type | File |
|------|------|
| Feature | tests/Feature/Hub/HubPushTest.php |

## Gotchas
- Default Eloquent date format drops microseconds on `timestampTz(6)` → `#[DateFormat]` on the model.
- laravel-data's default date cast uses `createFromFormat` (≤6 digits) → custom cast.
