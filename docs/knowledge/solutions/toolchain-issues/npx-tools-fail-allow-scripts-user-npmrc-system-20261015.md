---
module: "System"
date: "2026-10-15"
problem_type: "toolchain_issue"
component: "config"
symptoms:
  - "`npm error code EALLOWSCRIPTS`"
  - "`--allow-scripts is not allowed in project-scoped installs. Add the entries to the \"allowScripts\" field in package.json, or to .npmrc, instead.`"
root_cause: "env_config"
framework_version: "npm 11.19, Node 24, create-next-app 16.4, shadcn 4.21"
resolution_type: "environment_setup"
severity: "medium"
tags:
  - "npm11"
  - "allow-scripts"
  - "npmrc"
  - "npx"
  - "shadcn"
  - "create-next-app"
---

# Troubleshooting: create-next-app / shadcn installs fail: --allow-scripts not allowed in project-scoped installs

## Problem
Tools run via `npx` that spawn their own `npm install` failed, while a manual `npm install` in the same folder worked.

## Environment
- Module: System-wide
- Framework Version: npm 11.19, Node 24, create-next-app 16.4, shadcn 4.21
- Affected Component: config
- Date: 2026-10-15

## Symptoms
- `npm error code EALLOWSCRIPTS`
- `--allow-scripts is not allowed in project-scoped installs. Add the entries to the "allowScripts" field in package.json, or to .npmrc, instead.`

## What Didn't Work

**Attempted Solution 1:** retry `shadcn init` / pass other flags
- **Why it failed:** the flag came from the user config, not from the tool.

## Solution

The user's `~/.npmrc` contains `allow-scripts=@anthropic-ai/claude-code` (for a global install). npx forwards user config to child installs as a CLI-level setting, which npm 11 rejects in projects. Run such tools with the user config disabled for that command only (don't change the user's file).

```
NPM_CONFIG_USERCONFIG=/dev/null npx -y shadcn@4.21.5 init --defaults --yes
NPM_CONFIG_USERCONFIG=/dev/null npx -y shadcn@4.21.5 add button input ...
```

## Why This Works

npm 11 restricts `allow-scripts` to package.json `allowScripts` or project `.npmrc` for project installs; when inherited as a flag it errors. Pointing `NPM_CONFIG_USERCONFIG` at /dev/null removes the inherited setting.

## Prevention

- If `EALLOWSCRIPTS` appears from an npx tool, check `~/.npmrc` first.
- Approve needed install scripts per project with `npm install-scripts approve <pkg>`.

## Related Issues

No related issues documented yet.
