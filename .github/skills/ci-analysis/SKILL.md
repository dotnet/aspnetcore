---
name: ci-analysis
description: >
  Analyze CI build and test status from Azure DevOps and Helix for dotnet repository PRs.
  Use when checking CI status, investigating failures, determining if a PR is ready to merge,
  or given URLs containing dev.azure.com or helix.dot.net. Also use when asked "why is CI red",
  "test failures", "retry CI", "rerun tests", "is CI green", "build failed", "checks failing",
  or "flaky tests". DO NOT USE FOR: investigating stale codeflow PRs or dependency update health,
  tracing whether a commit has flowed from one repo to another, reviewing code changes for
  correctness or style.
---

# Azure DevOps and Helix CI Analysis

Analyze CI build status and test failures in Azure DevOps and Helix for dotnet repositories (runtime, sdk, aspnetcore, roslyn, and more).

> 🚨 **NEVER** use `gh pr review --approve` or `--request-changes`. Only `--comment` is allowed. Approval and blocking are human-only actions.

**Workflow**: Gather PR context (Step 0) → run the script → read human-readable output + `[CI_ANALYSIS_SUMMARY]` JSON → synthesize recommendations. The script collects data; you generate the advice. MCP tools (AzDO, Helix, GitHub) provide supplementary access when available; the script and `gh` CLI work independently when they're not.

**Accessing services**: There are several possible methods to access each service (AzDO, Helix, GitHub). Start with MCP tools, then fall back to CLI (`gh` for GitHub, `Invoke-RestMethod` for AzDO/Helix REST APIs). Explore all available options before determining you don't have access. For AzDO, multiple tool sets may exist for different organizations — match the org in the build URL to the correct tools (see [references/azdo-helix-reference.md](references/azdo-helix-reference.md#azure-devops-organizations)). If queries return null, check the org before trying other approaches. For complex investigations, track what you've tried in SQL to avoid repeating failed approaches.

## When to Use This Skill

- Checking CI status ("is CI passing?", "why is CI red?")
- Investigating CI failures or determining merge readiness
- Debugging Helix test issues or build errors
- URLs containing `dev.azure.com`, `helix.dot.net`, or GitHub PR links with failing checks
- Questions like "retry CI", "rerun tests", "test failures", "checks failing"
- Investigating canceled or timed-out jobs for recoverable results

**Not for**: GitHub Actions workflows, non-Helix repos, or build performance (use binlog analysis).

> 💡 **Per-repo CI patterns differ significantly.** Each dotnet repo structures test results differently (TRX availability, console log patterns, work item naming). Before investigating a repo you haven't seen before, check the empirical profiles in `dnceng-knowledge/ci-repo-profiles` — they document the fastest investigation path per repo and prevent wasted MCP calls.

## Quick Start

```powershell
# Analyze PR failures (most common) - defaults to dotnet/runtime
./scripts/Get-CIStatus.ps1 -PRNumber 123445 -ShowLogs

# Analyze by build ID
./scripts/Get-CIStatus.ps1 -BuildId 1276327 -ShowLogs

# Query specific Helix work item
./scripts/Get-CIStatus.ps1 -HelixJob "4b24b2c2-..." -WorkItem "System.Net.Http.Tests"

# Other dotnet repositories
./scripts/Get-CIStatus.ps1 -PRNumber 12345 -Repository "dotnet/aspnetcore"
```

For full parameter reference and mode details, see [references/script-modes.md](references/script-modes.md).

## Step 0: Gather Context (before running anything)

1. **Read PR metadata** — title, description, author, labels, linked issues
2. **Classify the PR type**:

| PR Type | How to detect | Interpretation shift |
|---------|--------------|---------------------|
| **Code PR** | Human author, code changes | Failures likely relate to the changes |
| **Flow/Codeflow PR** | Author is `dotnet-maestro[bot]`, "Update dependencies" | Missing packages may be behavioral, not infrastructure |
| **Backport** | Title mentions "backport", targets release branch | Check if test exists on target branch |
| **Merge PR** | Merging between branches | Conflicts cause failures, not individual changes |
| **Dependency update** | Bumps package versions, global.json | Build failures often trace to the dependency |

3. **Check existing comments** — has someone already diagnosed failures or is a retry pending?
4. **Note the changed files** — you'll use these for correlation after the script runs

## After the Script: Use Its Output

> 🚨 **The script already collected the data. Do NOT re-query AzDO or Helix for information the script already produced.** Parse the `[CI_ANALYSIS_SUMMARY]` JSON and the human-readable output first. Only make additional API calls for data the script *didn't* provide (e.g., deeper Helix log searches, binlog analysis, build progression).

**If the script found no builds** (e.g., AzDO builds expired, CI not triggered): report this to the user immediately. Don't spend turns re-querying AzDO with different org/project combinations — if the script couldn't find builds, they're likely unavailable. Offer alternatives: analyze by build ID if the user has one, check GitHub PR status for summary info, or note that Helix results may still be queryable directly even when AzDO builds have expired.

**If the script succeeded**: the `[CI_ANALYSIS_SUMMARY]` JSON contains `failedJobDetails`, `knownIssues`, `canceledJobNames`, `prCorrelation`, and `recommendationHint`. Use these fields — don't re-fetch the same data via MCP tools or REST APIs. To find specific details in large output, use `Select-String` or `grep` on the output file rather than re-running the script.

> 🚨 **Check build progression on multi-commit PRs.** If the PR has multiple commits, query AzDO for builds on `refs/pull/{PR}/merge` (sorted by queue time, top 10-20) — `gh pr checks` only shows the latest SHA. Present a progression table showing which builds passed/failed at which SHAs. This narrows failures to the commit that introduced them. See [references/build-progression-analysis.md](references/build-progression-analysis.md).

Then follow the detailed workflow in [references/analysis-workflow.md](references/analysis-workflow.md). Key principles:

1. **Cross-reference failures with known issues** — The script outputs `failedJobDetails` and `knownIssues` as separate lists. You must explicitly match each failure to a known issue (by error message, test name, or job type) or mark it **unmatched**. Don't present them as two independent lists — the user needs a per-failure verdict.
2. **Check Build Analysis status** — Green = all failures matched known issues. Red = some unmatched. Never claim "all known issues" when Build Analysis is red.
3. **Correlate with PR changes** — same files failing = likely PR-related.
4. **Verify before claiming** — don't call it "infrastructure" without Build Analysis match or target-branch verification. Don't call it "safe to retry" unless ALL failures are accounted for.

For interpreting error categories, crash recovery, and canceled jobs: [references/failure-interpretation.md](references/failure-interpretation.md)

For generating recommendations from `[CI_ANALYSIS_SUMMARY]` JSON: [references/recommendation-generation.md](references/recommendation-generation.md)

## Presenting Results

> 🚨 **Keep tables narrow — 4 short columns max (# | Job | Verdict | Issue).** Put error descriptions, work item lists, and evidence in **detail bullets below the table**, not in cells. Wide tables wrap and become unreadable in terminals.

> 🚨 **Use markdown links** for PRs (`[#121195](url)`), builds (`[Build 1305302](url)`), and jobs (`[job name](azdo-job-url)`). The script output and MCP tools provide URLs — thread them through.

Lead with a 1-2 sentence verdict, then the summary table, then detail bullets (one per failure), then recommended actions. For the full format example: [references/recommendation-generation.md](references/recommendation-generation.md).

## Anti-Patterns

> 🚨 **Every failure verdict needs evidence — no "Likely flaky" without proof.** Each row in your summary table must cite a specific source: known issue number, Build Analysis match, or target-branch verification. If Build Analysis didn't match it and you haven't verified the target branch, the verdict is **"Unmatched — needs investigation"**, not "Likely flaky." A test that *looks* like it could be flaky is not the same as one you've *verified* is flaky.

> ❌ **Don't label failures "infrastructure" without evidence.** Requires: Build Analysis match, identical failure on target branch, or confirmed outage. Exception: `tests-passed-reporter-failed` is genuinely infrastructure.

> ❌ **Don't dismiss timed-out builds.** A build "failed" due to AzDO timeout can have 100% passing Helix work items. Check Helix job status before concluding failure.

> ❌ **Missing packages on flow PRs ≠ infrastructure.** Flow PRs request *different* packages. Check *which* package and *why* before assuming feed delay.

> ❌ **Don't present failures and known issues as separate lists.** Cross-reference them: for each `failedJobDetails` entry, state whether it matches a `knownIssues` entry or is unmatched. An `unclassified` failure can still match a known issue by error pattern.

> ❌ **Don't say "safe to retry" with Build Analysis red.** Map each failing job to a specific known issue first.

> ❌ **Don't use `Invoke-RestMethod` or `curl` for AzDO/Helix when MCP tools are available.** Check your available tools for Azure DevOps and Helix operations first. REST API fallback is for when MCP tools are genuinely unavailable, not a first resort.

## References

- **Script modes & parameters**: [references/script-modes.md](references/script-modes.md)
- **Failure interpretation**: [references/failure-interpretation.md](references/failure-interpretation.md)
- **Recommendation generation**: [references/recommendation-generation.md](references/recommendation-generation.md)
- **Analysis workflow (Steps 1–3)**: [references/analysis-workflow.md](references/analysis-workflow.md)
- **Helix artifacts & binlogs**: [references/helix-artifacts.md](references/helix-artifacts.md)
- **Binlog comparison**: For cross-build binlog diffs, use deep investigation techniques from [references/delegation-patterns.md](references/delegation-patterns.md)
- **Build progression analysis**: [references/build-progression-analysis.md](references/build-progression-analysis.md)
- **Subagent delegation**: [references/delegation-patterns.md](references/delegation-patterns.md)
- **Azure CLI investigation**: [references/azure-cli.md](references/azure-cli.md)
- **Manual investigation**: [references/manual-investigation.md](references/manual-investigation.md)
- **SQL tracking**: [references/sql-tracking.md](references/sql-tracking.md)
- **AzDO/Helix details**: [references/azdo-helix-reference.md](references/azdo-helix-reference.md)

## Tips

1. Check if same test fails on target branch before assuming transient
2. Look for `[ActiveIssue]` attributes for known skipped tests
3. Use `-SearchMihuBot` for semantic search of related issues
4. `gh pr checks --json` fields: `bucket`, `completedAt`, `description`, `event`, `link`, `name`, `startedAt`, `state`, `workflow` — `state` has `SUCCESS`/`FAILURE` directly (no `conclusion` field)
5. "Canceled" ≠ "Failed" — canceled jobs may have recoverable Helix results. Helix data may persist even when AzDO builds have expired — query Helix directly if you have job IDs.
6. **Truncated failure details**: When `failedJobDetailsTruncated` is `true` in the JSON output, the `failedJobDetails` array is capped at `-MaxJobs` (default 5). The full failure count is always available in `totalFailedJobs`, and all failed job names are listed in `failedJobNames` — use these to assess the full scope before investigating details. Pass `-MaxJobs N` to increase the detail cap for builds with many failures.
