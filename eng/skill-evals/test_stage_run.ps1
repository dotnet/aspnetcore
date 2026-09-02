#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$stageRun = Join-Path $PSScriptRoot 'stage_run.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-skill-eval-staging-test-' + [guid]::NewGuid().ToString('N')
)
$trustedRoot = Join-Path $testRoot 'trusted'
$candidateRoot = Join-Path $testRoot 'candidate'
$destination = Join-Path $testRoot 'staged'
$trustedMarker = 'TRUSTED_CONTROL_MARKER'
$candidateControlMarker = 'CANDIDATE_CONTROL_CANARY_MUST_NOT_APPEAR'
$candidateDataMarker = 'CANDIDATE_DATA_MARKER'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Throws {
    param(
        [scriptblock]$Action,
        [string]$Expected
    )

    $message = ''
    try {
        & $Action
    } catch {
        $message = $_.Exception.Message
    }
    if ($message -notlike "*$Expected*") {
        throw "Expected an error containing '$Expected'; got '$message'."
    }
}

function Write-ControlPlane {
    param(
        [string]$Root,
        [string]$Marker
    )

    $evalRoot = Join-Path $Root 'eng/skill-evals'
    New-Item -ItemType Directory -Path $evalRoot -Force | Out-Null
    Set-Content (Join-Path $evalRoot 'run.ps1') $Marker
    Set-Content (Join-Path $evalRoot 'assert_results.ps1') $Marker
    Set-Content (Join-Path $evalRoot 'skills-vs-baseline.experiment.yaml') $Marker
    Set-Content (Join-Path $evalRoot 'skills-smoke.experiment.yaml') $Marker
}

New-Item -ItemType Directory -Path $trustedRoot, $candidateRoot | Out-Null
try {
    Write-ControlPlane $trustedRoot $trustedMarker
    Write-ControlPlane $candidateRoot $candidateControlMarker

    $candidateSkill = Join-Path $candidateRoot '.github/skills/widget'
    $candidateEval = Join-Path $candidateRoot 'eng/skill-evals/widget'
    New-Item -ItemType Directory -Path $candidateSkill -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $candidateEval 'fixtures') -Force |
        Out-Null
    Set-Content (Join-Path $candidateSkill 'SKILL.md') $candidateDataMarker
    Set-Content (Join-Path $candidateEval 'eval.vally.yaml') $candidateDataMarker
    Set-Content (Join-Path $candidateEval 'fixtures/input.txt') $candidateDataMarker
    Set-Content (Join-Path $candidateEval 'candidate-runner.ps1') $candidateControlMarker

    $unrelatedSkill = Join-Path $candidateRoot '.github/skills/unrelated'
    $unrelatedEval = Join-Path $candidateRoot 'eng/skill-evals/unrelated'
    New-Item -ItemType Directory -Path $unrelatedSkill, $unrelatedEval -Force |
        Out-Null
    Set-Content (Join-Path $unrelatedSkill 'SKILL.md') 'UNRELATED_DATA'
    Set-Content (Join-Path $unrelatedEval 'eval.vally.yaml') 'UNRELATED_DATA'

    & $stageRun `
        -TrustedRoot $trustedRoot `
        -CandidateRoot $candidateRoot `
        -EvalName widget `
        -Destination $destination

    $stagedFiles = @(
        Get-ChildItem $destination -Recurse -File -Force |
            ForEach-Object {
                [IO.Path]::GetRelativePath($destination, $_.FullName).Replace('\', '/')
            } |
            Sort-Object
    )
    $expectedFiles = @(
        '.github/skills/widget/SKILL.md'
        'eng/skill-evals/assert_results.ps1'
        'eng/skill-evals/run.ps1'
        'eng/skill-evals/skills-smoke.experiment.yaml'
        'eng/skill-evals/skills-vs-baseline.experiment.yaml'
        'eng/skill-evals/widget/eval.vally.yaml'
        'eng/skill-evals/widget/fixtures/input.txt'
    ) | Sort-Object
    Assert-True (
        @(Compare-Object $expectedFiles $stagedFiles).Count -eq 0
    ) "Unexpected staged files: $($stagedFiles -join ', ')."

    $stagedContent = $stagedFiles |
        ForEach-Object { Get-Content (Join-Path $destination $_) -Raw }
    Assert-True (
        @($stagedContent | Select-String $candidateControlMarker).Count -eq 0
    ) 'Candidate control-plane canary reached the staged tree.'
    Assert-True (
        @($stagedContent | Select-String $trustedMarker).Count -eq 4
    ) 'The trusted runner, assertion, and experiments were not all staged.'
    Assert-True (
        @($stagedContent | Select-String $candidateDataMarker).Count -eq 3
    ) 'The selected candidate skill, eval, and fixture were not all staged.'

    Assert-Throws {
        & $stageRun `
            -TrustedRoot $trustedRoot `
            -CandidateRoot $candidateRoot `
            -EvalName '../escape' `
            -Destination (Join-Path $testRoot 'escape')
    } 'Invalid eval name'

    Assert-Throws {
        & $stageRun `
            -TrustedRoot $trustedRoot `
            -CandidateRoot $candidateRoot `
            -EvalName widget `
            -Destination (Join-Path $candidateRoot 'staged')
    } 'must be outside both source trees'

    if (-not $IsWindows) {
        $link = Join-Path $candidateEval 'fixtures/link.txt'
        New-Item -ItemType SymbolicLink `
            -Path $link `
            -Target (Join-Path $candidateEval 'fixtures/input.txt') | Out-Null
        Assert-Throws {
            & $stageRun `
                -TrustedRoot $trustedRoot `
                -CandidateRoot $candidateRoot `
                -EvalName widget `
                -Destination (Join-Path $testRoot 'symlinked')
        } 'is a symlink or reparse point'
    }

    Write-Host 'Trusted skill-eval staging self-test passed.'
} finally {
    Remove-Item $testRoot -Recurse -Force
}
