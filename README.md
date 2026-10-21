# PaxScan

Offline-first task sync across three platforms (demo slice of the architecture in
[docs/brainstorms/2026-10-14-paxscan-sync-architecture-and-schema.md](docs/brainstorms/2026-10-14-paxscan-sync-architecture-and-schema.md);
plan: [docs/plans/2026-10-15-feature-task-sync-demo-plan.md](docs/plans/2026-10-15-feature-task-sync-demo-plan.md)).

| Part | Stack | Folder |
|---|---|---|
| Online API | Laravel 13, PostgreSQL 17, Sanctum, laravel-data, query-builder, Pest | `paxscan-online/backend` |
| Online admin portal | Next.js 16 (App Router, BFF), React Query, shadcn/ui | `paxscan-online/frontend` |
| Desktop hub | .NET 10, WPF + embedded Kestrel, EF Core SQLite | `paxscan-desktop` |
| Mobile | Flutter 3.47, Drift (encrypted SQLite), Riverpod | `paxscan-mobile` |
| Contract | OpenAPI 3.1 | `contracts/openapi.yaml` |

```
Web (Laravel) ──/api/v1/hub/*──▶ Desktop hub ◀──HTTPS, pinned cert──▶ Phones (offline-first)
     ▲ Next.js admin (BFF, httpOnly session)        QR pairing · /v1/pair /ping /sync/*
```

## Getting started

### Online (WSL / Linux)

PHP runs in a container (no local extensions needed):

```bash
docker compose up -d postgres backend
docker compose run --rm backend php artisan migrate --seed
docker compose run --rm backend php artisan paxscan:hub-token     # token for the desktop hub
docker compose run --rm backend ./vendor/bin/pest                 # tests

cd paxscan-online/frontend
cp .env.example .env.local && npm install
npm run dev                                                       # http://localhost:3000
```

Admin login: `admin@paxscan.test` / `password`.

### Desktop hub (Windows)

Requires the .NET 10 SDK.

```powershell
cd paxscan-desktop
dotnet test --solution PaxScan.slnx
dotnet run --project src/PaxScan.Desktop
```

Data, certificate and logs live in `%LOCALAPPDATA%\PaxScan`. For development without Windows, the headless
`tools/PaxScan.HubDevHost` serves the same hub API (`dotnet run --project tools/PaxScan.HubDevHost -- --help` style
flags: `--web`, `--token`, `--sync`, `--pair`, `--host`, `--port`, `--data`).

### Mobile

Requires Flutter 3.47.

```bash
cd paxscan-mobile
flutter pub get
(cd packages/paxscan_core && dart run build_runner build && dart test)
(cd app && flutter test && flutter run)
```

`packages/paxscan_core/tool/hub_smoke.dart '<pairing JSON>'` exercises a running hub exactly like the app.

## Demo

See [docs/demo-script.md](docs/demo-script.md).
