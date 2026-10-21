# Brainstorm: PaxScan Sync Architecture & Database Schema

**Date:** 2026-10-14
**Status:** Draft

## Knowledge Context

Knowledge base is freshly set up (0 solutions, 0 modules) — no prior learnings to apply. This brainstorm starts from the requirements and the stack decisions below.

**Stack (agreed):**

| Part | Technology |
|---|---|
| PaxScan Online | **Backend:** Laravel REST API + PostgreSQL · **Frontend:** Next.js (TypeScript, server-rendered on Node) admin portal — separate folders (master data, reports, app downloads) |
| PaxScan Desktop (hub) | C# .NET 10, WPF, embedded ASP.NET Core (Kestrel), SQLite + EF Core — one Windows installer |
| PaxScan Mobile | Flutter/Dart (Android + iOS), SQLite via Drift |
| Contracts | `contracts/openapi.yaml` shared by all apps |

## Project Structure

```
paxscan/
├── contracts/
│   └── openapi.yaml                 # source of truth: online API + hub API
├── paxscan-online/
│   ├── backend/                     # Laravel – REST API only, PostgreSQL, queues
│   └── frontend/                    # Next.js – admin portal, calls the backend API
├── paxscan-desktop/
│   ├── PaxScan.sln
│   ├── src/
│   │   ├── PaxScan.Domain/          # entities + business rules (claims, inspections, conflicts)
│   │   ├── PaxScan.Application/     # use cases: download, assign, reconcile, upload
│   │   ├── PaxScan.Infrastructure/  # EF Core + SQLite, online API client, backups, hotspot
│   │   ├── PaxScan.HubApi/          # embedded ASP.NET Core: pair, snapshot, sync endpoints
│   │   └── PaxScan.Desktop/         # WPF UI; hosts HubApi in-process (Generic Host)
│   └── tests/
└── paxscan-mobile/
    ├── packages/
    │   └── paxscan_core/            # Dart package: Drift DB, outbox, sync engine, hub API client, rules
    └── app/                         # Flutter UI: pairing, guard, chef screens; depends on paxscan_core
```

**Online — separate backend and frontend (user decision)**
- **Communication:** Next.js talks to Laravel only through the REST API. The two are deployed separately: Laravel on PHP-FPM/Nginx, Next.js on a Node host.
- **Auth:** Laravel Sanctum.
  - Use SPA cookie auth if both share a parent domain (e.g. `admin.` / `api.` subdomains). Set `SANCTUM_STATEFUL_DOMAINS` and CORS with credentials.
  - Otherwise use API tokens kept server-side in Next.js.
- **Next.js:**
  - Middleware does the session checks before pages render.
  - Route handlers can proxy to Laravel so secrets stay out of the browser.
- **Downloads page:** a Next.js page. The installer `.exe` and `.apk` are stored and served by Laravel, with release metadata in the database.
- **Hub endpoints:** the desktop hub calls the **Laravel backend directly** (`/api/hub/*`), never through Next.js.

**Desktop — layered projects, one program (my call)**
- The backend and the UI live in separate projects, but they ship as **one .exe and one installer**.
- The WPF app starts the hub API (Kestrel) inside the same process. Nobody installs a separate server, so the "one installable app" requirement still holds.
- **Rules live in the backend projects:** `Domain` and `Application` have no WPF or ASP.NET references. Both the WPF screens and the hub API call the same use cases, so the rules are written once and unit-testable.
- **Escape hatch:** if the hub must keep running while the UI is closed, `HubApi` can later move into a Windows Service without rewriting the rules.

**Mobile — core package + UI app (my call)**
- A Flutter app has no backend of its own; the desktop hub is its server.
- The split is `paxscan_core` (data, outbox, sync, rules — no Flutter widgets) plus `app` (screens).
- **Why:** the most critical code (saving transactions, the outbox, sync, duplicate blocking) can be tested headless and fast.
- **Setup:** a Dart pub workspace (or Melos) links the two.

**Shared contract**
- `contracts/openapi.yaml` generates the clients:
  - TypeScript for Next.js (`openapi-typescript`)
  - C# for the desktop's online-API client (NSwag or Kiota)
  - Dart for `paxscan_core`'s hub-API client
- Laravel implements the online API; `PaxScan.HubApi` implements the hub API.

## Problem / Goal

Design how data moves between Online → Desktop → Mobile and back, and the schema that supports it, so that:
- Desktop and mobile work with no internet; mobile works with no hub.
- Every transaction saved on a device is kept until it is durably stored upstream.
- Re-sending anything is always safe (idempotent).
- Duplicate meal claims are prevented where possible and detected where not.

## Operational decisions from this session

| Topic | Decision |
|---|---|
| Chef/pantry split | **Each chef device serves one tier.** The device only receives its tier's passengers. |
| Wrong-tier passenger | **Block** — "Not served at this station", nothing recorded. |
| Two devices on one tier | **Never** — desktop enforces one device per tier per day. |
| Guards | **Assigned decks/sections** — each guard device only receives its rooms. |
| Sync timing | **End of day only** (plus initial download in the morning). |
| Pairing / security | **QR pairing + pinned TLS.** |
| Network | **Windows Mobile Hotspot**, ≤ 8 devices. |

**Consequence:** with tier and section partitioning, cross-device duplicates are prevented *by design*. Conflict handling becomes a safety net rather than a daily workflow.

## Approaches Considered

### Duplicate meal claims

**A. Partition devices by tier (chosen)**
- *Description:* Desktop assigns exactly one tier to each chef device; that device only gets those passengers. Guards are partitioned by deck/section the same way.
- *Pros:* Duplicates across devices are impossible in normal operation. Smaller data on each device (also data minimization). Simple mental model for staff.
- *Cons:* Less flexibility (a passenger can't eat at another tier's station). Device replacement mid-day needs a handover rule (see risks).

**B. First claim received wins + flag**
- *Description:* Any device can serve anyone; the hub accepts the first claim it receives for an entitlement and flags later ones.
- *Pros:* Flexible on the floor.
- *Cons:* Duplicates happen routinely with end-of-day sync; admin review every day.
- *Role now:* **kept as the safety-net rule** for the rare cases partitioning can't cover (device swap, data restored twice).

**C. Flag all conflicts, decide later** — clean audit, but reports stay incomplete until review. Rejected.

**D. Earliest device timestamp wins** — relies on device clocks, which the spec forbids. Rejected.

### Pairing & transport

**A. QR pairing + pinned TLS (chosen)**
- *Description:*
  - On first run, the desktop generates a self-signed certificate (private key protected with Windows DPAPI).
  - Pairing screen shows a QR code containing `{hub_url, cert_sha256, pairing_token (one-time, 5 min), hub_id}`.
  - The Flutter app scans it, pins the certificate fingerprint, and sends `POST /pair` with device info.
  - The admin assigns user/role/tier/sections on the desktop; the device receives a device secret.
- *Pros:* Solves discovery (no IP typing), encryption, and identity in one step. No certificate install on phones. Works on iOS without App Transport Security (ATS) exceptions for plain HTTP.
- *Cons:* Re-pair needed if the desktop certificate is regenerated (e.g., new laptop).

**B. Manual IP + 6-digit code** — fallback if the camera fails.

**C. Plain HTTP** — rejected (unencrypted passenger data; iOS blocks it by default).

### Network

**Windows Mobile Hotspot (chosen)**
- The laptop is always `192.168.137.1`, so the QR URL is stable: `https://192.168.137.1:<port>`.
- The desktop app can start/stop the hotspot itself via the WinRT tethering API (`NetworkOperatorTetheringManager`), so the operator just presses one button.
- **Risk:** the hotspot may refuse to start without an internet connection to share. Prototype this on the real laptop early (M2). Fallback: travel router.
- **Limit:** 8 clients — matches the expected ≤ 8 devices.
- The installer must add a Windows Firewall inbound rule for the hub port (Private profile).

### Sync timing

End-of-day only (chosen) vs opportunistic mid-day sync. Partitioning removes the duplicate-claim argument for mid-day sync. The remaining argument is **data loss if a phone is lost or broken during the day**, which we accept with the mitigations below.

## Decision

### 1. Data ownership

| Data | Owner | Direction | Editable where |
|---|---|---|---|
| Passengers, tiers, promotions, entitlements, meal types, rooms, decks/sections, room assignments | Online | Online → Desktop → Mobile (partition-scoped) | Online only |
| Users, roles, permissions | Online | Online → Desktop (password hashes); PIN hash → Mobile | Online |
| Operational day (date + voyage), dataset version | Online creates; Desktop runs lifecycle | Both | Lifecycle on Desktop |
| Device pairings, device assignments (tier / sections) | Desktop | Desktop → Online (audit) | Desktop |
| Transactions (meal claim, claim void, room inspection) | **Creating device** | Mobile → Desktop → Online | **Immutable** — corrections are new transactions |
| Conflicts + resolutions | Desktop admin | Desktop → Online | Desktop |
| Audit log | Each app | Upward | Append-only |

### 2. Transaction identity & ordering

- `id`: **UUIDv7** generated on the device (time-sortable, unique without coordination).
- `device_seq`: per-device counter that only ever increases, stored in the same SQLite transaction as the record. It gives a gap-free order per device, and gaps reveal missing data.
- `device_created_at` (device clock, informational).
- `hub_received_at` (desktop clock).
- `clock_offset_ms`: measured at sync handshake as hub time − device time.
- Order across devices = `hub_received_at`, then `device_seq`. Device clocks are never used to decide outcomes.

### 3. Core schema (shared shape; Online = PostgreSQL, Desktop/Mobile = SQLite)

**Master data (downloaded, read-only on Desktop/Mobile)**
- `operational_days(id, service_date, voyage_code, location, dataset_version, status)`
- `tiers(id, code, name)`
- `passengers(id, operational_day_id, external_ref, full_name, tier_id, qr_token, id_doc_last4)`
- `decks / sections(id, code, name)`
- `rooms(id, operational_day_id, number, deck_id, section_id, expected_status)`
- `room_assignments(room_id, passenger_id)`
- `promotions(id, code, name, description)`
- `passenger_promotions(passenger_id, promotion_id)`
- `meal_types(id, code, name)`
- `entitlements(id, passenger_id, meal_type_id, service_date, quantity, source tier|promotion)`
- `prior_claims(entitlement_id, claimed_at, units)` — claims from earlier days or already on Online.

**Desktop-owned**
- `devices(id, name, platform, cert_pinned_at, status pending|active|revoked, last_seen_at, last_sync_at)`
- `device_assignments(device_id, operational_day_id, user_id, role, tier_id NULL, section_ids)`
  - unique `(operational_day_id, tier_id)` — this enforces "never two devices per tier".

**Transactions (identical shape on all three)**
- `transactions(id uuid, operational_day_id, type, device_id, user_id, device_seq, device_created_at, hub_received_at, clock_offset_ms, payload json, entitlement_key, business_status, upload_status, online_acked_at)`
  - unique `(device_id, device_seq)`
  - `type ∈ meal_claim | claim_void | room_inspection`
  - `entitlement_key` = `{passenger}:{date}:{meal_type}:{unit}` (for meal claims)
  - `business_status ∈ accepted | duplicate_conflict | rejected | voided`
  - `upload_status ∈ pending | uploading | acked | rejected`
  - Business status and upload status are separate columns.

**Projections (rebuildable from transactions)**
- `meal_claims`, `room_inspections`, `room_status_current`

**Other**
- `conflicts(id, kind, entitlement_key, transaction_ids, status, resolution, resolved_by, resolved_at)`
- `audit_log(id, actor_user_id, device_id, action, context json, at)`

**Mobile-only (Drift)**
- `outbox_state(transaction_id, state saved|sending|acked, attempts, last_error)`
- `meta(device_id, device_secret, hub_id, cert_sha256, dataset_version, last_seq)`

### 4. Sync protocol

1. **Online → Desktop (morning):**
   - `GET /api/hub/operational-days?date&voyage`
   - `GET /api/hub/operational-days/{id}/snapshot` (paged)
   - Desktop verifies counts + checksum, then marks the day `ready`.
2. **Desktop → Mobile (morning):**
   - After pairing/assignment, the device calls `GET /sync/snapshot`.
   - It receives **only** its partition: chef → its tier's passengers + entitlements + prior claims; guard → its sections' rooms + assigned passenger names.
   - Includes `dataset_version`.
3. **Mobile → Desktop (end of day):**
   - Handshake: `POST /sync/hello` with `{device_id, last_seq, pending_count, device_time}`. The desktop stores the clock offset and the expected count.
   - Push: `POST /sync/push` with batches of ≤ 200, ordered by `device_seq`. Each item gets its own result: `accepted | already_received | duplicate_conflict | rejected(reason)`.
   - **Every result means "durably stored on the desktop"**, including rejected items. The device marks an item `acked` only on an explicit per-item result; timeouts mean resend.
   - The desktop checks `device_seq` continuity against `last_seq` and flags any gap.
4. **Desktop → Online (when internet returns):**
   - Background upload of `upload_status=pending` in batches to `POST /api/hub/transactions`, idempotent by `id`, with per-item acks.
   - Rejected items stay visible for investigation and retry.
5. **Retention:**
   - Devices keep acked transactions until the desktop reports the day `uploaded`.
   - The desktop keeps everything permanently, plus backups.
   - If the desktop is restored from backup, `POST /sync/verify` lets devices re-push what's missing.

### 5. State machines

- **Mobile outbox item:**
  - `saved → sending → acked`
  - on failure, `sending → saved` (retry with backoff)
  - `acked → purged` only after the day is `uploaded`
- **Desktop upload:** `pending → uploading → acked | rejected`; `rejected → pending` on retry.
- **Operational day:** `downloading → verifying → ready → active → collecting → reconciled → uploading → uploaded → archived`.
  - `reconciled` requires every assigned device to report `pending_count = 0` with no `device_seq` gaps, and all conflicts resolved. Otherwise an admin override is required, recorded in the audit log, with "Device not returned" shown per device.
- **Device:** `pending → active → revoked`.

### 6. Business rules

- **Meal claim (device):**
  - Passenger must be in the device's tier, with a remaining entitlement unit not claimed in prior claims or the local outbox.
  - Otherwise blocked, nothing recorded.
- **Meal claim (desktop):**
  - Validates again; one accepted claim per `entitlement_key`.
  - A second claim → `duplicate_conflict` + a `conflicts` row (safety net).
  - Over-quantity or wrong tier → `rejected`, still stored.
- **Claim void:** a new transaction referencing the original claim; requires a reason; the supervisor role can be required.
- **Room inspection:**
  - All inspections are kept; current status = latest by device order.
  - An observed status different from the expected one → `discrepancy` flag on the report.
  - No cross-device conflicts, because sections are partitioned.
- **Online:** re-checks the rules against global data (e.g., another hub) and returns per-item results. New conflicts appear in the desktop report.

### 7. Security

- **Device credential:**
  - The device secret is stored in Android Keystore / iOS Keychain (`flutter_secure_storage`).
  - Requests are authenticated with a device token + user session.
- **User login:**
  - Username + password at the desktop when the device is assigned.
  - Then a **PIN** for offline unlock; the PIN hash is stored on the device.
  - Session expiry blocks sync, never offline work; the user re-authenticates at end-of-day sync.
- **Data minimization:**
  - Partition-scoped snapshots; `id_doc_last4` only; QR tokens, not raw IDs.
  - Wipe the snapshot after the day is `uploaded`, or on revoke (after pushing).
- **Data at rest:**
  - Mobile: encrypt the Drift DB (SQLCipher via `sqlcipher_flutter_libs`), key in Keystore/Keychain.
  - Desktop: BitLocker recommended; secrets via DPAPI.
- **Desktop ↔ Online:** HTTPS with a hub API credential issued by Online when the desktop is activated; revocable.

### 8. Main risks & mitigations

| Risk | Mitigation |
|---|---|
| Phone lost/broken during the day → that partition's day of data lost (end-of-day sync) | Encrypted local DB survives app crash/restart; prominent "unsynced count"; optional manual "Sync now" when near the hub; supervisor can reassign the tier |
| Mid-day device swap on a tier: replacement device doesn't know claims made by the broken device | Reassignment happens at the desktop: if the old device can be synced first, do so; otherwise the replacement starts with a warning banner, and the safety-net duplicate rule flags overlaps at sync |
| Windows hotspot won't start offline | Prototype early; fallback travel router; laptop IP then from config instead of fixed 192.168.137.1 |
| Desktop crash / disk loss | SQLite WAL + `synchronous=FULL`; scheduled backups to a second disk; devices keep acked data until `uploaded` and re-push via `/sync/verify` |
| iOS distribution | Needs Apple Developer Program; Ad Hoc (≤100 devices/yr) or Apple Business Manager custom app. iOS local-network permission prompt (`NSLocalNetworkUsageDescription`) |
| Contract drift across four codebases (Laravel, Next.js, desktop, mobile) | `contracts/openapi.yaml` is the source of truth; generate TS, C# and Dart clients from it; contract tests in CI for Laravel and HubApi |
| Online frontend and backend on different domains breaks Sanctum cookie auth | Host on subdomains of one domain, or use server-side tokens in Next.js; decide before building login |

## Open Questions

- **Entitlement units:** can a passenger claim the same meal type more than once per day (quantity > 1), or is it always 1?
- **Promotions:** do promotional benefits create extra meal entitlements, or other benefit types (drinks, upgrades) that also need claiming?
- **Multi-day voyages:** is one "operational day" per download, or a whole voyage downloaded once with daily closes?
- **Guard inspection fields:** what does a finding contain? Observed headcount? Photo? Notes? Photos affect sync size and storage.
- **Supervisor actions:** who can void a claim — the chef on the device, or only a supervisor on the desktop?
- **Online portal scope for MVP:** full CRUD for master data, or CSV import first?
- **Multiple hubs:** one laptop per voyage/location, or could two laptops be active for the same voyage?
- **Online hosting:** will the Next.js frontend and Laravel API share a parent domain (e.g. `admin.example.com` / `api.example.com`)? This decides the auth method.
- **WPF vs WinUI 3:** WPF recommended (mature, simpler embedded hosting, broader Windows support); confirm.

## Next Steps

- [x] Interactive simulation of the day's flow (download → slices → offline → sync → conflicts → upload): https://claude.ai/artifact/JyM4m6r7BhAumRHrPgJFFx
- [ ] Answer the open questions above
- [ ] Scaffold the folder structure above (Laravel API, Next.js app, .NET solution with 5 projects, Flutter workspace with core package + app)
- [ ] Prototype on the real Windows laptop: WPF + embedded Kestrel on `0.0.0.0` with self-signed cert, firewall rule, Mobile Hotspot started by the app with no internet, Flutter device on Android + iOS pinning the cert via QR
- [ ] Write `contracts/openapi.yaml` v0 (pairing, snapshot, hello/push/verify, hub ↔ online endpoints)
- [ ] Finalize the ERD from section 3 for all three databases
- [ ] `/kg:plan` for the MVP milestones (Online master data → Desktop download → pairing + snapshot → chef module → guard module → end-of-day sync → upload → conflicts/reports)
