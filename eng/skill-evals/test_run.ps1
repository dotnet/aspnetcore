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
$customRoot = Join-Path $testRoot 'custom-root'
$allSuiteRoot = Join-Path $testRoot 'all-suite-root'
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
[ordered]@{
    WorkingDirectory = (Get-Location).Path
    Arguments = [string[]]$args
} |
    ConvertTo-Json -Compress |
    Add-Content $env:SKILL_EVAL_RUNNER_RECORD
if ($args[0] -eq 'eval' -and ($args -join ' ') -like '*review-candidate-verifier*') {
    $output = $args[([array]::IndexOf($args, '--output-dir') + 1)]
    $run = Join-Path $output '2026-01-01T00-00-00'
    New-Item -ItemType Directory -Path $run -Force | Out-Null
    if ($env:SKILL_EVAL_FAKE_EXIT_MODE) {
        $rows = [Collections.Generic.List[object]]::new()
        foreach ($name in 'a', 'b', 'c') {
            foreach ($trial in 0..4) {
                $score = if (
                    $env:SKILL_EVAL_FAKE_EXIT_MODE -eq 'failed-gate' -and
                    $name -eq 'a' -and $trial -ge 3
                ) { 0 } else { 1 }
                $rows.Add(@{
                    type = 'trial-result'
                    runId = 'fake-component-run'
                    stimulus = $name
                    trialIndex = $trial
                    status = 'success'
                    gradeResult = @{
                        score = $score
                        details = @(
                            @{ configuredName = "evidence-reads-$name"; passed = $true; score = 1 },
                            @{ configuredName = "disposition-$name"; passed = $score -eq 1; score = $score }
                        )
                    }
                })
            }
        }
        if ($env:SKILL_EVAL_FAKE_EXIT_MODE -eq 'incomplete') {
            $rows.RemoveAt(0)
        }
        $rows.Add(@{
            type = 'run-summary'
            runId = 'fake-component-run'
            hadExecutionErrors = $false
        })
        $rows | ForEach-Object { $_ | ConvertTo-Json -Depth 10 -Compress } |
            Set-Content (Join-Path $run 'results.jsonl')
        exit 1
    }
    Set-Content (Join-Path $run 'results.jsonl') ''
    if ($env:SKILL_EVAL_FAKE_EXIT) {
        exit 1
    }
}
'@ | Set-Content $fakeVally
    Set-Content $specialized "name: specialized`n"
    Set-Content $customExperiment "name: custom`n"
    $customComponentDirectory = Join-Path $customRoot 'eng/skill-evals/review-candidate-verifier'
    New-Item -ItemType Directory -Path $customComponentDirectory -Force | Out-Null
    @'
name: review-candidate-verifier
defaults:
  runs: 7
scoring:
  threshold: 0.65
stimuli:
  - name: alpha
  - name: beta
'@ | Set-Content (Join-Path $customComponentDirectory 'eval.vally.yaml')
    $allComponentDirectory = Join-Path $allSuiteRoot 'eng/skill-evals/review-candidate-verifier'
    $allNextDirectory = Join-Path $allSuiteRoot 'eng/skill-evals/z-next'
    New-Item -ItemType Directory -Path $allComponentDirectory, $allNextDirectory -Force | Out-Null
    @'
name: review-candidate-verifier
defaults:
  runs: 5
scoring:
  threshold: 0.8
stimuli:
  - name: a
  - name: b
  - name: c
'@ | Set-Content (Join-Path $allComponentDirectory 'eval.vally.yaml')
    @'
name: z-next
stimuli:
  - name: next
'@ | Set-Content (Join-Path $allNextDirectory 'eval.vally.yaml')
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
    Assert-True ($validateInvocations.Count -eq 2) (
        'Default validation did not resolve generic experiments.'
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
    function Assert-CustomComponentPlan {
        param(
            [string]$Scenario,
            [int]$ExpectedRuns,
            [string[]]$RunArguments = @()
        )

        $scenarioOutput = Join-Path $testRoot "component-plan-$Scenario"
        & $runner Run -Root $customRoot -Suite review-candidate-verifier `
            -Vally $fakeVally -VallyPrefix @() -OutputDirectory $scenarioOutput `
            @RunArguments
        $invocation = Read-Invocation
        $plan = Get-Content (
            Join-Path $scenarioOutput '2026-01-01T00-00-00/plan-snapshot.json'
        ) -Raw | ConvertFrom-Json
        Assert-True (($plan.evals[0].plannedStimulusNames -join ',') -eq 'alpha,beta') (
            "$Scenario component plan did not derive stimulus names from the selected YAML."
        )
        Assert-True ($plan.evals[0].plannedStimulusCount -eq 2) (
            "$Scenario component plan did not derive its stimulus count from the selected YAML."
        )
        Assert-True ([Math]::Abs([double]$plan.evals[0].threshold - 0.65) -lt 0.000001) (
            "$Scenario component plan did not derive its threshold from the selected YAML."
        )
        Assert-True ($plan.evals[0].runs -eq $ExpectedRuns) (
            "$Scenario component plan did not record the effective run count."
        )
        $runValues = @(
            for ($index = 0; $index -lt $invocation.Count; $index++) {
                if ($invocation[$index] -eq '--runs') {
                    $invocation[$index + 1]
                } elseif ($invocation[$index] -like '--runs=*') {
                    $invocation[$index].Substring('--runs='.Length)
                }
            }
        )
        Assert-True ($runValues.Count -gt 0 -and $runValues[-1] -eq "$ExpectedRuns") (
            "$Scenario component invocation's effective final --runs value disagrees with the plan."
        )
    }

    $env:SKILL_EVAL_FAKE_EXIT = 'true'
    $failedOutput = Join-Path $testRoot 'component-exit-one'
    & $runner Run -Suite review-candidate-verifier -Vally $fakeVally -VallyPrefix @() -OutputDirectory $failedOutput
    Assert-True ($LASTEXITCODE -eq 1) 'Component Vally exit code was not propagated.'
    Assert-True (Test-Path (Join-Path $failedOutput '2026-01-01T00-00-00/plan-snapshot.json')) (
        'A failed component run lost its plan snapshot.'
    )
    Remove-Item Env:SKILL_EVAL_FAKE_EXIT

    function Invoke-AllSuiteExitOneScenario {
        param(
            [string]$Scenario,
            [bool]$ShouldContinue,
            [string]$ExpectedFailure
        )

        Remove-Item $record -ErrorAction SilentlyContinue
        $env:SKILL_EVAL_FAKE_EXIT_MODE = $Scenario
        $message = ''
        try {
            & $runner Run -Root $allSuiteRoot -Vally $fakeVally -VallyPrefix @() `
                -OutputDirectory (Join-Path $testRoot "all-exit-one-$Scenario")
        } catch {
            $message = $_.Exception.Message
        } finally {
            Remove-Item Env:SKILL_EVAL_FAKE_EXIT_MODE -ErrorAction SilentlyContinue
        }

        if ($ShouldContinue) {
            Assert-True ([string]::IsNullOrWhiteSpace($message)) (
                "A complete passing primary result set did not continue after Vally exit 1: $message"
            )
        } else {
            Assert-True ($message -like "*$ExpectedFailure*") (
                "$Scenario did not abort through the trusted result gate. Error: $message"
            )
        }
        $runInvocations = @(
            Read-Invocations |
                Where-Object { $_.Arguments[0] -in @('experiment', 'eval') }
        )
        $expectedCount = if ($ShouldContinue) { 2 } else { 1 }
        Assert-True ($runInvocations.Count -eq $expectedCount) (
            "$Scenario invoked $($runInvocations.Count) suites; expected $expectedCount."
        )
        if ($ShouldContinue) {
            Assert-True (($runInvocations[-1].Arguments -join ' ') -like '*z-next*') (
                'All-suite execution did not continue to the suite after the validated exit-1 result.'
            )
        }
    }

    Invoke-AllSuiteExitOneScenario -Scenario 'complete-passing' -ShouldContinue $true
    Invoke-AllSuiteExitOneScenario -Scenario 'incomplete' -ShouldContinue $false `
        -ExpectedFailure "Expected 15 'current' trial results; found 14"
    Invoke-AllSuiteExitOneScenario -Scenario 'failed-gate' -ShouldContinue $false `
        -ExpectedFailure "stimulus 'a' score 0.6 is below threshold 0.8"

    Assert-CustomComponentPlan -Scenario 'YamlDefault' -ExpectedRuns 7
    Assert-CustomComponentPlan -Scenario 'SeparateRunsOverride' -ExpectedRuns 2 `
        -RunArguments @('--runs', '2')
    Assert-CustomComponentPlan -Scenario 'EqualsRunsOverride' -ExpectedRuns 3 `
        -RunArguments @('--runs=3')

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
    Remove-Item Env:SKILL_EVAL_FAKE_EXIT_MODE -ErrorAction SilentlyContinue
    if ($hadPreviousVally) {
        $env:SKILL_EVAL_VALLY = $previousVally
    } else {
        Remove-Item Env:SKILL_EVAL_VALLY -ErrorAction SilentlyContinue
    }
    Remove-Item -Recurse -Force $testRoot
}
