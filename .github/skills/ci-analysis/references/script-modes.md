# Script Modes and Parameters

## Key Parameters

| Parameter | Description |
|-----------|-------------|
| `-PRNumber` | GitHub PR number to analyze |
| `-BuildId` | Azure DevOps build ID |
| `-ShowLogs` | Fetch and display Helix console logs |
| `-Repository` | Target repo (default: dotnet/runtime) |
| `-MaxJobs` | Max failed jobs to show (default: 5) |
| `-SearchMihuBot` | Search MihuBot for related issues |

## Three Modes

The script operates in three distinct modes depending on what information you have:

| You have... | Use | What you get |
|-------------|-----|-------------|
| A GitHub PR number | `-PRNumber 12345` | Full analysis: all builds, failures, known issues, structured JSON summary |
| An AzDO build ID | `-BuildId 1276327` | Single build analysis: timeline, failures, Helix results |
| A Helix job ID (optionally a specific work item) | `-HelixJob "..." [-WorkItem "..."]` | Deep dive: list work items for the job, or with `-WorkItem`, focus on a single work item's console logs, artifacts, and test results |

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

For recommendation generation, see [recommendation-generation.md](recommendation-generation.md).

### Build ID Mode (`-BuildId`)
1. Fetches the build timeline directly (skips PR discovery)
2. Performs steps 3–7 from PR Analysis Mode, but does **not** fetch Build Analysis known issues or correlate failures with PR file changes (those require a PR number). Still emits `[CI_ANALYSIS_SUMMARY]` JSON.

### Helix Job Mode (`-HelixJob` [and optional `-WorkItem`])
1. With `-HelixJob` alone: enumerates work items for the job and summarizes their status
2. With `-HelixJob` and `-WorkItem`: queries the specific work item for status and artifacts
3. Fetches console logs and file listings, displays detailed failure information
