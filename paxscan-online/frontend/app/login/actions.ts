"use server";

import { redirect } from "next/navigation";

import { laravelFetch } from "@/lib/laravel";
import { setSessionToken } from "@/lib/session";
import { loginSchema, type LoginValues } from "@/lib/validation/task-schema";
import type { UserResponse } from "@/types/task";

export interface LoginResult {
  message: string;
  fieldErrors?: Partial<Record<keyof LoginValues, string>>;
}

export async function login(values: LoginValues, next?: string): Promise<LoginResult> {
  const parsed = loginSchema.safeParse(values);
  if (!parsed.success) {
    return { message: "Check the highlighted fields." };
  }

  const { status, body } = await laravelFetch<{ token: string; user: UserResponse }>("/api/v1/auth/login", {
    method: "POST",
    token: null,
    body: JSON.stringify({ ...parsed.data, device_name: "admin-portal" }),
  });

  if (status === 429) {
    return { message: "Too many attempts. Wait a minute and try again." };
  }
  if (status !== 200 || !body?.data) {
    return {
      message: body?.message ?? "Login failed.",
      fieldErrors: body?.errors?.email ? { email: body.errors.email[0] } : undefined,
    };
  }
  if (body.data.user.role !== "admin") {
    return { message: "This account cannot use the admin portal." };
  }

  await setSessionToken(body.data.token);
  // Only same-site relative paths, never an open redirect.
  redirect(next?.startsWith("/") && !next.startsWith("//") ? next : "/tasks");
}
