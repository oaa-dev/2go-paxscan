import Link from "next/link";
import type { ReactNode } from "react";

import { TaskStatusBadge } from "@/components/task-status-badge";
import { TaskTypeBadge } from "@/components/task-type-badge";
import type { TaskResponse } from "@/types/task";

import { TaskRowActions } from "./actions";

export interface Column {
  key: string;
  header: string;
  className?: string;
  cell: (task: TaskResponse, onEdit: (task: TaskResponse) => void) => ReactNode;
}

const updated = new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" });

export const columns: Column[] = [
  {
    key: "name",
    header: "Name",
    cell: (task) => (
      <div className="min-w-0">
        <Link href={`/tasks/${task.id}`} className="font-medium hover:underline">
          {task.name}
        </Link>
        {task.description && <p className="truncate text-sm text-muted-foreground">{task.description}</p>}
      </div>
    ),
  },
  { key: "type", header: "Type", className: "w-28", cell: (task) => <TaskTypeBadge type={task.type} /> },
  { key: "status", header: "Status", className: "w-32", cell: (task) => <TaskStatusBadge status={task.status} /> },
  {
    key: "version",
    header: "Ver.",
    className: "w-16 text-right tabular-nums",
    cell: (task) => <span className="text-muted-foreground">{task.version}</span>,
  },
  {
    key: "updated",
    header: "Updated",
    className: "hidden w-44 md:table-cell",
    cell: (task) => <span className="text-sm text-muted-foreground">{updated.format(new Date(task.updated_at))}</span>,
  },
  {
    key: "actions",
    header: "",
    className: "w-12 text-right",
    cell: (task, onEdit) => <TaskRowActions task={task} onEdit={onEdit} />,
  },
];
