import type { TaskListParams } from "@/types/task";

/**
 * Builds Spatie QueryBuilder params: filters go out as `filter[name]`.
 * Laravel silently ignores filters it does not declare, so keep this in sync with
 * TaskRepository::allowedFilters().
 */
export function buildTaskQuery(params: TaskListParams): URLSearchParams {
  const query = new URLSearchParams();
  if (params.type) query.set("filter[type]", params.type);
  if (params.status) query.set("filter[status]", params.status);
  if (params.q?.trim()) query.set("filter[q]", params.q.trim());
  if (params.sort) query.set("sort", params.sort);
  if (params.page && params.page > 1) query.set("page", String(params.page));
  if (params.perPage) query.set("per_page", String(params.perPage));
  return query;
}
