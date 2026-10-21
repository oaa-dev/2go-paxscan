---
module: "TaskStatusUpdate"
date: "2026-10-15"
problem_type: "integration_issue"
component: "cast"
symptoms:
  - "`DateTimeImmutable::createFromFormat(\"Y-m-d\\TH:i:s.uP\", \"2026-10-15T08:00:00.1234567+00:00\")` returns false"
  - "laravel-data cannot cast `device_created_at` / `hub_received_at` from hub uploads"
root_cause: "date_format_mismatch"
framework_version: "Laravel 13.36, spatie/laravel-data 4.23, .NET 10"
resolution_type: "code_fix"
severity: "high"
tags:
  - "dotnet"
  - "datetimeoffset"
  - "laravel-data"
  - "carbon"
  - "timestamps"
  - "hub-sync"
---

# Troubleshooting: .NET DateTimeOffset timestamps (7 fractional digits) rejected by PHP date parsing

## Problem
The desktop hub (.NET) serializes DateTimeOffset as `2026-10-15T08:00:00.1234567+00:00`. PHP's `createFromFormat('Y-m-d\TH:i:s.uP')` — used by laravel-data's default date cast — accepts at most 6 fractional digits, so hub uploads would fail to cast.

## Environment
- Module: TaskStatusUpdate
- Framework Version: Laravel 13.36, spatie/laravel-data 4.23, .NET 10
- Affected Component: cast
- Date: 2026-10-15

## Symptoms
- `DateTimeImmutable::createFromFormat("Y-m-d\TH:i:s.uP", "2026-10-15T08:00:00.1234567+00:00")` returns false
- laravel-data cannot cast `device_created_at` / `hub_received_at` from hub uploads

## What Didn't Work

**Direct solution:** caught by testing PHP parsing of a real .NET timestamp before writing the DTO (`strtotime` and `Carbon::parse` succeed; `createFromFormat` fails).

## Solution

Added a custom cast that parses any ISO 8601 string with `CarbonImmutable::parse()` and normalizes to UTC, applied with `#[WithCast]` on the DTO date properties.

```
// app/Data/Casts/IsoDateTimeCast.php
public function cast(DataProperty $property, mixed $value, array $properties, CreationContext $context): CarbonImmutable|Uncastable
{
    return is_string($value) ? CarbonImmutable::parse($value)->utc() : Uncastable::create();
}

// app/Data/TaskStatusUpdateData.php
#[WithCast(IsoDateTimeCast::class)]
public CarbonImmutable $hubReceivedAt,
```

## Why This Works

.NET's round-trip format ("o") always writes 7 fractional digits (ticks = 100 ns). PHP's format parser treats `u` as up to 6 digits and rejects the extra one; the free-form parser behind `strtotime`/`Carbon::parse` tolerates longer fractions (truncating to microseconds). The FormRequest `date` rule already uses the free-form parser, so only the DTO cast had to change.

## Prevention

- Any PHP endpoint receiving .NET timestamps: parse with `Carbon::parse`, never `createFromFormat` with `.u`.
- Feature tests send real .NET-format timestamps (`hubItem()` in tests/Pest.php uses 7 digits).
- Alternatively make the .NET side emit 6 digits — but tolerant parsing on the receiver is safer.

## Related Issues

- See also: [eloquent-drops-microseconds-timestamptz-task-status-update-20261015.md](../database-issues/eloquent-drops-microseconds-timestamptz-task-status-update-20261015.md)

## Module Reference
See [module docs](../../modules/task-status-update.md) for full module documentation.
