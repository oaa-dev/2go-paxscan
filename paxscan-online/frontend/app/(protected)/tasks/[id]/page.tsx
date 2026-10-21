import type { Metadata } from "next";
import { Suspense } from "react";

import { Skeleton } from "@/components/ui/skeleton";

import { TaskDetail } from "./task-detail";

export const metadata: Metadata = { title: "Task" };

export default function TaskPage({ params }: PageProps<"/tasks/[id]">) {
  return (
    <Suspense fallback={<Skeleton className="h-64 w-full" />}>
      <TaskDetailForParams params={params} />
    </Suspense>
  );
}

async function TaskDetailForParams({ params }: Pick<PageProps<"/tasks/[id]">, "params">) {
  const { id } = await params;
  return <TaskDetail id={id} />;
}
