import "server-only";

import { env } from "@/lib/env";
import { getSessionToken } from "@/lib/session";
import type { ApiEnvelope } from "@/types/task";

/** Server-side call to Laravel with the session token (Server Components / Actions only). */
export async function laravelFetch<T>(
  path: string,
  init: RequestInit & { token?: string | null } = {},
): Promise<{ status: number; body: ApiEnvelope<T> | null }> {
  const token = init.token === undefined ? await getSessionToken() : init.token;
  const response = await fetch(`${env.laravelApiUrl}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
    cache: "no-store",
  });
  const body = (await response.json().catch(() => null)) as ApiEnvelope<T> | null;
  return { status: response.status, body };
}
