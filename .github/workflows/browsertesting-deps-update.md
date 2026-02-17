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
  bash: ["curl", "grep", "sed", "jq", "git"]
  web-fetch:

safe-outputs:
  create-pull-request:
    title-prefix: "[build-ops] "
    labels: [build-ops]
    draft: false
    base-branch: main
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

- The `PlaywrightVersion` variable in `eng/Versions.props`
- The Playwright Docker image tag in `src/Components/benchmarkapps/Wasm.Performance/dockerfile` — the image reference starts with `mcr.microsoft.com` and should use the matching version.

### How to look up latest NuGet versions

Use the NuGet API to find the latest stable version:

```
curl -s "https://api.nuget.org/v3-flatcontainer/{package-id-lowercase}/index.json"
```

The response contains a `versions` array. Pick the latest version that does NOT contain a prerelease suffix (no `-alpha`, `-beta`, `-preview`, `-rc`).

## Guidelines

- If all packages are already at their latest stable versions, report that no changes are needed.
- Only update to **stable** releases — skip prerelease versions.
- Keep Selenium.WebDriver and Selenium.Support on the same major version if possible.
- Make sure the Playwright Docker image tag in the dockerfile is consistent with the `PlaywrightVersion` in `eng/Versions.props`.
- Mention in discussion or ideally assign PR to `@dotnet/aspnet-build` for review.
