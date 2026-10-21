# Web Auth Module (admin portal session + BFF)

Stack: Next.js 16 App Router (`paxscan-online/frontend/`). All paths below are relative to `paxscan-online/frontend/`.

Login via server action → Laravel Sanctum token stored in an httpOnly cookie; the BFF route adds the token to Laravel calls. The token never reaches browser JavaScript.

## Route Files
| File | Type | Notes |
|------|------|-------|
| app/login/page.tsx | Page (server) | searchParams read inside Suspense |
| app/login/login-form.tsx | Client Component | react-hook-form + zod, calls server action |
| app/login/actions.ts | Server Action | login(): Laravel /api/v1/auth/login, admin role only, sets cookie, safe `next` redirect |
| app/(protected)/actions.ts | Server Action | logout(): revoke token, clear cookie |
| app/logout/route.ts | Route Handler | clears cookie on expired session → /login |
| app/api/backend/[...path]/route.ts | Route Handler (BFF) | allowlist `v1/tasks*`, `v1/auth/(me|logout)`; Origin check on mutations; Bearer from cookie; 401 clears cookie |
| proxy.ts | Proxy (Next 16 middleware) | no cookie → /login?next=…; cookie on /login → /tasks |

## Connected Files
| Category | File | Notes |
|----------|------|-------|
| Utility | lib/session.ts | get/set/clear httpOnly cookie (server-only, 8 h) |
| Utility | lib/session-cookie.ts | cookie name (shared with proxy.ts) |
| Utility | lib/laravel.ts | server-side fetch to Laravel with the session token |
| Utility | lib/auth.ts | getCurrentUser() — redirects to /logout on 401 (render behind Suspense) |
| Utility | lib/env.ts | LARAVEL_API_URL (server-only) |
| Config | .env.example / .env.local | LARAVEL_API_URL=http://localhost:8000 |

## Gotchas
- `cacheComponents: true`: cookies/params/searchParams reads must be inside `<Suspense>`; a streamed `redirect()` returns HTTP 200 with a client-side redirect.
- Cookies can't be cleared during render → the `/logout` route handler does it.
