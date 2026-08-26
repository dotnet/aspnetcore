#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [ValidateSet('baseline', 'skilled')]
    [string]$Variant = 'skilled'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$resolvedOutput = (Resolve-Path $OutputDirectory).Path
$runDirectories = @(Get-ChildItem $resolvedOutput -Directory)
if ($runDirectories.Count -ne 1) {
    throw "Expected exactly one Vally run under '$resolvedOutput'; found $($runDirectories.Count)."
}

$runDirectory = $runDirectories[0].FullName
$snapshotPath = Join-Path $runDirectory 'plan-snapshot.json'
if (-not (Test-Path $snapshotPath -PathType Leaf)) {
    throw "Missing Vally plan snapshot at '$snapshotPath'."
}

$snapshot = Get-Content $snapshotPath -Raw | ConvertFrom-Json -Depth 100
$plans = @($snapshot.evals | Where-Object variant -eq $Variant)
if ($plans.Count -ne 1) {
    throw "Expected exactly one '$Variant' plan; found $($plans.Count)."
}

$plan = $plans[0]
$resultsPath = Join-Path $runDirectory "$Variant/results.jsonl"
if (-not (Test-Path $resultsPath -PathType Leaf)) {
    throw "Missing Vally results at '$resultsPath'."
}

$results = @(
    Get-Content $resultsPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_ | ConvertFrom-Json -Depth 100 }
)
$expectedCount = [int]$plan.plannedStimulusCount * [int]$plan.runs
if ($results.Count -ne $expectedCount) {
    throw "Expected $expectedCount '$Variant' trial results; found $($results.Count)."
}

$incomplete = @($results | Where-Object status -ne 'success')
if ($incomplete.Count -gt 0) {
    throw "The '$Variant' variant has $($incomplete.Count) incomplete or failed trial(s)."
}

$scores = @($results | ForEach-Object { $_.gradeResult.score })
if ($scores.Count -ne $results.Count -or $scores -contains $null) {
    throw "The '$Variant' variant has trial results without grader scores."
}

$score = ($scores | Measure-Object -Average).Average
$threshold = [double]$plan.threshold
if ($score -lt $threshold) {
    throw "The '$Variant' score $score is below the required threshold $threshold."
}

Write-Host "The '$Variant' variant passed with score $score (threshold: $threshold)."
