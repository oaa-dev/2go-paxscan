import { ChefHatIcon, ShieldIcon } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { TASK_TYPE_LABEL, type TaskType } from "@/types/task";

export function TaskTypeBadge({ type }: { type: TaskType }) {
  const Icon = type === "chef" ? ChefHatIcon : ShieldIcon;
  return (
    <Badge variant="secondary" className="gap-1">
      <Icon className="size-3" />
      {TASK_TYPE_LABEL[type]}
    </Badge>
  );
}
