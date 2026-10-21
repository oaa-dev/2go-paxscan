import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { TaskStatusUpdateResponse } from "@/types/task";

type Result = TaskStatusUpdateResponse["result"];

const STYLE: Record<Result, string> = {
  accepted: "border-emerald-300 text-emerald-800 dark:text-emerald-200",
  conflict: "border-amber-400 bg-amber-50 text-amber-900 dark:bg-amber-950 dark:text-amber-200",
  rejected: "border-red-300 bg-red-50 text-red-800 dark:bg-red-950 dark:text-red-200",
};

export function UpdateResultBadge({ result }: { result: Result }) {
  return (
    <Badge variant="outline" className={cn("capitalize", STYLE[result])}>
      {result}
    </Badge>
  );
}
