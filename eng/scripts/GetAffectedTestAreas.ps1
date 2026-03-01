#requires -version 5
<#
.SYNOPSIS
    Determines which test areas should run based on files changed in a PR.

.DESCRIPTION
    This script takes a list of changed files (from git diff), maps them to top-level src/ areas,
    computes the transitive closure of reverse dependencies using the dependency graph in
    eng/AffectedProjectAreas.json, and outputs the set of areas whose tests should run.

    If infrastructure files are changed (eng/, global.json, Directory.Build.*, etc.), all tests run.

.PARAMETER TargetBranch
    The target branch to diff against (e.g., 'origin/main'). If not specified, reads from
    SYSTEM_PULLREQUEST_TARGETBRANCH environment variable.

.PARAMETER ChangedFiles
    Optional explicit list of changed files. If not specified, computed from git diff against TargetBranch.

.PARAMETER DependencyGraphPath
    Path to the dependency graph JSON file. Defaults to eng/AffectedProjectAreas.json.

.OUTPUTS
    A semicolon-delimited string of affected area names suitable for MSBuild property consumption.
    Returns empty string if all tests should run (no filtering).
#>
param(
    [string]$TargetBranch,
    [string[]]$ChangedFiles,
    [string]$DependencyGraphPath,
    [string]$OutputFile
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

$repoRoot = Resolve-Path "$PSScriptRoot/../.."

if (-not $DependencyGraphPath) {
    $DependencyGraphPath = Join-Path $repoRoot "eng/AffectedProjectAreas.json"
}

# Determine changed files
if (-not $ChangedFiles) {
    if (-not $TargetBranch) {
        $TargetBranch = $env:SYSTEM_PULLREQUEST_TARGETBRANCH
    }

    if ([string]::IsNullOrEmpty($TargetBranch)) {
        Write-Host "No target branch specified and SYSTEM_PULLREQUEST_TARGETBRANCH not set. Running all tests."
        return ""
    }

    if ($TargetBranch.StartsWith('refs/heads/')) {
        $TargetBranch = $TargetBranch.Replace('refs/heads/', '')
    }

    Write-Host "Computing changed files against origin/$TargetBranch"

    # Ensure the target branch ref is available (CI may use shallow clones)
    $refExists = & git rev-parse --verify "origin/$TargetBranch" 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "origin/$TargetBranch not found locally, fetching..."
        & git fetch origin "$TargetBranch" --depth=1 2>&1 | Write-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Host "git fetch failed. Running all tests."
            return ""
        }
    }

    $ChangedFiles = & git --no-pager diff "origin/$TargetBranch" --name-only --diff-filter=ACMRT
    if ($LASTEXITCODE -ne 0) {
        Write-Host "git diff failed. Running all tests."
        return ""
    }
}

if (-not $ChangedFiles -or $ChangedFiles.Count -eq 0) {
    Write-Host "No changed files detected. Running all tests."
    return ""
}

Write-Host "Found $($ChangedFiles.Count) changed file(s)"

# Patterns that trigger all tests (infrastructure/global changes)
# TODO: Re-enable these patterns after CI testing is complete.
# $runAllPatterns = @(
#     '^eng/',
#     '^global\.json$',
#     '^Directory\.Build\.',
#     '^NuGet\.config$',
#     '\.azure/',
#     '^build\.',
#     '^restore\.',
#     '^activate\.'
# )
$runAllPatterns = @()

foreach ($file in $ChangedFiles) {
    $normalizedFile = $file.Replace('\', '/')
    foreach ($pattern in $runAllPatterns) {
        if ($normalizedFile -match $pattern) {
            Write-Host "Infrastructure file changed: $file (matched '$pattern'). Running all tests."
            return ""
        }
    }
}

# Map changed files to affected src/ areas
$changedAreas = [System.Collections.Generic.HashSet[string]]::new()
foreach ($file in $ChangedFiles) {
    $normalizedFile = $file.Replace('\', '/')
    if ($normalizedFile.StartsWith('src/')) {
        $parts = $normalizedFile.Split('/')
        if ($parts.Length -ge 2) {
            [void]$changedAreas.Add($parts[1])
        }
    }
}

if ($changedAreas.Count -eq 0) {
    # TODO: Re-enable running all tests when no src/ areas changed after CI testing.
    Write-Host "No src/ areas changed. Only src/ area changes trigger test filtering. Skipping filtering."
    return ""
}

Write-Host "Directly changed areas: $($changedAreas -join ', ')"

# Load dependency graph
if (-not (Test-Path $DependencyGraphPath)) {
    Write-Host "Dependency graph not found at $DependencyGraphPath. Running all tests."
    return ""
}

$graph = Get-Content -Raw $DependencyGraphPath | ConvertFrom-Json

# Build reverse dependency map: area -> list of areas that depend on it
$reverseDeps = @{}
foreach ($area in $graph.PSObject.Properties.Name) {
    # Skip the _comment property
    if ($area -eq '_comment') { continue }
    $deps = $graph.$area
    foreach ($dep in $deps) {
        if (-not $reverseDeps.ContainsKey($dep)) {
            $reverseDeps[$dep] = [System.Collections.Generic.List[string]]::new()
        }
        $reverseDeps[$dep].Add($area)
    }
}

# Compute transitive closure of reverse dependencies for all changed areas
$affectedAreas = [System.Collections.Generic.HashSet[string]]::new()
$queue = [System.Collections.Generic.Queue[string]]::new()

foreach ($area in $changedAreas) {
    [void]$queue.Enqueue($area)
}

while ($queue.Count -gt 0) {
    $current = $queue.Dequeue()
    if ($affectedAreas.Contains($current)) {
        continue
    }
    [void]$affectedAreas.Add($current)

    if ($reverseDeps.ContainsKey($current)) {
        foreach ($dependent in $reverseDeps[$current]) {
            if (-not $affectedAreas.Contains($dependent)) {
                [void]$queue.Enqueue($dependent)
            }
        }
    }
}

$sortedAreas = $affectedAreas | Sort-Object
Write-Host "Affected areas ($($sortedAreas.Count) total): $($sortedAreas -join ', ')"

# Return semicolon-delimited string for MSBuild consumption
$result = $sortedAreas -join ';'

if ($OutputFile) {
    $result | Set-Content -Path $OutputFile -NoNewline
}

return $result
