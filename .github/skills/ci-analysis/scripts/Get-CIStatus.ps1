<#
.SYNOPSIS
    Retrieves test failures from Azure DevOps builds and Helix test runs.

.DESCRIPTION
    This script queries Azure DevOps for failed jobs in a build and retrieves
    the corresponding Helix console logs to show detailed test failure information.
    It can also directly query a specific Helix job and work item.

.PARAMETER BuildId
    The Azure DevOps build ID to query.

.PARAMETER PRNumber
    The GitHub PR number to find the associated build.

.PARAMETER HelixJob
    The Helix job ID (GUID) to query directly.

.PARAMETER WorkItem
    The Helix work item name to query (requires -HelixJob).

.PARAMETER Repository
    The GitHub repository (owner/repo format). Default: dotnet/aspnetcore

.PARAMETER Organization
    The Azure DevOps organization. Default: dnceng-public

.PARAMETER Project
    The Azure DevOps project GUID. Default: cbb18261-c48f-4abb-8651-8cdcb5474649

.PARAMETER ShowLogs
    If specified, fetches and displays the Helix console logs for failed tests.

.PARAMETER MaxJobs
    Maximum number of failed jobs to process. Default: 5

.PARAMETER MaxFailureLines
    Maximum number of lines to capture per test failure. Default: 50

.PARAMETER TimeoutSec
    Timeout in seconds for API calls. Default: 30

.PARAMETER ContextLines
    Number of context lines to show before errors. Default: 0

.PARAMETER NoCache
    Bypass cache and fetch fresh data for all API calls.

.PARAMETER CacheTTLSeconds
    Cache lifetime in seconds. Default: 30

.PARAMETER ClearCache
    Clear all cached files and exit.

.PARAMETER ContinueOnError
    Continue processing remaining jobs if an API call fails, showing partial results.

.PARAMETER SearchMihuBot
    Search MihuBot's semantic database for related issues and discussions.
    Uses https://mihubot.xyz/mcp to find conceptually related issues across dotnet repositories.

.PARAMETER FindBinlogs
    Scan work items in a Helix job to find which ones contain MSBuild binlog files.
    Useful when the failed work item doesn't have binlogs (e.g., unit tests) but you need
    to find related build tests that do have binlogs for deeper analysis.

.EXAMPLE
    .\Get-CIStatus.ps1 -BuildId 1276327

.EXAMPLE
    .\Get-CIStatus.ps1 -PRNumber 123445 -ShowLogs

.EXAMPLE
    .\Get-CIStatus.ps1 -PRNumber 123445 -Repository dotnet/aspnetcore

.EXAMPLE
    .\Get-CIStatus.ps1 -HelixJob "4b24b2c2-ad5a-4c46-8a84-844be03b1d51" -WorkItem "iOS.Device.Aot.Test"

.EXAMPLE
    .\Get-CIStatus.ps1 -BuildId 1276327 -SearchMihuBot

.EXAMPLE
    .\Get-CIStatus.ps1 -HelixJob "4b24b2c2-ad5a-4c46-8a84-844be03b1d51" -FindBinlogs
    # Scans work items to find which ones contain MSBuild binlog files

.EXAMPLE
    .\Get-CIStatus.ps1 -ClearCache
#>

[CmdletBinding(DefaultParameterSetName = 'BuildId')]
param(
    [Parameter(ParameterSetName = 'BuildId', Mandatory = $true)]
    [int]$BuildId,

    [Parameter(ParameterSetName = 'PRNumber', Mandatory = $true)]
    [int]$PRNumber,

    [Parameter(ParameterSetName = 'HelixJob', Mandatory = $true)]
    [string]$HelixJob,

    [Parameter(ParameterSetName = 'HelixJob')]
    [string]$WorkItem,

    [Parameter(ParameterSetName = 'ClearCache', Mandatory = $true)]
    [switch]$ClearCache,

    [string]$Repository = "dotnet/aspnetcore",
    [string]$Organization = "dnceng-public",
    [string]$Project = "cbb18261-c48f-4abb-8651-8cdcb5474649",
    [switch]$ShowLogs,
    [int]$MaxJobs = 5,
    [int]$MaxFailureLines = 50,
    [int]$TimeoutSec = 30,
    [int]$ContextLines = 0,
    [switch]$NoCache,
    [int]$CacheTTLSeconds = 30,
    [switch]$ContinueOnError,
    [switch]$SearchMihuBot,
    [switch]$FindBinlogs
)

$ErrorActionPreference = "Stop"

#region Caching Functions

# Cross-platform temp directory detection
function Get-TempDirectory {
    # Try common environment variables in order of preference
    $tempPath = $env:TEMP
    if (-not $tempPath) { $tempPath = $env:TMP }
    if (-not $tempPath) { $tempPath = $env:TMPDIR }  # macOS
    if (-not $tempPath -and $IsLinux) { $tempPath = "/tmp" }
    if (-not $tempPath -and $IsMacOS) { $tempPath = "/tmp" }
    if (-not $tempPath) {
        # Fallback: use .cache in user's home directory
        $home = $env:HOME
        if (-not $home) { $home = $env:USERPROFILE }
        if ($home) {
            $tempPath = Join-Path $home ".cache"
            if (-not (Test-Path $tempPath)) {
                New-Item -ItemType Directory -Path $tempPath -Force | Out-Null
            }
        }
    }
    if (-not $tempPath) {
        throw "Could not determine temp directory. Set TEMP, TMP, or TMPDIR environment variable."
    }
    return $tempPath
}

$script:TempDir = Get-TempDirectory

# Handle -ClearCache parameter
if ($ClearCache) {
    $cacheDir = Join-Path $script:TempDir "ci-analysis-cache"
    if (Test-Path $cacheDir) {
        $files = Get-ChildItem -Path $cacheDir -File
        $count = $files.Count
        Remove-Item -Path $cacheDir -Recurse -Force
        Write-Host "Cleared $count cached files from $cacheDir" -ForegroundColor Green
    }
    else {
        Write-Host "Cache directory does not exist: $cacheDir" -ForegroundColor Yellow
    }
    exit 0
}

# Setup caching
$script:CacheDir = Join-Path $script:TempDir "ci-analysis-cache"
if (-not (Test-Path $script:CacheDir)) {
    New-Item -ItemType Directory -Path $script:CacheDir -Force | Out-Null
}

# Clean up expired cache files on startup (files older than 2x TTL)
function Clear-ExpiredCache {
    param([int]$TTLSeconds = $CacheTTLSeconds)

    $maxAge = $TTLSeconds * 2
    $cutoff = (Get-Date).AddSeconds(-$maxAge)

    Get-ChildItem -Path $script:CacheDir -File -ErrorAction SilentlyContinue | Where-Object {
        $_.LastWriteTime -lt $cutoff
    } | ForEach-Object {
        Write-Verbose "Removing expired cache file: $($_.Name)"
        try {
            Remove-Item $_.FullName -Force -ErrorAction Stop
        }
        catch {
            Write-Verbose "Failed to remove cache file '$($_.Name)': $($_.Exception.Message)"
        }
    }
}

# Run cache cleanup at startup (non-blocking)
if (-not $NoCache) {
    Clear-ExpiredCache -TTLSeconds $CacheTTLSeconds
}

function Get-UrlHash {
    param([string]$Url)
    
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString(
            $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($Url))
        ).Replace("-", "")
    }
    finally {
        $sha256.Dispose()
    }
}

function Get-CachedResponse {
    param(
        [string]$Url,
        [int]$TTLSeconds = $CacheTTLSeconds
    )

    if ($NoCache) { return $null }

    $hash = Get-UrlHash -Url $Url
    $cacheFile = Join-Path $script:CacheDir "$hash.json"

    if (Test-Path $cacheFile) {
        $cacheInfo = Get-Item $cacheFile
        $age = (Get-Date) - $cacheInfo.LastWriteTime

        if ($age.TotalSeconds -lt $TTLSeconds) {
            Write-Verbose "Cache hit for $Url (age: $([int]$age.TotalSeconds) sec)"
            return Get-Content $cacheFile -Raw
        }
        else {
            Write-Verbose "Cache expired for $Url"
        }
    }

    return $null
}

function Set-CachedResponse {
    param(
        [string]$Url,
        [string]$Content
    )

    if ($NoCache) { return }

    $hash = Get-UrlHash -Url $Url
    $cacheFile = Join-Path $script:CacheDir "$hash.json"
    
    # Use atomic write: write to temp file, then rename
    $tempFile = Join-Path $script:CacheDir "$hash.tmp.$([System.Guid]::NewGuid().ToString('N'))"
    try {
        $Content | Set-Content -LiteralPath $tempFile -Force
        Move-Item -LiteralPath $tempFile -Destination $cacheFile -Force
        Write-Verbose "Cached response for $Url"
    }
    catch {
        # Clean up temp file on failure
        if (Test-Path $tempFile) {
            Remove-Item -LiteralPath $tempFile -Force -ErrorAction SilentlyContinue
        }
        Write-Verbose "Failed to cache response: $_"
    }
}

function Invoke-CachedRestMethod {
    param(
        [string]$Uri,
        [int]$TimeoutSec = 30,
        [switch]$AsJson,
        [switch]$SkipCache,
        [switch]$SkipCacheWrite
    )

    # Check cache first (unless skipping)
    if (-not $SkipCache) {
        $cached = Get-CachedResponse -Url $Uri
        if ($cached) {
            if ($AsJson) {
                try {
                    return $cached | ConvertFrom-Json -ErrorAction Stop
                }
                catch {
                    Write-Verbose "Failed to parse cached response as JSON, treating as cache miss: $_"
                }
            }
            else {
                return $cached
            }
        }
    }

    # Make the actual request
    Write-Verbose "GET $Uri"
    $response = Invoke-RestMethod -Uri $Uri -Method Get -TimeoutSec $TimeoutSec

    # Cache the response (unless skipping write)
    if (-not $SkipCache -and -not $SkipCacheWrite) {
        if ($AsJson -or $response -is [PSCustomObject]) {
            $content = $response | ConvertTo-Json -Depth 100 -Compress
            Set-CachedResponse -Url $Uri -Content $content
        }
        else {
            Set-CachedResponse -Url $Uri -Content $response
        }
    }

    return $response
}

#endregion Caching Functions

#region Validation Functions

function Test-RepositoryFormat {
    param([string]$Repo)
    
    # Validate repository format to prevent command injection
    $repoPattern = '^[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+$'
    if ($Repo -notmatch $repoPattern) {
        throw "Invalid repository format '$Repo'. Expected 'owner/repo' (e.g., 'dotnet/aspnetcore')."
    }
    return $true
}

function Get-SafeSearchTerm {
    param([string]$Term)
    
    # Sanitize search term to avoid passing unsafe characters to gh CLI
    # Keep: alphanumeric, spaces, dots, hyphens, colons (for namespaces like System.Net),
    # and slashes (for paths). These are safe for GitHub search and common in .NET names.
    $safeTerm = $Term -replace '[^\w\s\-.:/]', ''
    return $safeTerm.Trim()
}

#endregion Validation Functions

#region Azure DevOps API Functions

function Get-AzDOBuildIdFromPR {
    param([int]$PR)

    # Check for gh CLI dependency
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw "GitHub CLI (gh) is required for PR lookup. Install from https://cli.github.com/ or use -BuildId instead."
    }

    # Validate repository format
    Test-RepositoryFormat -Repo $Repository | Out-Null

    Write-Host "Finding builds for PR #$PR in $Repository..." -ForegroundColor Cyan
    Write-Verbose "Running: gh pr checks $PR --repo $Repository"

    # Use gh cli to get the checks with splatted arguments
    $checksOutput = & gh pr checks $PR --repo $Repository 2>&1
    $ghExitCode = $LASTEXITCODE

    if ($ghExitCode -ne 0 -and -not ($checksOutput | Select-String -Pattern "buildId=")) {
        throw "Failed to fetch CI status for PR #$PR in $Repository - check PR number and permissions"
    }

    # Check if PR has merge conflicts (no CI runs when mergeable_state is dirty)
    $prMergeState = $null
    $prMergeStateOutput = & gh api "repos/$Repository/pulls/$PR" --jq '.mergeable_state' 2>$null
    $ghMergeStateExitCode = $LASTEXITCODE
    if ($ghMergeStateExitCode -eq 0 -and $prMergeStateOutput) {
        $prMergeState = $prMergeStateOutput.Trim()
    } else {
        Write-Verbose "Could not determine PR merge state (gh exit code $ghMergeStateExitCode)."
    }

    # Find ALL failing Azure DevOps builds
    $failingBuilds = @{}
    foreach ($line in $checksOutput) {
        if ($line -match 'fail.*buildId=(\d+)') {
            $buildId = $Matches[1]
            # Extract pipeline name (first column before 'fail')
            $pipelineName = ($line -split '\s+fail')[0].Trim()
            if (-not $failingBuilds.ContainsKey($buildId)) {
                $failingBuilds[$buildId] = $pipelineName
            }
        }
    }

    if ($failingBuilds.Count -eq 0) {
        # No failing builds - try to find any build
        $anyBuild = $checksOutput | Select-String -Pattern "buildId=(\d+)" | Select-Object -First 1
        if ($anyBuild) {
            $anyBuildMatch = [regex]::Match($anyBuild.ToString(), "buildId=(\d+)")
            if ($anyBuildMatch.Success) {
                $buildIdStr = $anyBuildMatch.Groups[1].Value
                $buildIdInt = 0
                if ([int]::TryParse($buildIdStr, [ref]$buildIdInt)) {
                    return @{ BuildIds = @($buildIdInt); Reason = $null; MergeState = $prMergeState }
                }
            }
        }
        if ($prMergeState -eq 'dirty') {
            Write-Host "`nPR #$PR has merge conflicts (mergeable_state: dirty)" -ForegroundColor Red
            Write-Host "CI will not run until conflicts are resolved." -ForegroundColor Yellow
            Write-Host "Resolve conflicts and push to trigger CI, or use -BuildId to analyze a previous build." -ForegroundColor Gray
            return @{ BuildIds = @(); Reason = "MERGE_CONFLICTS"; MergeState = $prMergeState }
        }
        Write-Host "`nNo CI build found for PR #$PR in $Repository" -ForegroundColor Red
        Write-Host "The CI pipeline has not been triggered yet." -ForegroundColor Yellow
        return @{ BuildIds = @(); Reason = "NO_BUILDS"; MergeState = $prMergeState }
    }

    # Return all unique failing build IDs
    $buildIds = $failingBuilds.Keys | ForEach-Object { [int]$_ } | Sort-Object -Unique

    if ($buildIds.Count -gt 1) {
        Write-Host "Found $($buildIds.Count) failing builds:" -ForegroundColor Yellow
        foreach ($id in $buildIds) {
            Write-Host "  - Build $id ($($failingBuilds[$id.ToString()]))" -ForegroundColor Gray
        }
    }

    return @{ BuildIds = $buildIds; Reason = $null; MergeState = $prMergeState }
}

function Get-BuildAnalysisKnownIssues {
    param([int]$PR)

    # Check for gh CLI dependency
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Verbose "GitHub CLI (gh) not available for Build Analysis check"
        return @()
    }

    Write-Verbose "Fetching Build Analysis check for PR #$PR..."

    try {
        # Get the head commit SHA for the PR
        $headSha = gh pr view $PR --repo $Repository --json headRefOid --jq '.headRefOid' 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Verbose "Failed to get PR head SHA: $headSha"
            return @()
        }

        # Validate headSha is a valid git SHA (40 hex characters)
        if ($headSha -notmatch '^[a-fA-F0-9]{40}$') {
            Write-Verbose "Invalid head SHA format: $headSha"
            return @()
        }

        # Get the Build Analysis check run
        $checkRuns = gh api "repos/$Repository/commits/$headSha/check-runs" --jq '.check_runs[] | select(.name == "Build Analysis") | .output' 2>&1
        if ($LASTEXITCODE -ne 0 -or -not $checkRuns) {
            Write-Verbose "No Build Analysis check found"
            return @()
        }

        $output = $checkRuns | ConvertFrom-Json -ErrorAction SilentlyContinue
        if (-not $output -or -not $output.text) {
            Write-Verbose "Build Analysis check has no output text"
            return @()
        }

        # Parse known issues from the output text
        # Format: <a href="https://github.com/dotnet/aspnetcore/issues/117164">Issue Title</a>
        $knownIssues = @()
        $issuePattern = '<a href="(https://github\.com/[^/]+/[^/]+/issues/(\d+))">([^<]+)</a>'
        $matches = [regex]::Matches($output.text, $issuePattern)

        foreach ($match in $matches) {
            $issueUrl = $match.Groups[1].Value
            $issueNumber = $match.Groups[2].Value
            $issueTitle = $match.Groups[3].Value

            # Avoid duplicates
            if (-not ($knownIssues | Where-Object { $_.Number -eq $issueNumber })) {
                $knownIssues += @{
                    Number = $issueNumber
                    Url = $issueUrl
                    Title = $issueTitle
                }
            }
        }

        if ($knownIssues.Count -gt 0) {
            Write-Host "`nBuild Analysis found $($knownIssues.Count) known issue(s):" -ForegroundColor Yellow
            foreach ($issue in $knownIssues) {
                Write-Host "  - #$($issue.Number): $($issue.Title)" -ForegroundColor Gray
                Write-Host "    $($issue.Url)" -ForegroundColor DarkGray
            }
        }

        return $knownIssues
    }
    catch {
        Write-Verbose "Error fetching Build Analysis: $_"
        return @()
    }
}

function Get-PRChangedFiles {
    param(
        [int]$PR,
        [int]$MaxFiles = 100
    )

    # Check for gh CLI dependency
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Verbose "GitHub CLI (gh) not available for PR file lookup"
        return @()
    }

    Write-Verbose "Fetching changed files for PR #$PR..."

    try {
        # Get the file count first to avoid fetching huge PRs
        $fileCount = gh pr view $PR --repo $Repository --json files --jq '.files | length' 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Verbose "Failed to get PR file count: $fileCount"
            return @()
        }

        $count = [int]$fileCount
        if ($count -gt $MaxFiles) {
            Write-Verbose "PR has $count files (exceeds limit of $MaxFiles) - skipping correlation"
            Write-Host "PR has $count changed files - skipping detailed correlation (limit: $MaxFiles)" -ForegroundColor Gray
            return @()
        }

        # Get the list of changed files
        $filesJson = gh pr view $PR --repo $Repository --json files --jq '.files[].path' 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Verbose "Failed to get PR files: $filesJson"
            return @()
        }

        $files = $filesJson -split "`n" | Where-Object { $_ }
        return $files
    }
    catch {
        Write-Verbose "Error fetching PR files: $_"
        return @()
    }
}

function Get-PRCorrelation {
    param(
        [array]$ChangedFiles,
        [array]$AllFailures
    )

    $result = @{ CorrelatedFiles = @(); TestFiles = @() }
    if ($ChangedFiles.Count -eq 0 -or $AllFailures.Count -eq 0) { return $result }

    $failureText = ($AllFailures | ForEach-Object {
        $_.TaskName
        $_.JobName
        $_.Errors -join "`n"
        $_.HelixLogs -join "`n"
        $_.FailedTests -join "`n"
    }) -join "`n"

    foreach ($file in $ChangedFiles) {
        $fileName = [System.IO.Path]::GetFileNameWithoutExtension($file)
        $fileNameWithExt = [System.IO.Path]::GetFileName($file)
        $baseTestName = $fileName -replace '\.[^.]+$', ''

        $isCorrelated = $false
        if ($failureText -match [regex]::Escape($fileName) -or
            $failureText -match [regex]::Escape($fileNameWithExt) -or
            $failureText -match [regex]::Escape($file) -or
            ($baseTestName -and $failureText -match [regex]::Escape($baseTestName))) {
            $isCorrelated = $true
        }

        if ($isCorrelated) {
            $isTestFile = $file -match '\.Tests?\.' -or $file -match '[/\\]tests?[/\\]' -or $file -match 'Test\.cs$' -or $file -match 'Tests\.cs$'
            if ($isTestFile) { $result.TestFiles += $file } else { $result.CorrelatedFiles += $file }
        }
    }

    $result.CorrelatedFiles = @($result.CorrelatedFiles | Select-Object -Unique)
    $result.TestFiles = @($result.TestFiles | Select-Object -Unique)
    return $result
}

function Show-PRCorrelationSummary {
    param(
        [array]$ChangedFiles,
        [array]$AllFailures
    )

    if ($ChangedFiles.Count -eq 0) {
        return
    }

    $correlation = Get-PRCorrelation -ChangedFiles $ChangedFiles -AllFailures $AllFailures
    $correlatedFiles = $correlation.CorrelatedFiles
    $testFiles = $correlation.TestFiles

    # Show results
    if ($correlatedFiles.Count -gt 0 -or $testFiles.Count -gt 0) {
        Write-Host "`n=== PR Change Correlation ===" -ForegroundColor Magenta

        if ($testFiles.Count -gt 0) {
            Write-Host "⚠️  Test files changed by this PR are failing:" -ForegroundColor Yellow
            $shown = 0
            foreach ($file in $testFiles) {
                if ($shown -ge 10) {
                    Write-Host "    ... and $($testFiles.Count - 10) more test files" -ForegroundColor Gray
                    break
                }
                Write-Host "    $file" -ForegroundColor Red
                $shown++
            }
        }

        if ($correlatedFiles.Count -gt 0) {
            Write-Host "⚠️  Files changed by this PR appear in failures:" -ForegroundColor Yellow
            $shown = 0
            foreach ($file in $correlatedFiles) {
                if ($shown -ge 10) {
                    Write-Host "    ... and $($correlatedFiles.Count - 10) more files" -ForegroundColor Gray
                    break
                }
                Write-Host "    $file" -ForegroundColor Red
                $shown++
            }
        }

        Write-Host "`nCorrelated files found — check JSON summary for details." -ForegroundColor Yellow
    }
}

function Get-AzDOBuildStatus {
    param([int]$Build)

    $url = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/${Build}?api-version=7.0"

    try {
        # First check cache to see if we have a completed status
        $cached = Get-CachedResponse -Url $url
        if ($cached) {
            $cachedData = $cached | ConvertFrom-Json
            # Only use cache if build was completed - in-progress status goes stale quickly
            if ($cachedData.status -eq "completed") {
                return @{
                    Status = $cachedData.status
                    Result = $cachedData.result
                    StartTime = $cachedData.startTime
                    FinishTime = $cachedData.finishTime
                }
            }
            Write-Verbose "Skipping cached in-progress build status"
        }

        # Fetch fresh status
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec -AsJson -SkipCache

        # Only cache if completed
        if ($response.status -eq "completed") {
            $content = $response | ConvertTo-Json -Depth 10 -Compress
            Set-CachedResponse -Url $url -Content $content
        }

        return @{
            Status = $response.status        # notStarted, inProgress, completed
            Result = $response.result        # succeeded, failed, canceled (only set when completed)
            StartTime = $response.startTime
            FinishTime = $response.finishTime
        }
    }
    catch {
        Write-Verbose "Failed to fetch build status: $_"
        return $null
    }
}

function Get-AzDOTimeline {
    param(
        [int]$Build,
        [switch]$BuildInProgress
    )

    $url = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/$Build/timeline?api-version=7.0"
    Write-Host "Fetching build timeline..." -ForegroundColor Cyan

    try {
        # Don't cache timeline for in-progress builds - it changes as jobs complete
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec -AsJson -SkipCacheWrite:$BuildInProgress
        return $response
    }
    catch {
        if ($ContinueOnError) {
            Write-Warning "Failed to fetch build timeline: $_"
            return $null
        }
        throw "Failed to fetch build timeline: $_"
    }
}

function Get-FailedJobs {
    param($Timeline)

    if ($null -eq $Timeline -or $null -eq $Timeline.records) {
        return @()
    }

    $failedJobs = $Timeline.records | Where-Object {
        $_.type -eq "Job" -and $_.result -eq "failed"
    }

    return $failedJobs
}

function Get-CanceledJobs {
    param($Timeline)

    if ($null -eq $Timeline -or $null -eq $Timeline.records) {
        return @()
    }

    $canceledJobs = $Timeline.records | Where-Object {
        $_.type -eq "Job" -and $_.result -eq "canceled"
    }

    return $canceledJobs
}

function Get-HelixJobInfo {
    param($Timeline, $JobId)

    if ($null -eq $Timeline -or $null -eq $Timeline.records) {
        return @()
    }

    # Find tasks in this job that mention Helix
    $helixTasks = $Timeline.records | Where-Object {
        $_.parentId -eq $JobId -and
        $_.name -like "*Helix*" -and
        $_.result -eq "failed"
    }

    return $helixTasks
}

function Get-BuildLog {
    param([int]$Build, [int]$LogId)

    $url = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/$Build/logs/${LogId}?api-version=7.0"

    try {
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec
        return $response
    }
    catch {
        Write-Warning "Failed to fetch log ${LogId}: $_"
        return $null
    }
}

#endregion Azure DevOps API Functions

#region Log Parsing Functions

function Extract-HelixUrls {
    param([string]$LogContent)

    $urls = @()

    # First, normalize the content by removing line breaks that might split URLs
    $normalizedContent = $LogContent -replace "`r`n", "" -replace "`n", ""

    # Match Helix console log URLs - workitem names can contain dots, dashes, and other chars
    $urlMatches = [regex]::Matches($normalizedContent, 'https://helix\.dot\.net/api/[^/]+/jobs/[a-f0-9-]+/workitems/[^/\s]+/console')
    foreach ($match in $urlMatches) {
        $urls += $match.Value
    }

    Write-Verbose "Found $($urls.Count) Helix URLs"
    return $urls | Select-Object -Unique
}

function Extract-TestFailures {
    param([string]$LogContent)

    $failures = @()

    # Match test failure patterns from MSBuild output
    $pattern = 'error\s*:\s*.*Test\s+(\S+)\s+has failed'
    $failureMatches = [regex]::Matches($LogContent, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

    foreach ($match in $failureMatches) {
        $failures += @{
            TestName = $match.Groups[1].Value
            FullMatch = $match.Value
        }
    }

    Write-Verbose "Found $($failures.Count) test failures"
    return $failures
}

function Extract-BuildErrors {
    param(
        [string]$LogContent,
        [int]$Context = 5
    )

    $errors = @()
    $lines = $LogContent -split "`n"

    # Patterns for common build errors - ordered from most specific to least specific
    $errorPatterns = @(
        'error\s+CS\d+:.*',                        # C# compiler errors
        'error\s+MSB\d+:.*',                       # MSBuild errors
        'error\s+NU\d+:.*',                        # NuGet errors
        '\.pcm: No such file or directory',        # Clang module cache
        'EXEC\s*:\s*error\s*:.*',                  # Exec task errors
        'fatal error:.*',                          # Fatal errors (clang, etc)
        ':\s*error:',                              # Clang/GCC errors (file.cpp:123: error:)
        'undefined reference to',                  # Linker errors
        'cannot find -l',                          # Linker missing library
        'collect2: error:',                        # GCC linker wrapper errors
        '##\[error\].*'                            # AzDO error annotations (last - catch-all)
    )

    $combinedPattern = ($errorPatterns -join '|')

    # Track if we only found MSBuild wrapper errors
    $foundRealErrors = $false
    $msbWrapperLines = @()

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $combinedPattern) {
            # Skip MSBuild wrapper "exited with code" if we find real errors
            if ($lines[$i] -match 'exited with code \d+') {
                $msbWrapperLines += $i
                continue
            }

            # Skip duplicate MSBuild errors (they often repeat)
            if ($lines[$i] -match 'error MSB3073.*exited with code') {
                continue
            }

            $foundRealErrors = $true

            # Clean up the line (remove timestamps, etc)
            $cleanLine = $lines[$i] -replace '^\d{4}-\d{2}-\d{2}T[\d:\.]+Z\s*', ''
            $cleanLine = $cleanLine -replace '##\[error\]', 'ERROR: '

            # Add context lines if requested
            if ($Context -gt 0) {
                $contextStart = [Math]::Max(0, $i - $Context)
                $contextLines = @()
                for ($j = $contextStart; $j -lt $i; $j++) {
                    $contextLines += "  " + $lines[$j].Trim()
                }
                if ($contextLines.Count -gt 0) {
                    $errors += ($contextLines -join "`n")
                }
            }

            $errors += $cleanLine.Trim()
        }
    }

    # If we only found MSBuild wrapper errors, show context around them
    if (-not $foundRealErrors -and $msbWrapperLines.Count -gt 0) {
        $wrapperLine = $msbWrapperLines[0]
        # Look for real errors in the 50 lines before the wrapper error
        $searchStart = [Math]::Max(0, $wrapperLine - 50)
        for ($i = $searchStart; $i -lt $wrapperLine; $i++) {
            $line = $lines[$i]
            # Look for C++/clang/gcc style errors
            if ($line -match ':\s*error:' -or $line -match 'fatal error:' -or $line -match 'undefined reference') {
                $cleanLine = $line -replace '^\d{4}-\d{2}-\d{2}T[\d:\.]+Z\s*', ''
                $errors += $cleanLine.Trim()
            }
        }
    }

    return $errors | Select-Object -First 20 | Select-Object -Unique
}

function Extract-HelixLogUrls {
    param([string]$LogContent)

    $urls = @()

    # Match Helix console log URLs from log content
    # Pattern: https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems/{workItemName}/console
    $pattern = 'https://helix\.dot\.net/api/[^/]+/jobs/([a-f0-9-]+)/workitems/([^/\s]+)/console'
    $urlMatches = [regex]::Matches($LogContent, $pattern)

    foreach ($match in $urlMatches) {
        $urls += @{
            Url = $match.Value
            JobId = $match.Groups[1].Value
            WorkItem = $match.Groups[2].Value
        }
    }

    # Deduplicate by URL
    $uniqueUrls = @{}
    foreach ($url in $urls) {
        if (-not $uniqueUrls.ContainsKey($url.Url)) {
            $uniqueUrls[$url.Url] = $url
        }
    }

    return $uniqueUrls.Values
}

#endregion Log Parsing Functions

#region Known Issues Search

function Search-MihuBotIssues {
    param(
        [string[]]$SearchTerms,
        [string]$ExtraContext = "",
        [string]$Repository = "dotnet/aspnetcore",
        [bool]$IncludeOpen = $true,
        [bool]$IncludeClosed = $true,
        [int]$TimeoutSec = 30
    )

    $results = @()

    if (-not $SearchTerms -or $SearchTerms.Count -eq 0) {
        return $results
    }

    try {
        # MihuBot MCP endpoint - call as JSON-RPC style request
        $mcpUrl = "https://mihubot.xyz/mcp"

        # Build the request payload matching the MCP tool schema
        $payload = @{
            jsonrpc = "2.0"
            method = "tools/call"
            id = [guid]::NewGuid().ToString()
            params = @{
                name = "search_dotnet_repos"
                arguments = @{
                    repository = $Repository
                    searchTerms = $SearchTerms
                    extraSearchContext = $ExtraContext
                    includeOpen = $IncludeOpen
                    includeClosed = $IncludeClosed
                    includeIssues = $true
                    includePullRequests = $true
                    includeComments = $false
                }
            }
        } | ConvertTo-Json -Depth 10

        Write-Verbose "Calling MihuBot MCP endpoint with terms: $($SearchTerms -join ', ')"

        $response = Invoke-RestMethod -Uri $mcpUrl -Method Post -Body $payload -ContentType "application/json" -TimeoutSec $TimeoutSec

        # Parse MCP response
        if ($response.result -and $response.result.content) {
            foreach ($content in $response.result.content) {
                if ($content.type -eq "text" -and $content.text) {
                    $issueData = $content.text | ConvertFrom-Json -ErrorAction SilentlyContinue
                    if ($issueData) {
                        foreach ($issue in $issueData) {
                            $results += @{
                                Number = $issue.Number
                                Title = $issue.Title
                                Url = $issue.Url
                                Repository = $issue.Repository
                                State = $issue.State
                                Source = "MihuBot"
                            }
                        }
                    }
                }
            }
        }

        # Deduplicate by issue number and repo
        $unique = @{}
        foreach ($issue in $results) {
            $key = "$($issue.Repository)#$($issue.Number)"
            if (-not $unique.ContainsKey($key)) {
                $unique[$key] = $issue
            }
        }

        return $unique.Values | Select-Object -First 5
    }
    catch {
        Write-Verbose "MihuBot search failed: $_"
        return @()
    }
}

function Search-KnownIssues {
    param(
        [string]$TestName,
        [string]$ErrorMessage,
        [string]$Repository = "dotnet/aspnetcore"
    )

    # Search for known issues using the "Known Build Error" label
    # This label is used by Build Analysis across dotnet repositories

    $knownIssues = @()

    # Check if gh CLI is available
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Verbose "GitHub CLI not available for searching known issues"
        return $knownIssues
    }

    try {
        # Extract search terms from test name and error message
        $searchTerms = @()

        # First priority: Look for [FAIL] test names in the error message
        # Pattern: "TestName [FAIL]" - the test name comes BEFORE [FAIL]
        if ($ErrorMessage -match '(\S+)\s+\[FAIL\]') {
            $failedTest = $Matches[1]
            # Extract just the method name (after last .)
            if ($failedTest -match '\.([^.]+)$') {
                $searchTerms += $Matches[1]
            }
            # Also add the full test name
            $searchTerms += $failedTest
        }

        # Second priority: Extract test class/method from stack traces
        if ($ErrorMessage -match 'at\s+(\w+\.\w+)\(' -and $searchTerms.Count -eq 0) {
            $searchTerms += $Matches[1]
        }

        if ($TestName) {
            # Try to get the test method name from the work item
            if ($TestName -match '\.([^.]+)$') {
                $methodName = $Matches[1]
                # Only add if it looks like a test name (not just "Tests")
                if ($methodName -ne "Tests" -and $methodName.Length -gt 5) {
                    $searchTerms += $methodName
                }
            }
            # Also try the full test name if it's not too long and looks specific
            if ($TestName.Length -lt 100 -and $TestName -notmatch '^System\.\w+\.Tests$') {
                $searchTerms += $TestName
            }
        }

        # Third priority: Extract specific exception patterns (but not generic TimeoutException)
        if ($ErrorMessage -and $searchTerms.Count -eq 0) {
            # Look for specific exception types
            if ($ErrorMessage -match '(System\.(?:InvalidOperation|ArgumentNull|Format)\w*Exception)') {
                $searchTerms += $Matches[1]
            }
        }

        # Deduplicate and limit search terms
        $searchTerms = $searchTerms | Select-Object -Unique | Select-Object -First 3

        foreach ($term in $searchTerms) {
            if (-not $term) { continue }

            # Sanitize the search term to avoid passing unsafe characters to gh CLI
            $safeTerm = Get-SafeSearchTerm -Term $term
            if (-not $safeTerm) { continue }

            Write-Verbose "Searching for known issues with term: $safeTerm"

            # Search for open issues with the "Known Build Error" label
            $results = & gh issue list `
                --repo $Repository `
                --label "Known Build Error" `
                --state open `
                --search $safeTerm `
                --limit 3 `
                --json number,title,url 2>$null | ConvertFrom-Json

            if ($results) {
                foreach ($issue in $results) {
                    # Check if the title actually contains our search term (avoid false positives)
                    if ($issue.title -match [regex]::Escape($safeTerm)) {
                        $knownIssues += @{
                            Number = $issue.number
                            Title = $issue.title
                            Url = $issue.url
                            SearchTerm = $safeTerm
                        }
                    }
                }
            }

            # If we found issues, stop searching
            if ($knownIssues.Count -gt 0) {
                break
            }
        }

        # Deduplicate by issue number
        $unique = @{}
        foreach ($issue in $knownIssues) {
            if (-not $unique.ContainsKey($issue.Number)) {
                $unique[$issue.Number] = $issue
            }
        }

        return $unique.Values
    }
    catch {
        Write-Verbose "Failed to search for known issues: $_"
        return @()
    }
}

function Show-KnownIssues {
    param(
        [string]$TestName = "",
        [string]$ErrorMessage = "",
        [string]$Repository = $script:Repository,
        [switch]$IncludeMihuBot
    )

    # Search for known issues if we have a test name or error
    if ($TestName -or $ErrorMessage) {
        $knownIssues = Search-KnownIssues -TestName $TestName -ErrorMessage $ErrorMessage -Repository $Repository
        if ($knownIssues -and $knownIssues.Count -gt 0) {
            Write-Host "`n  Known Issues:" -ForegroundColor Magenta
            foreach ($issue in $knownIssues) {
                Write-Host "    #$($issue.Number): $($issue.Title)" -ForegroundColor Magenta
                Write-Host "    $($issue.Url)" -ForegroundColor Gray
            }
        }

        # Search MihuBot for related issues/discussions
        if ($IncludeMihuBot) {
            $searchTerms = @()

            # Extract meaningful search terms
            if ($ErrorMessage -match '(\S+)\s+\[FAIL\]') {
                $failedTest = $Matches[1]
                if ($failedTest -match '\.([^.]+)$') {
                    $searchTerms += $Matches[1]
                }
            }

            if ($TestName -and $TestName -match '\.([^.]+)$') {
                $methodName = $Matches[1]
                if ($methodName -ne "Tests" -and $methodName.Length -gt 5) {
                    $searchTerms += $methodName
                }
            }

            # Add test name as context
            if ($TestName) {
                $searchTerms += $TestName
            }

            $searchTerms = $searchTerms | Select-Object -Unique | Select-Object -First 3

            if ($searchTerms.Count -gt 0) {
                $mihuBotResults = Search-MihuBotIssues -SearchTerms $searchTerms -Repository $Repository -ExtraContext "test failure $TestName"
                if ($mihuBotResults -and $mihuBotResults.Count -gt 0) {
                    # Filter out issues already shown from Known Build Error search
                    $knownNumbers = @()
                    if ($knownIssues) {
                        $knownNumbers = $knownIssues | ForEach-Object { $_.Number }
                    }
                    $newResults = $mihuBotResults | Where-Object { $_.Number -notin $knownNumbers }

                    if ($newResults -and @($newResults).Count -gt 0) {
                        Write-Host "`n  Related Issues (MihuBot):" -ForegroundColor Blue
                        foreach ($issue in $newResults) {
                            $stateIcon = if ($issue.State -eq "open") { "[open]" } else { "[closed]" }
                            Write-Host "    #$($issue.Number): $($issue.Title) $stateIcon" -ForegroundColor Blue
                            Write-Host "    $($issue.Url)" -ForegroundColor Gray
                        }
                    }
                }
            }
        }
    }
}

#endregion Known Issues Search

#region Test Results Functions

function Get-AzDOTestResults {
    param(
        [string]$RunId,
        [string]$Org = "https://dev.azure.com/$Organization"
    )

    # Check if az devops CLI is available
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Write-Verbose "Azure CLI not available for fetching test results"
        return $null
    }

    try {
        Write-Verbose "Fetching test results for run $RunId via az devops CLI..."
        $results = az devops invoke `
            --org $Org `
            --area test `
            --resource Results `
            --route-parameters project=$Project runId=$RunId `
            --api-version 7.0 `
            --query "value[?outcome=='Failed'].{name:testCaseTitle, outcome:outcome, error:errorMessage}" `
            -o json 2>$null | ConvertFrom-Json

        return $results
    }
    catch {
        Write-Verbose "Failed to fetch test results via az devops: $_"
        return $null
    }
}

function Extract-TestRunUrls {
    param([string]$LogContent)

    $testRuns = @()

    # Match Azure DevOps Test Run URLs
    # Pattern: Published Test Run : https://dev.azure.com/dnceng-public/public/_TestManagement/Runs?runId=35626550&_a=runCharts
    $pattern = 'Published Test Run\s*:\s*(https://dev\.azure\.com/[^/]+/[^/]+/_TestManagement/Runs\?runId=(\d+)[^\s]*)'
    $matches = [regex]::Matches($LogContent, $pattern)

    foreach ($match in $matches) {
        $testRuns += @{
            Url = $match.Groups[1].Value
            RunId = $match.Groups[2].Value
        }
    }

    Write-Verbose "Found $($testRuns.Count) test run URLs"
    return $testRuns
}

function Get-LocalTestFailures {
    param(
        [object]$Timeline,
        [int]$BuildId
    )

    $localFailures = @()

    # Find failed test tasks (non-Helix)
    # Look for tasks with "Test" in name that have issues but no Helix URLs
    $testTasks = $Timeline.records | Where-Object {
        ($_.name -match 'Test|xUnit' -or $_.type -eq 'Task') -and
        $_.issues -and
        $_.issues.Count -gt 0
    }

    foreach ($task in $testTasks) {
        # Check if this task has test failures (XUnit errors)
        $testErrors = $task.issues | Where-Object {
            $_.message -match 'Tests failed:' -or
            $_.message -match 'error\s*:.*Test.*failed'
        }

        if ($testErrors.Count -gt 0) {
            # This is a local test failure - find the parent job for URL construction
            $parentJob = $Timeline.records | Where-Object { $_.id -eq $task.parentId -and $_.type -eq "Job" } | Select-Object -First 1

            $failure = @{
                TaskName = $task.name
                TaskId = $task.id
                ParentJobId = if ($parentJob) { $parentJob.id } else { $task.parentId }
                LogId = if ($task.log) { $task.log.id } else { $null }
                Issues = $testErrors
                TestRunUrls = @()
            }

            # Try to get test run URLs from the publish task
            $publishTask = $Timeline.records | Where-Object {
                $_.parentId -eq $task.parentId -and
                $_.name -match 'Publish.*Test.*Results' -and
                $_.log
            } | Select-Object -First 1

            if ($publishTask -and $publishTask.log) {
                $logContent = Get-BuildLog -Build $BuildId -LogId $publishTask.log.id
                if ($logContent) {
                    $testRunUrls = Extract-TestRunUrls -LogContent $logContent
                    $failure.TestRunUrls = $testRunUrls
                }
            }

            $localFailures += $failure
        }
    }

    return $localFailures
}

#endregion Test Results Functions

#region Helix API Functions

function Get-HelixJobDetails {
    param([string]$JobId)

    $url = "https://helix.dot.net/api/2019-06-17/jobs/$JobId"

    try {
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec -AsJson
        return $response
    }
    catch {
        Write-Warning "Failed to fetch Helix job ${JobId}: $_"
        return $null
    }
}

function Get-HelixWorkItems {
    param([string]$JobId)

    $url = "https://helix.dot.net/api/2019-06-17/jobs/$JobId/workitems"

    try {
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec -AsJson
        return $response
    }
    catch {
        Write-Warning "Failed to fetch work items for job ${JobId}: $_"
        return $null
    }
}

function Get-HelixWorkItemFiles {
    <#
    .SYNOPSIS
        Fetches work item files via the ListFiles endpoint which returns direct blob storage URIs.
    .DESCRIPTION
        Workaround for https://github.com/dotnet/dnceng/issues/6072:
        The Details endpoint returns incorrect permalink URIs for files in subdirectories
        and rejects unicode characters in filenames. The ListFiles endpoint returns direct
        blob storage URIs that always work, regardless of subdirectory depth or unicode.
    #>
    param([string]$JobId, [string]$WorkItemName)

    $encodedWorkItem = [uri]::EscapeDataString($WorkItemName)
    $url = "https://helix.dot.net/api/2019-06-17/jobs/$JobId/workitems/$encodedWorkItem/files"

    try {
        $files = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec -AsJson
        return $files
    }
    catch {
        Write-Warning "Failed to fetch files for work item ${WorkItemName}: $_"
        return $null
    }
}

function Get-HelixWorkItemDetails {
    param([string]$JobId, [string]$WorkItemName)

    $encodedWorkItem = [uri]::EscapeDataString($WorkItemName)
    $url = "https://helix.dot.net/api/2019-06-17/jobs/$JobId/workitems/$encodedWorkItem"

    try {
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec -AsJson

        # Replace Files from the Details endpoint with results from ListFiles.
        # The Details endpoint has broken URIs for subdirectory and unicode filenames
        # (https://github.com/dotnet/dnceng/issues/6072). ListFiles returns direct
        # blob storage URIs that always work.
        $listFiles = Get-HelixWorkItemFiles -JobId $JobId -WorkItemName $WorkItemName
        if ($null -ne $listFiles) {
            $response.Files = @($listFiles | ForEach-Object {
                [PSCustomObject]@{
                    FileName = $_.Name
                    Uri = $_.Link
                }
            })
        }

        return $response
    }
    catch {
        Write-Warning "Failed to fetch work item ${WorkItemName}: $_"
        return $null
    }
}

function Get-HelixConsoleLog {
    param([string]$Url)

    try {
        $response = Invoke-CachedRestMethod -Uri $Url -TimeoutSec $TimeoutSec
        return $response
    }
    catch {
        Write-Warning "Failed to fetch Helix log from ${Url}: $_"
        return $null
    }
}

function Find-WorkItemsWithBinlogs {
    <#
    .SYNOPSIS
        Scans work items in a Helix job to find which ones contain binlog files.
    .DESCRIPTION
        Not all work items produce binlogs - only build/publish tests do.
        This function helps locate work items that have binlogs for deeper analysis.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$JobId,
        [int]$MaxItems = 30,
        [switch]$IncludeDetails
    )

    $workItems = Get-HelixWorkItems -JobId $JobId
    if (-not $workItems) {
        Write-Warning "No work items found for job $JobId"
        return @()
    }

    Write-Host "Scanning up to $MaxItems work items for binlogs..." -ForegroundColor Gray

    $results = @()
    $scanned = 0

    foreach ($wi in $workItems | Select-Object -First $MaxItems) {
        $scanned++
        $details = Get-HelixWorkItemDetails -JobId $JobId -WorkItemName $wi.Name
        if ($details -and $details.Files) {
            $binlogs = @($details.Files | Where-Object { $_.FileName -like "*.binlog" })
            if ($binlogs.Count -gt 0) {
                $result = @{
                    Name = $wi.Name
                    BinlogCount = $binlogs.Count
                    Binlogs = $binlogs | ForEach-Object { $_.FileName }
                    ExitCode = $details.ExitCode
                    State = $details.State
                }
                if ($IncludeDetails) {
                    $result.BinlogUris = $binlogs | ForEach-Object { $_.Uri }
                }
                $results += $result
            }
        }

        # Progress indicator every 10 items
        if ($scanned % 10 -eq 0) {
            Write-Host "  Scanned $scanned/$MaxItems..." -ForegroundColor DarkGray
        }
    }

    return $results
}

#endregion Helix API Functions

#region Output Formatting

function Format-TestFailure {
    param(
        [string]$LogContent,
        [int]$MaxLines = $MaxFailureLines,
        [int]$MaxFailures = 3
    )

    $lines = $LogContent -split "`n"
    $allFailures = @()
    $currentFailure = @()
    $inFailure = $false
    $emptyLineCount = 0
    $failureCount = 0

    # Expanded failure detection patterns
    # CAUTION: These trigger "failure block" capture. Overly broad patterns (e.g. \w+Error:)
    # will grab Python harness/reporter noise and swamp the real test failure.
    $failureStartPatterns = @(
        '\[FAIL\]',
        'Assert\.\w+\(\)\s+Failure',
        'Expected:.*but was:',
        'BUG:',
        'FAILED\s*$',
        'END EXECUTION - FAILED',
        'System\.\w+Exception:',
        'Timed Out \(timeout'
    )
    $combinedPattern = ($failureStartPatterns -join '|')

    foreach ($line in $lines) {
        # Check for new failure start
        if ($line -match $combinedPattern) {
            # Save previous failure if exists
            if ($currentFailure.Count -gt 0) {
                $allFailures += ($currentFailure -join "`n")
                $failureCount++
                if ($failureCount -ge $MaxFailures) {
                    break
                }
            }
            # Start new failure
            $currentFailure = @($line)
            $inFailure = $true
            $emptyLineCount = 0
            continue
        }

        if ($inFailure) {
            $currentFailure += $line

            # Track consecutive empty lines to detect end of stack trace
            if ($line -match '^\s*$') {
                $emptyLineCount++
            }
            else {
                $emptyLineCount = 0
            }

            # Stop this failure after stack trace ends (2+ consecutive empty lines) or max lines reached
            if ($emptyLineCount -ge 2 -or $currentFailure.Count -ge $MaxLines) {
                $allFailures += ($currentFailure -join "`n")
                $currentFailure = @()
                $inFailure = $false
                $failureCount++
                if ($failureCount -ge $MaxFailures) {
                    break
                }
            }
        }
    }

    # Don't forget last failure
    if ($currentFailure.Count -gt 0 -and $failureCount -lt $MaxFailures) {
        $allFailures += ($currentFailure -join "`n")
    }

    if ($allFailures.Count -eq 0) {
        return $null
    }

    $result = $allFailures -join "`n`n--- Next Failure ---`n`n"

    if ($failureCount -ge $MaxFailures) {
        $result += "`n`n... (more failures exist, showing first $MaxFailures)"
    }

    return $result
}

# Helper to display test results from a test run
function Show-TestRunResults {
    param(
        [object[]]$TestRunUrls,
        [string]$Org = "https://dev.azure.com/$Organization"
    )

    if (-not $TestRunUrls -or $TestRunUrls.Count -eq 0) { return }

    Write-Host "`n  Test Results:" -ForegroundColor Yellow
    foreach ($testRun in $TestRunUrls) {
        Write-Host "    Run $($testRun.RunId): $($testRun.Url)" -ForegroundColor Gray

        $testResults = Get-AzDOTestResults -RunId $testRun.RunId -Org $Org
        if ($testResults -and $testResults.Count -gt 0) {
            Write-Host "`n    Failed tests ($($testResults.Count)):" -ForegroundColor Red
            foreach ($result in $testResults | Select-Object -First 10) {
                Write-Host "      - $($result.name)" -ForegroundColor White
            }
            if ($testResults.Count -gt 10) {
                Write-Host "      ... and $($testResults.Count - 10) more" -ForegroundColor Gray
            }
        }
    }
}

#endregion Output Formatting

#region Main Execution

# Main execution
try {
    # Handle direct Helix job query
    if ($PSCmdlet.ParameterSetName -eq 'HelixJob') {
        Write-Host "`n=== Helix Job $HelixJob ===" -ForegroundColor Yellow
        Write-Host "URL: https://helix.dot.net/api/jobs/$HelixJob" -ForegroundColor Gray

        # Get job details
        $jobDetails = Get-HelixJobDetails -JobId $HelixJob
        if ($jobDetails) {
            Write-Host "`nQueue: $($jobDetails.QueueId)" -ForegroundColor Cyan
            Write-Host "Source: $($jobDetails.Source)" -ForegroundColor Cyan
        }

        if ($WorkItem) {
            # Query specific work item
            Write-Host "`n--- Work Item: $WorkItem ---" -ForegroundColor Cyan

            $workItemDetails = Get-HelixWorkItemDetails -JobId $HelixJob -WorkItemName $WorkItem
            if ($workItemDetails) {
                Write-Host "  State: $($workItemDetails.State)" -ForegroundColor $(if ($workItemDetails.State -eq 'Passed') { 'Green' } else { 'Red' })
                Write-Host "  Exit Code: $($workItemDetails.ExitCode)" -ForegroundColor White
                Write-Host "  Machine: $($workItemDetails.MachineName)" -ForegroundColor Gray
                Write-Host "  Duration: $($workItemDetails.Duration)" -ForegroundColor Gray

                # Show artifacts with binlogs highlighted
                if ($workItemDetails.Files -and $workItemDetails.Files.Count -gt 0) {
                    Write-Host "`n  Artifacts:" -ForegroundColor Yellow
                    $binlogs = $workItemDetails.Files | Where-Object { $_.FileName -like "*.binlog" }
                    $otherFiles = $workItemDetails.Files | Where-Object { $_.FileName -notlike "*.binlog" }

                    # Show binlogs first with special formatting
                    foreach ($file in $binlogs | Select-Object -Unique FileName, Uri) {
                        Write-Host "    📋 $($file.FileName): $($file.Uri)" -ForegroundColor Cyan
                    }
                    if ($binlogs.Count -gt 0) {
                        Write-Host "    (Tip: Use MSBuild MCP server or https://live.msbuildlog.com/ to analyze binlogs)" -ForegroundColor DarkGray
                    }

                    # Show other files
                    foreach ($file in $otherFiles | Select-Object -Unique FileName, Uri | Select-Object -First 10) {
                        Write-Host "    $($file.FileName): $($file.Uri)" -ForegroundColor Gray
                    }
                }

                # Fetch console log
                $consoleUrl = "https://helix.dot.net/api/2019-06-17/jobs/$HelixJob/workitems/$WorkItem/console"
                Write-Host "`n  Console Log: $consoleUrl" -ForegroundColor Yellow

                $consoleLog = Get-HelixConsoleLog -Url $consoleUrl
                if ($consoleLog) {
                    $failureInfo = Format-TestFailure -LogContent $consoleLog
                    if ($failureInfo) {
                        Write-Host $failureInfo -ForegroundColor White

                        # Search for known issues
                        Show-KnownIssues -TestName $WorkItem -ErrorMessage $failureInfo -IncludeMihuBot:$SearchMihuBot
                    }
                    else {
                        # Show last 50 lines if no failure pattern detected
                        $lines = $consoleLog -split "`n"
                        $lastLines = $lines | Select-Object -Last 50
                        Write-Host ($lastLines -join "`n") -ForegroundColor White
                    }
                }
            }
        }
        else {
            # List all work items in the job
            Write-Host "`nWork Items:" -ForegroundColor Yellow
            $workItems = Get-HelixWorkItems -JobId $HelixJob
            if ($workItems) {
                Write-Host "  Total: $($workItems.Count)" -ForegroundColor Cyan
                Write-Host "  Checking for failures..." -ForegroundColor Gray

                # Need to fetch details for each to find failures (list API only shows 'Finished')
                $failedItems = @()
                foreach ($wi in $workItems | Select-Object -First 20) {
                    $details = Get-HelixWorkItemDetails -JobId $HelixJob -WorkItemName $wi.Name
                    if ($details -and $null -ne $details.ExitCode -and $details.ExitCode -ne 0) {
                        $failedItems += @{
                            Name = $wi.Name
                            ExitCode = $details.ExitCode
                            State = $details.State
                        }
                    }
                }

                if ($failedItems.Count -gt 0) {
                    Write-Host "`n  Failed Work Items:" -ForegroundColor Red
                    foreach ($wi in $failedItems | Select-Object -First $MaxJobs) {
                        Write-Host "    - $($wi.Name) (Exit: $($wi.ExitCode))" -ForegroundColor White
                    }
                    Write-Host "`n  Use -WorkItem '<name>' to see details" -ForegroundColor Gray
                }
                else {
                    Write-Host "  No failures found in first 20 work items" -ForegroundColor Green
                }

                Write-Host "`n  All work items:" -ForegroundColor Yellow
                foreach ($wi in $workItems | Select-Object -First 10) {
                    Write-Host "    - $($wi.Name)" -ForegroundColor White
                }
                if ($workItems.Count -gt 10) {
                    Write-Host "    ... and $($workItems.Count - 10) more" -ForegroundColor Gray
                }

                # Find work items with binlogs if requested
                if ($FindBinlogs) {
                    Write-Host "`n  === Binlog Search ===" -ForegroundColor Yellow
                    $binlogResults = Find-WorkItemsWithBinlogs -JobId $HelixJob -MaxItems 30 -IncludeDetails

                    if ($binlogResults.Count -gt 0) {
                        Write-Host "`n  Work items with binlogs:" -ForegroundColor Cyan
                        foreach ($result in $binlogResults) {
                            $stateColor = if ($result.ExitCode -eq 0) { 'Green' } else { 'Red' }
                            Write-Host "    $($result.Name)" -ForegroundColor $stateColor
                            Write-Host "      Binlogs ($($result.BinlogCount)):" -ForegroundColor Gray
                            foreach ($binlog in $result.Binlogs | Select-Object -First 5) {
                                Write-Host "        - $binlog" -ForegroundColor White
                            }
                            if ($result.Binlogs.Count -gt 5) {
                                Write-Host "        ... and $($result.Binlogs.Count - 5) more" -ForegroundColor DarkGray
                            }
                        }
                        Write-Host "`n  Tip: Use -WorkItem '<name>' to get full binlog URIs" -ForegroundColor DarkGray
                    }
                    else {
                        Write-Host "  No binlogs found in scanned work items." -ForegroundColor Yellow
                        Write-Host "  This job may contain only unit tests (which don't produce binlogs)." -ForegroundColor Gray
                    }
                }
            }
        }

        exit 0
    }

    # Get build ID(s) if using PR number
    $buildIds = @()
    $knownIssuesFromBuildAnalysis = @()
    $prChangedFiles = @()
    $noBuildReason = $null
    if ($PSCmdlet.ParameterSetName -eq 'PRNumber') {
        $buildResult = Get-AzDOBuildIdFromPR -PR $PRNumber
        if ($buildResult.Reason) {
            # No builds found — emit summary with reason and exit
            $noBuildReason = $buildResult.Reason
            $buildIds = @()
            $summary = [ordered]@{
                mode = "PRNumber"
                repository = $Repository
                prNumber = $PRNumber
                builds = @()
                totalFailedJobs = 0
                totalLocalFailures = 0
                lastBuildJobSummary = [ordered]@{
                    total = 0; succeeded = 0; failed = 0; canceled = 0; pending = 0; warnings = 0; skipped = 0
                }
                failedJobNames = @()
                failedJobDetails = @()
                canceledJobNames = @()
                knownIssues = @()
                prCorrelation = [ordered]@{
                    changedFileCount = 0
                    hasCorrelation = $false
                    correlatedFiles = @()
                }
                recommendationHint = if ($noBuildReason -eq "MERGE_CONFLICTS") { "MERGE_CONFLICTS" } else { "NO_BUILDS" }
                noBuildReason = $noBuildReason
                mergeState = $buildResult.MergeState
            }
            Write-Host ""
            Write-Host "[CI_ANALYSIS_SUMMARY]"
            Write-Host ($summary | ConvertTo-Json -Depth 5)
            Write-Host "[/CI_ANALYSIS_SUMMARY]"
            exit 0
        }
        $buildIds = @($buildResult.BuildIds)

        # Check Build Analysis for known issues
        $knownIssuesFromBuildAnalysis = @(Get-BuildAnalysisKnownIssues -PR $PRNumber)

        # Get changed files for correlation
        $prChangedFiles = @(Get-PRChangedFiles -PR $PRNumber)
        if ($prChangedFiles.Count -gt 0) {
            Write-Verbose "PR has $($prChangedFiles.Count) changed files"
        }
    }
    else {
        $buildIds = @($BuildId)
    }

    # Process each build
    $totalFailedJobs = 0
    $totalLocalFailures = 0
    $allFailuresForCorrelation = @()
    $allFailedJobNames = @()
    $allCanceledJobNames = @()
    $allFailedJobDetails = @()
    $lastBuildJobSummary = $null

    foreach ($currentBuildId in $buildIds) {
        Write-Host "`n=== Azure DevOps Build $currentBuildId ===" -ForegroundColor Yellow
        Write-Host "URL: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$currentBuildId" -ForegroundColor Gray

        # Get and display build status
        $buildStatus = Get-AzDOBuildStatus -Build $currentBuildId
        if ($buildStatus) {
            $statusColor = switch ($buildStatus.Status) {
                "inProgress" { "Cyan" }
                "completed" { if ($buildStatus.Result -eq "succeeded") { "Green" } else { "Red" } }
                default { "Gray" }
            }
            $statusText = $buildStatus.Status
            if ($buildStatus.Status -eq "completed" -and $buildStatus.Result) {
                $statusText = "$($buildStatus.Status) ($($buildStatus.Result))"
            }
            elseif ($buildStatus.Status -eq "inProgress") {
                $statusText = "IN PROGRESS - showing failures so far"
            }
            Write-Host "Status: $statusText" -ForegroundColor $statusColor
        }

        # Get timeline
        $isInProgress = $buildStatus -and $buildStatus.Status -eq "inProgress"
        $timeline = Get-AzDOTimeline -Build $currentBuildId -BuildInProgress:$isInProgress

        # Handle timeline fetch failure
        if (-not $timeline) {
            Write-Host "`nCould not fetch build timeline" -ForegroundColor Red
            Write-Host "Build URL: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$currentBuildId" -ForegroundColor Gray
            continue
        }

        # Get failed jobs
        $failedJobs = Get-FailedJobs -Timeline $timeline

        # Get canceled jobs (different from failed - typically due to dependency failures)
        $canceledJobs = Get-CanceledJobs -Timeline $timeline

        # Also check for local test failures (non-Helix)
        $localTestFailures = Get-LocalTestFailures -Timeline $timeline -BuildId $currentBuildId

        # Accumulate totals and compute job summary BEFORE any continue branches
        $totalFailedJobs += $failedJobs.Count
        $totalLocalFailures += $localTestFailures.Count
        $allFailedJobNames += @($failedJobs | ForEach-Object { $_.name })
        $allCanceledJobNames += @($canceledJobs | ForEach-Object { $_.name })

        $allJobs = @()
        $succeededJobs = 0
        $pendingJobs = 0
        $canceledJobCount = 0
        $skippedJobs = 0
        $warningJobs = 0
        if ($timeline -and $timeline.records) {
            $allJobs = @($timeline.records | Where-Object { $_.type -eq "Job" })
            $succeededJobs = @($allJobs | Where-Object { $_.result -eq "succeeded" }).Count
            $warningJobs = @($allJobs | Where-Object { $_.result -eq "succeededWithIssues" }).Count
            $pendingJobs = @($allJobs | Where-Object { -not $_.result -or $_.state -eq "pending" -or $_.state -eq "inProgress" }).Count
            $canceledJobCount = @($allJobs | Where-Object { $_.result -eq "canceled" }).Count
            $skippedJobs = @($allJobs | Where-Object { $_.result -eq "skipped" }).Count
        }
        $lastBuildJobSummary = [ordered]@{
            total = $allJobs.Count
            succeeded = $succeededJobs
            failed = if ($failedJobs) { $failedJobs.Count } else { 0 }
            canceled = $canceledJobCount
            pending = $pendingJobs
            warnings = $warningJobs
            skipped = $skippedJobs
        }

        if ((-not $failedJobs -or $failedJobs.Count -eq 0) -and $localTestFailures.Count -eq 0) {
            if ($buildStatus -and $buildStatus.Status -eq "inProgress") {
                Write-Host "`nNo failures yet - build still in progress" -ForegroundColor Cyan
                Write-Host "Run again later to check for failures, or use -NoCache to get fresh data" -ForegroundColor Gray
            }
            else {
                Write-Host "`nNo failed jobs found in build $currentBuildId" -ForegroundColor Green
            }
            # Still show canceled jobs if any
            if ($canceledJobs -and $canceledJobs.Count -gt 0) {
                Write-Host "`nNote: $($canceledJobs.Count) job(s) were canceled (not failed):" -ForegroundColor DarkYellow
                foreach ($job in $canceledJobs | Select-Object -First 5) {
                    Write-Host "  - $($job.name)" -ForegroundColor DarkGray
                }
                if ($canceledJobs.Count -gt 5) {
                    Write-Host "  ... and $($canceledJobs.Count - 5) more" -ForegroundColor DarkGray
                }
                Write-Host "  (Canceled jobs are typically due to earlier stage failures or timeouts)" -ForegroundColor DarkGray
            }
            continue
        }

        # Report local test failures first (these may exist even without failed jobs)
        if ($localTestFailures.Count -gt 0) {
            Write-Host "`n=== Local Test Failures (non-Helix) ===" -ForegroundColor Yellow
            Write-Host "Build: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$currentBuildId" -ForegroundColor Gray

            foreach ($failure in $localTestFailures) {
                Write-Host "`n--- $($failure.TaskName) ---" -ForegroundColor Cyan

                # Collect issues for correlation
                $issueMessages = $failure.Issues | ForEach-Object { $_.message }
                $allFailuresForCorrelation += @{
                    TaskName = $failure.TaskName
                    JobName = "Local Test"
                    Errors = $issueMessages
                    HelixLogs = @()
                    FailedTests = @()
                }

                # Show build and log links
                $jobLogUrl = "https://dev.azure.com/$Organization/$Project/_build/results?buildId=$currentBuildId&view=logs&j=$($failure.ParentJobId)"
                if ($failure.TaskId) {
                    $jobLogUrl += "&t=$($failure.TaskId)"
                }
                Write-Host "  Log: $jobLogUrl" -ForegroundColor Gray

                # Show issues
                foreach ($issue in $failure.Issues) {
                    Write-Host "  $($issue.message)" -ForegroundColor Red
                }

                # Show test run URLs if available
                if ($failure.TestRunUrls.Count -gt 0) {
                    Show-TestRunResults -TestRunUrls $failure.TestRunUrls -Org "https://dev.azure.com/$Organization"
                }

                # Try to get more details from the task log
                if ($failure.LogId) {
                    $logContent = Get-BuildLog -Build $currentBuildId -LogId $failure.LogId
                    if ($logContent) {
                        # Extract test run URLs from this log too
                        $additionalRuns = Extract-TestRunUrls -LogContent $logContent
                        if ($additionalRuns.Count -gt 0 -and $failure.TestRunUrls.Count -eq 0) {
                            Show-TestRunResults -TestRunUrls $additionalRuns -Org "https://dev.azure.com/$Organization"
                        }

                        # Search for known issues based on build errors and task name
                        $buildErrors = Extract-BuildErrors -LogContent $logContent
                        if ($buildErrors.Count -gt 0) {
                            Show-KnownIssues -ErrorMessage ($buildErrors -join "`n") -IncludeMihuBot:$SearchMihuBot
                        }
                        elseif ($failure.TaskName) {
                            # If no specific errors, try searching by task name
                            Show-KnownIssues -TestName $failure.TaskName -IncludeMihuBot:$SearchMihuBot
                        }
                    }
                }
            }
        }

        if (-not $failedJobs -or $failedJobs.Count -eq 0) {
            Write-Host "`n=== Summary ===" -ForegroundColor Yellow
            Write-Host "Local test failures: $($localTestFailures.Count)" -ForegroundColor Red
            Write-Host "Build URL: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$currentBuildId" -ForegroundColor Cyan
            continue
        }

        Write-Host "`nFound $($failedJobs.Count) failed job(s):" -ForegroundColor Red

        # Show canceled jobs if any (these are different from failed)
        if ($canceledJobs -and $canceledJobs.Count -gt 0) {
            Write-Host "Also $($canceledJobs.Count) job(s) were canceled (due to earlier failures/timeouts):" -ForegroundColor DarkYellow
            foreach ($job in $canceledJobs | Select-Object -First 3) {
                Write-Host "  - $($job.name)" -ForegroundColor DarkGray
            }
            if ($canceledJobs.Count -gt 3) {
                Write-Host "  ... and $($canceledJobs.Count - 3) more" -ForegroundColor DarkGray
            }
        }

        $processedJobs = 0
        $errorCount = 0
        foreach ($job in $failedJobs) {
            if ($processedJobs -ge $MaxJobs) {
                Write-Host "`n... and $($failedJobs.Count - $MaxJobs) more failed jobs (use -MaxJobs to see more)" -ForegroundColor Yellow
                break
            }

            try {
                Write-Host "`n--- $($job.name) ---" -ForegroundColor Cyan
                Write-Host "  Build: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$currentBuildId&view=logs&j=$($job.id)" -ForegroundColor Gray

                # Track per-job failure details for JSON summary
                $jobDetail = [ordered]@{
                    jobName = $job.name
                    buildId = $currentBuildId
                    errorSnippet = ""
                    helixWorkItems = @()
                    errorCategory = "unclassified"
                }

                # Get Helix tasks for this job
                $helixTasks = Get-HelixJobInfo -Timeline $timeline -JobId $job.id

                if ($helixTasks) {
                    foreach ($task in $helixTasks) {
                        if ($task.log) {
                            Write-Host "  Fetching Helix task log..." -ForegroundColor Gray
                            $logContent = Get-BuildLog -Build $currentBuildId -LogId $task.log.id

                            if ($logContent) {
                                # Extract test failures
                                $failures = Extract-TestFailures -LogContent $logContent

                                if ($failures.Count -gt 0) {
                                    Write-Host "  Failed tests:" -ForegroundColor Red
                                    foreach ($failure in $failures) {
                                        Write-Host "    - $($failure.TestName)" -ForegroundColor White
                                    }

                                    # Collect for PR correlation
                                    $allFailuresForCorrelation += @{
                                        TaskName = $task.name
                                        JobName = $job.name
                                        Errors = @()
                                        HelixLogs = @()
                                        FailedTests = $failures | ForEach-Object { $_.TestName }
                                    }
                                    $jobDetail.errorCategory = "test-failure"
                                    $jobDetail.errorSnippet = ($failures | Select-Object -First 3 | ForEach-Object { $_.TestName }) -join "; "
                                }

                            # Extract and optionally fetch Helix URLs
                            $helixUrls = Extract-HelixUrls -LogContent $logContent

                            if ($helixUrls.Count -gt 0 -and $ShowLogs) {
                                Write-Host "`n  Helix Console Logs:" -ForegroundColor Yellow

                                foreach ($url in $helixUrls | Select-Object -First 3) {
                                    Write-Host "`n  $url" -ForegroundColor Gray

                                    # Extract work item name from URL for known issue search
                                    $workItemName = ""
                                    if ($url -match '/workitems/([^/]+)/console') {
                                        $workItemName = $Matches[1]
                                        $jobDetail.helixWorkItems += $workItemName
                                    }

                                    $helixLog = Get-HelixConsoleLog -Url $url
                                    if ($helixLog) {
                                        $failureInfo = Format-TestFailure -LogContent $helixLog
                                        if ($failureInfo) {
                                            Write-Host $failureInfo -ForegroundColor White

                                            # Categorize failure from log content
                                            if ($failureInfo -match 'Timed Out \(timeout') {
                                                $jobDetail.errorCategory = "test-timeout"
                                            } elseif ($failureInfo -match 'Exit Code:\s*(139|134|-4)' -or $failureInfo -match 'createdump') {
                                                # Crash takes highest precedence — don't downgrade
                                                if ($jobDetail.errorCategory -notin @("crash")) {
                                                    $jobDetail.errorCategory = "crash"
                                                }
                                            } elseif ($failureInfo -match 'Traceback \(most recent call last\)' -and $helixLog -match 'Tests run:.*Failures:\s*0') {
                                                # Work item failed (non-zero exit from reporter crash) but all tests passed.
                                                # The Python traceback is from Helix infrastructure, not from the test itself.
                                                if ($jobDetail.errorCategory -notin @("crash", "test-timeout")) {
                                                    $jobDetail.errorCategory = "tests-passed-reporter-failed"
                                                }
                                            } elseif ($jobDetail.errorCategory -eq "unclassified") {
                                                $jobDetail.errorCategory = "test-failure"
                                            }
                                            if (-not $jobDetail.errorSnippet) {
                                                $jobDetail.errorSnippet = $failureInfo.Substring(0, [Math]::Min(200, $failureInfo.Length))
                                            }

                                            # Search for known issues
                                            Show-KnownIssues -TestName $workItemName -ErrorMessage $failureInfo -IncludeMihuBot:$SearchMihuBot
                                        }
                                        else {
                                            # No failure pattern matched — show tail of log
                                            $lines = $helixLog -split "`n"
                                            $lastLines = $lines | Select-Object -Last 20
                                            $tailText = $lastLines -join "`n"
                                            Write-Host $tailText -ForegroundColor White
                                            if (-not $jobDetail.errorSnippet) {
                                                $jobDetail.errorSnippet = $tailText.Substring(0, [Math]::Min(200, $tailText.Length))
                                            }
                                            Show-KnownIssues -TestName $workItemName -ErrorMessage $tailText -IncludeMihuBot:$SearchMihuBot
                                        }
                                    }
                                }
                            }
                            elseif ($helixUrls.Count -gt 0) {
                                Write-Host "`n  Helix logs available (use -ShowLogs to fetch):" -ForegroundColor Yellow
                                foreach ($url in $helixUrls | Select-Object -First 3) {
                                    Write-Host "    $url" -ForegroundColor Gray
                                }
                            }
                        }
                    }
                }
            }
                else {
                    # No Helix tasks - this is a build failure, extract actual errors
                    $buildTasks = $timeline.records | Where-Object {
                        $_.parentId -eq $job.id -and $_.result -eq "failed"
                    }

                    foreach ($task in $buildTasks | Select-Object -First 3) {
                        Write-Host "  Failed task: $($task.name)" -ForegroundColor Red

                        # Fetch and parse the build log for actual errors
                        if ($task.log) {
                            $logUrl = "https://dev.azure.com/$Organization/$Project/_build/results?buildId=$currentBuildId&view=logs&j=$($job.id)&t=$($task.id)"
                            Write-Host "  Log: $logUrl" -ForegroundColor Gray
                            $logContent = Get-BuildLog -Build $currentBuildId -LogId $task.log.id

                            if ($logContent) {
                                $buildErrors = Extract-BuildErrors -LogContent $logContent

                                if ($buildErrors.Count -gt 0) {
                                    # Collect for PR correlation
                                    $allFailuresForCorrelation += @{
                                        TaskName = $task.name
                                        JobName = $job.name
                                        Errors = $buildErrors
                                        HelixLogs = @()
                                        FailedTests = @()
                                    }
                                    $jobDetail.errorCategory = "build-error"
                                    if (-not $jobDetail.errorSnippet) {
                                        $snippet = ($buildErrors | Select-Object -First 2) -join "; "
                                        $jobDetail.errorSnippet = $snippet.Substring(0, [Math]::Min(200, $snippet.Length))
                                    }

                                    # Extract Helix log URLs from the full log content
                                    $helixLogUrls = Extract-HelixLogUrls -LogContent $logContent

                                    if ($helixLogUrls.Count -gt 0) {
                                        Write-Host "  Helix failures ($($helixLogUrls.Count)):" -ForegroundColor Red
                                        foreach ($helixLog in $helixLogUrls | Select-Object -First 5) {
                                            Write-Host "    - $($helixLog.WorkItem)" -ForegroundColor White
                                            Write-Host "      Log: $($helixLog.Url)" -ForegroundColor Gray
                                        }
                                        if ($helixLogUrls.Count -gt 5) {
                                            Write-Host "    ... and $($helixLogUrls.Count - 5) more" -ForegroundColor Gray
                                        }
                                    }
                                    else {
                                        Write-Host "  Build errors:" -ForegroundColor Red
                                        foreach ($err in $buildErrors | Select-Object -First 5) {
                                            Write-Host "    $err" -ForegroundColor White
                                        }
                                        if ($buildErrors.Count -gt 5) {
                                            Write-Host "    ... and $($buildErrors.Count - 5) more errors" -ForegroundColor Gray
                                        }
                                    }

                                    # Search for known issues
                                    Show-KnownIssues -ErrorMessage ($buildErrors -join "`n") -IncludeMihuBot:$SearchMihuBot
                                }
                                else {
                                    Write-Host "  (No specific errors extracted from log)" -ForegroundColor Gray
                                }
                            }
                        }
                    }
                }

            $allFailedJobDetails += $jobDetail
            $processedJobs++
        }
        catch {
            $errorCount++
            if ($ContinueOnError) {
                Write-Warning "  Error processing job '$($job.name)': $_"
            }
            else {
                throw [System.Exception]::new("Error processing job '$($job.name)': $($_.Exception.Message)", $_.Exception)
            }
        }
    }

    Write-Host "`n=== Build $currentBuildId Summary ===" -ForegroundColor Yellow
    if ($allJobs.Count -gt 0) {
        $parts = @()
        if ($succeededJobs -gt 0) { $parts += "$succeededJobs passed" }
        if ($warningJobs -gt 0) { $parts += "$warningJobs passed with warnings" }
        if ($failedJobs.Count -gt 0) { $parts += "$($failedJobs.Count) failed" }
        if ($canceledJobCount -gt 0) { $parts += "$canceledJobCount canceled" }
        if ($skippedJobs -gt 0) { $parts += "$skippedJobs skipped" }
        if ($pendingJobs -gt 0) { $parts += "$pendingJobs pending" }
        $jobSummary = $parts -join ", "
        $allSucceeded = ($failedJobs.Count -eq 0 -and $pendingJobs -eq 0 -and $canceledJobCount -eq 0 -and ($succeededJobs + $warningJobs + $skippedJobs) -eq $allJobs.Count)
        $summaryColor = if ($allSucceeded) { "Green" } elseif ($failedJobs.Count -gt 0) { "Red" } else { "Cyan" }
        Write-Host "Jobs: $($allJobs.Count) total ($jobSummary)" -ForegroundColor $summaryColor
    }
    else {
        Write-Host "Failed jobs: $($failedJobs.Count)" -ForegroundColor Red
    }
    if ($localTestFailures.Count -gt 0) {
        Write-Host "Local test failures: $($localTestFailures.Count)" -ForegroundColor Red
    }
    if ($errorCount -gt 0) {
        Write-Host "API errors (partial results): $errorCount" -ForegroundColor Yellow
    }
    Write-Host "Build URL: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$currentBuildId" -ForegroundColor Cyan
}

# Show PR change correlation if we have changed files
if ($prChangedFiles.Count -gt 0 -and $allFailuresForCorrelation.Count -gt 0) {
    Show-PRCorrelationSummary -ChangedFiles $prChangedFiles -AllFailures $allFailuresForCorrelation
}

# Overall summary if multiple builds
if ($buildIds.Count -gt 1) {
    Write-Host "`n=== Overall Summary ===" -ForegroundColor Magenta
    Write-Host "Analyzed $($buildIds.Count) builds" -ForegroundColor White
    Write-Host "Total failed jobs: $totalFailedJobs" -ForegroundColor Red
    Write-Host "Total local test failures: $totalLocalFailures" -ForegroundColor Red

    if ($knownIssuesFromBuildAnalysis.Count -gt 0) {
        Write-Host "`nKnown Issues (from Build Analysis):" -ForegroundColor Yellow
        foreach ($issue in $knownIssuesFromBuildAnalysis) {
            Write-Host "  - #$($issue.Number): $($issue.Title)" -ForegroundColor Gray
            Write-Host "    $($issue.Url)" -ForegroundColor DarkGray
        }
    }
}

# Build structured summary and emit as JSON
$summary = [ordered]@{
    mode = $PSCmdlet.ParameterSetName
    repository = $Repository
    prNumber = if ($PSCmdlet.ParameterSetName -eq 'PRNumber') { $PRNumber } else { $null }
    builds = @($buildIds | ForEach-Object {
        [ordered]@{
            buildId = $_
            url = "https://dev.azure.com/$Organization/$Project/_build/results?buildId=$_"
        }
    })
    totalFailedJobs = $totalFailedJobs
    totalLocalFailures = $totalLocalFailures
    lastBuildJobSummary = if ($lastBuildJobSummary) { $lastBuildJobSummary } else { [ordered]@{
        total = 0; succeeded = 0; failed = 0; canceled = 0; pending = 0; warnings = 0; skipped = 0
    } }
    failedJobNames = @($allFailedJobNames)
    failedJobDetails = @($allFailedJobDetails)
    failedJobDetailsTruncated = ($allFailedJobNames.Count -gt $allFailedJobDetails.Count)
    canceledJobNames = @($allCanceledJobNames)
    knownIssues = @($knownIssuesFromBuildAnalysis | ForEach-Object {
        [ordered]@{ number = $_.Number; title = $_.Title; url = $_.Url }
    })
    prCorrelation = [ordered]@{
        changedFileCount = $prChangedFiles.Count
        hasCorrelation = $false
        correlatedFiles = @()
    }
    recommendationHint = ""
}

# Compute PR correlation using shared helper
if ($prChangedFiles.Count -gt 0 -and $allFailuresForCorrelation.Count -gt 0) {
    $correlation = Get-PRCorrelation -ChangedFiles $prChangedFiles -AllFailures $allFailuresForCorrelation
    $allCorrelated = @($correlation.CorrelatedFiles) + @($correlation.TestFiles) | Select-Object -Unique
    $summary.prCorrelation.hasCorrelation = $allCorrelated.Count -gt 0
    $summary.prCorrelation.correlatedFiles = @($allCorrelated)
}

# Compute recommendation hint
# Priority: KNOWN_ISSUES wins over LIKELY_PR_RELATED intentionally.
# When both exist, SKILL.md "Mixed signals" guidance tells the agent to separate them.
if (-not $lastBuildJobSummary -and $buildIds.Count -gt 0) {
    $summary.recommendationHint = "REVIEW_REQUIRED"
} elseif ($knownIssuesFromBuildAnalysis.Count -gt 0) {
    $summary.recommendationHint = "KNOWN_ISSUES_DETECTED"
} elseif ($totalFailedJobs -eq 0 -and $totalLocalFailures -eq 0) {
    $summary.recommendationHint = "BUILD_SUCCESSFUL"
} elseif ($summary.prCorrelation.hasCorrelation) {
    $summary.recommendationHint = "LIKELY_PR_RELATED"
} elseif ($prChangedFiles.Count -gt 0 -and $allFailuresForCorrelation.Count -gt 0) {
    $summary.recommendationHint = "POSSIBLY_TRANSIENT"
} else {
    $summary.recommendationHint = "REVIEW_REQUIRED"
}

Write-Host ""
Write-Host "[CI_ANALYSIS_SUMMARY]"
Write-Host ($summary | ConvertTo-Json -Depth 5)
Write-Host "[/CI_ANALYSIS_SUMMARY]"

}
catch {
    Write-Error "Error: $_"
    exit 1
}

#endregion Main Execution
