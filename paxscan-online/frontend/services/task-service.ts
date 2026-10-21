import { api } from "@/lib/axios";
import { buildTaskQuery } from "@/lib/query-params";
import type {
  ApiEnvelope,
  Paginated,
  TaskInput,
  TaskListParams,
  TaskPatch,
  TaskResponse,
} from "@/types/task";

export const taskService = {
  async list(params: TaskListParams): Promise<Paginated<TaskResponse>> {
    const response = await api.get<Paginated<TaskResponse>>("/v1/tasks", { params: buildTaskQuery(params) });
    return response.data;
  },

  async get(id: string): Promise<TaskResponse> {
    const response = await api.get<ApiEnvelope<TaskResponse>>(`/v1/tasks/${id}`, {
      params: { include: "statusUpdates" },
    });
    return response.data.data!;
  },

  async create(input: TaskInput): Promise<TaskResponse> {
    const response = await api.post<ApiEnvelope<TaskResponse>>("/v1/tasks", input);
    return response.data.data!;
  },

  async update(id: string, patch: TaskPatch): Promise<TaskResponse> {
    const response = await api.put<ApiEnvelope<TaskResponse>>(`/v1/tasks/${id}`, patch);
    return response.data.data!;
  },

  async remove(id: string): Promise<void> {
    await api.delete(`/v1/tasks/${id}`);
  },
};
