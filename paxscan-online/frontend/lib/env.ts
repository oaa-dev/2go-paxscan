import "server-only";

function required(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing environment variable ${name}`);
  }
  return value;
}

export const env = {
  get laravelApiUrl(): string {
    return required("LARAVEL_API_URL").replace(/\/$/, "");
  },
  isProduction: process.env.NODE_ENV === "production",
};
