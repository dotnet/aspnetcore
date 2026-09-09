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

$bash = Get-Command bash -ErrorAction Stop
$hostedPolicyMatch = [regex]::Match(
    $workflow,
    '(?ms)^          # BEGIN HOSTED EVAL POLICY\r?\n(?<script>.*?)^          # END HOSTED EVAL POLICY\r?$'
)
if (-not $hostedPolicyMatch.Success) {
    throw 'The hosted-eval selection policy could not be extracted for behavioral testing.'
}
$hostedPolicyScript = (
    $hostedPolicyMatch.Groups['script'].Value -split '\r?\n' |
        ForEach-Object {
            if ($_.StartsWith('          ')) {
                $_.Substring(10)
            } elseif ($_.Length -eq 0) {
                ''
            } else {
                throw "Unexpected indentation in the hosted-eval policy: '$_'"
            }
        }
) -join "`n"

$hostedPolicyTestRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-hosted-eval-policy-test-' + [guid]::NewGuid().ToString('N')
)
New-Item -ItemType Directory -Path $hostedPolicyTestRoot | Out-Null
try {
    $hostedPolicyPath = Join-Path $hostedPolicyTestRoot 'hosted-eval-policy.sh'
    Set-Content $hostedPolicyPath @"
set -euo pipefail
$hostedPolicyScript

filter_hosted_evals investigate-issue review-public-api validate-blazor-feature
[[ "`${ELIGIBLE_EVALS[*]}" == "review-public-api validate-blazor-feature" ]]

filter_hosted_evals investigate-issue
[[ "`${#ELIGIBLE_EVALS[@]}" -eq 0 ]]

filter_hosted_evals review-public-api investigate-issue validate-blazor-feature
[[ "`${ELIGIBLE_EVALS[*]}" == "review-public-api validate-blazor-feature" ]]

if reject_explicit_hosted_eval investigate-issue; then
  echo 'Explicit excluded eval unexpectedly passed.' >&2
  exit 1
fi
reject_explicit_hosted_eval review-public-api
"@ -Encoding utf8NoBOM

    & $bash.Source $hostedPolicyPath
    if ($LASTEXITCODE -ne 0) {
        throw "The hosted-eval selection policy test failed with exit code $LASTEXITCODE."
    }
} finally {
    Remove-Item -Recurse -Force $hostedPolicyTestRoot
}

$affectedNormalization = $workflow.IndexOf(
    'mapfile -t AFFECTED < <(printf ''%s\n'' "${AFFECTED[@]}" | sed ''/^$/d'' | sort -u)',
    [StringComparison]::Ordinal
)
$hostedFilterCall = $workflow.IndexOf(
    'filter_hosted_evals "${AFFECTED[@]}"',
    [StringComparison]::Ordinal
)
$centralAssignment = $workflow.IndexOf(
    'AFFECTED=("${ALL_EVALS[@]}")',
    [StringComparison]::Ordinal
)
$emptyEligibleGuard = $workflow.IndexOf(
    'if [[ "${#AFFECTED[@]}" -eq 0 ]]; then',
    [StringComparison]::Ordinal
)
$statusClaimLookup = $workflow.IndexOf(
    'CURRENT_STATE="$(',
    [StringComparison]::Ordinal
)
if ($centralAssignment -lt 0 -or
    $affectedNormalization -lt 0 -or
    $hostedFilterCall -le $affectedNormalization -or
    $hostedFilterCall -le $centralAssignment -or
    $emptyEligibleGuard -le $hostedFilterCall -or
    $statusClaimLookup -le $emptyEligibleGuard) {
    throw 'The hosted exclusion must run after affected-eval and central-change classification and before status claiming.'
}

foreach ($explicitRejectionFragment in @(
    '- name: Reject ineligible explicit evaluation',
    "inputs.eval == 'investigate-issue'",
    "inputs.eval != 'investigate-issue'"
)) {
    if ($workflow -notmatch [regex]::Escape($explicitRejectionFragment)) {
        throw "The direct-dispatch path is missing '$explicitRejectionFragment'."
    }
}

$stagingPolicyMatch = [regex]::Match(
    $workflow,
    '(?ms)^          # BEGIN HOSTED EVAL STAGING POLICY\r?\n(?<script>.*?)^          # END HOSTED EVAL STAGING POLICY\r?$'
)
if (-not $stagingPolicyMatch.Success) {
    throw 'The hosted-eval staging policy could not be extracted for behavioral testing.'
}
$stagingPolicyScript = (
    $stagingPolicyMatch.Groups['script'].Value -split '\r?\n' |
        ForEach-Object {
            if ($_.StartsWith('          ')) {
                $_.Substring(10)
            } elseif ($_.Length -eq 0) {
                ''
            } else {
                throw "Unexpected indentation in the hosted-eval staging policy: '$_'"
            }
        }
) -join "`n"

& ([scriptblock]::Create(
    "`$evalNames = @('review-public-api')`n$stagingPolicyScript"
))

$stagingRejected = $false
try {
    & ([scriptblock]::Create(
        "`$evalNames = @('review-public-api', 'investigate-issue')`n$stagingPolicyScript"
    ))
} catch {
    $stagingRejected = $_.Exception.Message -like "*'investigate-issue' is not eligible for hosted model evaluation*"
}
if (-not $stagingRejected) {
    throw 'The common staging boundary accepted the excluded investigate-issue lane.'
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
