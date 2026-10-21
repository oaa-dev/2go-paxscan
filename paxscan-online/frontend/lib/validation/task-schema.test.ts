import { describe, expect, it } from "vitest";

import { taskFormSchema } from "./task-schema";

describe("taskFormSchema", () => {
  const valid = { name: "Prep breakfast", type: "chef", description: "", status: "pending" };

  it("turns an empty description into null (clears it on the server)", () => {
    expect(taskFormSchema.parse(valid).description).toBeNull();
  });

  it("trims the name and rejects blank names", () => {
    expect(taskFormSchema.parse({ ...valid, name: "  Patrol  " }).name).toBe("Patrol");
    expect(taskFormSchema.safeParse({ ...valid, name: "   " }).success).toBe(false);
  });

  it("matches the server limits and enums", () => {
    expect(taskFormSchema.safeParse({ ...valid, name: "x".repeat(151) }).success).toBe(false);
    expect(taskFormSchema.safeParse({ ...valid, type: "pilot" }).success).toBe(false);
    expect(taskFormSchema.safeParse({ ...valid, status: "cancelled" }).success).toBe(false);
  });
});
