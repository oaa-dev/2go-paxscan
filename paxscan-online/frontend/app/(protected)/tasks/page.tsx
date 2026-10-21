import type { Metadata } from "next";
import { Suspense } from "react";

import { TaskList } from "./task-list";

export const metadata: Metadata = { title: "Tasks" };

export default function TasksPage() {
  return (
    <Suspense>
      <TaskList />
    </Suspense>
  );
}
