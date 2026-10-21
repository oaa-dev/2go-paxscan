---
module: "Web Auth"
date: "2026-10-15"
problem_type: "build_error"
component: "page"
symptoms:
  - "`fetch(...), cookies(), headers(), params, searchParams, or connection() accessed outside of <Suspense> prevents the route from being prerendered`"
  - "`Error occurred prerendering page \"/login\"` (stack points at the root Providers component)"
root_cause: "missing_suspense"
framework_version: "Next.js 16.4 (cacheComponents: true), React 19.3"
resolution_type: "code_fix"
severity: "high"
tags:
  - "nextjs16"
  - "cache-components"
  - "suspense"
  - "cookies"
  - "searchparams"
  - "ppr"
---

# Troubleshooting: Next.js 16 build fails: request data accessed outside <Suspense> with cacheComponents

## Problem
`next build` failed prerendering `/login` because the page awaited `searchParams` at the top level; the protected layout awaited the session the same way.

## Environment
- Module: Web Auth
- Framework Version: Next.js 16.4 (cacheComponents: true), React 19.3
- Affected Component: page
- Date: 2026-10-15

## Symptoms
- `fetch(...), cookies(), headers(), params, searchParams, or connection() accessed outside of <Suspense> prevents the route from being prerendered`
- `Error occurred prerendering page "/login"` (stack points at the root Providers component)

## What Didn't Work

**Direct solution:** read `node_modules/next/dist/docs/01-app/02-guides/authentication-with-cache-components.md`, which prescribes the pattern.

## Solution

Move every request-time read (session cookie, `searchParams`, `params`) into an async child component rendered inside `<Suspense>`; keep layouts free of top-level session awaits.

```
export default function LoginPage({ searchParams }: PageProps<"/login">) {
  return (<Suspense fallback={<LoginForm />}><LoginFormWithRedirect searchParams={searchParams} /></Suspense>);
}
async function LoginFormWithRedirect({ searchParams }: Pick<PageProps<"/login">, "searchParams">) {
  const { next } = await searchParams;
  return <LoginForm next={typeof next === "string" ? next : undefined} />;
}
```

## Why This Works

With Cache Components (default in create-next-app 16.4), pages are partially prerendered: the static shell is built at build time and request data streams in. Reading request data outside a boundary would block the whole shell, so the build rejects it.

## Prevention

- Read `next/dist/docs` for the installed version before writing App Router code.
- Session reads live in a data-access function (`lib/auth.ts`) called from components behind Suspense.
- A `redirect()` inside a streamed boundary returns HTTP 200 + client redirect; clear cookies in a route handler (`/logout`), not during render.

## Related Issues

No related issues documented yet.

## Module Reference
See [module docs](../../modules/web-auth.md) for full module documentation.
