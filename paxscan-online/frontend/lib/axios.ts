import axios from "axios";

/** Browser → BFF (same origin). The BFF adds the Laravel token from the httpOnly cookie. */
export const api = axios.create({
  baseURL: "/api/backend",
  headers: { Accept: "application/json", "Content-Type": "application/json" },
});

api.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error) && error.response?.status === 401 && typeof window !== "undefined") {
      // Full reload on purpose: the session is gone, so drop every cached query too.
      // eslint-disable-next-line @next/next/no-location-assign-relative-destination
      window.location.assign("/login");
    }
    return Promise.reject(error);
  },
);

/** Best message for a failed request: Laravel's envelope message, else a generic one. */
export function apiErrorMessage(error: unknown): string {
  if (axios.isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? "The server could not be reached.";
  }
  return "Something went wrong.";
}

/** Laravel validation errors (422) keyed by field. */
export function apiFieldErrors(error: unknown): Record<string, string[]> {
  if (axios.isAxiosError<{ errors?: Record<string, string[]> }>(error) && error.response?.status === 422) {
    return error.response.data?.errors ?? {};
  }
  return {};
}
