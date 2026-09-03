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
if ($selectedRefCount -ne 3) {
    throw "Expected three full-ref default-branch guards; found $selectedRefCount."
}

$expectedRefCount = ([regex]::Matches(
    $workflow,
    [regex]::Escape('EXPECTED_REF="refs/heads/$DEFAULT_BRANCH"')
)).Count
if ($expectedRefCount -ne 3) {
    throw "Expected three closed default-branch comparisons; found $expectedRefCount."
}

$copilotCredentialExpression = @'
COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}
'@.Trim()
$copilotCredentialExpressionCount = ([regex]::Matches(
    $workflow,
    [regex]::Escape($copilotCredentialExpression)
)).Count
if ($copilotCredentialExpressionCount -ne 1) {
    throw "Expected one static Copilot PAT mapping with the repository fallback; found $copilotCredentialExpressionCount."
}

$patPoolJob = [regex]::Match(
    $workflow,
    '(?ms)^  pat_pool:\r?\n(?<job>.*?)(?=^  \S|\z)'
)
if (-not $patPoolJob.Success) {
    throw 'The workflow does not contain the trusted PAT-pool selector job.'
}

$patPoolJobText = $patPoolJob.Groups['job'].Value
foreach ($patNumber in 0..9) {
    $secretReference = "COPILOT_PAT_${patNumber}: `${{ secrets.COPILOT_PAT_${patNumber} }}"
    if ($patPoolJobText -notmatch [regex]::Escape($secretReference)) {
        throw "The PAT-pool selector does not inspect COPILOT_PAT_$patNumber."
    }
}
if ($patPoolJobText -notmatch [regex]::Escape('PAT_INDEX=$((RANDOM % ${#PAT_NUMBERS[@]}))')) {
    throw 'The PAT-pool selector does not randomly balance across configured entries.'
}
if ($patPoolJobText -notmatch [regex]::Escape('echo "copilot_pat_number=$PAT_NUMBER" >> "$GITHUB_OUTPUT"')) {
    throw 'The PAT-pool selector does not expose the selected numeric slot.'
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
