#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$assertResults = Join-Path $PSScriptRoot 'assert_results.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-skill-eval-results-test-' + [guid]::NewGuid().ToString('N')
)
$output = Join-Path $testRoot 'output'
$run = Join-Path $output 'run'
$failures = [Collections.Generic.List[string]]::new()

function Write-Results {
    param(
        [ValidateSet('baseline', 'skilled')]
        [string]$Variant,

        [double]$Score,

        [string]$Status = 'success',

        [switch]$OmitScore
    )

    $variantDirectory = Join-Path $run $Variant
    New-Item -ItemType Directory -Path $variantDirectory -Force | Out-Null
    $result = [ordered]@{
        type = 'trial-result'
        variant = $Variant
        status = $Status
        stimulus = 'example'
        itemId = "$Variant-example-0"
    }
    if (-not $OmitScore) {
        $result.gradeResult = @{ score = $Score }
    }
    $result |
        ConvertTo-Json -Compress |
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
            $failures.Add("Expected failure containing '$ExpectedMessage'; got '$($_.Exception.Message)'.")
            return
        }
    }
    if (-not $threw) {
        $failures.Add($FailureMessage)
        Write-Host "  [FAIL] $FailureMessage"
    } else {
        Write-Host "  [OK] rejected: $ExpectedMessage"
    }
}

function New-ContractFixture {
    param([int]$Runs = 1)

    $stimuli = @(
        @{
            name = 'requires-output'
            graders = @(
                @{ type = 'output-matches'; config = @{ pattern = 'Required' } },
                @{ type = 'output-matches'; config = @{ pattern = 'Canary'; negate = $true } },
                @{ type = 'prompt' }
            )
        },
        @{ name = 'prompt-only'; graders = @(@{ type = 'prompt' }) }
    )
    $fixture = @{ snapshot = @{ evals = @() } }
    foreach ($variant in @('baseline', 'skilled')) {
        $fixture.snapshot.evals += @{
            variant = $variant
            plannedStimulusCount = $stimuli.Count
            runs = $Runs
            threshold = 0.6
            stimuli = $stimuli
        }
        $fixture[$variant] = @(
            foreach ($stimulus in $stimuli) {
                for ($trial = 0; $trial -lt $Runs; $trial++) {
                    $grade = @{ score = $(if ($variant -eq 'skilled') { 0.9 } else { 0.1 }) }
                    if ($stimulus.name -eq 'requires-output') {
                        $grade.details = @(
                            foreach ($grader in $stimulus.graders) {
                                @{
                                    graderType = $grader.type
                                    name = $grader.type
                                    passed = ($variant -eq 'skilled')
                                }
                            }
                        )
                    }
                    @{
                        type = 'trial-result'
                        variant = $variant
                        status = 'success'
                        stimulus = $stimulus.name
                        itemId = "$variant-$($stimulus.name)-$trial"
                        gradeResult = $grade
                    }
                }
            }
        )
    }
    return $fixture
}

function Write-ContractFixture {
    param([hashtable]$Fixture)

    $Fixture.snapshot | ConvertTo-Json -Depth 20 | Set-Content (Join-Path $run 'plan-snapshot.json')
    foreach ($variant in @('baseline', 'skilled')) {
        $directory = Join-Path $run $variant
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
        $Fixture[$variant] | ForEach-Object {
            $_ | ConvertTo-Json -Depth 20 -Compress
        } | Set-Content (Join-Path $directory 'results.jsonl')
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
                runs = 1
                threshold = 0.6
                stimuli = @(@{ name = 'example'; graders = @(@{ type = 'prompt' }) })
            },
            @{
                variant = 'skilled'
                plannedStimulusCount = 1
                runs = 1
                threshold = 0.6
                stimuli = @(@{ name = 'example'; graders = @(@{ type = 'prompt' }) })
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
    } "'skilled' score 0.5 is below" 'A below-threshold skilled result was accepted.'

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

    $fixture = New-ContractFixture -Runs 2
    Write-ContractFixture $fixture
    & $assertResults -OutputDirectory $output
    Write-Host '  [OK] multiple trials, failing baseline graders, and prompt-only results'

    $fixture = New-ContractFixture
    $details = $fixture.skilled[0].gradeResult.details
    $fixture.skilled[0].gradeResult.details = @($details[2], $details[1], $details[0])
    Write-ContractFixture $fixture
    & $assertResults -OutputDirectory $output
    Write-Host '  [OK] reordered graders with repeated display names'

    $cases = @(
        @{
            name = 'high-score positive match failure'
            mutate = { param($f) $f.skilled[0].gradeResult.details[0].passed = $false }
            message = "deterministic contract failures: requires-output"
        },
        @{
            name = 'high-score negated match failure'
            mutate = { param($f) $f.skilled[0].gradeResult.details[1].passed = $false }
            message = "deterministic contract failures: requires-output"
        },
        @{
            name = 'missing grader details'
            mutate = { param($f) $f.skilled[0].gradeResult.Remove('details') }
            message = 'grader coverage'
        },
        @{
            name = 'missing required grader'
            mutate = { param($f) $f.skilled[0].gradeResult.details = $f.skilled[0].gradeResult.details[1..2] }
            message = 'grader coverage'
        },
        @{
            name = 'extra duplicate grader'
            mutate = { param($f) $f.skilled[0].gradeResult.details += $f.skilled[0].gradeResult.details[0] }
            message = 'grader coverage'
        },
        @{
            name = 'wrong grader type with unchanged count'
            mutate = { param($f) $f.skilled[0].gradeResult.details[0].graderType = 'prompt' }
            message = 'grader coverage'
        },
        @{
            name = 'missing grader type'
            mutate = { param($f) $f.skilled[0].gradeResult.details[0].Remove('graderType') }
            message = 'grader coverage'
        },
        @{
            name = 'missing passed field'
            mutate = { param($f) $f.skilled[0].gradeResult.details[0].Remove('passed') }
            message = 'non-Boolean'
        },
        @{
            name = 'string-valued passed field'
            mutate = { param($f) $f.skilled[0].gradeResult.details[0].passed = 'true' }
            message = 'non-Boolean'
        },
        @{
            name = 'null passed field'
            mutate = { param($f) $f.skilled[0].gradeResult.details[0].passed = $null }
            message = 'non-Boolean'
        },
        @{
            name = 'duplicate stimulus hiding a missing trial'
            mutate = { param($f) $f.skilled[1].stimulus = 'requires-output' }
            message = 'trial coverage'
        },
        @{
            name = 'unplanned stimulus'
            mutate = { param($f) $f.skilled[1].stimulus = 'unknown' }
            message = 'unplanned stimulus'
        },
        @{
            name = 'missing stimulus identity'
            mutate = { param($f) $f.skilled[0].Remove('stimulus') }
            message = 'unplanned stimulus'
        },
        @{
            name = 'duplicate trial identity'
            mutate = { param($f) $f.skilled[1].itemId = $f.skilled[0].itemId }
            message = 'missing or duplicate trial identity'
        },
        @{
            name = 'missing trial identity'
            mutate = { param($f) $f.skilled[0].Remove('itemId') }
            message = 'missing or duplicate trial identity'
        },
        @{
            name = 'missing planned stimulus metadata'
            mutate = { param($f) $f.snapshot.evals[1].Remove('stimuli') }
            message = 'planned stimulus coverage'
        },
        @{
            name = 'duplicate planned stimulus'
            mutate = { param($f) $f.snapshot.evals[1].stimuli = @($f.snapshot.evals[1].stimuli[0], $f.snapshot.evals[1].stimuli[0]) }
            message = 'planned stimulus coverage'
        }
    )
    foreach ($case in $cases) {
        $fixture = New-ContractFixture
        & $case.mutate $fixture
        Write-ContractFixture $fixture
        Assert-Throws {
            & $assertResults -OutputDirectory $output
        } $case.message "$($case.name) was accepted."
    }

    if ($failures.Count -gt 0) {
        throw ($failures -join [Environment]::NewLine)
    }
    Write-Host 'Skill-eval result assertion self-test passed.'
} finally {
    Remove-Item -Recurse -Force $testRoot
}
