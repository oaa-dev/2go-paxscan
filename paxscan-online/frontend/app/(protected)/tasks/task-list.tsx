"use client";

import { PlusIcon, SearchIcon } from "lucide-react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useTasks } from "@/hooks/use-tasks";
import { apiErrorMessage } from "@/lib/axios";
import {
  TASK_STATUSES,
  TASK_STATUS_LABEL,
  TASK_TYPES,
  TASK_TYPE_LABEL,
  type TaskListParams,
  type TaskResponse,
  type TaskStatus,
  type TaskType,
} from "@/types/task";

import { columns } from "./columns";
import { TaskFormDialog } from "./task-form-dialog";

const ALL = "all";
const TYPE_ITEMS = { [ALL]: "All types", ...TASK_TYPE_LABEL };
const STATUS_ITEMS = { [ALL]: "All statuses", ...TASK_STATUS_LABEL };

function readParams(search: URLSearchParams): TaskListParams {
  const type = search.get("type");
  const status = search.get("status");
  return {
    type: TASK_TYPES.includes(type as TaskType) ? (type as TaskType) : undefined,
    status: TASK_STATUSES.includes(status as TaskStatus) ? (status as TaskStatus) : undefined,
    q: search.get("q") ?? undefined,
    page: Number(search.get("page") ?? "1") || 1,
    sort: "-updated_at",
    perPage: 15,
  };
}

export function TaskList() {
  const router = useRouter();
  const pathname = usePathname();
  const search = useSearchParams();
  const params = readParams(search);
  const { data, isPending, isError, error, isFetching } = useTasks(params);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<TaskResponse | null>(null);
  const [query, setQuery] = useState(params.q ?? "");

  // Filters live in the URL so a filtered view can be shared or reloaded.
  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(search);
    if (value && value !== ALL) next.set(key, value);
    else next.delete(key);
    if (key !== "page") next.delete("page");
    router.replace(`${pathname}?${next.toString()}`);
  };

  useEffect(() => {
    const handle = setTimeout(() => {
      if ((params.q ?? "") !== query) setParam("q", query || null);
    }, 300);
    return () => clearTimeout(handle);
  }, [query]); // eslint-disable-line react-hooks/exhaustive-deps

  const openCreate = () => {
    setEditing(null);
    setDialogOpen(true);
  };
  const openEdit = (task: TaskResponse) => {
    setEditing(task);
    setDialogOpen(true);
  };

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Tasks</h1>
          <p className="text-sm text-muted-foreground">Downloaded by hubs and updated from chef and guard devices.</p>
        </div>
        <Button className="ml-auto" onClick={openCreate}>
          <PlusIcon /> New task
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <div className="relative w-full sm:w-64">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search by name"
            className="pl-8"
            aria-label="Search tasks by name"
          />
        </div>
        <Select items={TYPE_ITEMS} value={params.type ?? ALL} onValueChange={(v) => setParam("type", v)}>
          <SelectTrigger className="w-36" aria-label="Filter by type">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {Object.entries(TYPE_ITEMS).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select items={STATUS_ITEMS} value={params.status ?? ALL} onValueChange={(v) => setParam("status", v)}>
          <SelectTrigger className="w-40" aria-label="Filter by status">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {Object.entries(STATUS_ITEMS).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {isFetching && !isPending && <span className="text-sm text-muted-foreground">Updating…</span>}
      </div>

      <div className="overflow-hidden rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              {columns.map((column) => (
                <TableHead key={column.key} className={column.className}>
                  {column.header}
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {isPending &&
              Array.from({ length: 5 }, (_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={columns.length}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}
            {isError && (
              <TableRow>
                <TableCell colSpan={columns.length} className="py-10 text-center text-destructive">
                  {apiErrorMessage(error)}
                </TableCell>
              </TableRow>
            )}
            {data?.data.length === 0 && (
              <TableRow>
                <TableCell colSpan={columns.length} className="py-10 text-center text-muted-foreground">
                  No tasks match these filters.
                </TableCell>
              </TableRow>
            )}
            {data?.data.map((task) => (
              <TableRow key={task.id}>
                {columns.map((column) => (
                  <TableCell key={column.key} className={column.className}>
                    {column.cell(task, openEdit)}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {data && data.meta.last_page > 1 && (
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>
            {data.meta.from}–{data.meta.to} of {data.meta.total}
          </span>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={data.meta.current_page <= 1}
              onClick={() => setParam("page", String(data.meta.current_page - 1))}
            >
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={data.meta.current_page >= data.meta.last_page}
              onClick={() => setParam("page", String(data.meta.current_page + 1))}
            >
              Next
            </Button>
          </div>
        </div>
      )}

      <TaskFormDialog open={dialogOpen} onOpenChange={setDialogOpen} task={editing} />
    </div>
  );
}
