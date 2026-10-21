---
module: "Mobile App"
date: "2026-10-15"
problem_type: "build_error"
component: "config"
symptoms:
  - "`Because paxscan_app depends on flutter_test from sdk which depends on test_api 0.7.12 ... test 1.33.0 is forbidden.`"
  - "Earlier: `drift_dev >=2.28.2 is incompatible with build_runner >=2.17.1`"
root_cause: "dependency_mismatch"
framework_version: "Flutter 3.47.7, Dart 3.13.5 pub workspaces"
resolution_type: "dependency_update"
severity: "medium"
tags:
  - "pub-workspace"
  - "test_api"
  - "flutter_test"
  - "version-solving"
  - "build_runner"
---

# Troubleshooting: pub get fails in a Dart workspace: core package test version vs Flutter-pinned test_api

## Problem
Adding the Flutter app to the workspace broke resolution because the pure-Dart core used the latest `test`.

## Environment
- Module: Mobile App
- Framework Version: Flutter 3.47.7, Dart 3.13.5 pub workspaces
- Affected Component: config
- Date: 2026-10-15

## Symptoms
- `Because paxscan_app depends on flutter_test from sdk which depends on test_api 0.7.12 ... test 1.33.0 is forbidden.`
- Earlier: `drift_dev >=2.28.2 is incompatible with build_runner >=2.17.1`

## What Didn't Work

**Direct solution:** version-solver output names the conflict.

## Solution

Relax the core's `test` constraint (`^1.26.0`) so it resolves to a version compatible with Flutter's pinned `test_api`; pin `build_runner` below 2.17 to match drift_dev.

```
# packages/paxscan_core/pubspec.yaml
dev_dependencies:
  build_runner: ">=2.5.0 <2.17.0"
  test: ^1.26.0
```

## Why This Works

Pub workspaces use a single shared resolution; any SDK-pinned package (flutter_test → test_api) constrains every member, including pure-Dart packages.

## Prevention

- In a Flutter workspace, don't pin `test` to the newest release in Dart-only members.
- Check `flutter pub get` from the workspace root after adding members.

## Related Issues

- See also: [sqlcipher-flutter-libs-eol-encryption-mobile-app-20261015.md](../toolchain-issues/sqlcipher-flutter-libs-eol-encryption-mobile-app-20261015.md)

## Module Reference
See [module docs](../../modules/mobile-app.md) for full module documentation.
