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
$resultsByVariant = @{}
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
    $resultsByVariant[$variant] = $results
}

$skilledPlan = $plansByVariant['skilled']
$stimuliProperty = $skilledPlan.PSObject.Properties['stimuli']
if (-not $stimuliProperty -or @($stimuliProperty.Value).Count -ne $skilledPlan.plannedStimulusCount) {
    throw "The 'skilled' plan has invalid planned stimulus coverage."
}

$stimuliByName = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
$trialCounts = [Collections.Generic.Dictionary[string, int]]::new([StringComparer]::Ordinal)
foreach ($stimulus in $stimuliProperty.Value) {
    $name = if ($null -ne $stimulus) { $stimulus.PSObject.Properties['name'] }
    if (-not $name -or $name.Value -isnot [string] -or
        [string]::IsNullOrWhiteSpace($name.Value) -or $stimuliByName.ContainsKey($name.Value)) {
        throw "The 'skilled' plan has invalid planned stimulus coverage."
    }
    $graders = $stimulus.PSObject.Properties['graders']
    if (-not $graders -or $null -eq $graders.Value) {
        throw "The 'skilled' stimulus '$($name.Value)' has invalid planned grader coverage."
    }
    foreach ($grader in $graders.Value) {
        $type = if ($null -ne $grader) { $grader.PSObject.Properties['type'] }
        if (-not $type -or $type.Value -isnot [string] -or [string]::IsNullOrWhiteSpace($type.Value)) {
            throw "The 'skilled' stimulus '$($name.Value)' has invalid planned grader coverage."
        }
    }
    $stimuliByName.Add($name.Value, $stimulus)
    $trialCounts.Add($name.Value, 0)
}

$trialIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($result in $resultsByVariant['skilled']) {
    $name = $result.PSObject.Properties['stimulus']
    if (-not $name -or $name.Value -isnot [string] -or -not $stimuliByName.ContainsKey($name.Value)) {
        throw "The 'skilled' variant has a missing or unplanned stimulus."
    }
    $id = $result.PSObject.Properties['itemId']
    if (-not $id -or $id.Value -isnot [string] -or
        [string]::IsNullOrWhiteSpace($id.Value) -or -not $trialIds.Add($id.Value)) {
        throw "The 'skilled' variant has a missing or duplicate trial identity."
    }
    $trialCounts[$name.Value]++
}
foreach ($name in $trialCounts.Keys) {
    if ($trialCounts[$name] -ne $skilledPlan.runs) {
        throw "The 'skilled' stimulus '$name' has invalid trial coverage: expected $($skilledPlan.runs), found $($trialCounts[$name])."
    }
}

$contractFailures = [Collections.Generic.List[string]]::new()
foreach ($result in $resultsByVariant['skilled']) {
    $expectedTypes = @($stimuliByName[$result.stimulus].graders | ForEach-Object type)
    if ($expectedTypes -cnotcontains 'output-matches') {
        continue
    }

    $detailsProperty = $result.gradeResult.PSObject.Properties['details']
    if (-not $detailsProperty -or @($detailsProperty.Value).Count -ne $expectedTypes.Count) {
        throw "The 'skilled' stimulus '$($result.stimulus)' has invalid grader coverage."
    }
    $actualTypes = [Collections.Generic.List[string]]::new()
    foreach ($detail in $detailsProperty.Value) {
        $type = if ($null -ne $detail) { $detail.PSObject.Properties['graderType'] }
        if (-not $type -or $type.Value -isnot [string]) {
            throw "The 'skilled' stimulus '$($result.stimulus)' has invalid grader coverage."
        }
        $actualTypes.Add($type.Value)
    }
    # Grader display names can repeat and ordering is not part of this contract.
    if (Compare-Object $expectedTypes $actualTypes.ToArray() -CaseSensitive) {
        throw "The 'skilled' stimulus '$($result.stimulus)' has invalid grader coverage."
    }
    foreach ($detail in $detailsProperty.Value) {
        if ($detail.graderType -cne 'output-matches') {
            continue
        }
        $passed = $detail.PSObject.Properties['passed']
        if (-not $passed -or $passed.Value -isnot [bool]) {
            throw "The 'skilled' stimulus '$($result.stimulus)' has a non-Boolean deterministic grader result."
        }
        if (-not $passed.Value) {
            $contractFailures.Add($result.stimulus)
        }
    }
}
if ($contractFailures.Count -gt 0) {
    throw "The 'skilled' variant has deterministic contract failures: $(($contractFailures | Sort-Object -Unique) -join ', ')."
}

$baselineScore = $scoresByVariant['baseline']
$skilledScore = $scoresByVariant['skilled']
$skilledThreshold = [double]$plansByVariant['skilled'].threshold
if ($skilledScore -lt $skilledThreshold) {
    throw "The 'skilled' score $skilledScore is below the required threshold $skilledThreshold."
}

Write-Host "Both variants produced complete successful results. Baseline score: $baselineScore. Skilled score: $skilledScore (threshold: $skilledThreshold)."
