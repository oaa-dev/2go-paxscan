---
module: "Desktop Hub"
date: "2026-10-15"
problem_type: "connectivity_issue"
component: "hub_endpoint"
symptoms:
  - "Phone banner: \"Working offline · N not synced yet\""
  - "Hub log: `Device was not authenticated. Failure message: Unknown or revoked device.`"
  - "`adb reverse --list` empty after replugging; no inbound firewall rule for TCP 5443"
root_cause: "wrong_advertised_address"
framework_version: ".NET 10 hub, Flutter 3.47 app, Windows 11 Mobile Hotspot"
resolution_type: "environment_setup"
severity: "high"
tags:
  - "adb-reverse"
  - "firewall"
  - "hotspot"
  - "pairing"
  - "revoke"
  - "advertised-host"
---

# Troubleshooting: Phone shows "Working offline" unless the USB cable is connected

## Problem
The paired phone synced only while plugged in via USB; on the laptop's hotspot it stayed "Working offline". Earlier the user had also clicked Revoke, so the hub rejected the phone.

## Environment
- Module: Desktop Hub
- Framework Version: .NET 10 hub, Flutter 3.47 app, Windows 11 Mobile Hotspot
- Affected Component: hub_endpoint
- Date: 2026-10-15

## Symptoms
- Phone banner: "Working offline · N not synced yet"
- Hub log: `Device was not authenticated. Failure message: Unknown or revoked device.`
- `adb reverse --list` empty after replugging; no inbound firewall rule for TCP 5443

## What Didn't Work

**Attempted Solution 1:** connect the phone to the laptop hotspot
- **Why it failed:** the phone stores the hub URL from the pairing QR (`https://127.0.0.1:5443`), which only works through `adb reverse`; and Windows Firewall blocks inbound 5443 on the hotspot.

## Solution

Three separate fixes:
1. USB path: re-run `adb reverse tcp:5443 tcp:5443` after every replug.
2. Cable-free path: add an inbound firewall rule (admin), set the hub's advertised address to `192.168.137.1` (Windows Mobile Hotspot), and re-pair.
3. Revoked device: unpair on the phone (or clear app data) and pair again. Revoke is permanent; to "disconnect", just unplug/leave the network.

```
netsh advfirewall firewall add rule name="PaxScan Hub" dir=in action=allow protocol=TCP localport=5443 profile=any
```

## Why This Works

The QR embeds `hub_url`; the device keeps it after pairing (pinning is by certificate fingerprint, the address is fixed). `127.0.0.1` on a phone is the phone itself. Windows blocks unsolicited inbound connections without a rule. Revoked devices fail `DeviceAuthenticator` (status must be Active).

## Prevention

- Pick the advertised address for the network phones will actually use (hotspot: 192.168.137.1).
- The installer should add the firewall rule; managed (domain) laptops may need IT.
- Consider letting the phone edit the hub address without re-pairing (pin keeps it safe).
- A revoked phone with unsynced updates cannot unpair (gap: add "discard and unpair").

## Related Issues

No related issues documented yet.

## Module Reference
See [module docs](../../modules/desktop-hub.md) for full module documentation.
