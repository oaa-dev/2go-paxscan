import { ClipboardListIcon, LogOutIcon } from "lucide-react";
import Link from "next/link";
import { Suspense } from "react";

import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { getCurrentUser } from "@/lib/auth";

import { logout } from "./actions";

export default function ProtectedLayout({ children }: LayoutProps<"/">) {
  return (
    <div className="min-h-screen">
      <header className="border-b bg-background">
        <div className="mx-auto flex h-14 max-w-6xl items-center gap-6 px-4">
          <Link href="/tasks" className="font-semibold">
            PaxScan Online
          </Link>
          <nav className="flex items-center gap-4 text-sm">
            <Link href="/tasks" className="flex items-center gap-1.5 text-muted-foreground hover:text-foreground">
              <ClipboardListIcon className="size-4" /> Tasks
            </Link>
          </nav>
          <div className="ml-auto flex items-center gap-3 text-sm">
            <Suspense fallback={<Skeleton className="h-5 w-24" />}>
              <CurrentUserName />
            </Suspense>
            <form action={logout}>
              <Button type="submit" variant="ghost" size="sm">
                <LogOutIcon /> Sign out
              </Button>
            </form>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6">{children}</main>
    </div>
  );
}

/** Reads the session at request time, so it streams in behind Suspense. */
async function CurrentUserName() {
  const user = await getCurrentUser();
  return <span className="hidden text-muted-foreground sm:inline">{user.name}</span>;
}
