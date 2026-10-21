---
module: "Mobile App"
date: "2026-10-15"
problem_type: "platform_issue"
component: "platform_config"
symptoms:
  - "`adb.exe: failed to install app-debug.apk: Failure [INSTALL_FAILED_USER_RESTRICTED: Install canceled by user]`"
root_cause: "platform_restriction"
framework_version: "Android 16 (HyperOS, Xiaomi 24090RA29G), adb 37"
resolution_type: "environment_setup"
severity: "low"
tags:
  - "adb"
  - "xiaomi"
  - "hyperos"
  - "install-via-usb"
  - "usb-debugging"
---

# Troubleshooting: adb install fails on Xiaomi: INSTALL_FAILED_USER_RESTRICTED

## Problem
The APK built fine but could not be installed over USB.

## Environment
- Module: Mobile App
- Framework Version: Android 16 (HyperOS, Xiaomi 24090RA29G), adb 37
- Affected Component: platform_config
- Date: 2026-10-15

## Symptoms
- `adb.exe: failed to install app-debug.apk: Failure [INSTALL_FAILED_USER_RESTRICTED: Install canceled by user]`

## What Didn't Work

**Direct solution:** known HyperOS/MIUI restriction.

## Solution

On the phone: Developer options → enable **Install via USB** (may require Mi account sign-in/SIM) and **USB debugging (Security settings)**; keep the phone unlocked and approve the on-screen prompt; re-run `adb install -r`.

```
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" install -r app\build\app\outputs\flutter-apk\app-debug.apk
```

## Why This Works

HyperOS blocks package installs from adb unless "Install via USB" is enabled, and shows a time-limited confirmation that counts as "canceled by user" if not answered.

## Prevention

- Include Xiaomi's extra developer toggles in device setup docs.

## Related Issues

- See also: [android-ndk-not-installed-new-android-cli-mobile-app-20261015.md](../toolchain-issues/android-ndk-not-installed-new-android-cli-mobile-app-20261015.md)

## Module Reference
See [module docs](../../modules/mobile-app.md) for full module documentation.
