"use client";

import { ArrowLeftIcon, ArrowRightIcon, PencilIcon } from "lucide-react";
import Link from "next/link";
import { useState } from "react";

import { TaskStatusBadge } from "@/components/task-status-badge";
import { TaskTypeBadge } from "@/components/task-type-badge";
import { UpdateResultBadge } from "@/components/update-result-badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useTask } from "@/hooks/use-tasks";
import { apiErrorMessage } from "@/lib/axios";

import { TaskFormDialog } from "../task-form-dialog";

const time = new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "medium" });

export function TaskDetail({ id }: { id: string }) {
  const { data: task, isPending, isError, error } = useTask(id);
  const [editOpen, setEditOpen] = useState(false);

  if (isPending) {
    return <Skeleton className="h-64 w-full" />;
  }
  if (isError || !task) {
    return <p className="text-destructive">{apiErrorMessage(error)}</p>;
  }

  const updates = task.status_updates ?? [];
  const conflicts = updates.filter((u) => u.result === "conflict").length;

  return (
    <div className="grid gap-6">
      <div>
        <Link href="/tasks" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeftIcon className="size-4" /> Tasks
        </Link>
      </div>

      <Card>
        <CardHeader className="flex flex-row flex-wrap items-start gap-3">
          <div className="grid gap-1.5">
            <div className="flex items-center gap-2">
              <TaskTypeBadge type={task.type} />
              <TaskStatusBadge status={task.status} />
              <span className="text-sm text-muted-foreground">version {task.version}</span>
            </div>
            <CardTitle className="text-2xl">{task.name}</CardTitle>
            {task.description && <CardDescription className="text-base">{task.description}</CardDescription>}
          </div>
          <Button variant="outline" className="ml-auto" onClick={() => setEditOpen(true)}>
            <PencilIcon /> Edit
          </Button>
        </CardHeader>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Status updates from devices</CardTitle>
          <CardDescription>
            {updates.length === 0
              ? "No device has changed this task yet."
              : `${updates.length} update(s), newest first${conflicts ? ` · ${conflicts} conflict(s): the device had an older version of the task; the last update received was applied.` : "."}`}
          </CardDescription>
        </CardHeader>
        {updates.length > 0 && (
          <CardContent>
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Received by hub</TableHead>
                    <TableHead>Change</TableHead>
                    <TableHead>Result</TableHead>
                    <TableHead>Device</TableHead>
                    <TableHead className="text-right">Seq</TableHead>
                    <TableHead className="text-right">Base ver.</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {updates.map((update) => (
                    <TableRow key={update.id} className={update.result === "conflict" ? "bg-amber-50/60 dark:bg-amber-950/30" : undefined}>
                      <TableCell className="whitespace-nowrap">{time.format(new Date(update.hub_received_at))}</TableCell>
                      <TableCell>
                        <span className="inline-flex items-center gap-1.5">
                          <TaskStatusBadge status={update.from_status} />
                          <ArrowRightIcon className="size-3.5 text-muted-foreground" />
                          <TaskStatusBadge status={update.to_status} />
                        </span>
                      </TableCell>
                      <TableCell>
                        <UpdateResultBadge result={update.result} />
                        {update.reject_reason && (
                          <span className="ml-2 text-xs text-muted-foreground">{update.reject_reason}</span>
                        )}
                      </TableCell>
                      <TableCell className="font-mono text-xs">{update.device_id.slice(0, 8)}</TableCell>
                      <TableCell className="text-right tabular-nums">{update.device_seq}</TableCell>
                      <TableCell className="text-right tabular-nums">{update.base_version}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        )}
      </Card>

      <TaskFormDialog open={editOpen} onOpenChange={setEditOpen} task={task} />
    </div>
  );
}
