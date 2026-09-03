#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$runner = Join-Path $PSScriptRoot 'run.ps1'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-skill-eval-runner-test-' + [guid]::NewGuid().ToString('N')
)
$fakeVally = Join-Path $testRoot 'fake-vally.ps1'
$record = Join-Path $testRoot 'invocation.txt'
$specialized = Join-Path $testRoot 'specialized.vally.yaml'
$customExperiment = Join-Path $testRoot 'custom.experiment.yaml'
$output = Join-Path $testRoot 'results'
$relativeOutput = 'artifacts/skill-eval-runner-selftest'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Invocation {
    $invocations = @(Read-Invocations)
    return @($invocations[-1].Arguments)
}

function Read-Invocations {
    $invocations = @(
        Get-Content $record |
            ForEach-Object { $_ | ConvertFrom-Json }
    )
    Assert-True ($invocations.Count -gt 0) 'Fake Vally did not capture an invocation.'
    foreach ($invocation in $invocations) {
        if ($invocation.Arguments[0] -notin @('experiment', 'eval')) {
            continue
        }
        Assert-True ($invocation.WorkingDirectory -ne $repoRoot) (
            'Run used the repository as its working directory.'
        )
        Assert-True (-not (Test-Path $invocation.WorkingDirectory)) (
            'The isolated working directory was not removed.'
        )
    }
    return $invocations
}

New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    @'
if ($args -contains '--version') {
    Write-Output 'fake-vally 1.0'
    return
}
if ($env:SKILL_EVAL_FAKE_FAILURE) {
    throw 'fake Vally failure'
}
[ordered]@{
    WorkingDirectory = (Get-Location).Path
    Arguments = [string[]]$args
} |
    ConvertTo-Json -Compress |
    Add-Content $env:SKILL_EVAL_RUNNER_RECORD
'@ | Set-Content $fakeVally
    Set-Content $specialized "name: specialized`n"
    Set-Content $customExperiment "name: custom`n"
    $env:SKILL_EVAL_RUNNER_RECORD = $record

    Push-Location $testRoot
    try {
        & $runner -Vally $fakeVally -VallyPrefix @()
    } finally {
        Pop-Location
    }
    $validateInvocations = @(
        Read-Invocations |
            Where-Object { $_.Arguments[0] -eq 'experiment' }
    )
    Assert-True ($validateInvocations.Count -eq 2) (
        'Default validation did not resolve both experiments.'
    )
    foreach ($invocation in $validateInvocations) {
        Assert-True ($invocation.Arguments[1] -eq 'run') (
            'Default validation did not use Vally experiment run.'
        )
        Assert-True ($invocation.Arguments -contains '--dry-run') (
            'Default validation could invoke models.'
        )
        Assert-True ($invocation.Arguments -contains '--compare') (
            'Default validation did not resolve comparison mode.'
        )
    }
    Assert-True ($validateInvocations.Arguments -contains (
        Join-Path $repoRoot 'eng/skill-evals/skills-vs-baseline.experiment.yaml'
    )) 'Default validation did not resolve the standard experiment.'
    Assert-True ($validateInvocations.Arguments -contains (
        Join-Path $repoRoot 'eng/skill-evals/skills-smoke.experiment.yaml'
    )) 'Default validation did not resolve the smoke experiment.'

    Remove-Item $record
    & $runner Validate `
        -Experiment $customExperiment `
        -Vally $fakeVally `
        -VallyPrefix @()
    $customValidateInvocations = @(
        Read-Invocations |
            Where-Object { $_.Arguments[0] -eq 'experiment' }
    )
    Assert-True ($customValidateInvocations.Count -eq 1) (
        'Custom validation unexpectedly changed the selected experiment.'
    )
    Assert-True ($customValidateInvocations[0].Arguments -contains $customExperiment) (
        'Custom validation did not use the selected experiment.'
    )
    Assert-True ($customValidateInvocations[0].Arguments -contains '--dry-run') (
        'Custom validation could invoke models.'
    )

    & $runner Run `
        -Eval eng/skill-evals/review-public-api/eval.vally.yaml `
        -Vally $fakeVally `
        -VallyPrefix @() `
        -OutputDirectory $relativeOutput `
        '--workers' '2'
    $standardInvocation = Read-Invocation
    Assert-True ($standardInvocation[0] -eq 'experiment') 'Standard run did not use Vally experiment.'
    Assert-True ($standardInvocation[1] -eq 'run') 'Standard run did not use Vally experiment run.'
    Assert-True ($standardInvocation -contains '--compare') 'Standard run omitted A/B comparison.'
    Assert-True ($standardInvocation -contains '--eval-filter') 'Standard run omitted --eval-filter.'
    Assert-True ($standardInvocation -contains 'review-public-api/eval.vally.yaml') (
        'Standard run did not resolve the repository-relative eval path.'
    )
    Assert-True ($standardInvocation -contains '--workers') 'Run did not forward additional arguments.'
    Assert-True ($standardInvocation -contains '2') 'Run did not forward the workers value.'
    Assert-True ($standardInvocation -contains (Join-Path $repoRoot $relativeOutput)) (
        'Run did not resolve the repository-relative output path.'
    )

    & $runner Run `
        -Eval eng/skill-evals/review-public-api/eval.vally.yaml `
        -Experiment eng/skill-evals/skills-smoke.experiment.yaml `
        -Vally $fakeVally `
        -VallyPrefix @() `
        -OutputDirectory $output
    $smokeInvocation = Read-Invocation
    Assert-True ($smokeInvocation -contains (
        Join-Path $repoRoot 'eng/skill-evals/skills-smoke.experiment.yaml'
    )) 'Smoke run did not use the selected experiment.'

    & $runner Run `
        -Eval $specialized `
        -Vally $fakeVally `
        -VallyPrefix @() `
        -OutputDirectory $output
    $specializedInvocation = Read-Invocation
    Assert-True ($specializedInvocation[0] -eq 'eval') 'Specialized run did not use Vally eval.'
    Assert-True ($specializedInvocation -contains '--eval-spec') 'Specialized run omitted --eval-spec.'

    $env:SKILL_EVAL_FAKE_FAILURE = 'true'
    $failurePropagated = $false
    try {
        & $runner Run -Vally $fakeVally -VallyPrefix @() -OutputDirectory $output
    } catch {
        $failurePropagated = $_.Exception.Message -like '*fake Vally failure*'
    }
    Assert-True $failurePropagated 'Vally invocation failure was not propagated.'

    Write-Host 'Skill-eval runner self-test passed.'
} finally {
    Remove-Item Env:SKILL_EVAL_RUNNER_RECORD -ErrorAction SilentlyContinue
    Remove-Item Env:SKILL_EVAL_FAKE_FAILURE -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $testRoot
}
