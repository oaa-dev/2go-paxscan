---
module: "Mobile App"
date: "2026-10-15"
problem_type: "toolchain_issue"
component: "platform_config"
symptoms:
  - "`Android sdkmanager did not install NDK 28.2.13676358 into C:\\Users\\...\\Android\\Sdk`"
  - "`Package ndk not found.` / `Package 28.2.13676358 not found.`"
  - "`WARNING: The SDK Manager CLI tool (sdkmanager) is deprecated. Android CLI will be used instead.`"
root_cause: "deprecated_tooling"
framework_version: "Flutter 3.47.7, Android cmdline-tools 16111833, NDK 28.2.13676358, Windows 11"
resolution_type: "environment_setup"
severity: "high"
tags:
  - "android"
  - "ndk"
  - "sdkmanager"
  - "android-cli"
  - "windows"
  - "flutter-build"
---

# Troubleshooting: flutter build apk fails: NDK not installed; sdkmanager package names rejected

## Problem
First Android build on Windows failed because the NDK was missing; installing it with `sdkmanager "ndk;28.2.13676358"` silently installed nothing.

## Environment
- Module: Mobile App
- Framework Version: Flutter 3.47.7, Android cmdline-tools 16111833, NDK 28.2.13676358, Windows 11
- Affected Component: platform_config
- Date: 2026-10-15

## Symptoms
- `Android sdkmanager did not install NDK 28.2.13676358 into C:\Users\...\Android\Sdk`
- `Package ndk not found.` / `Package 28.2.13676358 not found.`
- `WARNING: The SDK Manager CLI tool (sdkmanager) is deprecated. Android CLI will be used instead.`

## What Didn't Work

**Attempted Solution 1:** let Gradle auto-install the NDK
- **Why it failed:** auto-install goes through the deprecated `sdkmanager` wrapper with the old package syntax.

**Attempted Solution 2:** `sdkmanager.bat --install "ndk;28.2.13676358" "platforms;android-36" "build-tools;36.0.0"`
- **Why it failed:** `cmd.exe` splits batch arguments on `;`, and the new tool expects `/`-separated names.

## Solution

Use the new Android CLI with the new package names.

```
& "$env:LOCALAPPDATA\Android\Sdk\cmdline-tools\latest\bin\android.exe" sdk --sdk="$env:LOCALAPPDATA\Android\Sdk" install ndk/28.2.13676358 platforms/android-36 build-tools/36.0.0
```

## Why This Works

Recent cmdline-tools replace `sdkmanager` with `android sdk` (package names `ndk/<ver>`, `platforms/android-36`); the `.bat` shim is a deprecated wrapper and `;` is an argument separator for batch files.

## Prevention

- Use `android sdk list --all` to find package names.
- Install the NDK version Flutter asks for (`flutter.ndkVersion`) before the first build.

## Related Issues

- See also: [xiaomi-install-failed-user-restricted-mobile-app-20261015.md](../platform-issues/xiaomi-install-failed-user-restricted-mobile-app-20261015.md)

## Module Reference
See [module docs](../../modules/mobile-app.md) for full module documentation.
