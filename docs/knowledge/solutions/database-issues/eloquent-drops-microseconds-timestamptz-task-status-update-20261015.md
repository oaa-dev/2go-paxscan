---
module: "TaskStatusUpdate"
date: "2026-10-15"
problem_type: "database_issue"
component: "eloquent_model"
symptoms:
  - "Pest: `Failed asserting that two strings are identical. -'2026-10-15T08:00:00.765432' +'2026-10-15T08:00:00.000000'`"
  - "Updates received within the same second lose their relative order"
root_cause: "lost_precision"
framework_version: "Laravel 13.36, PostgreSQL 17"
resolution_type: "code_fix"
severity: "high"
tags:
  - "eloquent"
  - "dateformat"
  - "microseconds"
  - "postgres"
  - "timestamptz"
  - "ordering"
---

# Troubleshooting: Eloquent truncates microseconds when saving to timestampTz(6) columns

## Problem
`hub_received_at` orders device updates, and the column is `timestampTz(6)`, but values were stored truncated to whole seconds.

## Environment
- Module: TaskStatusUpdate
- Framework Version: Laravel 13.36, PostgreSQL 17
- Affected Component: eloquent_model
- Date: 2026-10-15

## Symptoms
- Pest: `Failed asserting that two strings are identical. -'2026-10-15T08:00:00.765432' +'2026-10-15T08:00:00.000000'`
- Updates received within the same second lose their relative order

## What Didn't Work

**Direct solution:** the feature test asserting the stored microseconds failed on the first run, pointing straight at serialization.

## Solution

Set the model's storage date format to include microseconds and offset using Laravel 13's attribute.

```
// app/Models/TaskStatusUpdate.php
#[DateFormat('Y-m-d H:i:s.uP')]
#[WithoutIncrementing]
class TaskStatusUpdate extends Model
```

## Why This Works

Eloquent serializes datetime attributes with the model's `$dateFormat` (default `Y-m-d H:i:s`) before binding them. The column precision is irrelevant if the string sent to Postgres has no fraction. Declaring a format with `.u` (and `P` for timestamptz) keeps the full value.

## Prevention

- Any model with `timestamp(…, 6)` / `timestampTz(…, 6)` columns needs `#[DateFormat('Y-m-d H:i:s.uP')]` (or `$dateFormat`).
- Assert stored precision in a test when ordering depends on it.

## Related Issues

- See also: [net-7-digit-timestamps-rejected-by-laravel-data-task-status-update-20261015.md](../integration-issues/net-7-digit-timestamps-rejected-by-laravel-data-task-status-update-20261015.md)

## Module Reference
See [module docs](../../modules/task-status-update.md) for full module documentation.
