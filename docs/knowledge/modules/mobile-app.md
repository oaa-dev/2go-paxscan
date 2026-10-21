# Mobile App Module

Stack: Flutter 3.47 / Dart 3.13 pub workspace (`paxscan-mobile/`). All paths below are relative to `paxscan-mobile/`.

Offline-first phone app: pairs with the hub by QR (pinned cert), stores tasks and an outbox in an encrypted Drift DB, syncs hello → push → snapshot, heartbeats every 30 s. `packages/paxscan_core` is Flutter-free (headless tests); `app/` is the UI.

## Core package (packages/paxscan_core/)
| Category | File | Notes |
|----------|------|-------|
| Drift table | packages/paxscan_core/lib/src/data/db/tables.dart | Tasks, Outbox, Meta |
| Database | packages/paxscan_core/lib/src/data/db/app_database.dart | `openEncryptedDatabase` (PRAGMA key, WAL, FULL); `hasCipherSupport` |
| Generated | packages/paxscan_core/lib/src/data/db/app_database.g.dart | `dart run build_runner build` |
| Hub client | packages/paxscan_core/lib/src/data/hub/hub_api_client.dart | DioHubApiClient, HubConnection, error classification |
| Hub client | packages/paxscan_core/lib/src/data/hub/pinned_http.dart | no trusted roots; accept only sha256(DER) == pin |
| DTO | packages/paxscan_core/lib/src/data/hub/hub_dto.dart | wire shapes (snake_case) |
| Secure store | packages/paxscan_core/lib/src/data/secure/secure_store.dart | interface + MemorySecureStore; keys: device_secret, database_key |
| Domain | packages/paxscan_core/lib/src/domain/{result,task_enums}.dart | sealed Result/Failure kinds; TaskStatus/TaskType wire values |
| Pairing | packages/paxscan_core/lib/src/pairing/pairing_payload.dart | QR parse/validate (https, 64-hex pin) |
| Repository | packages/paxscan_core/lib/src/repositories/pairing_repository.dart | pair, connection, unpair (blocked while unsynced) |
| Repository | packages/paxscan_core/lib/src/repositories/task_repository.dart | watch tasks/unsynced; changeStatus = one transaction (seq + outbox + optimistic task + local version bump) |
| Outbox / sync engine | packages/paxscan_core/lib/src/sync/sync_engine.dart | acks only on explicit result; batches ≤200; snapshot replaces tasks then re-applies unsent |
| Heartbeat | packages/paxscan_core/lib/src/sync/heartbeat.dart | /v1/ping every 30 s; runs sync on `sync_requested` |
| Tool | packages/paxscan_core/tool/hub_smoke.dart | E2E against a running hub with the real client |

## App (app/)
| Category | File | Notes |
|----------|------|-------|
| Entry | app/lib/main.dart, app/lib/core/bootstrap.dart | open encrypted DB (key in keystore), seed paired state |
| Riverpod provider | app/lib/core/providers.dart | DI: repositories, sync engine, heartbeat, streams |
| Platform | app/lib/core/flutter_secure_store.dart | Keystore/Keychain (this-device-only) |
| Routing | app/lib/routing/router.dart | go_router; redirect unpaired → /pair |
| Flutter view | app/lib/ui/pairing/pair_view.dart (+ pair_view_model.dart) | mobile_scanner; manual paste fallback; scanner injectable for tests |
| Flutter view | app/lib/ui/tasks/task_list_view.dart (+ task_list_view_model.dart) | filters, unsynced badge, Sync now |
| Flutter view | app/lib/ui/tasks/task_detail_view.dart | SegmentedButton status change |
| Widgets | app/lib/ui/tasks/widgets/{hub_banner,status_chip}.dart | reachability/sync banner |
| Controller | app/lib/ui/tasks/sync_controller.dart | manual sync state |
| Flutter view | app/lib/ui/settings/settings_view.dart | hub info, unpair |
| Lifecycle | app/lib/app.dart | heartbeat only in foreground; stop on dispose |
| Platform config | app/android/app/src/main/AndroidManifest.xml, app/android/app/build.gradle.kts, app/ios/Runner/Info.plist | CAMERA, no cleartext, minSdk 24; iOS camera/local-network strings |
| Workspace | pubspec.yaml | `hooks.user_defines.sqlite3.source: sqlite3mc` (encryption) |

## Tests
| Type | File |
|------|------|
| Core (headless) | packages/paxscan_core/test/{task_repository,sync_engine,pairing,encrypted_database,pinning}_test.dart (+ fakes.dart, fixtures/*.pem) |
| Widget | app/test/app_flow_test.dart (+ fake_hub.dart) |

## Gotchas
- `sqlcipher_flutter_libs` is EOL — encryption comes from sqlite3mc build hooks.
- Workspace = one resolution: core's `test` must match Flutter's pinned `test_api`.
- Hub URL is fixed at pairing; changing network path (USB ↔ hotspot) requires re-pairing.
- Windows build: NDK 28.2 required; new Android CLI package names use `/` (`ndk/28.2.13676358`). Xiaomi needs "Install via USB".
