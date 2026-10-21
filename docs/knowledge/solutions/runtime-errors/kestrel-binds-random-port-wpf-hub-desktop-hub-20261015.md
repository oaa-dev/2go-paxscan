---
module: "Desktop Hub"
date: "2026-10-15"
problem_type: "runtime_error"
component: "host_builder"
symptoms:
  - "Log: `Now listening on: https://[::]:52305`"
  - "`netstat` showed nothing listening on :5443"
root_cause: "lifecycle_timing"
framework_version: ".NET 10, ASP.NET Core Kestrel, WPF"
resolution_type: "code_fix"
severity: "critical"
tags:
  - "kestrel"
  - "configurekestrel"
  - "port"
  - "wpf"
  - "generic-host"
  - "startup-order"
---

# Troubleshooting: Embedded Kestrel in WPF listened on a random port instead of 5443

## Problem
The hub window opened, but phones couldn't reach port 5443: Kestrel bound a random port. Only visible when running the real app on Windows (tests use TestServer).

## Environment
- Module: Desktop Hub
- Framework Version: .NET 10, ASP.NET Core Kestrel, WPF
- Affected Component: host_builder
- Date: 2026-10-15

## Symptoms
- Log: `Now listening on: https://[::]:52305`
- `netstat` showed nothing listening on :5443

## What Didn't Work

**Direct solution:** the startup log line showed the wrong port; the cause was the order of host build vs. reading the port from SQLite.

## Solution

Bind the URL after the database is initialized and the port is known (`app.Urls`), and resolve the certificate lazily via `ConfigureHttpsDefaults`.

```
// Hosting/HubHostBuilder.cs
builder.WebHost.ConfigureKestrel(kestrel => kestrel.ConfigureHttpsDefaults(https =>
{
    var certificates = kestrel.ApplicationServices.GetRequiredService<CertificateService>();
    https.ServerCertificateSelector = (_, _) => certificates.GetOrCreate();
}));

// App.xaml.cs (after DatabaseInitializer + reading settings)
_host.Urls.Clear();
_host.Urls.Add($"https://0.0.0.0:{runtime.Port}");
await _host.StartAsync();
```

## Why This Works

`ConfigureKestrel` callbacks run when `KestrelServerOptions` are first resolved, which happens during `builder.Build()` — before `App.OnStartup` had migrated the DB and copied the port into `HubRuntime`. The callback saw Port = 0 and `ListenAnyIP(0)` picks a free port. `app.Urls` is read at `StartAsync`, after the port is known.

## Prevention

- Never read DB-backed settings inside `ConfigureKestrel`; set endpoints via `app.Urls` before `StartAsync`, or load settings before `Build()`.
- Smoke-test the real host on the target OS (TestServer can't catch binding issues).

## Related Issues

No related issues documented yet.

## Module Reference
See [module docs](../../modules/desktop-hub.md) for full module documentation.
