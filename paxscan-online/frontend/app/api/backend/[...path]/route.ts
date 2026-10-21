import type { NextRequest } from "next/server";

import { env } from "@/lib/env";
import { SESSION_COOKIE } from "@/lib/session-cookie";

/**
 * Backend-for-frontend proxy: the browser calls /api/backend/v1/..., this handler adds the
 * Sanctum token from the httpOnly cookie and forwards to Laravel.
 *
 * Only admin endpoints are reachable; hub sync and login are not proxied.
 */
const ALLOWED = /^v1\/(tasks(\/[0-9a-f-]{36})?|auth\/(me|logout))$/i;
const SAFE_METHODS = new Set(["GET", "HEAD"]);

async function forward(request: NextRequest, ctx: RouteContext<"/api/backend/[...path]">) {
  const { path } = await ctx.params;
  const target = path.join("/");
  if (!ALLOWED.test(target)) {
    return Response.json({ success: false, message: "Not found." }, { status: 404 });
  }

  // CSRF: state-changing requests must come from this site.
  if (!SAFE_METHODS.has(request.method)) {
    const origin = request.headers.get("origin");
    if (!origin || origin !== request.nextUrl.origin) {
      return Response.json({ success: false, message: "Invalid origin." }, { status: 403 });
    }
  }

  const token = request.cookies.get(SESSION_COOKIE)?.value;
  if (!token) {
    return Response.json({ success: false, message: "Unauthenticated." }, { status: 401 });
  }

  const upstream = await fetch(`${env.laravelApiUrl}/api/${target}${request.nextUrl.search}`, {
    method: request.method,
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: SAFE_METHODS.has(request.method) ? undefined : await request.text(),
    cache: "no-store",
  });

  const response = new Response(upstream.body, {
    status: upstream.status,
    headers: { "Content-Type": upstream.headers.get("Content-Type") ?? "application/json" },
  });
  if (upstream.status === 401) {
    response.headers.append("Set-Cookie", `${SESSION_COOKIE}=; Path=/; Max-Age=0; HttpOnly; SameSite=Lax`);
  }
  return response;
}

export { forward as GET, forward as POST, forward as PUT, forward as DELETE };
