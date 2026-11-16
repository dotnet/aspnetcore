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

        [switch]$OmitScore
    )

    $variantDirectory = Join-Path $run $Variant
    New-Item -ItemType Directory -Path $variantDirectory -Force | Out-Null
    $result = [ordered]@{
        type = 'trial-result'
        variant = $Variant
        status = $Status
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
                runs = 1
                threshold = 0.6
            },
            @{
                variant = 'skilled'
                plannedStimulusCount = 1
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

    Write-Host 'Skill-eval result assertion self-test passed.'
} finally {
    Remove-Item -Recurse -Force $testRoot
}
