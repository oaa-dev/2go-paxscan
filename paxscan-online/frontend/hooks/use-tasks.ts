"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { apiErrorMessage } from "@/lib/axios";
import { taskService } from "@/services/task-service";
import type { TaskInput, TaskListParams, TaskPatch } from "@/types/task";

export const taskKeys = {
  all: ["tasks"] as const,
  list: (params: TaskListParams) => ["tasks", params] as const,
  detail: (id: string) => ["task", id] as const,
};

export function useTasks(params: TaskListParams) {
  return useQuery({
    queryKey: taskKeys.list(params),
    queryFn: () => taskService.list(params),
    placeholderData: keepPreviousData,
  });
}

export function useTask(id: string) {
  return useQuery({ queryKey: taskKeys.detail(id), queryFn: () => taskService.get(id) });
}

export function useCreateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: TaskInput) => taskService.create(input),
    onSuccess: (task) => {
      toast.success(`Task "${task.name}" created`);
      void queryClient.invalidateQueries({ queryKey: taskKeys.all });
    },
  });
}

export function useUpdateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, patch }: { id: string; patch: TaskPatch }) => taskService.update(id, patch),
    onSuccess: (task) => {
      toast.success(`Task "${task.name}" updated`);
      void queryClient.invalidateQueries({ queryKey: taskKeys.all });
      void queryClient.invalidateQueries({ queryKey: taskKeys.detail(task.id) });
    },
  });
}

export function useDeleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => taskService.remove(id),
    onSuccess: (_, id) => {
      toast.success("Task deleted");
      void queryClient.invalidateQueries({ queryKey: taskKeys.all });
      queryClient.removeQueries({ queryKey: taskKeys.detail(id) });
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  });
}
