---
name: ci-analysis
description: Analyze CI build and test status from Azure DevOps and Helix for dotnet repository PRs. Use when checking CI status, investigating failures, determining if a PR is ready to merge, or given URLs containing dev.azure.com or helix.dot.net. Also use when asked "why is CI red", "test failures", "retry CI", "rerun tests", "is CI green", "build failed", "checks failing", or "flaky tests".
---

# Azure DevOps and Helix CI Analysis

Analyze CI build status and test failures in Azure DevOps and Helix for dotnet repositories (aspnetcore, runtime, sdk, roslyn, and more).

> 🚨 **NEVER** use `gh pr review --approve` or `--request-changes`. Only `--comment` is allowed. Approval and blocking are human-only actions.

**Workflow**: Gather PR context (Step 0) → run the script → read the human-readable output + `[CI_ANALYSIS_SUMMARY]` JSON → synthesize recommendations yourself. The script collects data; you generate the advice. For supplementary investigation beyond the script, MCP tools (AzDO, Helix, GitHub) provide structured access when available; the script and `gh` CLI work independently when they're not.

## When to Use This Skill

Use this skill when:
- Checking CI status on a PR ("is CI passing?", "what's the build status?", "why is CI red?")
- Investigating CI failures or checking why a PR's tests are failing
- Determining if a PR is ready to merge based on CI results
- Debugging Helix test issues or analyzing build errors
- Given URLs containing `dev.azure.com`, `helix.dot.net`, or GitHub PR links with failing checks
- Asked questions like "why is this PR failing", "analyze the CI", "is CI green", "retry CI", "rerun tests", or "test failures"
- Investigating canceled or timed-out jobs for recoverable results

## Script Limitations

The `Get-CIStatus.ps1` script targets **Azure DevOps + Helix** infrastructure specifically. It won't help with:
- **GitHub Actions** workflows (different API, different log format)
- Repos not using **Helix** for test distribution (no Helix work items to query)
- Pure **build performance** questions (use MSBuild binlog analysis instead)

However, the analysis patterns in this skill (interpreting failures, correlating with PR changes, distinguishing infrastructure vs. code issues) apply broadly even outside AzDO/Helix.

## Quick Start

```powershell
# Analyze PR failures (most common) - defaults to dotnet/aspnetcore
./scripts/Get-CIStatus.ps1 -PRNumber 123445 -ShowLogs

# Analyze by build ID
./scripts/Get-CIStatus.ps1 -BuildId 1276327 -ShowLogs

# Query specific Helix work item
./scripts/Get-CIStatus.ps1 -HelixJob "4b24b2c2-..." -WorkItem "System.Net.Http.Tests"

# Other dotnet repositories
./scripts/Get-CIStatus.ps1 -PRNumber 12345 -Repository "dotnet/runtime"
./scripts/Get-CIStatus.ps1 -PRNumber 67890 -Repository "dotnet/sdk"
./scripts/Get-CIStatus.ps1 -PRNumber 11111 -Repository "dotnet/roslyn"
```

## Key Parameters

| Parameter | Description |
|-----------|-------------|
| `-PRNumber` | GitHub PR number to analyze |
| `-BuildId` | Azure DevOps build ID |
| `-ShowLogs` | Fetch and display Helix console logs |
| `-Repository` | Target repo (default: dotnet/aspnetcore) |
| `-MaxJobs` | Max failed jobs to show (default: 5) |
| `-SearchMihuBot` | Search MihuBot for related issues |

## Three Modes

The script operates in three distinct modes depending on what information you have:

| You have... | Use | What you get |
|-------------|-----|-------------|
| A GitHub PR number | `-PRNumber 12345` | Full analysis: all builds, failures, known issues, structured JSON summary |
| An AzDO build ID | `-BuildId 1276327` | Single build analysis: timeline, failures, Helix results |
| A Helix job ID (optionally a specific work item) | `-HelixJob "..." [-WorkItem "..."]` | Deep dive: list work items for the job, or with `-WorkItem`, focus on a single work item's console logs, artifacts, and test results |

> ❌ **Don't guess the mode.** If the user gives a PR URL, use `-PRNumber`. If they paste an AzDO build link, extract the build ID. If they reference a specific Helix job, use `-HelixJob`.

## What the Script Does

### PR Analysis Mode (`-PRNumber`)
1. Discovers AzDO builds associated with the PR (from GitHub check status; for full build history, query AzDO builds on `refs/pull/{PR}/merge` branch)
2. Fetches Build Analysis for known issues
3. Gets failed jobs from Azure DevOps timeline
4. **Separates canceled jobs from failed jobs** (canceled may be dependency-canceled or timeout-canceled)
5. Extracts Helix work item failures from each failed job
6. Fetches console logs (with `-ShowLogs`)
7. Searches for known issues with "Known Build Error" label
8. Correlates failures with PR file changes
9. **Emits structured summary** — `[CI_ANALYSIS_SUMMARY]` JSON block with all key facts for the agent to reason over

> **After the script runs**, you (the agent) generate recommendations. The script collects data; you synthesize the advice. See [Generating Recommendations](#generating-recommendations) below.

### Build ID Mode (`-BuildId`)
1. Fetches the build timeline directly (skips PR discovery)
2. Performs steps 3–7 from PR Analysis Mode, but does **not** fetch Build Analysis known issues or correlate failures with PR file changes (those require a PR number). Still emits `[CI_ANALYSIS_SUMMARY]` JSON.

### Helix Job Mode (`-HelixJob` [and optional `-WorkItem`])
1. With `-HelixJob` alone: enumerates work items for the job and summarizes their status
2. With `-HelixJob` and `-WorkItem`: queries the specific work item for status and artifacts
3. Fetches console logs and file listings, displays detailed failure information

## Interpreting Results

**Known Issues section**: Failures matching existing GitHub issues - these are tracked and being investigated.

**Build Analysis check status**: The "Build Analysis" GitHub check is **green** only when *every* failure is matched to a known issue. If it's **red**, at least one failure is unaccounted for — do NOT claim "all failures are known issues" just because some known issues were found. You must verify each failing job is covered by a specific known issue before calling it safe to retry.

**Canceled/timed-out jobs**: Jobs canceled due to earlier stage failures or AzDO timeouts. Dependency-canceled jobs don't need investigation. **Timeout-canceled jobs may have all-passing Helix results** — the "failure" is just the AzDO job wrapper timing out, not actual test failures. To verify: use `hlx_status` on each Helix job in the timed-out build (include passed work items). If all work items passed, the build effectively passed.

> ❌ **Don't dismiss timed-out builds.** A build marked "failed" due to a 3-hour AzDO timeout can have 100% passing Helix work items. Check before concluding it failed.

**PR Change Correlation**: Files changed by PR appearing in failures - likely PR-related.

**Build errors**: Compilation failures need code fixes.

**Helix failures**: Test failures on distributed infrastructure.

**Local test failures**: Some repos (e.g., dotnet/sdk) run tests directly on build agents. These can also match known issues - search for the test name with the "Known Build Error" label.

**Per-failure details** (`failedJobDetails` in JSON): Each failed job includes `errorCategory`, `errorSnippet`, and `helixWorkItems`. Use these for per-job classification instead of applying a single `recommendationHint` to all failures.

Error categories: `test-failure`, `build-error`, `test-timeout`, `crash` (exit codes 139/134/-4), `tests-passed-reporter-failed` (all tests passed but reporter crashed — genuinely infrastructure), `unclassified` (investigate manually).

> ⚠️ **`crash` does NOT always mean tests failed.** Exit code -4 often means the Helix work item wrapper timed out *after* tests completed. Always check `testResults.xml` before concluding a crash is a real failure. See [Recovering Results from Crashed/Canceled Jobs](#recovering-results-from-crashedcanceled-jobs).

> ⚠️ **Be cautious labeling failures as "infrastructure."** Only conclude infrastructure with strong evidence: Build Analysis match, identical failure on target branch, or confirmed outage. Exception: `tests-passed-reporter-failed` is genuinely infrastructure.

> ❌ **Missing packages on flow PRs ≠ infrastructure.** Flow PRs can cause builds to request *different* packages. Check *which* package and *why* before assuming feed delay.

### Recovering Results from Crashed/Canceled Jobs

When an AzDO job is canceled (timeout) or Helix work items show `Crash` (exit code -4), the tests may have actually passed. Follow this procedure:

1. **Find the Helix job IDs** — Read the AzDO "Send to Helix" step log and search for lines containing `Sent Helix Job`. Extract the job GUIDs.

2. **Check Helix job status** — Get pass/fail summary for each job. Look at `failedCount` vs `passedCount`.

3. **For work items marked Crash/Failed** — Check if tests actually passed despite the crash. Try structured test results first (TRX parsing), then search for pass/fail counts in result files without downloading, then download as last resort:
   - Parse the XML: `total`, `passed`, `failed` attributes on the `<assembly>` element
   - If `failed=0` and `passed > 0`, the tests passed — the "crash" is the wrapper timing out after test completion

4. **Verdict**:
   - All work items passed or crash-with-passing-results → **Tests effectively passed.** The failure is infrastructure (wrapper timeout).
   - Some work items have `failed > 0` in testResults.xml → **Real test failures.** Investigate those specific tests.
   - No testResults.xml uploaded → Tests may not have run at all. Check console logs for errors.

> This pattern is common with long-running test suites (e.g., WasmBuildTests) where tests complete but the Helix work item wrapper exceeds its timeout during result upload or cleanup.

## Generating Recommendations

After the script outputs the `[CI_ANALYSIS_SUMMARY]` JSON block, **you** synthesize recommendations. Do not parrot the JSON — reason over it.

### Decision logic

Read `recommendationHint` as a starting point, then layer in context:

| Hint | Action |
|------|--------|
| `BUILD_SUCCESSFUL` | No failures. Confirm CI is green. |
| `KNOWN_ISSUES_DETECTED` | Known tracked issues found — but this does NOT mean all failures are covered. Check the Build Analysis check status: if it's red, some failures are unmatched. Only recommend retry for failures that specifically match a known issue; investigate the rest. |
| `LIKELY_PR_RELATED` | Failures correlate with PR changes. Lead with "fix these before retrying" and list `correlatedFiles`. |
| `POSSIBLY_TRANSIENT` | Failures could not be automatically classified — does NOT mean they are transient. Use `failedJobDetails` to investigate each failure individually. |
| `REVIEW_REQUIRED` | Could not auto-determine cause. Review failures manually. |
| `MERGE_CONFLICTS` | PR has merge conflicts — CI won't run. Tell the user to resolve conflicts. Offer to analyze a previous build by ID. |
| `NO_BUILDS` | No AzDO builds found (CI not triggered). Offer to check if CI needs to be triggered or analyze a previous build. |

Then layer in nuance the heuristic can't capture:

- **Mixed signals**: Some failures match known issues AND some correlate with PR changes → separate them. Known issues = safe to retry; correlated = fix first.
- **Canceled jobs with recoverable results**: If `canceledJobNames` is non-empty, mention that canceled jobs may have passing Helix results (see "Recovering Results from Crashed/Canceled Jobs").
- **Build still in progress**: If `lastBuildJobSummary.pending > 0`, note that more failures may appear.
- **Multiple builds**: If `builds` has >1 entry, `lastBuildJobSummary` reflects only the last build — use `totalFailedJobs` for the aggregate count.
- **BuildId mode**: `knownIssues` and `prCorrelation` won't be populated. Say "Build Analysis and PR correlation not available in BuildId mode."

### How to Retry

- **AzDO builds**: Comment `/azp run {pipeline-name}` on the PR (e.g., `/azp run dotnet-sdk-public`)
- **All pipelines**: Comment `/azp run` to retry all failing pipelines
- **Helix work items**: Cannot be individually retried — must re-run the entire AzDO build

### Tone and output format

Be direct. Lead with the most important finding. Structure your response as:
1. **Summary verdict** (1-2 sentences) — Is CI green? Failures PR-related? Known issues?
2. **Failure details** (2-4 bullets) — what failed, why, evidence
3. **Recommended actions** (numbered) — retry, fix, investigate. Include `/azp run` commands.

Synthesize from: JSON summary (structured facts) + human-readable output (details/logs) + Step 0 context (PR type, author intent).

## Analysis Workflow

### Step 0: Gather Context (before running anything)

Before running the script, read the PR to understand what you're analyzing. Context changes how you interpret every failure.

1. **Read PR metadata** — title, description, author, labels, linked issues
2. **Classify the PR type** — this determines your interpretation framework:

| PR Type | How to detect | Interpretation shift |
|---------|--------------|---------------------|
| **Code PR** | Human author, code changes | Failures likely relate to the changes |
| **Flow/Codeflow PR** | Author is `dotnet-maestro[bot]`, title mentions "Update dependencies" | Missing packages may be behavioral, not infrastructure (see anti-pattern below) |
| **Backport** | Title mentions "backport", targets a release branch | Failures may be branch-specific; check if test exists on target branch |
| **Merge PR** | Merging between branches (e.g., release → main) | Conflicts and merge artifacts cause failures, not the individual changes |
| **Dependency update** | Bumps package versions, global.json changes | Build failures often trace to the dependency, not the PR's own code |

3. **Check existing comments** — has someone already diagnosed the failures? Is there a retry pending?
4. **Note the changed files** — you'll use these to evaluate correlation after the script runs

> ❌ **Don't skip Step 0.** Running the script without PR context leads to misdiagnosis — especially for flow PRs where "package not found" looks like infrastructure but is actually a code issue.

### Step 1: Run the script

Run with `-ShowLogs` for detailed failure info.

### Step 2: Analyze results

1. **Check Build Analysis** — If the Build Analysis GitHub check is **green**, all failures matched known issues and it's safe to retry. If it's **red**, some failures are unaccounted for — you must identify which failing jobs are covered by known issues and which are not. For 3+ failures, use SQL tracking to avoid missed matches (see [references/sql-tracking.md](references/sql-tracking.md)).
2. **Correlate with PR changes** — Same files failing = likely PR-related
3. **Compare with baseline** — If a test passes on the target branch but fails on the PR, compare Helix binlogs. See [references/binlog-comparison.md](references/binlog-comparison.md) — **delegate binlog download/extraction to subagents** to avoid burning context on mechanical work.
4. **Check build progression** — If the PR has multiple builds (multiple pushes), check whether earlier builds passed. A failure that appeared after a specific push narrows the investigation to those commits. See [references/build-progression-analysis.md](references/build-progression-analysis.md). Present findings as facts, not fix recommendations.
5. **Interpret patterns** (but don't jump to conclusions):
   - Same error across many jobs → Real code issue
   - Build Analysis flags a known issue → That *specific failure* is safe to retry (but others may not be)
   - Failure is **not** in Build Analysis → Investigate further before assuming transient
   - Device failures, Docker pulls, network timeouts → *Could* be infrastructure, but verify against the target branch first
   - Test timeout but tests passed → Executor issue, not test failure
6. **Check for mismatch with user's question** — The script only reports builds for the current head SHA. If the user asks about a job, error, or cancellation that doesn't appear in the results, **ask** if they're referring to a prior build. Common triggers:
   - User mentions a canceled job but `canceledJobNames` is empty
   - User says "CI is failing" but the latest build is green
   - User references a specific job name not in the current results
   Offer to re-run with `-BuildId` if the user can provide the earlier build ID from AzDO.

### Step 3: Verify before claiming

Before stating a failure's cause, verify your claim:

- **"Infrastructure failure"** → Did Build Analysis flag it? Does the same test pass on the target branch? If neither, don't call it infrastructure.
- **"Transient/flaky"** → Has it failed before? Is there a known issue? A single non-reproducing failure isn't enough to call it flaky.
- **"PR-related"** → Do the changed files actually relate to the failing test? Correlation in the script output is heuristic, not proof.
- **"Safe to retry"** → Are ALL failures accounted for (known issues or infrastructure), or are you ignoring some? Check the Build Analysis check status — if it's red, not all failures are matched. Map each failing job to a specific known issue before concluding "safe to retry."
- **"Not related to this PR"** → Have you checked if the test passes on the target branch? Don't assume — verify.

## References

- **Helix artifacts & binlogs**: See [references/helix-artifacts.md](references/helix-artifacts.md)
- **Binlog comparison (passing vs failing)**: See [references/binlog-comparison.md](references/binlog-comparison.md)
- **Build progression (commit-to-build correlation)**: See [references/build-progression-analysis.md](references/build-progression-analysis.md)
- **Subagent delegation patterns**: See [references/delegation-patterns.md](references/delegation-patterns.md)
- **Azure CLI deep investigation**: See [references/azure-cli.md](references/azure-cli.md)
- **Manual investigation steps**: See [references/manual-investigation.md](references/manual-investigation.md)
- **SQL tracking for investigations**: See [references/sql-tracking.md](references/sql-tracking.md)
- **AzDO/Helix details**: See [references/azdo-helix-reference.md](references/azdo-helix-reference.md)

## Tips

1. Check if same test fails on the target branch before assuming transient
2. Look for `[ActiveIssue]` attributes for known skipped tests
3. Use `-SearchMihuBot` for semantic search of related issues
4. Use binlog analysis tools to search binlogs for Helix job IDs, build errors, and properties
5. `gh pr checks --json` valid fields: `bucket`, `completedAt`, `description`, `event`, `link`, `name`, `startedAt`, `state`, `workflow` — no `conclusion` field, `state` has `SUCCESS`/`FAILURE` directly
6. "Canceled" ≠ "Failed" — canceled jobs may have recoverable Helix results. Check artifacts before concluding results are lost.
