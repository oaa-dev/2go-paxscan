import { describe, expect, it } from "vitest";

import { buildTaskQuery } from "./query-params";

describe("buildTaskQuery", () => {
  it("sends filters as Spatie filter[...] params", () => {
    const query = buildTaskQuery({ type: "chef", status: "in_progress", q: " prep " });

    expect(query.get("filter[type]")).toBe("chef");
    expect(query.get("filter[status]")).toBe("in_progress");
    expect(query.get("filter[q]")).toBe("prep");
    expect(query.has("type")).toBe(false);
  });

  it("omits empty filters and the first page", () => {
    const query = buildTaskQuery({ q: "  ", page: 1 });

    expect(query.toString()).toBe("");
  });

  it("includes sort, page and per_page", () => {
    const query = buildTaskQuery({ sort: "-updated_at", page: 3, perPage: 15 });

    expect(query.toString()).toBe("sort=-updated_at&page=3&per_page=15");
  });
});
