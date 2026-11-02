#requires -Version 7.0
param(
    [ValidateSet('DryRun', 'Run')]
    [string]$Action = 'DryRun',

    [string]$CurrentSkill,

    [string]$CandidateSkill,

    [string]$OutputDirectory = 'artifacts/skill-evals/reviewer-regression',

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$sharedRunner = Join-Path $repoRoot 'eng/skill-evals/run.ps1'
$template = Join-Path $PSScriptRoot 'reviewer-regression.experiment.yaml'
$eval = Join-Path $PSScriptRoot 'reviewer-regression.vally.yaml'
$defaultSkill = Join-Path $repoRoot '.github/skills/review-pull-request'
$current = if ($CurrentSkill) { (Resolve-Path $CurrentSkill).Path } else { $defaultSkill }
$candidate = if ($CandidateSkill) { (Resolve-Path $CandidateSkill).Path } else { $defaultSkill }
$temporaryExperiment = Join-Path $PSScriptRoot '.reviewer-regression.experiment.generated.yaml'

function ConvertTo-YamlPath {
    param([string]$Path)
    return "'" + $Path.Replace('\', '/').Replace("'", "''") + "'"
}

$content = Get-Content $template -Raw
$content = $content.Replace(
    '../../../.github/skills/review-pull-request # current-skill',
    "$(ConvertTo-YamlPath $current) # current-skill"
)
$content = $content.Replace(
    '../../../.github/skills/review-pull-request # candidate-skill',
    "$(ConvertTo-YamlPath $candidate) # candidate-skill"
)

try {
    Set-Content $temporaryExperiment $content -Encoding utf8NoBOM
    if ($Action -eq 'DryRun') {
        & $sharedRunner Validate -Eval $eval -Experiment $temporaryExperiment
    } else {
        & $sharedRunner Run -Experiment $temporaryExperiment -OutputDirectory $OutputDirectory @Arguments
    }
} finally {
    Remove-Item $temporaryExperiment -ErrorAction SilentlyContinue
}
