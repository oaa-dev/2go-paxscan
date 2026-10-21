# Desktop Hub Module

Stack: .NET 10 — WPF + embedded Kestrel + EF Core SQLite (`paxscan-desktop/`). All paths below are relative to `paxscan-desktop/`.

One Windows app: the WPF UI and the hub API share one WebApplication host/DI container. Syncs tasks with Online (upload first, then download), pairs phones by QR with a pinned self-signed cert, monitors devices, stores device updates.

## Layers
| Project | Path | Role |
|---------|------|------|
| Domain | src/PaxScan.Domain/ | entities + rules, no dependencies |
| Application | src/PaxScan.Application/ | use-case handlers (no MediatR), Result<T>, abstractions |
| Infrastructure | src/PaxScan.Infrastructure/ | EF Core SQLite, web client, DPAPI, certificate, QR |
| HubApi | src/PaxScan.HubApi/ | minimal API `/v1/*`, device auth scheme |
| Desktop | src/PaxScan.Desktop/ | WPF MVVM (CommunityToolkit.Mvvm), host bootstrap |
| Dev host | tools/PaxScan.HubDevHost/Program.cs | headless hub for Linux/WSL dev (`--web --token --sync --pair --host --port --data`) |

## Connected Files
| Category | File | Notes |
|----------|------|-------|
| Domain entity | src/PaxScan.Domain/Tasks/TaskItem.cs | ApplyStatusUpdate (accepted/conflict/invalid), ReplaceFromWeb(keepLocalStatus) |
| Domain entity | src/PaxScan.Domain/Sync/TaskStatusUpdate.cs | immutable; upload bookkeeping (pending/uploading/acked/rejected/skipped) |
| Domain entity | src/PaxScan.Domain/Devices/Device.cs | Heartbeat, Hello (clears SyncRequested), RecordPushed (seq gap), RequestSync, Revoke; online window 60 s |
| Domain entity | src/PaxScan.Domain/Pairing/PairingToken.cs | single use, 5 min |
| Domain entity | src/PaxScan.Domain/Settings/HubSettings.cs | singleton row: hub id, web URL, DPAPI token, advertised host, port |
| Use case | src/PaxScan.Application/Sync/PushUpdatesHandler.cs | idempotent by id, hub-clock stamping, gap detection, ≤200 items |
| Use case | src/PaxScan.Application/Sync/SyncWithWebHandler.cs | upload (each item once per sync) → download → checksum verify |
| Use case | src/PaxScan.Application/Sync/HelloHandler.cs, BuildSnapshotHandler.cs | handshake; device snapshot (all tasks in demo) |
| Use case | src/PaxScan.Application/Pairing/*.cs | CreatePairingTokenHandler, PairDeviceHandler |
| Use case | src/PaxScan.Application/Devices/*.cs | DeviceAuthenticator, RecordHeartbeatHandler, DeviceAdminHandlers (request sync, revoke), DeviceMonitorQuery |
| Use case | src/PaxScan.Application/Settings/UpdateSettingsHandler.cs | URL/port validation, DPAPI-protect token |
| Query | src/PaxScan.Application/Tasks/HubQueries.cs | screens' read models |
| Common | src/PaxScan.Application/Common/{Result,TaskChecksum,WireJson}.cs | snake_case JSON + enums |
| Db context | src/PaxScan.Infrastructure/Persistence/HubDbContext.cs | UTC-ticks DateTimeOffset, snake enum strings |
| Db | src/PaxScan.Infrastructure/Persistence/{SqlitePragmaInterceptor,WriteGate,DatabaseInitializer}.cs | WAL + synchronous=FULL; single writer; migrate + seed settings |
| EF migration | src/PaxScan.Infrastructure/Persistence/Migrations/ | `dotnet ef migrations add … --project src/PaxScan.Infrastructure` |
| Certificate | src/PaxScan.Infrastructure/Security/CertificateService.cs | ECDSA P-256 self-signed, 5 y, PFX + DPAPI password; fingerprint = sha256(DER) |
| Security | src/PaxScan.Infrastructure/Security/{SecretService,SecretProtectors}.cs | 256-bit secrets, constant-time verify; DPAPI (Windows) / dev fallback |
| Web API client | src/PaxScan.Infrastructure/Web/WebApiClient.cs | hand-written; standard resilience handler (POST retries safe: idempotent) |
| Pairing | src/PaxScan.Infrastructure/Pairing/QrPayloadFactory.cs | `{v,hub_url,cert_sha256,pairing_token,hub_id}` → PNG |
| Hub endpoint | src/PaxScan.HubApi/HubEndpoints.cs | /v1/pair, /v1/ping, /v1/sync/hello, /v1/sync/push, /v1/sync/snapshot |
| Auth handler | src/PaxScan.HubApi/DeviceAuthenticationHandler.cs | `Authorization: Device <id>:<secret>` |
| Host builder | src/PaxScan.Desktop/Hosting/HubHostBuilder.cs | Serilog file logs, Kestrel HTTPS via ConfigureHttpsDefaults |
| Host | src/PaxScan.Desktop/App.xaml.cs | DB init → read port → `app.Urls.Add(https://0.0.0.0:{port})` → StartAsync |
| View model | src/PaxScan.Desktop/ViewModels/*.cs | Devices (5 s monitor), Tasks (Sync with Online), Updates log, Settings, PairDevice (QR countdown, test connection) |
| WPF view | src/PaxScan.Desktop/Views/MainWindow.xaml, PairDeviceDialog.xaml | tabs: Devices, Tasks, Updates log, Settings |

## Endpoints (Kestrel, HTTPS, default port 5443)
| Method | URI | Auth |
|--------|-----|------|
| POST | /v1/pair | pairing token (rate-limited 10/min) |
| POST | /v1/ping | device |
| POST | /v1/sync/hello | device |
| POST | /v1/sync/push | device |
| GET | /v1/sync/snapshot | device |

## Runtime data
`%LOCALAPPDATA%\PaxScan\` — hub.db (+wal), hub-cert.pfx, hub-cert.key (DPAPI), logs/hub-YYYYMMDD.log

## Tests
| Type | File |
|------|------|
| Domain | tests/PaxScan.Domain.Tests/{TaskItemTests,DeviceTests}.cs |
| Application (real SQLite) | tests/PaxScan.Application.Tests/{PairingTests,PushUpdatesTests,SyncWithWebTests,TestHub}.cs |
| HubApi (TestHost) | tests/PaxScan.HubApi.Tests/HubApiTests.cs |

Run: `dotnet test --solution PaxScan.slnx` (Microsoft Testing Platform via global.json).

## Gotchas
- Don't read the port inside `ConfigureKestrel` — it runs before the DB is ready (was binding a random port).
- SQLite can't ORDER BY DateTimeOffset → stored as UTC ticks.
- WPF projects lack System.IO / System.Net.Http implicit usings.
- Phones reach the hub only via adb reverse (127.0.0.1) or a LAN/hotspot IP with an inbound firewall rule for the port.
