---
question: "How do I run PaxScan (web, desktop hub, phone)?"
modules: [Online, Desktop, Mobile]
tags: [run, setup, docker, wpf, flutter, adb, pairing]
date: 2026-10-15
sources:
  - README.md
  - docker-compose.yml
  - docs/demo-script.md
---

# How do I run PaxScan (web, desktop hub, phone)?

Start order: **Online (WSL) → Desktop hub (Windows) → Phone**, then sync.

## 1. Online API — Laravel + Postgres (WSL)

```bash
cd ~/www/paxscan
docker compose up -d postgres backend        # API → http://localhost:8000
```

First time only:

```bash
docker compose run --rm backend php artisan migrate --seed
docker compose run --rm backend php artisan paxscan:hub-token   # paste into the hub's Settings
```

## 2. Web admin — Next.js (WSL, second terminal)

```bash
cd ~/www/paxscan/paxscan-online/frontend
npm run dev                                  # → http://localhost:3000
```

Login: `admin@paxscan.test` / `password` (demo account from the seeder).

## 3. Desktop hub — WPF (Windows PowerShell)

```powershell
$env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"
& "C:\Users\ijoo222\Desktop\2go\paxscan\paxscan-desktop\src\PaxScan.Desktop\bin\Debug\net10.0-windows\PaxScan.exe"
```

After code changes: `cd C:\Users\ijoo222\Desktop\2go\paxscan\paxscan-desktop; dotnet run --project src/PaxScan.Desktop`

## 4. Phone connection

- **USB:** plug in the phone, then run `& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" reverse tcp:5443 tcp:5443`. Hub Settings: address `127.0.0.1`, port `5443`.
- **Hotspot (no cable):** needs an admin firewall rule for inbound TCP 5443. Hub Settings: address `192.168.137.1`, port `5443`.

## 5. Pair (first time only)

Hub **Devices → Pair new device**, then scan the QR with the PaxScan app.

## 6. Daily flow

1. Web: create or edit tasks.
2. Hub: **Tasks → Sync with Online** (downloads tasks).
3. Phone: tap sync, or hub **Request sync on all**. Work, offline if needed.
4. Phone: sync again to send changes to the hub.
5. Hub: **Sync with Online** (uploads); the web shows each task's history.

## Stop

Close the hub window, press `Ctrl+C` in `npm run dev`, then run `docker compose down` (Postgres data is kept in its volume).

## Reinstall the phone app after mobile changes (PowerShell)

```powershell
cd C:\Users\ijoo222\Desktop\2go\paxscan\paxscan-mobile\app
$env:JAVA_HOME = "$env:LOCALAPPDATA\Programs\jdk-21"; $env:Path = "$env:USERPROFILE\dev\flutter\bin;$env:Path"
flutter run
```

Note: the WSL copy (`~/www/paxscan`) is the source of truth. The Windows copy (`Desktop\2go\paxscan`) is used for the desktop and mobile builds, so sync changes between them.
