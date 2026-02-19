# Manual Investigation Guide

If the script doesn't provide enough information, use these manual investigation steps.

## Table of Contents
- [Get Build Timeline](#get-build-timeline)
- [Find Helix Tasks](#find-helix-tasks)
- [Get Build Logs](#get-build-logs)
- [Query Helix APIs](#query-helix-apis)
- [Download Artifacts](#download-artifacts)
- [Analyze Binlogs](#analyze-binlogs)
- [Extract Environment Variables](#extract-environment-variables)

## Get Build Timeline

```powershell
$buildId = 1276327
$response = Invoke-RestMethod -Uri "https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_apis/build/builds/$buildId/timeline?api-version=7.0"
$failedJobs = $response.records | Where-Object { $_.type -eq "Job" -and $_.result -eq "failed" }
$failedJobs | Select-Object id, name, result | Format-Table
```

## Find Helix Tasks

```powershell
$jobId = "90274d9a-fbd8-54f8-6a7d-8dfc4e2f6f3f"  # From timeline
$helixTasks = $response.records | Where-Object { $_.parentId -eq $jobId -and $_.name -like "*Helix*" }
$helixTasks | Select-Object id, name, result, log | Format-Table
```

## Get Build Logs

```powershell
$logId = 565  # From task.log.id
$logContent = Invoke-RestMethod -Uri "https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_apis/build/builds/$buildId/logs/${logId}?api-version=7.0"
$logContent | Select-String -Pattern "error|FAIL" -Context 2,5
```

## Query Helix APIs

> 💡 **Prefer MCP tools when available** — they handle most Helix queries without manual curl commands. Use the APIs below only as fallback.

```bash
# Get job details
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID"

# List work items
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems"

# Get work item details
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems/WORK_ITEM_NAME"

# Get console log
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems/WORK_ITEM_NAME/console"
```

## Download Artifacts

```powershell
$workItem = Invoke-RestMethod -Uri "https://helix.dot.net/api/2019-06-17/jobs/$jobId/workitems/$workItemName"
$workItem.Files | ForEach-Object { Write-Host "$($_.FileName): $($_.Uri)" }
```

Common artifacts:
- `console.*.log` - Console output
- `*.binlog` - MSBuild binary logs
- `run-*.log` - XHarness/test runner logs
- Core dumps and crash reports

## Analyze Binlogs

Binlogs contain detailed MSBuild execution traces for diagnosing:
- AOT compilation failures
- Static web asset issues
- NuGet restore problems
- Target execution order issues

**Using MSBuild binlog MCP tools:**

Load the binlog, then search for errors/diagnostics or specific queries. The binlog MCP tools handle loading, searching, and extracting task details.

**Manual Analysis:**
Use [MSBuild Structured Log Viewer](https://msbuildlog.com/) or https://live.msbuildlog.com/

## Extract Environment Variables

```bash
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems/WORK_ITEM_NAME/console" | grep "DOTNET_"
```

Example output:
```
DOTNET_JitStress=1
DOTNET_TieredCompilation=0
DOTNET_GCStress=0xC
```

These are critical for reproducing failures locally.
