#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$assertResults = Join-Path $PSScriptRoot 'assert_results.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-skill-eval-results-test-' + [guid]::NewGuid().ToString('N')
)
$output = Join-Path $testRoot 'output'
$run = Join-Path $output 'run'
$skilled = Join-Path $run 'skilled'

function Write-Results {
    param(
        [double]$Score,
        [string]$Status = 'success'
    )

    @{
        type = 'trial-result'
        variant = 'skilled'
        status = $Status
        gradeResult = @{ score = $Score }
    } | ConvertTo-Json -Compress | Set-Content (Join-Path $skilled 'results.jsonl')
}

function Assert-Throws {
    param(
        [scriptblock]$Action,
        [string]$Message
    )

    $threw = $false
    try {
        & $Action
    } catch {
        $threw = $true
    }
    if (-not $threw) {
        throw $Message
    }
}

New-Item -ItemType Directory -Path $skilled | Out-Null
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

    Write-Results -Score 0.8
    & $assertResults -OutputDirectory $output

    Write-Results -Score 0.5
    Assert-Throws {
        & $assertResults -OutputDirectory $output
    } 'A below-threshold skilled result was accepted.'

    Write-Results -Score 0.8 -Status error
    Assert-Throws {
        & $assertResults -OutputDirectory $output
    } 'An incomplete skilled result was accepted.'

    Remove-Item (Join-Path $skilled 'results.jsonl')
    Assert-Throws {
        & $assertResults -OutputDirectory $output
    } 'Missing skilled results were accepted.'

    Write-Host 'Skill-eval result assertion self-test passed.'
} finally {
    Remove-Item -Recurse -Force $testRoot
}
