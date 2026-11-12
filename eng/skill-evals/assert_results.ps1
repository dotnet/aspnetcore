#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
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
$scoresByVariant = @{}
$plansByVariant = @{}
foreach ($variant in @('baseline', 'skilled')) {
    $plans = @($snapshot.evals | Where-Object variant -eq $variant)
    if ($plans.Count -ne 1) {
        throw "Expected exactly one '$variant' plan; found $($plans.Count)."
    }

    $plan = $plans[0]
    $plansByVariant[$variant] = $plan
    $resultsPath = Join-Path $runDirectory "$variant/results.jsonl"
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
        throw "Expected $expectedCount '$variant' trial results; found $($results.Count)."
    }

    $incomplete = @(
        $results |
            Where-Object {
                -not $_.PSObject.Properties['status'] -or
                $_.status -ne 'success'
            }
    )
    if ($incomplete.Count -gt 0) {
        throw "The '$variant' variant has $($incomplete.Count) incomplete or failed trial(s)."
    }

    $scores = [Collections.Generic.List[double]]::new()
    foreach ($result in $results) {
        $gradeResultProperty = $result.PSObject.Properties['gradeResult']
        $scoreProperty = if ($gradeResultProperty -and $null -ne $gradeResultProperty.Value) {
            $gradeResultProperty.Value.PSObject.Properties['score']
        }
        if (-not $scoreProperty -or $null -eq $scoreProperty.Value) {
            throw "The '$variant' variant has trial results without grader scores."
        }
        $scores.Add([double]$scoreProperty.Value)
    }

    $scoresByVariant[$variant] = ($scores | Measure-Object -Average).Average
}

$baselineScore = $scoresByVariant['baseline']
$skilledScore = $scoresByVariant['skilled']
$skilledThreshold = [double]$plansByVariant['skilled'].threshold
if ($skilledScore -lt $skilledThreshold) {
    throw "The 'skilled' score $skilledScore is below the required threshold $skilledThreshold."
}

Write-Host "Both variants produced complete successful results. Baseline score: $baselineScore. Skilled score: $skilledScore (threshold: $skilledThreshold)."
