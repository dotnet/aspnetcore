#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$resolvedOutput = (Resolve-Path $OutputDirectory).Path
$runDirectories = @(Get-ChildItem $resolvedOutput -Directory)
if ($runDirectories.Count -gt 0 -and
    @($runDirectories | Where-Object { Test-Path (Join-Path $_.FullName 'plan-snapshot.json') }).Count -eq 0) {
    foreach ($directory in $runDirectories) {
        & $PSCommandPath -OutputDirectory $directory.FullName
    }
    return
}
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
$variants = if ($snapshot.evals.Count -eq 1 -and $snapshot.evals[0].variant -eq 'current') {
    @('current')
} else {
    @('baseline', 'skilled')
}
foreach ($variant in $variants) {
    $plans = @($snapshot.evals | Where-Object variant -eq $variant)
    if ($plans.Count -ne 1) {
        throw "Expected exactly one '$variant' plan; found $($plans.Count)."
    }

    $plan = $plans[0]
    $names = @($plan.plannedStimulusNames)
    if ($names.Count -lt 1 -or $names.Count -ne [int]$plan.plannedStimulusCount -or
        @($names | Sort-Object -Unique).Count -ne $names.Count -or
        [int]$plan.runs -lt 1) {
        throw "The '$variant' plan has invalid stimulus identities or trial counts."
    }
    $plansByVariant[$variant] = $plan
    $resultsPath = Join-Path $runDirectory $(if ($variant -eq 'current') { 'results.jsonl' } else { "$variant/results.jsonl" })
    if (-not (Test-Path $resultsPath -PathType Leaf)) {
        throw "Missing Vally results at '$resultsPath'."
    }

    $results = @(
        Get-Content $resultsPath |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_ | ConvertFrom-Json -Depth 100 }
    )
    $completion = $null
    if ($variant -eq 'current') {
        $summaries = @($results | Where-Object type -eq 'run-summary')
        if ($summaries.Count -ne 1 -or $results[-1].type -ne 'run-summary' -or
            $summaries[0].hadExecutionErrors -isnot [bool] -or $summaries[0].hadExecutionErrors -or
            [string]::IsNullOrWhiteSpace($summaries[0].runId)) {
            throw 'The component output has an invalid or missing completion summary.'
        }
        $completion = $summaries[0]
        $results = @($results | Where-Object type -ne 'run-summary')
    }
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
    $scoresByStimulus = @{}
    $identities = [Collections.Generic.HashSet[string]]::new()
    foreach ($result in $results) {
        if ($result.type -ne 'trial-result' -or
            ($completion -and $result.runId -ne $completion.runId) -or
            ($variant -ne 'current' -and (
                -not $result.PSObject.Properties['variant'] -or $result.variant -ne $variant
            )) -or
            -not $result.PSObject.Properties['stimulus'] -or
            $result.stimulus -notin $names) {
            throw "The '$variant' variant has an unplanned or missing stimulus."
        }
        $trialIndex = if ($result.PSObject.Properties['trialIndex']) {
            $result.trialIndex
        } elseif ([int]$plan.runs -eq 1) {
            0
        } else {
            $null
        }
        if ($null -eq $trialIndex -or
            ($trialIndex -isnot [long] -and $trialIndex -isnot [int]) -or
            $trialIndex -lt 0 -or $trialIndex -ge [int]$plan.runs -or
            -not $identities.Add("$($result.stimulus):$trialIndex")) {
            throw "The '$variant' variant has an invalid or duplicate trial identity."
        }
        $gradeResultProperty = $result.PSObject.Properties['gradeResult']
        $scoreProperty = if ($gradeResultProperty -and $null -ne $gradeResultProperty.Value) {
            $gradeResultProperty.Value.PSObject.Properties['score']
        }
        if (-not $scoreProperty -or $null -eq $scoreProperty.Value) {
            throw "The '$variant' variant has trial results without grader scores."
        }
        if ($scoreProperty.Value -is [string] -or $scoreProperty.Value -is [bool] -or
            -not [double]::IsFinite([double]$scoreProperty.Value) -or
            [double]$scoreProperty.Value -lt 0 -or [double]$scoreProperty.Value -gt 1) {
            throw "The '$variant' variant has an invalid grader score."
        }
        $score = [double]$scoreProperty.Value
        if ($variant -eq 'current' -and $score -notin @(0, 1)) {
            throw 'Component disposition scores must be binary.'
        }
        $scores.Add($score)
        if (-not $scoresByStimulus.ContainsKey($result.stimulus)) {
            $scoresByStimulus[$result.stimulus] = [Collections.Generic.List[double]]::new()
        }
        $scoresByStimulus[$result.stimulus].Add($score)
        $isReview = $plan.PSObject.Properties['evalName'] -and $plan.evalName -eq 'review-pull-request'
        if ($variant -eq 'current' -or $isReview) {
            $requiredReceipts = @("evidence-reads-$($result.stimulus)")
            if ($isReview) {
                $requiredReceipts += "guidance-reads-$($result.stimulus)"
                if ($variant -eq 'skilled') {
                    $requiredReceipts += "treatment-reads-$($result.stimulus)"
                }
            }
            if (-not $gradeResultProperty.Value.PSObject.Properties['details']) {
                throw "The '$variant' trial lacks read-integrity results."
            }
            $dispositions = @($gradeResultProperty.Value.details |
                Where-Object configuredName -eq "disposition-$($result.stimulus)")
            if ($dispositions.Count -ne 1 -or $dispositions[0].score -ne $score -or
                ($dispositions[0].PSObject.Properties['status'] -and $dispositions[0].status -eq 'error')) {
                throw "The '$variant' trial lacks a valid primary disposition result."
            }
            foreach ($receipt in $requiredReceipts) {
                $details = @($gradeResultProperty.Value.details | Where-Object configuredName -eq $receipt)
                if ($details.Count -ne 1 -or $details[0].passed -isnot [bool] -or
                    -not $details[0].passed -or $details[0].score -is [string] -or $details[0].score -ne 1) {
                    throw "The '$variant' trial '$($result.stimulus):$trialIndex' failed read integrity: $receipt."
                }
            }
        }
    }

    $scoresByVariant[$variant] = ($scores | Measure-Object -Average).Average
    foreach ($name in $names) {
        if (-not $scoresByStimulus.ContainsKey($name) -or
            $scoresByStimulus[$name].Count -ne [int]$plan.runs) {
            throw "The '$variant' stimulus '$name' has incomplete trial coverage."
        }
        $caseScore = ($scoresByStimulus[$name] | Measure-Object -Average).Average
        if ($variant -ne 'baseline' -and $caseScore -lt [double]$plan.threshold) {
            throw "The '$variant' stimulus '$name' score $caseScore is below threshold $($plan.threshold)."
        }
        Write-Host "$variant/$name score: $caseScore ($($plan.runs) complete trials)."
    }
}

Write-Host 'All planned trials completed. Every gated stimulus met its own threshold.'
