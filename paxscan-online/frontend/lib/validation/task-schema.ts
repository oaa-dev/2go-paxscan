import { z } from "zod";

import { TASK_STATUSES, TASK_TYPES } from "@/types/task";

/** Mirrors StoreTaskRequest / UpdateTaskRequest in Laravel. */
export const taskFormSchema = z.object({
  name: z.string().trim().min(1, "Name is required.").max(150, "Name must be at most 150 characters."),
  type: z.enum(TASK_TYPES as [string, ...string[]], { message: "Choose a type." }),
  description: z
    .string()
    .trim()
    .max(2000, "Description must be at most 2000 characters.")
    .transform((v) => (v === "" ? null : v)),
  status: z.enum(TASK_STATUSES as [string, ...string[]], { message: "Choose a status." }),
});

export type TaskFormInput = z.input<typeof taskFormSchema>;
export type TaskFormValues = z.output<typeof taskFormSchema>;

export const loginSchema = z.object({
  email: z.email("Enter a valid email."),
  password: z.string().min(1, "Password is required."),
});

export type LoginValues = z.infer<typeof loginSchema>;
