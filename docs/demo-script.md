# Demo: task sync round trip

Shows the PaxScan sync loop with one `tasks` table: **Web → Desktop hub → Mobile (offline) → Desktop hub → Web**.

## 0. Start Online (WSL)

```bash
docker compose up -d postgres backend           # Laravel API on http://localhost:8000
docker compose run --rm backend php artisan migrate --seed
docker compose run --rm backend php artisan paxscan:hub-token   # copy the token (shown once)

cd paxscan-online/frontend && npm install && npm run dev        # admin portal on http://localhost:3000
```

Sign in at http://localhost:3000 with `admin@paxscan.test` / `password`.

## 1. Create tasks on the web

Create a couple of tasks (one chef, one guard). Each starts at **version 1**.

## 2. Desktop hub downloads them

On the Windows laptop, run PaxScan (`paxscan-desktop`, `dotnet run --project src/PaxScan.Desktop`).

1. **Settings** → Online API URL `http://<wsl-or-server-ip>:8000`, paste the hub token, pick the LAN address phones will use, **Save**.
2. Copy the **firewall command** from Settings into an Administrator terminal (once).
3. **Tasks** → **Sync with Online** → "downloaded N tasks".

> No Windows machine? The headless dev hub runs the same API in WSL:
> `dotnet run --project paxscan-desktop/tools/PaxScan.HubDevHost -- --web http://localhost:8000 --token <token> --sync --pair --host <lan-ip>`

## 3. Pair two phones

**Devices** → **Pair new device** → scan the QR with the PaxScan app on each phone (or "Enter code manually" on an
emulator and paste the payload the dev hub prints). Both phones appear as **Online**; the app shows all tasks.

## 4. Work offline, including a conflict

1. Turn Wi-Fi **off** on both phones.
2. Phone A: set task *X* to **In progress**. Phone B: set the same task *X* to **Done**.
   Each phone shows an unsynced badge — the changes are saved locally (encrypted DB).

## 5. Reconnect and collect

1. Turn Wi-Fi back on.
2. Desktop **Devices** → **Request sync on all**. Within 30 s each phone pings, sees the request and syncs
   (or tap the sync icon on a phone).
3. Desktop **Updates log**: both updates; the one received second is highlighted as a **conflict**
   (it was based on an older version). The last one received wins.

## 6. Upload to the web

Desktop **Tasks** → **Sync with Online** → "uploaded 2 (conflicts 1)".
On the web, open task *X*: the status history shows both device updates, the conflict badge, and the final status.

## 7. Crash safety (optional)

Change a status on a phone, start a sync and kill the app mid-sync. Reopen and sync again: the update is sent
again and the hub answers `already_received` — no duplicate, nothing lost.
