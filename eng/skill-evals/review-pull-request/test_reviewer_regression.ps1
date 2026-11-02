#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

& (Join-Path $PSScriptRoot 'check_reviewer_regression.ps1') -Quiet

$temp = Join-Path ([IO.Path]::GetTempPath()) "reviewer-regression-$([guid]::NewGuid().ToString('N'))"
New-Item $temp -ItemType Directory | Out-Null
try {
    $graderWorkspace = Join-Path $temp 'grader'
    New-Item (Join-Path $graderWorkspace '.reviewer-eval') -ItemType Directory -Force | Out-Null
    Copy-Item (
        Join-Path $PSScriptRoot 'reviewer-regression.labels.json'
    ) (Join-Path $graderWorkspace '.reviewer-eval/labels.json')
    $resultBlock = [ordered]@{
        schemaVersion = 1
        pr = 70106
        results = @(
            [ordered]@{ id = 'V21'; tier = 'DISCARDED'; surfaced = $false; summary = 'not supported' }
            [ordered]@{ id = 'V32'; tier = 'DISCARDED'; surfaced = $false; summary = 'report only' }
            [ordered]@{ id = 'V36'; tier = 'DISCARDED'; surfaced = $false; summary = 'report only' }
            [ordered]@{ id = 'L69'; tier = 'DISCARDED'; surfaced = $false; summary = 'not supported' }
        )
        novelFindings = @()
    }
    $output = "REVIEWER_REGRESSION_RESULT`n``````json`n$($resultBlock | ConvertTo-Json -Depth 20 -Compress)`n``````"
    $graderInput = [ordered]@{
        trajectory = [ordered]@{ output = $output }
        stimulus = [ordered]@{ name = 'reviewer-regression-pr-70106' }
    }
    $graderInputPath = Join-Path $graderWorkspace 'input.json'
    Set-Content $graderInputPath ($graderInput | ConvertTo-Json -Depth 20) -Encoding utf8NoBOM
    $env:EVALUATE_WORKSPACE = $graderWorkspace
    $env:EVALUATE_GRADER_INPUT = $graderInputPath
    try {
        $grade = & (Join-Path $PSScriptRoot 'grade_reviewer_regression.ps1') -PullRequest 70106 |
            ConvertFrom-Json -Depth 100
        if (-not $grade.passed) {
            throw 'Deterministic grader self-test expected a passing result.'
        }

        $resultBlock.results = @($resultBlock.results | Where-Object id -ne 'L69')
        $graderInput.trajectory.output = "REVIEWER_REGRESSION_RESULT`n``````json`n$($resultBlock | ConvertTo-Json -Depth 20 -Compress)`n``````"
        Set-Content $graderInputPath ($graderInput | ConvertTo-Json -Depth 20) -Encoding utf8NoBOM
        $grade = & (Join-Path $PSScriptRoot 'grade_reviewer_regression.ps1') -PullRequest 70106 |
            ConvertFrom-Json -Depth 100
        if ($grade.passed) {
            throw 'Deterministic grader self-test expected a missing required row to fail.'
        }
    } finally {
        Remove-Item Env:EVALUATE_WORKSPACE, Env:EVALUATE_GRADER_INPUT
    }

    $stimuli = @('reviewer-regression-pr-70106', 'reviewer-regression-pr-69782')
    $records = @(
        foreach ($variant in 'current', 'candidate') {
            foreach ($stimulus in $stimuli) {
                for ($run = 1; $run -le 5; $run++) {
                    [ordered]@{
                        type = 'trial-result'
                        status = 'success'
                        variant = $variant
                        stimulus = $stimulus
                        gradeResult = [ordered]@{
                            details = @(
                                [ordered]@{
                                    name = 'reviewer-regression'
                                    passed = $true
                                    evidence = 'pass'
                                    metadata = [ordered]@{ adjudicationPending = $false }
                                }
                            )
                        }
                    } | ConvertTo-Json -Depth 20 -Compress
                }
            }
        }
    )
    $passPath = Join-Path $temp 'pass.jsonl'
    Set-Content $passPath $records -Encoding utf8NoBOM
    & (Join-Path $PSScriptRoot 'assert_reviewer_regression.ps1') -Results $passPath

    $failed = $false
    $broken = @($records)
    $broken[10] = $broken[10].Replace('"passed":true', '"passed":false')
    $failPath = Join-Path $temp 'fail.jsonl'
    Set-Content $failPath $broken -Encoding utf8NoBOM
    try {
        & (Join-Path $PSScriptRoot 'assert_reviewer_regression.ps1') -Results $failPath
    } catch {
        $failed = $true
    }
    if (-not $failed) {
        throw 'Assertion self-test expected a deterministic failure.'
    }

    $failed = $false
    $missingPath = Join-Path $temp 'missing.jsonl'
    Set-Content $missingPath @($records | Select-Object -SkipLast 1) -Encoding utf8NoBOM
    try {
        & (Join-Path $PSScriptRoot 'assert_reviewer_regression.ps1') -Results $missingPath
    } catch {
        $failed = $true
    }
    if (-not $failed) {
        throw 'Assertion self-test expected a missing trial to fail.'
    }

    $failed = $false
    $errored = @($records)
    $errored[10] = $errored[10].Replace('"status":"success"', '"status":"error"')
    $errorPath = Join-Path $temp 'error.jsonl'
    Set-Content $errorPath $errored -Encoding utf8NoBOM
    try {
        & (Join-Path $PSScriptRoot 'assert_reviewer_regression.ps1') -Results $errorPath
    } catch {
        $failed = $true
    }
    if (-not $failed) {
        throw 'Assertion self-test expected an execution error to fail.'
    }
} finally {
    Remove-Item $temp -Recurse -Force
}

Write-Host 'Reviewer regression self-tests passed.'
