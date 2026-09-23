#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$checker = Join-Path $PSScriptRoot 'assert_investigate_issue_run.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-investigate-checker-test-' + [guid]::NewGuid().ToString('N')
)

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

function New-Fixture {
    param(
        [string]$Name,
        [scriptblock]$Mutate
    )

    $root = Join-Path $testRoot $Name
    New-Item -ItemType Directory -Path $root | Out-Null
    $cells = [Collections.Generic.List[object]]::new()
    $scores = @{
        'low-r0-skilled' = 0.5
        'high-r0-skilled' = 0.9
        'low-r0-baseline' = 0.1
        'high-r0-baseline' = 0.2
    }
    foreach ($case in @(
        @{ name = 'low'; cohort = 'original-21' },
        @{ name = 'high'; cohort = 'javier' }
    )) {
        foreach ($variant in @('baseline', 'skilled')) {
            $cellId = "$($case.name)-r0-$variant"
            $cellRoot = Join-Path $root "cells/$cellId"
            $outputRoot = Join-Path $cellRoot 'output'
            $runRoot = Join-Path $outputRoot 'native-run'
            $resultRoot = Join-Path $runRoot $variant
            New-Item -ItemType Directory -Path $resultRoot -Force | Out-Null
            $evalPath = Join-Path $cellRoot 'eval.yaml'
            $experimentPath = Join-Path $cellRoot 'experiment.yaml'
            Set-Content $evalPath "name: $($case.name)" -Encoding utf8NoBOM
            Set-Content $experimentPath 'name: experiment' -Encoding utf8NoBOM
            $graders = @(
                @{ type = 'output-matches' },
                @{ type = 'prompt' }
            )
            $snapshotPath = Join-Path $runRoot 'plan-snapshot.json'
            @{
                evals = @(
                    @{
                        variant = $variant
                        runs = 1
                        plannedStimulusCount = 1
                        model = 'actor-model'
                        judgeModel = 'judge-model'
                        executorName = 'copilot-sdk'
                        argvHash = "argv-$cellId"
                        stimuli = @(@{ name = $case.name; graders = $graders })
                    }
                )
            } | ConvertTo-Json -Depth 20 |
                Set-Content $snapshotPath -Encoding utf8NoBOM
            $details = @(
                @{ graderType = 'output-matches'; passed = $true },
                @{ graderType = 'prompt'; passed = $true }
            )
            $resultsPath = Join-Path $resultRoot 'results.jsonl'
            @{
                type = 'trial-result'
                variant = $variant
                status = 'success'
                stimulus = $case.name
                evalName = 'investigate-issue'
                model = 'actor-model'
                itemId = 'trial-0'
                gradeResult = @{
                    score = $scores[$cellId]
                    details = $details
                }
            } | ConvertTo-Json -Depth 20 -Compress |
                Set-Content $resultsPath -Encoding utf8NoBOM
            @{
                cellId = $cellId
                state = 'complete'
                exitCode = $(if ($cellId -eq 'low-r0-baseline') { 1 } else { 0 })
                structuredComplete = $true
                snapshotPath = (Resolve-Path $snapshotPath).Path
                snapshotHash = (Get-FileHash $snapshotPath -Algorithm SHA256).Hash.ToLowerInvariant()
                resultsPath = (Resolve-Path $resultsPath).Path
                resultsHash = (Get-FileHash $resultsPath -Algorithm SHA256).Hash.ToLowerInvariant()
            } | ConvertTo-Json |
                Set-Content (Join-Path $cellRoot 'receipt.json') -Encoding utf8NoBOM
            $cells.Add([ordered]@{
                cellId = $cellId
                pairId = "$($case.name)-r0"
                stimulus = $case.name
                cohort = $case.cohort
                variant = $variant
                repetition = 0
                storageCase = 'none'
                destination = $null
                fixtureHashes = @{}
                outputRoot = $outputRoot
                evalPath = $evalPath
                experimentPath = $experimentPath
                evalHash = (Get-FileHash $evalPath -Algorithm SHA256).Hash.ToLowerInvariant()
                experimentHash = (Get-FileHash $experimentPath -Algorithm SHA256).Hash.ToLowerInvariant()
                canonicalInputHash = "canonical-$($case.name)"
                pairNormalizedHash = "normalized-$($case.name)"
                graderHash = "grader-$($case.name)"
                scoringHash = "scoring-$($case.name)"
                judgeConfigHash = "judge-$($case.name)"
                resolvedConfigHash = "config-$($case.name)-$variant"
                frozenProjectionHash = "projection-$($case.name)-$variant"
                projectionPath = $evalPath
                projectionHash = (Get-FileHash $evalPath -Algorithm SHA256).Hash.ToLowerInvariant()
                backendName = 'local'
                executorName = 'copilot-sdk'
                expectedPlans = 1
                expectedResults = 1
                expectedVariant = $variant
                expectedStimulus = $case.name
                expectedModel = 'actor-model'
                expectedJudgeModel = 'judge-model'
                expectedEvalName = 'investigate-issue'
            })
        }
    }
    $cases = @(
        foreach ($case in @(
            @{ name = 'low'; cohort = 'original-21' },
            @{ name = 'high'; cohort = 'javier' }
        )) {
            $projections = [ordered]@{}
            $projections["$($case.name)-r0-baseline"] = [ordered]@{
                resolvedConfigHash = "config-$($case.name)-baseline"
                frozenProjectionHash = "projection-$($case.name)-baseline"
                backendName = 'local'
                executorName = 'copilot-sdk'
                graderHash = "grader-$($case.name)"
                scoringHash = "scoring-$($case.name)"
                judgeConfigHash = "judge-$($case.name)"
            }
            $projections["$($case.name)-r0-skilled"] = [ordered]@{
                resolvedConfigHash = "config-$($case.name)-skilled"
                frozenProjectionHash = "projection-$($case.name)-skilled"
                backendName = 'local'
                executorName = 'copilot-sdk'
                graderHash = "grader-$($case.name)"
                scoringHash = "scoring-$($case.name)"
                judgeConfigHash = "judge-$($case.name)"
            }
            [ordered]@{
                name = $case.name
                cohort = $case.cohort
                canonicalInputHash = "canonical-$($case.name)"
                graderHash = "grader-$($case.name)"
                scoringHash = "scoring-$($case.name)"
                judgeConfigHash = "judge-$($case.name)"
                runs = 1
                projections = $projections
            }
        }
    )
    $manifest = [ordered]@{
        schemaVersion = 1
        skilledThreshold = 0.6
        expectedCells = 4
        expectedPairs = 2
        cases = $cases
        cells = $cells
    }
    if ($Mutate) {
        & $Mutate $manifest
    }
    $manifestPath = Join-Path $root 'manifest.json'
    $manifest | ConvertTo-Json -Depth 30 | Set-Content $manifestPath -Encoding utf8NoBOM
    return $manifestPath
}

function Assert-Rejected {
    param([string]$Name, [scriptblock]$Mutate, [string]$Expected)

    $path = New-Fixture $Name $Mutate
    $rejected = $false
    try {
        & $checker -Manifest $path
    } catch {
        $rejected = [string]::IsNullOrEmpty($Expected) -or $_.Exception.Message -like "*$Expected*"
    }
    Assert-True $rejected "$Name was not rejected with '$Expected'."
    Write-Host "  [OK] rejected $Name"
}

New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    $passing = New-Fixture 'passing' $null
    & $checker -Manifest $passing
    $assessment = Get-Content (Join-Path (Split-Path $passing -Parent) 'assessment/assessment.json') -Raw |
        ConvertFrom-Json
    Assert-True ([Math]::Abs([double]$assessment.skilledMean - 0.7) -lt 0.00001) (
        'Selection-wide skilled mean was not 0.7.'
    )
    Assert-True ([double]$assessment.cohorts.'original-21'.mean -eq 0.5) (
        'Lower cohort mean was not reported.'
    )
    Assert-True ($assessment.runtimeAcceptance -ceq 'not-assessed-by-structural-checker') (
        "Structural-only assessment overstated runtime acceptance: '$($assessment.runtimeAcceptance)'."
    )
    Write-Host '  [OK] MixedCohortMeansPreserveSelectionWideThreshold'
    Write-Host '  [OK] NonzeroGradeExitPreservesCompleteBaselinePair'

    $effectBound = New-Fixture 'effect-bound' $null
    $effectAssessment = Join-Path (Split-Path $effectBound -Parent) 'actor-effect.json'
    [ordered]@{
        schemaVersion = 1
        action = 'ActorTrace'
        manifest = (Resolve-Path $effectBound).Path
        manifestHash = (Get-FileHash $effectBound -Algorithm SHA256).Hash.ToLowerInvariant()
        status = 'partial'
        coveredGates = @('SuccessfulSaveHasExactReadbackParity')
    } | ConvertTo-Json | Set-Content $effectAssessment -Encoding utf8NoBOM
    & $checker -Manifest $effectBound -EffectAssessment $effectAssessment
    $effectBoundResult = Get-Content (
        Join-Path (Split-Path $effectBound -Parent) 'assessment/assessment.json'
    ) -Raw | ConvertFrom-Json
    Assert-True (
        $effectBoundResult.runtimeAcceptance.StartsWith(
            'partially-assessed: covered [SuccessfulSaveHasExactReadbackParity]',
            [StringComparison]::Ordinal
        )
    ) (
        "Partial actor effects were overstated or not bound into runtime acceptance: '$($effectBoundResult.runtimeAcceptance)'."
    )
    Write-Host '  [OK] StructuralCheckerRequiresBoundEffectAssessment'

    $partialRepetitions = New-Fixture 'partial-repetitions' $null
    $actorEffectGates = @(
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
    $partialEffect = Join-Path (Split-Path $partialRepetitions -Parent) 'partial-repetitions-effect.json'
    $partialManifest = Get-Content $partialRepetitions -Raw | ConvertFrom-Json -Depth 100
    [ordered]@{
        schemaVersion = 1
        action = 'ActorTrace'
        manifest = (Resolve-Path $partialRepetitions).Path
        manifestHash = (Get-FileHash $partialRepetitions -Algorithm SHA256).Hash.ToLowerInvariant()
        status = 'partial'
        coveredGates = $actorEffectGates
        pendingGates = @()
        results = @(
            for ($index = 0; $index -lt $partialManifest.cells.Count; $index++) {
                [ordered]@{
                    cellId = $partialManifest.cells[$index].cellId
                    state = if ($index -eq 0) { 'not-exercised' } else { 'passed' }
                }
            }
        )
    } | ConvertTo-Json -Depth 20 | Set-Content $partialEffect -Encoding utf8NoBOM
    $partialExecution = Join-Path (Split-Path $partialRepetitions -Parent) 'execution-effect.json'
    [ordered]@{
        schemaVersion = 1
        action = 'ExecutionReceipt'
        manifest = (Resolve-Path $partialRepetitions).Path
        manifestHash = (Get-FileHash $partialRepetitions -Algorithm SHA256).Hash.ToLowerInvariant()
        status = 'passed'
        coveredGates = @('ExecutionReceiptMatchesToolsAndCleanup')
        pendingGates = @()
    } | ConvertTo-Json | Set-Content $partialExecution -Encoding utf8NoBOM
    & $checker -Manifest $partialRepetitions -EffectAssessment @(
        $partialEffect, $partialExecution
    )
    $partialResult = Get-Content (
        Join-Path (Split-Path $partialRepetitions -Parent) 'assessment/assessment.json'
    ) -Raw | ConvertFrom-Json
    Assert-True (
        $partialResult.runtimeAcceptance.StartsWith('partially-assessed:', [StringComparison]::Ordinal) -and
        $partialResult.runtimeAcceptance.Contains(
            'incomplete assessment/cell evidence remains',
            [StringComparison]::Ordinal
        )
    ) 'One exercised result promoted other not-exercised cells to full runtime acceptance.'
    Write-Host '  [OK] PartialRepetitionsCannotBecomeFullRuntimeAcceptance'

    $crossManifest = New-Fixture 'cross-manifest-execution' $null
    $crossExecution = Join-Path (Split-Path $crossManifest -Parent) 'cross-execution.json'
    [ordered]@{
        schemaVersion = 1
        action = 'ExecutionReceipt'
        manifest = '/different/manifest.json'
        manifestHash = 'different'
        status = 'passed'
        coveredGates = @('ExecutionReceiptMatchesToolsAndCleanup')
        pendingGates = @()
    } | ConvertTo-Json | Set-Content $crossExecution -Encoding utf8NoBOM
    $rejected = $false
    try {
        & $checker -Manifest $crossManifest -EffectAssessment $crossExecution
    } catch {
        $rejected = $_.Exception.Message -like '*ExecutionReceipt*does not bind*'
    }
    Assert-True $rejected 'A cross-manifest ExecutionReceipt was accepted.'

    $wrongAction = New-Fixture 'wrong-action-gate' $null
    $wrongActionEffect = Join-Path (Split-Path $wrongAction -Parent) 'wrong-action.json'
    [ordered]@{
        schemaVersion = 1
        action = 'ExecutionReceipt'
        manifest = (Resolve-Path $wrongAction).Path
        manifestHash = (Get-FileHash $wrongAction -Algorithm SHA256).Hash.ToLowerInvariant()
        status = 'passed'
        coveredGates = @('SuccessfulSaveHasExactReadbackParity')
        pendingGates = @()
    } | ConvertTo-Json | Set-Content $wrongActionEffect -Encoding utf8NoBOM
    $rejected = $false
    try {
        & $checker -Manifest $wrongAction -EffectAssessment $wrongActionEffect
    } catch {
        $rejected = $_.Exception.Message -like '*outside its permitted set*'
    }
    Assert-True $rejected 'A gate assigned to the wrong effect action was accepted.'

    $unknownAction = New-Fixture 'unknown-effect-action' $null
    $unknownActionEffect = Join-Path (Split-Path $unknownAction -Parent) 'unknown-action.json'
    [ordered]@{
        schemaVersion = 1
        action = 'OtherProbe'
        status = 'passed'
        coveredGates = @()
        pendingGates = @()
    } | ConvertTo-Json | Set-Content $unknownActionEffect -Encoding utf8NoBOM
    $rejected = $false
    try {
        & $checker -Manifest $unknownAction -EffectAssessment $unknownActionEffect
    } catch {
        $rejected = $_.Exception.Message -like '*unknown action*'
    }
    Assert-True $rejected 'An unknown effect action was accepted.'

    $noncanonicalAction = New-Fixture 'noncanonical-effect-action' $null
    $noncanonicalActionEffect = Join-Path (
        Split-Path $noncanonicalAction -Parent
    ) 'noncanonical-action.json'
    [ordered]@{
        schemaVersion = 1
        action = 'actortrace'
        status = 'passed'
        coveredGates = @()
        pendingGates = @()
    } | ConvertTo-Json | Set-Content $noncanonicalActionEffect -Encoding utf8NoBOM
    $rejected = $false
    try {
        & $checker -Manifest $noncanonicalAction -EffectAssessment $noncanonicalActionEffect
    } catch {
        $rejected = $_.Exception.Message -like '*unknown action*'
    }
    Assert-True $rejected 'A noncanonical effect action was accepted.'

    $wrongCaseStatus = New-Fixture 'wrong-case-effect-status' $null
    $wrongCaseStatusEffect = Join-Path (
        Split-Path $wrongCaseStatus -Parent
    ) 'wrong-case-status.json'
    [ordered]@{
        schemaVersion = 1
        action = 'ExecutionReceipt'
        manifest = (Resolve-Path $wrongCaseStatus).Path
        manifestHash = (Get-FileHash $wrongCaseStatus -Algorithm SHA256).Hash.ToLowerInvariant()
        status = 'Passed'
        coveredGates = @()
        pendingGates = @()
    } | ConvertTo-Json | Set-Content $wrongCaseStatusEffect -Encoding utf8NoBOM
    $rejected = $false
    try {
        & $checker -Manifest $wrongCaseStatus -EffectAssessment $wrongCaseStatusEffect
    } catch {
        $rejected = $_.Exception.Message -like '*no accepted coverage*'
    }
    Assert-True $rejected 'A noncanonical effect status was accepted.'

    $duplicateAction = New-Fixture 'duplicate-effect-action' $null
    $duplicateExecutionA = Join-Path (Split-Path $duplicateAction -Parent) 'execution-a.json'
    $duplicateExecutionB = Join-Path (Split-Path $duplicateAction -Parent) 'execution-b.json'
    foreach ($path in @($duplicateExecutionA, $duplicateExecutionB)) {
        [ordered]@{
            schemaVersion = 1
            action = 'ExecutionReceipt'
            manifest = (Resolve-Path $duplicateAction).Path
            manifestHash = (Get-FileHash $duplicateAction -Algorithm SHA256).Hash.ToLowerInvariant()
            status = 'passed'
            coveredGates = @('ExecutionReceiptMatchesToolsAndCleanup')
            pendingGates = @()
        } | ConvertTo-Json | Set-Content $path -Encoding utf8NoBOM
    }
    $rejected = $false
    try {
        & $checker -Manifest $duplicateAction -EffectAssessment @(
            $duplicateExecutionA,
            $duplicateExecutionB
        )
    } catch {
        $rejected = $_.Exception.Message -like '*supplied more than once*'
    }
    Assert-True $rejected 'Duplicate singleton effect assessments were accepted.'

    $completeActions = New-Fixture 'complete-actions' $null
    $completeRoot = Split-Path $completeActions -Parent
    $completeManifest = Get-Content $completeActions -Raw | ConvertFrom-Json -Depth 100
    $completeHash = (Get-FileHash $completeActions -Algorithm SHA256).Hash.ToLowerInvariant()
    $completeActor = Join-Path $completeRoot 'complete-actor.json'
    [ordered]@{
        schemaVersion = 1
        action = 'ActorTrace'
        manifest = (Resolve-Path $completeActions).Path
        manifestHash = $completeHash
        status = 'passed'
        coveredGates = $actorEffectGates
        pendingGates = @()
        results = @(
            $completeManifest.cells | ForEach-Object {
                [ordered]@{ cellId = $_.cellId; state = 'passed' }
            }
        )
    } | ConvertTo-Json -Depth 20 | Set-Content $completeActor -Encoding utf8NoBOM
    $completeExecution = Join-Path $completeRoot 'complete-execution.json'
    [ordered]@{
        schemaVersion = 1
        action = 'ExecutionReceipt'
        manifest = (Resolve-Path $completeActions).Path
        manifestHash = $completeHash
        status = 'passed'
        coveredGates = @('ExecutionReceiptMatchesToolsAndCleanup')
        pendingGates = @()
    } | ConvertTo-Json | Set-Content $completeExecution -Encoding utf8NoBOM
    & $checker -Manifest $completeActions -EffectAssessment @(
        $completeActor, $completeExecution
    )
    $completeResult = Get-Content (Join-Path $completeRoot 'assessment/assessment.json') -Raw |
        ConvertFrom-Json
    Assert-True (
        $completeResult.runtimeAcceptance -ceq 'passed: all required effect gates'
    ) 'A valid complete action/gate set could not reach full runtime acceptance.'
    Write-Host '  [OK] EffectActionsAreBoundSingletonAndGateScoped'
    Write-Host '  [OK] CompleteActionGateSetCanPass'

    Assert-Rejected 'missing-cell' {
        param($m)
        $m.cells = @($m.cells)[0..2]
    } 'require 4 cells'
    Assert-Rejected 'missing-whole-pair-with-rewritten-counts' {
        param($m)
        $m.cells = @($m.cells | Where-Object stimulus -CEQ 'low')
        $m.expectedCells = 2
        $m.expectedPairs = 1
    } 'counters do not match'
    Assert-Rejected 'duplicate-cell' {
        param($m)
        $m.cells[1].cellId = $m.cells[0].cellId
    } 'Duplicate cell ID'
    Assert-Rejected 'wrong-model' {
        param($m)
        $result = Join-Path $m.cells[0].outputRoot 'native-run/baseline/results.jsonl'
        $row = Get-Content $result -Raw | ConvertFrom-Json
        $row.model = 'wrong-model'
        $row | ConvertTo-Json -Depth 20 -Compress | Set-Content $result -Encoding utf8NoBOM
        $receiptPath = Join-Path (Split-Path $m.cells[0].outputRoot -Parent) 'receipt.json'
        $receipt = Get-Content $receiptPath -Raw | ConvertFrom-Json
        $receipt.resultsHash = (Get-FileHash $result -Algorithm SHA256).Hash.ToLowerInvariant()
        $receipt | ConvertTo-Json | Set-Content $receiptPath -Encoding utf8NoBOM
    } 'native result identity mismatch'
    Assert-Rejected 'mismatched-pair' {
        param($m)
        $m.cells[1].graderHash = 'different'
    } "frozen case 'graderHash'"
    Assert-Rejected 'unplanned-stimulus' {
        param($m)
        $m.cells[0].cellId = 'other-r0-baseline'
        $m.cells[0].stimulus = 'other'
        $m.cells[0].expectedStimulus = 'other'
    } 'not planned'
    Assert-Rejected 'wrong-repetition' {
        param($m)
        $m.cells[0].repetition = 1
    } 'unplanned repetition'
    Assert-Rejected 'mismatched-case-cohort' {
        param($m)
        $m.cells[0].cohort = 'wrong'
    } 'unplanned cohort'
    Assert-Rejected 'frozen-grader-config-mismatch' {
        param($m)
        $m.cells[0].resolvedConfigHash = 'rewritten'
    } "frozen baseline projection 'resolvedConfigHash'"
    Assert-Rejected 'deterministic-failure' {
        param($m)
        $result = Join-Path $m.cells[1].outputRoot 'native-run/skilled/results.jsonl'
        $row = Get-Content $result -Raw | ConvertFrom-Json
        $row.gradeResult.details[0].passed = $false
        $row | ConvertTo-Json -Depth 20 -Compress | Set-Content $result -Encoding utf8NoBOM
        $receiptPath = Join-Path (Split-Path $m.cells[1].outputRoot -Parent) 'receipt.json'
        $receipt = Get-Content $receiptPath -Raw | ConvertFrom-Json
        $receipt.resultsHash = (Get-FileHash $result -Algorithm SHA256).Hash.ToLowerInvariant()
        $receipt | ConvertTo-Json | Set-Content $receiptPath -Encoding utf8NoBOM
    } 'deterministic contract failures'
    Assert-Rejected 'reused-output' {
        param($m)
        $m.cells[2].outputRoot = $m.cells[0].outputRoot
        $m.cells[2].stimulus = $m.cells[0].stimulus
        $m.cells[2].expectedStimulus = $m.cells[0].expectedStimulus
    } ''
    Write-Host '  [OK] SplitCheckerRejectsMissingDuplicateOrMismatchedResults'
    Write-Host '  [OK] SplitCheckerPreservesGradeAndCohortRules'
    Write-Host 'Investigate-issue split-result checker self-test passed.'
} finally {
    Remove-Item $testRoot -Recurse -Force
}
