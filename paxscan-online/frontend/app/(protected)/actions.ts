"use server";

import { redirect } from "next/navigation";

import { laravelFetch } from "@/lib/laravel";
import { clearSession } from "@/lib/session";

export async function logout(): Promise<void> {
  // Revoke the token on Laravel too; ignore failures (already expired, offline).
  await laravelFetch("/api/v1/auth/logout", { method: "POST" }).catch(() => undefined);
  await clearSession();
  redirect("/login");
}
