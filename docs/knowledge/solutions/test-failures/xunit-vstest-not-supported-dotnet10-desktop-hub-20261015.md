---
module: "Desktop Hub"
date: "2026-10-15"
problem_type: "test_failure"
component: "test"
symptoms:
  - "`error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`"
root_cause: "deprecated_tooling"
framework_version: ".NET 10 SDK 10.0.402, xUnit v3 4.0.2"
resolution_type: "config_change"
severity: "medium"
tags:
  - "xunit"
  - "microsoft-testing-platform"
  - "dotnet-test"
  - "global-json"
---

# Troubleshooting: dotnet test fails on .NET 10: VSTest target no longer supported by Microsoft.Testing.Platform

## Problem
All test projects built, but `dotnet test` refused to run them.

## Environment
- Module: Desktop Hub
- Framework Version: .NET 10 SDK 10.0.402, xUnit v3 4.0.2
- Affected Component: test
- Date: 2026-10-15

## Symptoms
- `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`

## What Didn't Work

**Direct solution:** the error links to aka.ms/dotnet-test-mtp-error.

## Solution

Opt in to Microsoft Testing Platform in `global.json`, drop `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`, and run `dotnet test --solution`.

```
// global.json
{ "sdk": { "version": "10.0.400", "rollForward": "latestFeature" },
  "test": { "runner": "Microsoft.Testing.Platform" } }
```

## Why This Works

xUnit v3 test projects are executables on Microsoft.Testing.Platform; on the .NET 10 SDK `dotnet test` defaults to the VSTest bridge, which MTP no longer supports.

## Prevention

- New .NET 10 test setups: xunit.v3 + MTP opt-in; no VSTest packages.
- CI uses `dotnet test --solution X.slnx`.

## Related Issues

No related issues documented yet.

## Module Reference
See [module docs](../../modules/desktop-hub.md) for full module documentation.
