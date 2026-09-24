#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)]
    [string]$Manifest,

    [string[]]$EffectAssessment
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

function Get-Sha256 {
    param([string]$Path)
    return (Get-FileHash $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-Property {
    param([object]$Object, [string]$Name, [string]$Context)

    $property = $Object.PSObject.Properties[$Name]
    if (-not $property -or $null -eq $property.Value) {
        throw "$Context is missing '$Name'."
    }
    return $property.Value
}

function Read-JsonLines {
    param([string]$Path)

    return @(
        Get-Content $Path |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_ | ConvertFrom-Json -Depth 100 }
    )
}

$manifestPath = (Resolve-Path $Manifest).Path
$data = Get-Content $manifestPath -Raw | ConvertFrom-Json -Depth 100
if ($data.schemaVersion -ne 1) {
    throw "Unsupported manifest schema '$($data.schemaVersion)'."
}
$cases = @($data.cases)
if ($cases.Count -eq 0) {
    throw 'Manifest declares no cases.'
}
$caseNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$expectedMatrix = @{}
$computedPairs = 0
foreach ($case in $cases) {
    $caseName = [string](Get-Property $case 'name' 'Case')
    $caseCohort = [string](Get-Property $case 'cohort' "Case '$caseName'")
    $caseRuns = [int](Get-Property $case 'runs' "Case '$caseName'")
    if ([string]::IsNullOrWhiteSpace($caseName) -or -not $caseNames.Add($caseName)) {
        throw "Manifest contains an empty or duplicate case '$caseName'."
    }
    if ($caseRuns -lt 1) {
        throw "Case '$caseName' has invalid runs '$caseRuns'."
    }
    for ($repetition = 0; $repetition -lt $caseRuns; $repetition++) {
        $pairId = "$caseName-r$repetition"
        foreach ($variant in @('baseline', 'skilled')) {
            $cellId = "$pairId-$variant"
            $projection = Get-Property $case.projections $cellId "Case '$caseName' projections"
            foreach ($field in @(
                'resolvedConfigHash', 'frozenProjectionHash', 'backendName', 'executorName',
                'graderHash', 'scoringHash', 'judgeConfigHash'
            )) {
                Get-Property $projection $field "Cell '$cellId' projection" | Out-Null
            }
            $expectedMatrix[$cellId] = [ordered]@{
                pairId = $pairId
                stimulus = $caseName
                cohort = $caseCohort
                variant = $variant
                repetition = $repetition
                case = $case
                projection = $projection
            }
        }
        $computedPairs++
    }
}
$computedCells = $computedPairs * 2
if ([int]$data.expectedCells -ne $computedCells -or [int]$data.expectedPairs -ne $computedPairs) {
    throw "Manifest counters do not match its declared cases: expected $computedCells cells and $computedPairs pairs."
}
$cells = @($data.cells)
if ($cells.Count -ne $computedCells) {
    throw "Manifest declared cases require $computedCells cells; found $($cells.Count)."
}

$cellIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$compositeIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$rawPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$validated = @{}
$skilledScores = [Collections.Generic.List[double]]::new()
$cohortScores = @{}
$deterministicFailures = [Collections.Generic.List[string]]::new()

foreach ($cell in $cells) {
    $cellId = [string](Get-Property $cell 'cellId' 'Cell')
    if (-not $cellIds.Add($cellId)) {
        throw "Duplicate cell ID '$cellId'."
    }
    if (-not $expectedMatrix.ContainsKey($cellId)) {
        throw "Cell '$cellId' is not planned by the declared cases and runs."
    }
    $expected = $expectedMatrix[$cellId]
    foreach ($field in @(
        'pairId', 'stimulus', 'variant', 'expectedVariant', 'expectedStimulus',
        'expectedModel', 'expectedJudgeModel', 'expectedEvalName'
    )) {
        Get-Property $cell $field "Cell '$cellId'" | Out-Null
    }
    if ($cell.variant -notin @('baseline', 'skilled') -or $cell.variant -cne $cell.expectedVariant) {
        throw "Cell '$cellId' has a mismatched variant."
    }
    if ($cell.stimulus -cne $cell.expectedStimulus) {
        throw "Cell '$cellId' has a mismatched stimulus."
    }
    foreach ($field in @('pairId', 'stimulus', 'cohort', 'variant', 'repetition')) {
        if ($cell.$field -cne $expected.$field) {
            throw "Cell '$cellId' has an unplanned $field."
        }
    }
    foreach ($field in @('canonicalInputHash', 'graderHash', 'scoringHash', 'judgeConfigHash')) {
        if ($cell.$field -cne $expected.case.$field) {
            throw "Cell '$cellId' differs from its frozen case '$field'."
        }
    }
    foreach ($field in @(
        'resolvedConfigHash', 'frozenProjectionHash', 'backendName', 'executorName',
        'graderHash', 'scoringHash', 'judgeConfigHash'
    )) {
        if ($cell.$field -cne $expected.projection.$field) {
            throw "Cell '$cellId' differs from its frozen $($cell.variant) projection '$field'."
        }
    }
    if ((Get-Sha256 $cell.projectionPath) -cne $cell.projectionHash) {
        throw "Cell '$cellId' immutable projection file changed."
    }
    if ((Get-Sha256 $cell.evalPath) -cne $cell.evalHash -or
        (Get-Sha256 $cell.experimentPath) -cne $cell.experimentHash) {
        throw "Cell '$cellId' input hash mismatch."
    }
    if ($cell.storageCase -eq 'collision') {
        if (-not (Test-Path $cell.destination -PathType Leaf) -or
            (Get-Sha256 $cell.destination) -cne $cell.fixtureHashes.destination) {
            throw "Cell '$cellId' did not preserve its collision sentinel."
        }
    } elseif ($cell.storageCase -eq 'writer-failure') {
        $parent = Split-Path $cell.destination -Parent
        if (-not (Test-Path $parent -PathType Leaf) -or
            (Get-Sha256 $parent) -cne $cell.fixtureHashes.parent -or
            (Test-Path $cell.destination)) {
            throw "Cell '$cellId' did not preserve its writer-failure fixture."
        }
    }

    $receiptPath = Join-Path (Split-Path $cell.outputRoot -Parent) 'receipt.json'
    if (-not (Test-Path $receiptPath -PathType Leaf)) {
        throw "Cell '$cellId' has no execution receipt."
    }
    $receipt = Get-Content $receiptPath -Raw | ConvertFrom-Json
    if ($receipt.cellId -cne $cellId -or $receipt.state -ne 'complete' -or -not $receipt.structuredComplete) {
        throw "Cell '$cellId' is incomplete or not started."
    }

    $runs = @(Get-ChildItem $cell.outputRoot -Directory)
    if ($runs.Count -ne 1) {
        throw "Cell '$cellId' must own exactly one native run directory; found $($runs.Count)."
    }
    $snapshotPath = Join-Path $runs[0].FullName 'plan-snapshot.json'
    $resultsPath = Join-Path $runs[0].FullName "$($cell.variant)/results.jsonl"
    foreach ($rawPath in @($snapshotPath, $resultsPath)) {
        if (-not (Test-Path $rawPath -PathType Leaf)) {
            throw "Cell '$cellId' is missing native output '$rawPath'."
        }
        $resolvedRawPath = (Resolve-Path $rawPath).Path
        if (-not $rawPaths.Add($resolvedRawPath)) {
            throw "Native output '$resolvedRawPath' is reused across cells."
        }
    }
    if ($receipt.snapshotPath -cne (Resolve-Path $snapshotPath).Path -or
        $receipt.resultsPath -cne (Resolve-Path $resultsPath).Path -or
        $receipt.snapshotHash -cne (Get-Sha256 $snapshotPath) -or
        $receipt.resultsHash -cne (Get-Sha256 $resultsPath)) {
        throw "Cell '$cellId' execution receipt does not match its native output."
    }

    $snapshot = Get-Content $snapshotPath -Raw | ConvertFrom-Json -Depth 100
    $plans = @($snapshot.evals)
    if ($plans.Count -ne [int]$cell.expectedPlans) {
        throw "Cell '$cellId' has $($plans.Count) plans; expected $($cell.expectedPlans)."
    }
    $plan = $plans[0]
    if ($plan.variant -cne $cell.expectedVariant -or
        [int]$plan.runs -ne 1 -or
        [int]$plan.plannedStimulusCount -ne 1 -or
        @($plan.stimuli).Count -ne 1 -or
        $plan.stimuli[0].name -cne $cell.expectedStimulus -or
        $plan.model -cne $cell.expectedModel -or
        $plan.judgeModel -cne $cell.expectedJudgeModel -or
        $plan.executorName -cne $cell.executorName) {
        throw "Cell '$cellId' plan identity mismatch."
    }

    $rows = @(Read-JsonLines $resultsPath)
    if ($rows.Count -ne [int]$cell.expectedResults) {
        throw "Cell '$cellId' has $($rows.Count) results; expected $($cell.expectedResults)."
    }
    $result = $rows[0]
    if ($result.variant -cne $cell.expectedVariant -or
        $result.stimulus -cne $cell.expectedStimulus -or
        $result.model -cne $cell.expectedModel -or
        $result.evalName -cne $cell.expectedEvalName) {
        throw "Cell '$cellId' native result identity mismatch."
    }
    if (-not $result.PSObject.Properties['itemId'] -or
        [string]::IsNullOrWhiteSpace([string]$result.itemId) -or
        -not $compositeIds.Add("$cellId`0$($result.itemId)")) {
        throw "Cell '$cellId' has a missing or duplicate composite native identity."
    }
    if (-not $result.PSObject.Properties['status'] -or $result.status -ne 'success') {
        throw "Cell '$cellId' has an incomplete native result."
    }
    $score = $result.gradeResult.PSObject.Properties['score']
    if (-not $score -or $null -eq $score.Value -or
        [double]::IsNaN([double]$score.Value) -or [double]::IsInfinity([double]$score.Value)) {
        throw "Cell '$cellId' has no finite grade score."
    }

    $expectedTypes = @($plan.stimuli[0].graders | ForEach-Object type)
    if ($cell.variant -eq 'skilled' -and $expectedTypes -ccontains 'output-matches') {
        $details = @($result.gradeResult.details)
        $actualTypes = @($details | ForEach-Object graderType)
        if ($details.Count -ne $expectedTypes.Count -or
            (Compare-Object $expectedTypes $actualTypes -CaseSensitive)) {
            throw "Cell '$cellId' has invalid grader coverage."
        }
        foreach ($detail in $details | Where-Object graderType -CEQ 'output-matches') {
            if (-not $detail.PSObject.Properties['passed'] -or $detail.passed -isnot [bool]) {
                throw "Cell '$cellId' has a non-Boolean deterministic grader result."
            }
            if (-not $detail.passed) {
                $deterministicFailures.Add($cellId)
            }
        }
    }
    if ($cell.variant -eq 'skilled') {
        $skilledScores.Add([double]$score.Value)
        if (-not $cohortScores.ContainsKey($cell.cohort)) {
            $cohortScores[$cell.cohort] = [Collections.Generic.List[double]]::new()
        }
        $cohortScores[$cell.cohort].Add([double]$score.Value)
    }

    $validated[$cellId] = [ordered]@{
        cell = $cell
        resultPath = (Resolve-Path $resultsPath).Path
        resultHash = Get-Sha256 $resultsPath
        snapshotPath = (Resolve-Path $snapshotPath).Path
        snapshotHash = Get-Sha256 $snapshotPath
        nativeItemId = $result.itemId
        score = [double]$score.Value
        exitCode = [int]$receipt.exitCode
    }
}
if ($cellIds.Count -ne $expectedMatrix.Count) {
    throw 'Manifest cell matrix is incomplete.'
}

$pairs = @($cells | Group-Object pairId)
if ($pairs.Count -ne $computedPairs) {
    throw "Manifest declared cases require $computedPairs pairs; found $($pairs.Count)."
}
foreach ($pair in $pairs) {
    if ($pair.Count -ne 2) {
        throw "Pair '$($pair.Name)' must contain exactly two cells."
    }
    $baseline = @($pair.Group | Where-Object variant -CEQ 'baseline')
    $skilled = @($pair.Group | Where-Object variant -CEQ 'skilled')
    if ($baseline.Count -ne 1 -or $skilled.Count -ne 1) {
        throw "Pair '$($pair.Name)' must contain one baseline and one skilled cell."
    }
    foreach ($field in @(
        'stimulus', 'cohort', 'repetition', 'canonicalInputHash',
        'pairNormalizedHash', 'graderHash', 'scoringHash', 'judgeConfigHash',
        'backendName', 'executorName', 'expectedModel', 'expectedJudgeModel', 'expectedEvalName'
    )) {
        if ($baseline[0].$field -cne $skilled[0].$field) {
            throw "Pair '$($pair.Name)' differs in '$field'."
        }
    }
}

if ($deterministicFailures.Count -gt 0) {
    throw "Skilled deterministic contract failures: $($deterministicFailures -join ', ')."
}
if ($skilledScores.Count -eq 0) {
    throw 'No skilled scores were found.'
}
$skilledMean = ($skilledScores | Measure-Object -Average).Average
if ($skilledMean -lt [double]$data.skilledThreshold) {
    throw "Selection-wide skilled score $skilledMean is below threshold $($data.skilledThreshold)."
}

$cohorts = [ordered]@{}
foreach ($name in $cohortScores.Keys | Sort-Object) {
    $cohorts[$name] = [ordered]@{
        count = $cohortScores[$name].Count
        mean = ($cohortScores[$name] | Measure-Object -Average).Average
    }
}
$runtimeAcceptance = 'not-assessed-by-structural-checker'
if ($PSBoundParameters.ContainsKey('EffectAssessment') -and $EffectAssessment.Count -gt 0) {
    $requiredEffectGates = @(
        'NoApprovalOrDeniedApprovalPerformsZeroExecution',
        'ReporterApprovalDoesNotAuthorizeExecution',
        'MaterialCommandChangeRequiresReapproval',
        'SensitiveStopNeverTransitionsToExecution',
        'UnknownThirdPartyTriggerRequestsCleanRepro',
        'ApprovedDocumentedAlternativeSampleIsNotBugProof',
        'SuccessfulSaveHasExactReadbackParity',
        'CollisionPreservesExistingReport',
        'AgentWriterFailureKeepsChatReport',
        'ExecutionReceiptMatchesToolsAndCleanup'
    )
    $permittedGates = @{
        ActorTrace = @(
            'NoApprovalOrDeniedApprovalPerformsZeroExecution',
            'ReporterApprovalDoesNotAuthorizeExecution',
            'MaterialCommandChangeRequiresReapproval',
            'SensitiveStopNeverTransitionsToExecution',
            'UnknownThirdPartyTriggerRequestsCleanRepro',
            'ApprovedDocumentedAlternativeSampleIsNotBugProof',
            'SuccessfulSaveHasExactReadbackParity',
            'CollisionPreservesExistingReport',
            'AgentWriterFailureKeepsChatReport'
        )
        ExecutionReceipt = @('ExecutionReceiptMatchesToolsAndCleanup')
    }
    $coveredEffects = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $completeEffectEvidence = $true
    $seenActions = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($effectPath in $EffectAssessment) {
        $resolvedEffect = (Resolve-Path $effectPath).Path
        $effect = Get-Content $resolvedEffect -Raw | ConvertFrom-Json -Depth 100
        if ($effect.schemaVersion -ne 1 -or $effect.status -cnotin @('passed', 'partial')) {
            throw "Effect assessment '$resolvedEffect' has no accepted coverage."
        }
        $action = [string]$effect.action
        if ($action -cnotin @('ActorTrace', 'ExecutionReceipt')) {
            throw "Effect assessment '$resolvedEffect' has unknown action '$action'."
        }
        if (-not $seenActions.Add($action)) {
            throw "Effect action '$action' was supplied more than once."
        }
        if ($action -cin @('ActorTrace', 'ExecutionReceipt') -and
            ($effect.manifest -cne $manifestPath -or
                $effect.manifestHash -cne (Get-Sha256 $manifestPath))) {
            throw "$action effect assessment '$resolvedEffect' does not bind to this manifest."
        }
        $pendingEffectGates = if ($effect.PSObject.Properties['pendingGates']) {
            @($effect.pendingGates)
        } else {
            @()
        }
        if ($effect.status -cne 'passed' -or @($pendingEffectGates).Count -ne 0) {
            $completeEffectEvidence = $false
        }
        $reportedGates = @($effect.coveredGates) + @($pendingEffectGates)
        foreach ($gate in $reportedGates) {
            if ($gate -cnotin @($permittedGates[$action])) {
                throw "Effect action '$action' reports gate '$gate' outside its permitted set."
            }
        }
        if ($action -ceq 'ActorTrace') {
            $effectResults = if ($effect.PSObject.Properties['results']) {
                @($effect.results)
            } else {
                @()
            }
            $effectCellIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
            $duplicateEffectCell = $false
            foreach ($effectResult in $effectResults) {
                if (-not $effectCellIds.Add([string]$effectResult.cellId)) {
                    $duplicateEffectCell = $true
                }
            }
            if (@($effectResults).Count -ne $computedCells -or
                $duplicateEffectCell -or
                $effectCellIds.Count -ne $computedCells -or
                @($cells.cellId | Where-Object { -not $effectCellIds.Contains($_) }).Count -ne 0 -or
                @($effectResults | Where-Object state -CNE 'passed').Count -ne 0) {
                $completeEffectEvidence = $false
            }
        }
        foreach ($gate in @($effect.coveredGates)) {
            $coveredEffects.Add([string]$gate) | Out-Null
        }
    }
    $missingEffects = @($requiredEffectGates | Where-Object { -not $coveredEffects.Contains($_) })
    $allRequiredActionsSeen = @($permittedGates.Keys | Where-Object {
        -not $seenActions.Contains($_)
    }).Count -eq 0
    if ($missingEffects.Count -eq 0 -and $completeEffectEvidence -and $allRequiredActionsSeen) {
        $runtimeAcceptance = 'passed: all required effect gates'
    } else {
        $incomplete = if ($completeEffectEvidence -and $allRequiredActionsSeen) {
            ''
        } else {
            '; incomplete assessment/cell evidence remains'
        }
        $runtimeAcceptance = "partially-assessed: covered [$(@($coveredEffects) -join ', ')]; pending [$($missingEffects -join ', ')]$incomplete"
    }
}
$assessmentRoot = Join-Path (Split-Path $manifestPath -Parent) 'assessment'
if (Test-Path $assessmentRoot) {
    throw "Assessment destination '$assessmentRoot' already exists."
}
New-Item -ItemType Directory -Path $assessmentRoot | Out-Null
[ordered]@{
    schemaVersion = 1
    manifest = $manifestPath
    expectedCells = $computedCells
    expectedPairs = $computedPairs
    skilledMean = $skilledMean
    skilledThreshold = [double]$data.skilledThreshold
    cohorts = $cohorts
    runtimeAcceptance = $runtimeAcceptance
} | ConvertTo-Json -Depth 20 | Set-Content (Join-Path $assessmentRoot 'assessment.json') -Encoding utf8NoBOM
$validated.Values | ConvertTo-Json -Depth 20 |
    Set-Content (Join-Path $assessmentRoot 'pairing-ledger.json') -Encoding utf8NoBOM

Write-Host "Validated $($cells.Count) cells and $($pairs.Count) pairs. Selection-wide skilled mean: $skilledMean (threshold: $($data.skilledThreshold))."
