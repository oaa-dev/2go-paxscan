import "server-only";

import { cookies } from "next/headers";

import { env } from "@/lib/env";
import { SESSION_COOKIE } from "@/lib/session-cookie";

export { SESSION_COOKIE };

/** The httpOnly cookie holds the Sanctum token; browser JavaScript can never read it. */
const MAX_AGE_SECONDS = 60 * 60 * 8;

export async function getSessionToken(): Promise<string | undefined> {
  return (await cookies()).get(SESSION_COOKIE)?.value;
}

export async function setSessionToken(token: string): Promise<void> {
  (await cookies()).set(SESSION_COOKIE, token, {
    httpOnly: true,
    secure: env.isProduction,
    sameSite: "lax",
    path: "/",
    maxAge: MAX_AGE_SECONDS,
  });
}

export async function clearSession(): Promise<void> {
  (await cookies()).delete(SESSION_COOKIE);
}
