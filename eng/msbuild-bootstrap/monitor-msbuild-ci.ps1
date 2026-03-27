<#
.SYNOPSIS
    Monitors aspnetcore PR #66003 CI for MSB3030 errors.
    Checks the "Build Analysis" check run on the PR to determine if CI is done.
    If MSB3030 found: logs alert and stops. Otherwise: merges main and retriggers CI.

.NOTES
    Prerequisites:
    - Git configured with push access to ilonatommy/aspnetcore
    - Network access to dev.azure.com and api.github.com (public, no auth needed)
#>

param(
    [string]$RepoPath = "D:\aspnetcore-root\aspnetcore",
    [string]$Branch = "msbuild-bootstrap-investigation",
    [string]$LogFile = "D:\aspnetcore-root\msbuild-monitor.log",
    [string]$PRNumber = "66003",
    [string]$GHOwner = "dotnet",
    [string]$GHRepo = "aspnetcore",
    [string]$AzDOProjectId = "cbb18261-c48f-4abb-8651-8cdcb5474649",
    [int]$CIDefinitionId = 83,
    [string]$LastBuildFile = "D:\aspnetcore-root\msbuild-monitor-lastbuild.txt"
)

$ErrorActionPreference = "Stop"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $entry = "[$timestamp] [$Level] $Message"
    Write-Host $entry
    Add-Content -Path $LogFile -Value $entry
}

function Get-BuildAnalysisStatus {
    # Get the "Build Analysis" check run from the PR's latest commit
    $url = "https://api.github.com/repos/$GHOwner/$GHRepo/pulls/$PRNumber"
    try {
        $pr = Invoke-RestMethod -Uri $url -Method Get -ContentType "application/json" -Headers @{ "User-Agent" = "msbuild-monitor" }
        $sha = $pr.head.sha

        $checksUrl = "https://api.github.com/repos/$GHOwner/$GHRepo/commits/$sha/check-runs?per_page=100"
        $checks = Invoke-RestMethod -Uri $checksUrl -Method Get -ContentType "application/json" -Headers @{ "User-Agent" = "msbuild-monitor" }

        $buildAnalysis = $checks.check_runs | Where-Object { $_.name -eq "Build Analysis" } | Select-Object -First 1
        return $buildAnalysis
    } catch {
        Write-Log "Failed to query GitHub checks API: $_" "ERROR"
        return $null
    }
}

function Get-LatestCIBuild {
    $url = "https://dev.azure.com/dnceng-public/public/_apis/build/builds?branchName=refs/pull/$PRNumber/merge&definitions=$CIDefinitionId&queryOrder=queueTimeDescending&`$top=1&api-version=7.0"
    try {
        $response = Invoke-RestMethod -Uri $url -Method Get -ContentType "application/json"
        if ($response.count -gt 0) { return $response.value[0] }
    } catch {
        Write-Log "Failed to query AzDO builds API: $_" "ERROR"
    }
    return $null
}

function Get-BuildTimeline {
    param([int]$BuildId)
    $url = "https://dev.azure.com/dnceng-public/$AzDOProjectId/_apis/build/builds/$BuildId/timeline?api-version=7.0"
    try { return Invoke-RestMethod -Uri $url -Method Get -ContentType "application/json" }
    catch { Write-Log "Failed to get build timeline: $_" "ERROR"; return $null }
}

function Get-BuildLogs {
    param([int]$BuildId)
    $url = "https://dev.azure.com/dnceng-public/$AzDOProjectId/_apis/build/builds/$BuildId/logs?api-version=7.0"
    try { return Invoke-RestMethod -Uri $url -Method Get -ContentType "application/json" }
    catch { Write-Log "Failed to get build logs list: $_" "ERROR"; return $null }
}

function Search-LogForMSB3030 {
    param([int]$BuildId, [int]$LogId)
    $url = "https://dev.azure.com/dnceng-public/$AzDOProjectId/_apis/build/builds/$BuildId/logs/$($LogId)?api-version=7.0"
    try {
        $content = Invoke-RestMethod -Uri $url -Method Get
        $found = @()
        foreach ($line in ($content -split "`n")) {
            if ($line -match "MSB3030|MSB3026|Could not copy.*file|error.*copy.*file") {
                $found += $line.Trim()
            }
        }
        return $found
    } catch { return @() }
}

function Merge-MainAndPush {
    Push-Location $RepoPath
    try {
        Write-Log "Fetching upstream/main..."
        git fetch upstream main 2>&1 | Out-Null

        Write-Log "Checking out $Branch..."
        git checkout $Branch 2>&1 | Out-Null

        Write-Log "Merging upstream/main into $Branch..."
        $mergeOutput = git merge upstream/main --no-edit 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Log "Merge failed: $mergeOutput" "ERROR"
            git merge --abort 2>&1 | Out-Null
            return $false
        }

        $status = git --no-pager log HEAD..upstream/main --oneline 2>&1
        if ([string]::IsNullOrWhiteSpace($status)) {
            Write-Log "Already up to date. Creating empty commit to retrigger CI..."
            git commit --allow-empty -m "Retrigger CI for MSBuild bootstrap investigation $(Get-Date -Format 'yyyy-MM-dd HH:mm')" 2>&1 | Out-Null
        } else {
            Write-Log "Merged new commits from upstream/main"
        }

        Write-Log "Pushing to origin..."
        $pushOutput = git push origin $Branch 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Log "Push failed: $pushOutput" "ERROR"
            return $false
        }

        Write-Log "Push successful - CI will retrigger"
        return $true
    } finally {
        Pop-Location
    }
}

# === MAIN ===

Write-Log "========== MSBuild CI Monitor Run =========="

# Step 1: Check "Build Analysis" check run on the PR
Write-Log "Checking Build Analysis status for PR #$PRNumber..."
$buildAnalysis = Get-BuildAnalysisStatus

if (-not $buildAnalysis) {
    Write-Log "Could not find Build Analysis check run. Will retry next cycle." "WARN"
    exit 0
}

Write-Log "Build Analysis: status=$($buildAnalysis.status), conclusion=$($buildAnalysis.conclusion)"

if ($buildAnalysis.status -ne "completed") {
    Write-Log "Build Analysis not completed yet. CI still running. Skipping."
    exit 0
}

# Step 2: Get the latest aspnetcore-ci build to search its logs
$build = Get-LatestCIBuild
if (-not $build) {
    Write-Log "No aspnetcore-ci builds found" "WARN"
    Merge-MainAndPush
    exit 0
}

$buildId = $build.id
$buildResult = $build.result
$buildUrl = $build._links.web.href
Write-Log "Latest aspnetcore-ci build: #$buildId, result=$buildResult"
Write-Log "URL: $buildUrl"

# Step 3: Skip if we already processed this build
if (Test-Path $LastBuildFile) {
    $lastProcessed = (Get-Content $LastBuildFile -Raw).Trim()
    if ($lastProcessed -eq "$buildId") {
        Write-Log "Build #$buildId already processed. Skipping."
        exit 0
    }
}
Set-Content -Path $LastBuildFile -Value "$buildId"

# Step 4: If build succeeded, retrigger
if ($buildResult -eq "succeeded") {
    Write-Log "Build #$buildId succeeded (no MSB3030). Merging main and retriggering..."
    Merge-MainAndPush
    exit 0
}

# Step 5: Build failed - search logs for MSB3030
Write-Log "Build #$buildId failed/partially failed. Searching logs for MSB3030..."

$foundMSB3030 = $false
$msbErrors = @()

$timeline = Get-BuildTimeline -BuildId $buildId
if ($timeline) {
    $failedTasks = $timeline.records | Where-Object { $_.result -eq "failed" -and $_.log }
    Write-Log "Found $($failedTasks.Count) failed tasks"
    foreach ($task in $failedTasks) {
        $errors = Search-LogForMSB3030 -BuildId $buildId -LogId $task.log.id
        foreach ($err in $errors) {
            $foundMSB3030 = $true
            $msbErrors += "[$($task.name)] $err"
            Write-Log "  FOUND: $err" "ALERT"
        }
    }
}

$logsList = Get-BuildLogs -BuildId $buildId
if ($logsList) {
    $searchedIds = @($failedTasks | ForEach-Object { $_.log.id })
    $largeLogs = $logsList.value | Where-Object { $_.lineCount -gt 100 -and $_.id -notin $searchedIds }
    Write-Log "Also searching $($largeLogs.Count) large logs..."
    foreach ($log in $largeLogs) {
        $errors = Search-LogForMSB3030 -BuildId $buildId -LogId $log.id
        foreach ($err in $errors) {
            $foundMSB3030 = $true
            $msbErrors += "[log-$($log.id)] $err"
            Write-Log "  FOUND in log $($log.id): $err" "ALERT"
        }
    }
}

if ($foundMSB3030) {
    Write-Log "=== MSB3030/MSB3026 ERROR DETECTED! ===" "ALERT"
    Write-Log "Build: $buildUrl" "ALERT"
    foreach ($err in $msbErrors) { Write-Log "  $err" "ALERT" }

    # Post comment on the PR
    $errorList = ($msbErrors | ForEach-Object { "- ``$_``" }) -join "`n"
    $comment = @"
## :rotating_light: MSB3030/MSB3026 Error Detected (Automated)

**Build:** [#$buildId]($buildUrl)

**Errors found:**
$errorList

**Action required:** Collect binlog from this build and share with MSBuild team (@OvesN on [dotnet/msbuild#12927](https://github.com/dotnet/msbuild/issues/12927)).

_This comment was posted automatically by the CI monitor script._
"@
    try {
        Push-Location $RepoPath
        gh pr comment $PRNumber --repo "$GHOwner/$GHRepo" --body $comment
        Write-Log "Posted comment on PR #$PRNumber" "ALERT"
        Pop-Location
    } catch {
        Write-Log "Failed to post PR comment: $_" "ERROR"
    }

    # Disable the scheduled task so it stops running
    try {
        Disable-ScheduledTask -TaskName "MSBuild-CI-Monitor" -ErrorAction SilentlyContinue
        Write-Log "Disabled scheduled task MSBuild-CI-Monitor"
    } catch {
        schtasks /Change /TN "MSBuild-CI-Monitor" /DISABLE 2>&1 | Out-Null
        Write-Log "Disabled scheduled task via schtasks"
    }

    exit 0
} else {
    Write-Log "Build failed but NOT due to MSB3030. Unrelated failure."
    Write-Log "Merging main and retriggering CI..."
    Merge-MainAndPush
    exit 0
}
