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
$previousVally = [Environment]::GetEnvironmentVariable('SKILL_EVAL_VALLY')
$hadPreviousVally = Test-Path Env:SKILL_EVAL_VALLY

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
if ($args[0] -eq 'eval' -and ($args -join ' ') -like '*review-candidate-verifier*') {
    $output = $args[([array]::IndexOf($args, '--output-dir') + 1)]
    $run = Join-Path $output '2026-01-01T00-00-00'
    New-Item -ItemType Directory -Path $run -Force | Out-Null
    Set-Content (Join-Path $run 'results.jsonl') ''
    if ($env:SKILL_EVAL_FAKE_EXIT) {
        exit 1
    }
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
    $env:SKILL_EVAL_VALLY = $fakeVally

    Push-Location $testRoot
    try {
        & $runner
    } finally {
        Pop-Location
    }
    $validateInvocations = @(
        Read-Invocations |
            Where-Object { $_.Arguments[0] -eq 'experiment' }
    )
    Assert-True ($validateInvocations.Count -eq 4) (
        'Default validation did not resolve generic and reviewer experiments.'
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
    Assert-True ($validateInvocations.Arguments -contains (
        Join-Path $repoRoot 'eng/skill-evals/review-vs-baseline.experiment.yaml'
    )) 'Default validation did not resolve the reviewer experiment.'
    Assert-True ($validateInvocations.Arguments -contains (
        Join-Path $repoRoot 'eng/skill-evals/review-smoke.experiment.yaml'
    )) 'Default validation did not resolve the reviewer smoke experiment.'

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

    Remove-Item $record
    & $runner Validate `
        -Suite review-pull-request `
        -Vally $fakeVally `
        -VallyPrefix @()
    $suiteInvocations = @(Read-Invocations)
    $suiteLint = @($suiteInvocations | Where-Object { $_.Arguments[0] -eq 'lint' })
    Assert-True ($suiteLint.Count -eq 1) 'Suite validation linted unrelated specifications.'
    Assert-True ($suiteLint[0].Arguments -contains (
        Join-Path $repoRoot 'eng/skill-evals/review-pull-request/eval.vally.yaml'
    )) 'Suite validation did not lint the selected specification.'
    $suitePlans = @($suiteInvocations | Where-Object { $_.Arguments[0] -eq 'experiment' })
    Assert-True ($suitePlans.Count -eq 2) 'Suite validation did not resolve both experiments.'
    foreach ($invocation in $suitePlans) {
        Assert-True ($invocation.Arguments -contains '--dry-run') 'Suite validation could invoke models.'
        Assert-True ($invocation.Arguments -contains '--eval-filter') 'Suite validation omitted its filter.'
        Assert-True ($invocation.Arguments -contains 'review-pull-request/eval.vally.yaml') 'Suite validation selected the wrong eval.'
    }

    & $runner Run `
        -Suite review-pull-request `
        -Vally $fakeVally `
        -VallyPrefix @() `
        -OutputDirectory $output
    $suiteRun = Read-Invocation
    Assert-True ($suiteRun[0] -eq 'experiment') 'Suite run bypassed experiment skill ownership.'
    Assert-True ($suiteRun -contains 'review-pull-request/eval.vally.yaml') 'Suite run selected the wrong eval.'
    Assert-True ($suiteRun -contains (Join-Path $repoRoot 'eng/skill-evals/review-vs-baseline.experiment.yaml')) (
        'Reviewer run used the generic treatment.'
    )
    & $runner Run -Suite review-pull-request `
        -Experiment eng/skill-evals/skills-smoke.experiment.yaml `
        -Vally $fakeVally -VallyPrefix @() -OutputDirectory $output
    Assert-True ((Read-Invocation) -contains (Join-Path $repoRoot 'eng/skill-evals/review-smoke.experiment.yaml')) (
        'Reviewer smoke run used the generic treatment.'
    )

    foreach ($smoke in @($false, $true)) {
        $componentOutput = Join-Path $testRoot "component-$smoke"
        New-Item -ItemType Directory -Path (Join-Path $componentOutput '2025-12-31T00-00-00') -Force | Out-Null
        $options = @{ Suite = 'review-candidate-verifier'; Vally = $fakeVally; VallyPrefix = @(); OutputDirectory = $componentOutput }
        if ($smoke) {
            $options.Experiment = 'eng/skill-evals/skills-smoke.experiment.yaml'
        }
        & $runner Run @options
        $componentRun = Read-Invocation
        Assert-True ($componentRun[0] -eq 'eval') 'Component run used a skill comparison experiment.'
        $plan = Get-Content (Join-Path $componentOutput '2026-01-01T00-00-00/plan-snapshot.json') -Raw | ConvertFrom-Json
        $runs = if ($smoke) { 1 } else { 5 }
        Assert-True ($plan.evals[0].runs -eq $runs) 'Component plan has the wrong repeat count.'
        Assert-True ($componentRun[([array]::IndexOf($componentRun, '--runs') + 1)] -eq "$runs") 'Component invocation has the wrong repeat count.'
        Assert-True ($plan.evals[0].threshold -eq 0.8 -and $plan.evals[0].plannedStimulusCount -eq 3) (
            'Component plan lost its per-case 4/5 disposition gate.'
        )
    }
    $componentOutput = Join-Path $testRoot 'component-review-smoke'
    & $runner Run -Suite review-candidate-verifier `
        -Experiment eng/skill-evals/review-smoke.experiment.yaml `
        -Vally $fakeVally -VallyPrefix @() -OutputDirectory $componentOutput
    $componentRun = Read-Invocation
    Assert-True ($componentRun[([array]::IndexOf($componentRun, '--runs') + 1)] -eq '1') (
        'Reviewer smoke selection unexpectedly ran the full component suite.'
    )

    $env:SKILL_EVAL_FAKE_EXIT = 'true'
    $failedOutput = Join-Path $testRoot 'component-exit-one'
    & $runner Run -Suite review-candidate-verifier -Vally $fakeVally -VallyPrefix @() -OutputDirectory $failedOutput
    Assert-True ($LASTEXITCODE -eq 1) 'Component Vally exit code was not propagated.'
    Assert-True (Test-Path (Join-Path $failedOutput '2026-01-01T00-00-00/plan-snapshot.json')) (
        'A failed component run lost its plan snapshot.'
    )
    Remove-Item Env:SKILL_EVAL_FAKE_EXIT

    Remove-Item $record
    & $runner Validate -Eval eng/skill-evals/review-candidate-verifier/eval.vally.yaml `
        -Vally $fakeVally -VallyPrefix @()
    $componentValidation = @(Read-Invocations)
    Assert-True ($componentValidation.Count -eq 1 -and $componentValidation[0].Arguments[0] -eq 'lint') (
        'Component -Eval validation planned unrelated comparison experiments.'
    )

    Remove-Item $record
    & $runner Run -Vally $fakeVally -VallyPrefix @() -OutputDirectory (Join-Path $testRoot 'all-results')
    $allRuns = @(Read-Invocations | Where-Object { $_.Arguments[0] -in @('experiment', 'eval') })
    $suiteCount = @(Get-ChildItem (Join-Path $repoRoot 'eng/skill-evals') -Directory |
        Where-Object { Test-Path (Join-Path $_.FullName 'eval.vally.yaml') }).Count
    Assert-True ($allRuns.Count -eq $suiteCount) 'Default run omitted a standard suite.'
    Assert-True (($allRuns.Arguments -join ' ') -like '*review-candidate-verifier*') 'Default run omitted the component suite.'
    Assert-True ($allRuns.Arguments -contains (Join-Path $repoRoot 'eng/skill-evals/review-vs-baseline.experiment.yaml')) (
        'Default run omitted the reviewer comparison.'
    )

    foreach ($invalidSuite in @('../outside', '..', 'missing-suite')) {
        $rejected = $false
        try {
            & $runner Lint -Suite $invalidSuite -Vally $fakeVally -VallyPrefix @()
        } catch {
            $rejected = $_.Exception.Message -match 'Invalid suite name|Missing suite specification'
        }
        Assert-True $rejected "Invalid or missing suite '$invalidSuite' was not rejected."
    }
    $ambiguousSelectionRejected = $false
    try {
        & $runner Lint -Suite review-pull-request -Eval $specialized -Vally $fakeVally -VallyPrefix @()
    } catch {
        $ambiguousSelectionRejected = $_.Exception.Message -like '*Specify either -Suite or -Eval*'
    }
    Assert-True $ambiguousSelectionRejected 'Conflicting suite and eval selectors were not rejected.'

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
    Remove-Item Env:SKILL_EVAL_FAKE_EXIT -ErrorAction SilentlyContinue
    if ($hadPreviousVally) {
        $env:SKILL_EVAL_VALLY = $previousVally
    } else {
        Remove-Item Env:SKILL_EVAL_VALLY -ErrorAction SilentlyContinue
    }
    Remove-Item -Recurse -Force $testRoot
}
