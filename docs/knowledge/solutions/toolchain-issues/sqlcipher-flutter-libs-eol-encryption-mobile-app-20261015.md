---
module: "Mobile App"
date: "2026-10-15"
problem_type: "toolchain_issue"
component: "drift_table"
symptoms:
  - "pub.dev: `sqlcipher_flutter_libs 0.7.0+eol`, `sqlite3_flutter_libs 0.6.0+eol`"
root_cause: "deprecated_tooling"
framework_version: "Drift 2.35, sqlite3 3.7, Flutter 3.47"
resolution_type: "dependency_update"
severity: "medium"
tags:
  - "drift"
  - "sqlite3mc"
  - "sqlcipher"
  - "encryption"
  - "build-hooks"
---

# Troubleshooting: sqlcipher_flutter_libs is EOL — encrypt Drift with SQLite3MultipleCiphers build hooks

## Problem
The planned encryption package is end-of-life (`Not used anymore, update to version 3.x of package:sqlite3 instead`).

## Environment
- Module: Mobile App
- Framework Version: Drift 2.35, sqlite3 3.7, Flutter 3.47
- Affected Component: drift_table
- Date: 2026-10-15

## Symptoms
- pub.dev: `sqlcipher_flutter_libs 0.7.0+eol`, `sqlite3_flutter_libs 0.6.0+eol`

## What Didn't Work

**Direct solution:** checked package status before writing the DB layer.

## Solution

Select SQLite3MultipleCiphers via hook user_defines in the workspace root, set `PRAGMA key` in Drift's `setup`, and assert cipher support.

```
# paxscan-mobile/pubspec.yaml (workspace root)
hooks:
  user_defines:
    sqlite3:
      source: sqlite3mc

// app_database.dart
NativeDatabase.createInBackground(file, setup: (db) {
  if (!hasCipherSupport(db)) throw StateError('SQLite3MultipleCiphers is not bundled');
  db..execute("PRAGMA key = '...';")..execute('PRAGMA journal_mode = WAL;')..execute('PRAGMA synchronous = FULL;');
});
```

## Why This Works

sqlite3 3.x bundles SQLite via Dart build hooks; the `source` user-define switches the bundled binary to sqlite3mc, which implements encryption behind the normal C API. `PRAGMA cipher` exists only in that build, so it verifies the right binary is loaded.

## Prevention

- Hook user_defines must be in the workspace root pubspec.
- Test that the DB file is unreadable without the key (encrypted_database_test.dart).

## Related Issues

- See also: [workspace-test-api-version-conflict-mobile-app-20261015.md](../build-errors/workspace-test-api-version-conflict-mobile-app-20261015.md)

## Module Reference
See [module docs](../../modules/mobile-app.md) for full module documentation.
