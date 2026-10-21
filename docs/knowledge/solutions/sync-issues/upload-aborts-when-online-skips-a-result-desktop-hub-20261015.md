---
module: "Desktop Hub"
date: "2026-10-15"
problem_type: "sync_issue"
component: "use_case_handler"
symptoms:
  - "Sync with Online reports \"Online returned no results\" although most items were acked"
  - "Task download skipped; mutation test of the pending-status rule passed when it should fail"
root_cause: "ack_without_result"
framework_version: ".NET 10, EF Core 10 SQLite"
resolution_type: "code_fix"
severity: "high"
tags:
  - "sync"
  - "upload"
  - "idempotency"
  - "per-item-ack"
  - "mutation-testing"
---

# Troubleshooting: Hub upload aborted the whole sync when Online omitted a result for one item

## Problem
`SyncWithWebHandler` re-selected unanswered updates in the next batch; when only those remained, the "no progress" guard threw and the sync failed without downloading tasks.

## Environment
- Module: Desktop Hub
- Framework Version: .NET 10, EF Core 10 SQLite
- Affected Component: use_case_handler
- Date: 2026-10-15

## Symptoms
- Sync with Online reports "Online returned no results" although most items were acked
- Task download skipped; mutation test of the pending-status rule passed when it should fail

## What Didn't Work

**Attempted Solution 1:** a test where Online returns no results at all
- **Why it failed:** it never reached the download step, so the test passed for the wrong reason — found by deliberately breaking the rule (mutation check) and seeing the test stay green.

## Solution

Track the ids attempted in this sync and exclude them from later batches; unanswered items return to `pending` for the next sync. Added a test with a partial answer.

```
var attempted = new HashSet<Guid>();
batch = await db.StatusUpdates
    .Where(u => Unsent.Contains(u.UploadStatus) && !attempted.Contains(u.Id))
    ...
batch.ForEach(u => { u.MarkUploading(); attempted.Add(u.Id); });
```

## Why This Works

The loop terminated only when no unsent rows remained, but "unanswered" rows are legitimately unsent. Bounding each sync to one attempt per item keeps the per-item ack rule (ack only on explicit result) without livelock or false failures.

## Prevention

- Any batch loop over "items not yet acked" must exclude items already attempted in the same run.
- Mutation-check sync tests: remove the rule, confirm the test fails.

## Related Issues

No related issues documented yet.

## Module Reference
See [module docs](../../modules/desktop-hub.md) for full module documentation.
