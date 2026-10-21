"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { Controller, useForm } from "react-hook-form";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { useCreateTask, useUpdateTask } from "@/hooks/use-tasks";
import { apiErrorMessage, apiFieldErrors } from "@/lib/axios";
import { taskFormSchema, type TaskFormInput, type TaskFormValues } from "@/lib/validation/task-schema";
import {
  TASK_STATUSES,
  TASK_STATUS_LABEL,
  TASK_TYPES,
  TASK_TYPE_LABEL,
  type TaskResponse,
  type TaskStatus,
  type TaskType,
} from "@/types/task";

const EMPTY: TaskFormInput = { name: "", type: "chef", description: "", status: "pending" };

interface TaskFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Present when editing. */
  task?: TaskResponse | null;
}

export function TaskFormDialog({ open, onOpenChange, task }: TaskFormDialogProps) {
  const createTask = useCreateTask();
  const updateTask = useUpdateTask();
  const form = useForm<TaskFormInput, unknown, TaskFormValues>({
    resolver: zodResolver(taskFormSchema),
    defaultValues: EMPTY,
  });

  // A reused dialog must not carry the previous task's values.
  useEffect(() => {
    if (open) {
      form.reset(
        task
          ? { name: task.name, type: task.type, description: task.description ?? "", status: task.status }
          : EMPTY,
      );
    }
  }, [open, task?.id]); // eslint-disable-line react-hooks/exhaustive-deps

  const onSubmit = form.handleSubmit(async (values) => {
    const payload = {
      name: values.name,
      type: values.type as TaskType,
      description: values.description,
      status: values.status as TaskStatus,
    };
    try {
      if (task) {
        await updateTask.mutateAsync({ id: task.id, patch: payload });
      } else {
        await createTask.mutateAsync(payload);
      }
      onOpenChange(false);
    } catch (error) {
      const fieldErrors = apiFieldErrors(error);
      for (const [field, messages] of Object.entries(fieldErrors)) {
        form.setError(field as keyof TaskFormInput, { message: messages[0] });
      }
      if (Object.keys(fieldErrors).length === 0) {
        form.setError("root", { message: apiErrorMessage(error) });
      }
    }
  });

  const { errors, isSubmitting } = form.formState;
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <form onSubmit={onSubmit} className="grid gap-4" noValidate>
          <DialogHeader>
            <DialogTitle>{task ? "Edit task" : "New task"}</DialogTitle>
            <DialogDescription>
              {task
                ? "Changes reach hubs on their next sync with Online."
                : "The task is sent to hubs on their next sync with Online."}
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-1.5">
            <Label htmlFor="task-name">Name</Label>
            <Input id="task-name" aria-invalid={!!errors.name} {...form.register("name")} />
            {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-1.5">
              <Label>Type</Label>
              <Controller
                control={form.control}
                name="type"
                render={({ field }) => (
                  <Select items={TASK_TYPE_LABEL} value={field.value} onValueChange={(v) => v && field.onChange(v)}>
                    <SelectTrigger className="w-full" aria-invalid={!!errors.type}>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {TASK_TYPES.map((type) => (
                        <SelectItem key={type} value={type}>
                          {TASK_TYPE_LABEL[type]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              {errors.type && <p className="text-sm text-destructive">{errors.type.message}</p>}
            </div>
            <div className="grid gap-1.5">
              <Label>Status</Label>
              <Controller
                control={form.control}
                name="status"
                render={({ field }) => (
                  <Select items={TASK_STATUS_LABEL} value={field.value} onValueChange={(v) => v && field.onChange(v)}>
                    <SelectTrigger className="w-full" aria-invalid={!!errors.status}>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {TASK_STATUSES.map((status) => (
                        <SelectItem key={status} value={status}>
                          {TASK_STATUS_LABEL[status]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              {errors.status && <p className="text-sm text-destructive">{errors.status.message}</p>}
            </div>
          </div>

          <div className="grid gap-1.5">
            <Label htmlFor="task-description">Description</Label>
            <Textarea id="task-description" rows={4} aria-invalid={!!errors.description} {...form.register("description")} />
            {errors.description && <p className="text-sm text-destructive">{errors.description.message}</p>}
          </div>

          {errors.root && <p role="alert" className="text-sm text-destructive">{errors.root.message}</p>}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Saving…" : task ? "Save changes" : "Create task"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
