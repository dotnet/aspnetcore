#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$assertResults = Join-Path $PSScriptRoot 'assert_results.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-skill-eval-results-test-' + [guid]::NewGuid().ToString('N')
)
$output = Join-Path $testRoot 'output'
$run = Join-Path $output 'run'

function Write-Results {
    param(
        [ValidateSet('baseline', 'skilled')]
        [string]$Variant,

        [double]$Score,

        [string]$Status = 'success',

        [switch]$OmitScore,

        [object[]]$Details
    )

    $variantDirectory = Join-Path $run $Variant
    New-Item -ItemType Directory -Path $variantDirectory -Force | Out-Null
    $result = [ordered]@{
        type = 'trial-result'
        variant = $Variant
        stimulus = 'case-01'
        status = $Status
    }
    if (-not $OmitScore) {
        $result.gradeResult = @{ score = $Score }
        if ($PSBoundParameters.ContainsKey('Details')) {
            $result.gradeResult.details = $Details
        }
    }
    $result |
        ConvertTo-Json -Depth 10 -Compress |
        Set-Content (Join-Path $variantDirectory 'results.jsonl')
}

function Reset-Results {
    Remove-Item (Join-Path $run 'baseline') -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $run 'skilled') -Recurse -Force -ErrorAction SilentlyContinue
}

function Assert-Throws {
    param(
        [scriptblock]$Action,
        [string]$ExpectedMessage,
        [string]$FailureMessage
    )

    $threw = $false
    try {
        & $Action
    } catch {
        $threw = $true
        if ($_.Exception.Message -notlike "*$ExpectedMessage*") {
            throw "Expected failure containing '$ExpectedMessage'; got '$($_.Exception.Message)'."
        }
    }
    if (-not $threw) {
        throw $FailureMessage
    }
}

New-Item -ItemType Directory -Path $run -Force | Out-Null
try {
    @{
        type = 'experiment-plan-snapshot'
        evals = @(
            @{
                variant = 'baseline'
                plannedStimulusCount = 1
                plannedStimulusNames = @('case-01')
                runs = 1
                threshold = 0.6
            },
            @{
                variant = 'skilled'
                plannedStimulusCount = 1
                plannedStimulusNames = @('case-01')
                runs = 1
                threshold = 0.6
            }
        )
    } | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $run 'plan-snapshot.json')

    Write-Results -Variant baseline -Score 0.1
    Write-Results -Variant skilled -Score 0.8
    & $assertResults -OutputDirectory $output

    Reset-Results
    Write-Results -Variant skilled -Score 0.8
    Assert-Throws {
        & $assertResults -OutputDirectory $output
    } 'Missing Vally results' 'Missing baseline results were accepted.'

    Reset-Results
    Write-Results -Variant baseline -Score 0.1 -Status error
    Write-Results -Variant skilled -Score 0.8
    Assert-Throws {
        & $assertResults -OutputDirectory $output
    } "'baseline' variant has 1 incomplete" 'An incomplete baseline result was accepted.'

    Reset-Results
    Write-Results -Variant baseline -Score 0.1 -OmitScore
    Write-Results -Variant skilled -Score 0.8
    Assert-Throws {
        & $assertResults -OutputDirectory $output
    } "'baseline' variant has trial results without grader scores" (
        'A baseline result without a grader score was accepted.'
    )

    Reset-Results
    Write-Results -Variant baseline -Score 0.1
    Write-Results -Variant skilled -Score 0.5
    Assert-Throws {
        & $assertResults -OutputDirectory $output
    } "'skilled' stimulus 'case-01' score 0.5 is below" 'A below-threshold skilled result was accepted.'

    Reset-Results
    Write-Results -Variant baseline -Score 0.1
    Write-Results -Variant skilled -Score 0.8 -Status error
    Assert-Throws {
        & $assertResults -OutputDirectory $output
    } "'skilled' variant has 1 incomplete" 'An incomplete skilled result was accepted.'

    Reset-Results
    Write-Results -Variant baseline -Score 0.1
    Assert-Throws {
        & $assertResults -OutputDirectory $output
    } 'Missing Vally results' 'Missing skilled results were accepted.'

    Reset-Results
    function Write-ComponentResults {
        param(
            [scriptblock]$Mutation = {},
            [scriptblock]$CompletionMutation = {}
        )

        @{
            evals = @(@{
                variant = 'current'
                plannedStimulusNames = @('a', 'b', 'c')
                plannedStimulusCount = 3
                runs = 5
                threshold = 0.8
            })
        } | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $run 'plan-snapshot.json')
        $rows = [Collections.Generic.List[object]]::new()
        foreach ($name in 'a', 'b', 'c') {
            foreach ($trial in 0..4) {
                $rows.Add(@{
                    type = 'trial-result'
                    runId = 'component-run'
                    stimulus = $name
                    trialIndex = $trial
                    status = 'success'
                    gradeResult = @{
                        score = $(if ($trial -eq 4) { 0 } else { 1 })
                        details = @(
                            @{
                                configuredName = "evidence-reads-$name"
                                passed = $true
                                score = 1
                            },
                            @{
                                configuredName = "disposition-$name"
                                passed = $trial -ne 4
                                score = $(if ($trial -eq 4) { 0 } else { 1 })
                            }
                        )
                    }
                })
            }
        }
        & $Mutation $rows
        $completion = @{
            type = 'run-summary'
            runId = 'component-run'
            hadExecutionErrors = $false
        }
        & $CompletionMutation $completion
        $rows.Add($completion)
        $rows | ForEach-Object { $_ | ConvertTo-Json -Depth 10 -Compress } |
            Set-Content (Join-Path $run 'results.jsonl')
    }

    Write-ComponentResults
    & $assertResults -OutputDirectory $output
    $controls = @(
        @{ Name = 'AverageCannotMaskOneWeakCase'; Expected = "stimulus 'a' score 0.6"; Mutation = {
            param($rows)
            $rows[3].gradeResult.score = 0
            $rows[3].gradeResult.details[1].score = 0
            foreach ($row in $rows | Where-Object stimulus -ne 'a') {
                $row.gradeResult.score = 1
                $row.gradeResult.details[1].score = 1
            }
        } }
        @{ Name = 'DuplicateTrial'; Expected = 'duplicate trial identity'; Mutation = {
            param($rows)
            $rows[1].trialIndex = 0
        } }
        @{ Name = 'MissingTrial'; Expected = 'Expected 15'; Mutation = {
            param($rows)
            $rows.RemoveAt(0)
        } }
        @{ Name = 'SubstitutedStimulus'; Expected = 'unplanned or missing stimulus'; Mutation = {
            param($rows)
            $rows[0].stimulus = 'other'
        } }
        @{ Name = 'OutOfRangeTrial'; Expected = 'invalid or duplicate trial identity'; Mutation = {
            param($rows)
            $rows[0].trialIndex = 5
        } }
        @{ Name = 'FailedTrial'; Expected = 'incomplete or failed'; Mutation = {
            param($rows)
            $rows[0].status = 'error'
        } }
        @{ Name = 'StringScore'; Expected = 'invalid grader score'; Mutation = {
            param($rows)
            $rows[0].gradeResult.score = '1'
        } }
        @{ Name = 'NonbinaryComponentScore'; Expected = 'must be binary'; Mutation = {
            param($rows)
            $rows[0].gradeResult.score = 0.9
        } }
        @{ Name = 'MissingReadIntegrity'; Expected = 'lacks read-integrity'; Mutation = {
            param($rows)
            $rows[0].gradeResult.Remove('details')
        } }
        @{ Name = 'FailedReadIntegrity'; Expected = 'failed read integrity'; Mutation = {
            param($rows)
            $rows[0].gradeResult.details[0].passed = $false
        } }
        @{ Name = 'MissingReceipt'; Expected = 'failed read integrity'; Mutation = {
            param($rows)
            $rows[0].gradeResult.details = @($rows[0].gradeResult.details[1])
        } }
        @{ Name = 'DuplicateReceipt'; Expected = 'failed read integrity'; Mutation = {
            param($rows)
            $rows[0].gradeResult.details += $rows[0].gradeResult.details[0]
        } }
        @{ Name = 'DispositionGraderError'; Expected = 'valid primary disposition'; Mutation = {
            param($rows)
            $rows[0].gradeResult.details[1].status = 'error'
        } }
        @{ Name = 'SummaryIsNotATrial'; Expected = 'completion summary'; Mutation = {
            param($rows)
            $rows[0].type = 'run-summary'
        } }
    )
    foreach ($control in $controls) {
        Write-ComponentResults $control.Mutation
        Assert-Throws { & $assertResults -OutputDirectory $output } $control.Expected $control.Name
    }
    Write-ComponentResults -CompletionMutation {
        param($completion)
        $completion.hadExecutionErrors = $true
    }
    Assert-Throws { & $assertResults -OutputDirectory $output } 'completion summary' (
        'A component run with execution errors was accepted.'
    )
    Write-ComponentResults -CompletionMutation {
        param($completion)
        $completion.runId = 'other-run'
    }
    Assert-Throws { & $assertResults -OutputDirectory $output } 'unplanned or missing stimulus' (
        'Component trials from another run were accepted.'
    )
    Write-ComponentResults
    $componentLines = @(Get-Content (Join-Path $run 'results.jsonl'))
    $componentLines[0..13] + $componentLines[-1] |
        Set-Content (Join-Path $run 'results.jsonl')
    Assert-Throws { & $assertResults -OutputDirectory $output } "15 'current' trial results; found 14" (
        'The terminal summary replaced a missing component trial.'
    )
    $componentLines[0..14] | Set-Content (Join-Path $run 'results.jsonl')
    Assert-Throws { & $assertResults -OutputDirectory $output } 'completion summary' (
        'A component run without a terminal summary was accepted.'
    )
    @($componentLines[-1]) + $componentLines[0..14] |
        Set-Content (Join-Path $run 'results.jsonl')
    Assert-Throws { & $assertResults -OutputDirectory $output } 'completion summary' (
        'A nonterminal component summary was accepted.'
    )

    function Write-ReviewResults {
        param([scriptblock]$Mutation = {})

        @{
            evals = @(
                foreach ($variant in 'baseline', 'skilled') {
                    @{
                        evalName = 'review-pull-request'
                        variant = $variant
                        plannedStimulusNames = @('case-01')
                        plannedStimulusCount = 1
                        runs = 1
                        threshold = 1
                    }
                }
            )
        } | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $run 'plan-snapshot.json')
        foreach ($variant in 'baseline', 'skilled') {
            $receipts = @(
                foreach ($name in 'evidence-reads', 'guidance-reads', 'treatment-reads') {
                    @{
                        configuredName = "$name-case-01"
                        passed = $name -ne 'treatment-reads' -or $variant -eq 'skilled'
                        score = $(if ($name -eq 'treatment-reads' -and $variant -eq 'baseline') { 0 } else { 1 })
                    }
                }
                @{
                    configuredName = 'disposition-case-01'
                    passed = $true
                    score = 1
                }
            )
            & $Mutation $variant $receipts
            Write-Results -Variant $variant -Score 1 -Details $receipts
        }
    }

    Write-ReviewResults
    & $assertResults -OutputDirectory $output
    foreach ($variantToFail in 'baseline', 'skilled') {
        foreach ($receiptToFail in 'evidence-reads', 'guidance-reads') {
            Write-ReviewResults {
                param($variant, $receipts)
                if ($variant -eq $variantToFail) {
                    ($receipts | Where-Object configuredName -eq "$receiptToFail-case-01").passed = $false
                }
            }
            Assert-Throws { & $assertResults -OutputDirectory $output } 'failed read integrity' (
                "$variantToFail accepted missing $receiptToFail integrity."
            )
        }
    }
    Write-ReviewResults {
        param($variant, $receipts)
        if ($variant -eq 'skilled') {
            ($receipts | Where-Object configuredName -eq 'treatment-reads-case-01').passed = $false
        }
    }
    Assert-Throws { & $assertResults -OutputDirectory $output } 'treatment-reads-case-01' (
        'Skilled accepted missing treatment integrity.'
    )

    Write-ReviewResults
    $combinedOutput = Join-Path $testRoot 'combined'
    New-Item -ItemType Directory -Path $combinedOutput | Out-Null
    Copy-Item $output (Join-Path $combinedOutput 'review-pull-request') -Recurse
    Write-ComponentResults
    Copy-Item $output (Join-Path $combinedOutput 'review-candidate-verifier') -Recurse
    & $assertResults -OutputDirectory $combinedOutput

    Write-Host 'Skill-eval result assertion self-test passed.'
} finally {
    Remove-Item -Recurse -Force $testRoot
}
