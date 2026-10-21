import "server-only";

import { redirect } from "next/navigation";

import { laravelFetch } from "@/lib/laravel";
import type { UserResponse } from "@/types/task";

/** Data access layer for the signed-in admin. Must be rendered behind <Suspense>. */
export async function getCurrentUser(): Promise<UserResponse> {
  const { status, body } = await laravelFetch<UserResponse>("/api/v1/auth/me");
  if (status === 401 || !body?.data) {
    // Rendering cannot clear cookies; the /logout route handler does.
    redirect("/logout");
  }
  return body.data;
}
