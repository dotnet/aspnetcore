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

$reportJob = [regex]::Match(
    $workflow,
    '(?ms)^  report:\r?\n(?<job>.*?)(?=^  \S|\z)'
)
if (-not $reportJob.Success) {
    throw 'The workflow does not contain the report job.'
}

$reportCondition = [regex]::Match(
    $reportJob.Groups['job'].Value,
    '(?ms)^    if: >-\r?\n(?<condition>(?:      .*(?:\r?\n|$))+?)(?=^    \S|\z)'
)
if (-not $reportCondition.Success) {
    throw 'The report job does not contain a multiline condition.'
}

$claimedConditionCount = ([regex]::Matches(
    $reportCondition.Groups['condition'].Value,
    "(?m)^      needs\.run\.outputs\.claimed != 'false'\r?$"
)).Count
if ($claimedConditionCount -ne 1) {
    throw "Expected the report condition to retain the claimed-output guard; found $claimedConditionCount."
}

Write-Host 'Skill-eval workflow guard self-test passed.'
