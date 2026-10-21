import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { TASK_STATUS_LABEL, type TaskStatus } from "@/types/task";

const STYLE: Record<TaskStatus, string> = {
  pending: "border-border bg-muted text-muted-foreground",
  in_progress: "border-amber-300 bg-amber-50 text-amber-800 dark:border-amber-700 dark:bg-amber-950 dark:text-amber-200",
  done: "border-emerald-300 bg-emerald-50 text-emerald-800 dark:border-emerald-700 dark:bg-emerald-950 dark:text-emerald-200",
};

export function TaskStatusBadge({ status }: { status: TaskStatus }) {
  return (
    <Badge variant="outline" className={cn("font-medium", STYLE[status])}>
      {TASK_STATUS_LABEL[status]}
    </Badge>
  );
}
