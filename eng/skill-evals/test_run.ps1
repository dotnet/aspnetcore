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
$output = Join-Path $testRoot 'results'
$relativeOutput = 'artifacts/skill-eval-runner-selftest'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Invocation {
    $lines = @(Get-Content $record)
    Assert-True ($lines.Count -gt 1) 'Fake Vally did not capture an invocation.'
    Assert-True ($lines[0] -ne $repoRoot) 'Run used the repository as its working directory.'
    Assert-True (-not (Test-Path $lines[0])) 'The isolated working directory was not removed.'
    return $lines
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
[IO.File]::WriteAllLines(
    $env:SKILL_EVAL_RUNNER_RECORD,
    @((Get-Location).Path) + [string[]]$args
)
'@ | Set-Content $fakeVally
    Set-Content $specialized "name: specialized`n"
    $env:SKILL_EVAL_RUNNER_RECORD = $record

    Push-Location $testRoot
    try {
        & $runner -Vally $fakeVally -VallyPrefix @()
    } finally {
        Pop-Location
    }
    $validateInvocation = Read-Invocation
    Assert-True ($validateInvocation[1] -eq 'experiment') (
        'Default validation did not resolve the experiment.'
    )
    Assert-True ($validateInvocation[2] -eq 'run') (
        'Default validation did not use Vally experiment run.'
    )
    Assert-True ($validateInvocation -contains '--dry-run') (
        'Default validation could invoke models.'
    )
    Assert-True ($validateInvocation -contains '--compare') (
        'Default validation did not resolve comparison mode.'
    )

    & $runner Run `
        -Eval eng/skill-evals/review-public-api/eval.vally.yaml `
        -Vally $fakeVally `
        -VallyPrefix @() `
        -OutputDirectory $relativeOutput `
        '--workers' '2'
    $standardInvocation = Read-Invocation
    Assert-True ($standardInvocation[1] -eq 'experiment') 'Standard run did not use Vally experiment.'
    Assert-True ($standardInvocation[2] -eq 'run') 'Standard run did not use Vally experiment run.'
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
    Assert-True ($specializedInvocation[1] -eq 'eval') 'Specialized run did not use Vally eval.'
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
