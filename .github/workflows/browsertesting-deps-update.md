---
on:
  schedule: every 1mo
  workflow_dispatch:

description: >
  Monthly workflow that checks for newer Selenium and Playwright versions
  and opens a PR to update them in the repository.

permissions:
  contents: read
  pull-requests: read
  issues: read

network:
  allowed:
    - defaults
    - dotnet
    - containers

tools:
  github:
  edit:
  bash: ["grep", "sed", "jq", "git"]

mcp-servers:
  nuget:
    container: "mcr.microsoft.com/dotnet/sdk:10.0"
    entrypoint: "dnx"
    entrypointArgs: ["NuGet.Mcp.Server", "--source", "https://api.nuget.org/v3/index.json", "--yes"]
    allowed: ["get-latest-package-version"]

safe-outputs:
  create-pull-request:
    title-prefix: "[build-ops] "
    labels: [build-ops]
    draft: false
    base-branch: main
  add-comment:
    target: "*"
    max: 1
---

# Update Browser-Testing Dependencies (Selenium & Playwright)

Selenium is used in the aspnetcore repo for automated E2E integration testing.
Playwright is used for some benchmarking apps. Both need to be kept up to date.

## Task

Check for the latest stable versions of the following NuGet packages and update them in the repository if newer versions are available.

### 1. Selenium packages

Look up the latest stable versions of these NuGet packages:

- **Selenium.WebDriver** — https://www.nuget.org/packages/Selenium.WebDriver/
- **Selenium.Support** — https://www.nuget.org/packages/Selenium.Support/

Then update the corresponding version variables in `eng/Versions.props`:

- `SeleniumWebDriverVersion`
- `SeleniumSupportVersion`

### 2. Playwright packages

Look up the latest stable version of the Playwright package:

- **Microsoft.Playwright** — https://www.nuget.org/packages/Microsoft.Playwright/

Then update:

- The `MicrosoftPlaywrightVersion` variable in `eng/Versions.props`
- The Playwright Docker image tag in `src/Components/benchmarkapps/Wasm.Performance/dockerfile` — the image reference starts with `mcr.microsoft.com` and should use the matching version.

### How to look up latest NuGet versions

Use the NuGet MCP server's `get-latest-package-version` tool to look up each package:
- `Selenium.WebDriver`
- `Selenium.Support`
- `Microsoft.Playwright`

Do NOT use `curl`, `web-fetch`, or any direct HTTP requests to the NuGet API — they are blocked by the network firewall.

## Guidelines

- If all packages are already at their latest stable versions, report that no changes are needed.
- Only update to **stable** releases — skip prerelease versions.
- Keep Selenium.WebDriver and Selenium.Support on the same major version if possible.
- Make sure the Playwright Docker image tag in the dockerfile is consistent with the `PlaywrightVersion` in `eng/Versions.props`.
- Use the `edit` tool to modify files directly. Do NOT use `git commit`, `git push`, or `git config` commands — the `create-pull-request` safe output handles committing and pushing automatically.

## Output

When all edits are done, use the `create-pull-request` safe output to open the PR. Include a summary of what was updated in the PR body.

After the PR is created, use the `add-comment` safe output to post the following note on the created PR:

> **Note:** After merging, push all packages to dotnet-public repo by queuing a build from [dotnet-migrate-package](https://dev.azure.com/dnceng/internal/_build?definitionId=931&_a=summary).
