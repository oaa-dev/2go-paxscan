import type { components } from "@/types/api";

type Schemas = components["schemas"];

// Wire shapes generated from contracts/openapi.yaml — the single definition for the UI.
export type TaskResponse = Schemas["Task"];
export type TaskStatusUpdateResponse = Schemas["TaskStatusUpdate"];
export type TaskType = Schemas["TaskType"];
export type TaskStatus = Schemas["TaskStatus"];
export type TaskInput = Schemas["TaskInput"];
export type TaskPatch = Schemas["TaskPatch"];
export type UserResponse = Schemas["User"];
export type PaginationMeta = Schemas["PaginationMeta"];
export type PaginationLinks = Schemas["PaginationLinks"];

export interface ApiEnvelope<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: Record<string, string[]>;
}

/** Paginated responses spread the paginator: rows at `data`, plus `links` and `meta`. */
export interface Paginated<T> extends ApiEnvelope<T[]> {
  data: T[];
  links: PaginationLinks;
  meta: PaginationMeta;
}

export const TASK_TYPES: readonly TaskType[] = ["chef", "guard"];
export const TASK_STATUSES: readonly TaskStatus[] = ["pending", "in_progress", "done"];

export const TASK_TYPE_LABEL: Record<TaskType, string> = { chef: "Chef", guard: "Guard" };
export const TASK_STATUS_LABEL: Record<TaskStatus, string> = {
  pending: "Pending",
  in_progress: "In progress",
  done: "Done",
};

export interface TaskListParams {
  type?: TaskType;
  status?: TaskStatus;
  q?: string;
  sort?: "name" | "-name" | "updated_at" | "-updated_at";
  page?: number;
  perPage?: number;
}
