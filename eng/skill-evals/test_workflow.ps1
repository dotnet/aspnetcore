#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$workflowPath = Join-Path $PSScriptRoot '../../.github/workflows/skill-evals.yml'
$workflow = Get-Content $workflowPath -Raw

if ($workflow -match '(?m)^\s{2}pull_request_review:') {
    throw 'The PAT-backed workflow must not activate from pull_request_review.'
}
if ($workflow -match 'REVIEW_(BODY|ACTOR|SHA|PR_NUMBER)|pull_request_review') {
    throw 'The workflow still contains pull-request-review resolution logic.'
}

$selectedRefCount = ([regex]::Matches(
    $workflow,
    [regex]::Escape('SELECTED_REF: ${{ github.ref }}')
)).Count
if ($selectedRefCount -ne 2) {
    throw "Expected two full-ref default-branch guards; found $selectedRefCount."
}

$expectedRefCount = ([regex]::Matches(
    $workflow,
    [regex]::Escape('EXPECTED_REF="refs/heads/$DEFAULT_BRANCH"')
)).Count
if ($expectedRefCount -ne 2) {
    throw "Expected two closed default-branch comparisons; found $expectedRefCount."
}

Write-Host 'Skill-eval workflow guard self-test passed.'
