# Pipeline Health: aspnetcore-ci (Public + Internal)
**Date:** 2026-04-30 08:29 PDT  
**Window:** 24 hours (Apr 29 15:29 UTC – Apr 30 15:29 UTC)  
**Pipelines:** aspnetcore-ci (public, def 83) + aspnetcore-ci-official (internal, def 21)

## Public Pipeline (aspnetcore-ci, def 83)

### Failed Builds

| Build | Type | Source | Failure Detail |
|-------|------|--------|---------------|
| 1402234 | Other PR | PR #66443 (FileExtensionSignInfo for .cab) | Java compilation error — `gradlew compileJava` exit code 1 |
| 1402271 | Rolling | main | MSB3030 copy race — file not found during `_CopyFilesMarkedCopyLocal` |
| 1402568 | Other PR | PR #66529 (ConfigureHostConfiguration) | Helix queue timeout — canceled after ~4h |
| 1402604 | Rolling | release/11.0-preview4 | Helix queue timeout — canceled after ~4h |
| 1402743 | Other PR | PR #66507 (Update Helix queues) | Helix queue timeout — canceled after ~4h |
| 1402801 | Forward Flow | release/10.0 ← dotnet/dotnet (PR #66530) | Helix queue timeout — canceled after ~4h |
| 1402816 | Other PR | PR #66531 (CssClass for ValidationSummary) | Helix queue timeout — canceled after ~4h |
| 1402825 | Other PR | PR #66532 (StringSyntax DateTimeFormat) | CS1010 — Newline in constant (code error in PR) |
| 1402912 | Other PR | PR #66355 (Map Obsolete to deprecated in OpenAPI) | Helix queue timeout — canceled after ~4h |
| 1402939 | Other PR | PR #66533 (FeatureReferences race fix) | CS0518 — IsExternalInit not defined (code error in PR) |
| 1403008 | Forward Flow | main ← dotnet/dotnet (PR #66534) | Helix queue timeout — canceled after ~4h |
| 1403258 | Other PR | PR #66383 (ppc64le NU1101 fix) | Helix test failure — InMemory.FunctionalTests |
| 1403388 | Other PR | PR #66528 (RenderFragment serialization) | Helix test failure — Components.Endpoints.Tests (3 queues) |
| 1403633 | Other PR | PR #66540 (Update MoqVersion) | Helix test failure — Mvc.Core.Test + Mvc.FunctionalTests (3 queues) |

### Summary

| Type | Completed | ✅ Pass | ❌ Fail | Pass Rate |
|------|-----------|---------|---------|-----------|
| Rolling | 2 | 0 | 2 | 0% |
| Forward Flow | 2 | 0 | 2 | 0% |
| Other PR | 13 | 3 | 10 | 23% |
| **Total** | **17** | **3** | **14** | **18%** |

*4 builds currently in progress.*

### Failure Trends

| Pattern | Hits | Window | Status |
|---------|------|--------|--------|
| Helix queue timeout (canceled ~4h) | 7/17 | Apr 29 19:00 – Apr 30 06:05 UTC | ❌ Helix infrastructure issue — queues starved |
| Helix test failures (real) | 3/17 | Apr 30 | ❌ Needs investigation per test |
| MSBuild copy race (MSB3030) | 1/17 | Apr 29 (main rolling) | ⏳ Fix in progress PR #66440 + dotnet/msbuild#13599 |
| PR code errors (CS1010, CS0518, Java) | 3/17 | Apr 29-30 | N/A — author fixes needed |

---

## Internal Pipeline (aspnetcore-ci-official, def 21)

### Failed Builds

| Build | Type | Source | Failure Detail |
|-------|------|--------|---------------|
| 2963502 | Rolling | release/11.0-preview4 | NU1903 — RepoTasks.csproj vulnerability audit (fix not yet on this branch) |
| 2963644 | Other PR | Internal PR #60605 | Java compilation error — gradlew compileJava exit code 1 |
| 2963990 | Rolling | internal/release/8.0-nonstable | dotnet-install failed — SDK download failure |

### Canceled Builds (Normal — Superseded)

8 builds canceled due to batchedCI supersession or PR push updates — normal behavior.

### Partially Succeeded Builds

| Build | Type | Source | Detail |
|-------|------|--------|--------|
| 2964092 | Rolling | internal/release/8.0 | TSA onboarding exception (non-blocking) |
| 2964100 | Rolling | internal/release/8.0-nonstable | Warnings only |

### Summary (excluding normal cancellations)

| Type | Completed | ✅ Pass | ⚠️ Partial | ❌ Fail | Effective Pass Rate |
|------|-----------|---------|------------|---------|-------------------|
| Rolling | 5 | 1 | 2 | 2 | 60% (counting partial as pass) |
| Other PR | 1 | 0 | 0 | 1 | 0% |
| **Total** | **6** | **1** | **2** | **3** | **50%** |

*1 build currently in progress (main).*

### Failure Trends

| Pattern | Hits | Window | Status |
|---------|------|--------|--------|
| NU1903 (release/11.0-preview4) | 1/6 | Apr 29 | ❌ Fix (PR #66466) hasn't been backported to this branch |
| dotnet-install failure (8.0) | 1/6 | Apr 30 | ❌ Transient — SDK download failure |

---

## Key Observations

1. **Helix queue starvation is the #1 issue.** 7 of 17 public builds (41%) were canceled after ~4 hours waiting for Helix agents. All occurred in the Apr 29 evening – Apr 30 early morning UTC window.

2. **Public pass rate is critically low at 18%.** Excluding the 7 Helix timeouts, the effective pass rate is 3/10 = 30% — still poor due to real test failures and PR code errors.

3. **Both rolling builds on public failed.** Main hit MSB3030 copy race. Release/11.0-preview4 hit Helix queue timeout. Zero green rolling builds in 24 hours.

4. **Internal is healthier at 50%.** NU1903 on release/11.0-preview4 needs a backport of PR #66466. Release/9.0 succeeded cleanly.

5. **Components.Endpoints.Tests** failed on 3 separate Helix queues in build 1403388 — likely a real test regression, not infrastructure.

## Methodology

- **Public:** Queried AzDO REST API for aspnetcore-ci (def 83), last 24 hours, all statuses.
- **Internal:** Queried AzDO REST API for aspnetcore-ci-official (def 21) with azureauth token, last 24 hours.
- **PR classification:** Used `gh pr view` for author and title. Forward Flow = author `app/dotnet-maestro`.
- **Failure investigation:** Examined timeline records, extracted error messages, checked build durations (~4h = Helix queue starvation).
