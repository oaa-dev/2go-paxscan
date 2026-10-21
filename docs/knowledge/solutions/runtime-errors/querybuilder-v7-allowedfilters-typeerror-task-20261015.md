---
module: "Task"
date: "2026-10-15"
problem_type: "runtime_error"
component: "repository"
symptoms:
  - "`TypeError: Spatie\\QueryBuilder\\QueryBuilder::allowedFilters(): Argument #1 must be of type Spatie\\QueryBuilder\\AllowedFilter|string, array given`"
  - "All filter tests failed with 500"
root_cause: "api_breaking_change"
framework_version: "Laravel 13.36, spatie/laravel-query-builder 7.3"
resolution_type: "code_fix"
severity: "high"
tags:
  - "spatie"
  - "query-builder"
  - "variadic"
  - "breaking-change"
  - "filters"
---

# Troubleshooting: Spatie QueryBuilder v7: allowedFilters()/allowedSorts() no longer accept an array

## Problem
Every task list request returned HTTP 500 after following the rules' `protected function allowedFilters(): array` pattern.

## Environment
- Module: Task
- Framework Version: Laravel 13.36, spatie/laravel-query-builder 7.3
- Affected Component: repository
- Date: 2026-10-15

## Symptoms
- `TypeError: Spatie\QueryBuilder\QueryBuilder::allowedFilters(): Argument #1 must be of type Spatie\QueryBuilder\AllowedFilter|string, array given`
- All filter tests failed with 500

## What Didn't Work

**Direct solution:** the TypeError named the exact signature change.

## Solution

Spread the arrays into the now-variadic methods.

```
// app/Repositories/TaskRepository.php
return QueryBuilder::for(Task::class)
    ->allowedFilters(...$this->allowedFilters())
    ->allowedSorts(...$this->allowedSorts())
    ->defaultSort('-updated_at');
```

## Why This Works

v7 changed `allowedFilters`, `allowedSorts`, `allowedIncludes` to variadic `AllowedFilter|string ...$filters`. Passing one array is a type error in strict PHP.

## Prevention

- Keep returning arrays from `allowedFilters()` (rules/laravel.md §4) but always spread them on v7+.
- Filter tests (one dataset per filter) catch this immediately.

## Related Issues

No related issues documented yet.

## Module Reference
See [module docs](../../modules/task.md) for full module documentation.
