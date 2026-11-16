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
if ($args[0] -eq 'experiment' -and $args -notcontains '--dry-run') {
    $output = $args[([array]::IndexOf($args, '--output-dir') + 1)]
    $run = Join-Path $output ('experiment-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $run -Force | Out-Null
    $variants = @('baseline', 'skilled')
    for ($index = 0; $index -lt $args.Count; $index++) {
        if ($args[$index] -eq '--variant') {
            $variants = @($args[++$index])
        } elseif ($args[$index] -like '--variant=*') {
            $variants = @($args[$index].Substring('--variant='.Length))
        }
    }
    @{
        evals = @($variants | ForEach-Object {
            @{ variant = $_; plannedStimulusNames = @('s'); plannedStimulusCount = 1; runs = 1; threshold = $(if ($env:SKILL_EVAL_FAKE_EXPERIMENT_INVALID) { 'invalid' } else { 0 }) }
        })
    } | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $run 'plan-snapshot.json')
    foreach ($variant in $variants) {
        New-Item -ItemType Directory -Path (Join-Path $run $variant) | Out-Null
        @{ type = 'trial-result'; variant = $variant; stimulus = 's'; status = 'success'; gradeResult = @{ score = 1 } } |
            ConvertTo-Json -Depth 10 -Compress |
            Set-Content (Join-Path $run "$variant/results.jsonl")
    }
}
if ($args[0] -eq 'eval' -and ($args -join ' ') -like '*review-candidate-verifier*') {
    $output = $args[([array]::IndexOf($args, '--output-dir') + 1)]
    $run = Join-Path $output '2026-01-01T00-00-00'
    New-Item -ItemType Directory -Path $run -Force | Out-Null
    if ($env:SKILL_EVAL_FAKE_RESULTS -ne 'empty') {
        $runs = [int]$args[([array]::IndexOf($args, '--runs') + 1)]
        $names = @(Get-Content $args[([array]::IndexOf($args, '--eval-spec') + 1)] |
            Where-Object { $_ -match '^  - name: (\S+)$' } |
            ForEach-Object { $Matches[1] })
        $rows = [Collections.Generic.List[object]]::new()
        foreach ($name in $names) {
            foreach ($trial in 0..($runs - 1)) {
                $score = if (
                    $env:SKILL_EVAL_FAKE_EXIT_MODE -eq 'failed-gate' -and
                    $name -eq $names[0] -and $trial -ge 3
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
                            @{
                                configuredName = "evidence-reads-$name"
                                passed = -not ($env:SKILL_EVAL_FAKE_EXIT_MODE -eq 'failed-receipt' -and $name -eq $names[0] -and $trial -eq 0)
                                score = 1
                            },
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
            Set-Content -LiteralPath (Join-Path $run 'results.jsonl')
    } else {
        Set-Content -LiteralPath (Join-Path $run 'results.jsonl') ''
    }
}
if ($env:SKILL_EVAL_FAKE_EXIT_CODE) {
    exit [int]$env:SKILL_EVAL_FAKE_EXIT_CODE
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
    New-Item -ItemType Directory -Path $allComponentDirectory -Force | Out-Null
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

    foreach ($syntax in @('Separate', 'Equals')) {
        $variantArguments = if ($syntax -eq 'Separate') { @('--variant', 'skilled') } else { @('--variant=skilled') }
        & $runner Run -Suite review-public-api -Vally $fakeVally -VallyPrefix @() `
            -OutputDirectory (Join-Path $testRoot "selected-variant-$syntax") -Arguments $variantArguments
        $invocation = Read-Invocation
        foreach ($argument in $variantArguments) {
            Assert-True ($invocation -contains $argument) 'Selected variant was not forwarded to Vally.'
        }
        Write-Host "SelectedVariant${syntax}Syntax passed."
    }

    $experimentCheckFailures = [Collections.Generic.List[string]]::new()
    foreach ($case in @(
        @{ Name = 'UnselectedExperiment'; Selection = ''; Arguments = '@()' },
        @{ Name = 'SeparateEvalFilter'; Selection = '-Suite review-public-api'; Arguments = "@('--eval-filter', 'other/eval.vally.yaml')" },
        @{ Name = 'EqualsEvalFilter'; Selection = '-Suite review-public-api'; Arguments = "@('--eval-filter=other/eval.vally.yaml')" },
        @{ Name = 'SeparateShard'; Selection = '-Suite review-public-api'; Arguments = "@('--shard', '1/2', '--run-id', 'test')" },
        @{ Name = 'EqualsShard'; Selection = '-Suite review-public-api'; Arguments = "@('--shard=1/2', '--run-id=test')" },
        @{ Name = 'DryRunExperiment'; Selection = '-Suite review-public-api'; Arguments = "@('--dry-run')" },
        @{ Name = 'SelectedExperimentChecked'; Selection = '-Suite review-public-api'; Arguments = '@()'; Checked = $true },
        @{ Name = 'SelectedEvalChecked'; Selection = "-Eval 'eng/skill-evals/review-public-api/eval.vally.yaml'"; Arguments = '@()'; Checked = $true }
    )) {
        foreach ($exitCode in @(0, 1)) {
            $env:SKILL_EVAL_FAKE_EXPERIMENT_INVALID = 'true'
            $env:SKILL_EVAL_FAKE_EXIT_CODE = "$exitCode"
            try {
                $caseOutput = Join-Path $testRoot "$($case.Name)-$exitCode"
                $command = "& '$runner' Run -Experiment '$customExperiment' $($case.Selection) " +
                    "-Vally '$fakeVally' -VallyPrefix @() -OutputDirectory '$caseOutput' " +
                    "-Arguments $($case.Arguments); exit `$LASTEXITCODE"
                $messages = (& pwsh -NoProfile -Command $command 2>&1 | Out-String)
                $actualExitCode = $LASTEXITCODE
            } finally {
                Remove-Item Env:SKILL_EVAL_FAKE_EXPERIMENT_INVALID
                Remove-Item Env:SKILL_EVAL_FAKE_EXIT_CODE
            }
            $checked = $case.ContainsKey('Checked')
            $expectedExitCode = if ($checked) { 1 } else { $exitCode }
            $hasCheckFailure = $messages -like "*plan has an invalid threshold*"
            $hasWarning = $messages -like '*result-set checks were not applied*'
            $expectsWarning = -not $checked -and $case.Name -ne 'DryRunExperiment'
            if ($actualExitCode -ne $expectedExitCode -or $hasCheckFailure -ne $checked -or
                $hasWarning -ne $expectsWarning) {
                $experimentCheckFailures.Add(
                    "$($case.Name)Exit$exitCode expected exit $expectedExitCode, checked=$checked, warning=$expectsWarning; " +
                    "got exit $actualExitCode, checked=$hasCheckFailure, warning=$hasWarning. $messages"
                )
                Write-Host "$($case.Name)Exit$exitCode failed."
            } else {
                Write-Host "$($case.Name)Exit$exitCode passed."
            }
        }
    }
    Assert-True ($experimentCheckFailures.Count -eq 0) ($experimentCheckFailures -join "`n")

    $validationRoot = Join-Path $testRoot 'validation-root'
    $validationEvalRoot = Join-Path $validationRoot 'eng/skill-evals'
    New-Item -ItemType Directory -Force -Path `
        (Join-Path $validationEvalRoot 'review-public-api'), `
        (Join-Path $validationEvalRoot 'review-pull-request'), `
        (Join-Path $validationRoot '.github/skills/review-public-api') | Out-Null
    Set-Content (Join-Path $validationEvalRoot 'review-public-api/eval.vally.yaml') 'name: review-public-api'
    Set-Content (Join-Path $validationEvalRoot 'review-pull-request/eval.vally.yaml') 'name: review-pull-request'
    Set-Content (Join-Path $validationRoot '.github/skills/review-public-api/SKILL.md') 'skill'
    Set-Content (Join-Path $validationEvalRoot 'skills-vs-baseline.experiment.yaml') 'standard'
    Set-Content (Join-Path $validationEvalRoot 'skills-smoke.experiment.yaml') 'smoke'
    & git init -q $validationRoot
    & git -C $validationRoot add eng/skill-evals/review-public-api/eval.vally.yaml
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not set up the validation fixture.'
    }
    Remove-Item $record
    & $runner Validate -Root $validationRoot -Suite review-public-api -Vally $fakeVally -VallyPrefix @()
    $genericValidation = @(Read-Invocations | Where-Object { $_.Arguments[0] -eq 'experiment' })
    Assert-True ($genericValidation.Count -eq 2) (
        'Generic suite validation planned the unrelated review experiments.'
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
        $componentOutput = Join-Path $testRoot "component-[$smoke]"
        New-Item -ItemType Directory -Path (Join-Path $componentOutput '2025-12-31T00-00-00') -Force | Out-Null
        $options = @{ Suite = 'review-candidate-verifier'; Vally = $fakeVally; VallyPrefix = @(); OutputDirectory = $componentOutput }
        if ($smoke) {
            $options.Experiment = 'eng/skill-evals/skills-smoke.experiment.yaml'
        }
        & $runner Run @options
        $componentRun = Read-Invocation
        Assert-True ($componentRun[0] -eq 'eval') 'Component run used a skill comparison experiment.'
        $plan = Get-Content -LiteralPath (Join-Path $componentOutput '2026-01-01T00-00-00/plan-snapshot.json') -Raw | ConvertFrom-Json
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
            [double]$ExpectedThreshold = 0.65,
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
        Assert-True ([Math]::Abs([double]$plan.evals[0].threshold - $ExpectedThreshold) -lt 0.000001) (
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

    function Invoke-ComponentFailure {
        param([string]$Scenario, [hashtable]$Environment, [string]$ExpectedFailure)

        foreach ($entry in $Environment.GetEnumerator()) {
            Set-Item "Env:$($entry.Key)" $entry.Value
        }
        $scenarioOutput = Join-Path $testRoot "component-failure-$Scenario"
        $message = ''
        try {
            & $runner Run -Suite review-candidate-verifier -Vally $fakeVally -VallyPrefix @() -OutputDirectory $scenarioOutput
        } catch {
            $message = $_.Exception.Message
        } finally {
            foreach ($key in $Environment.Keys) {
                Remove-Item "Env:$key" -ErrorAction SilentlyContinue
            }
        }
        Assert-True ($message -like "*$ExpectedFailure*") (
            "$Scenario direct component run did not fail through the result gate. Error: $message"
        )
        Assert-True (Test-Path (Join-Path $scenarioOutput '2026-01-01T00-00-00/plan-snapshot.json')) (
            "$Scenario component run lost its plan snapshot."
        )
    }

    Invoke-ComponentFailure -Scenario 'ExitOneEmpty' `
        -Environment @{ SKILL_EVAL_FAKE_EXIT_CODE = '1'; SKILL_EVAL_FAKE_RESULTS = 'empty' } `
        -ExpectedFailure 'invalid or missing completion summary'
    Invoke-ComponentFailure -Scenario 'ExitZeroFailedGate' `
        -Environment @{ SKILL_EVAL_FAKE_EXIT_MODE = 'failed-gate' } `
        -ExpectedFailure "stimulus 'a' score 0.6 is below threshold 0.8"
    Invoke-ComponentFailure -Scenario 'ExitZeroFailedReceipt' `
        -Environment @{ SKILL_EVAL_FAKE_EXIT_MODE = 'failed-receipt' } `
        -ExpectedFailure "'a:0' failed read integrity: evidence-reads-a"

    foreach ($invalidRuns in @('0', '1.5', '"5"')) {
        $componentSpecPath = Join-Path $customComponentDirectory 'eval.vally.yaml'
        $validComponentSpec = Get-Content $componentSpecPath -Raw
        Set-Content $componentSpecPath ($validComponentSpec.Replace('runs: 7', "runs: $invalidRuns"))
        $message = ''
        try {
            & $runner Run -Root $customRoot -Suite review-candidate-verifier `
                -Vally $fakeVally -VallyPrefix @() `
                -OutputDirectory (Join-Path $testRoot 'invalid-yaml-runs')
        } catch {
            $message = $_.Exception.Message
        } finally {
            Set-Content $componentSpecPath $validComponentSpec
        }
        Assert-True ($message -eq 'The component defaults.runs must be a positive integer.') (
            "Invalid YAML runs '$invalidRuns' was not rejected clearly. Error: $message"
        )
    }

    function Invoke-AllSuiteExitOneScenario {
        param(
            [string]$Scenario,
            [bool]$ShouldContinue,
            [string]$ExpectedFailure
        )

        Remove-Item $record -ErrorAction SilentlyContinue
        $env:SKILL_EVAL_FAKE_EXIT_MODE = $Scenario
        $env:SKILL_EVAL_FAKE_EXIT_CODE = '1'
        $message = ''
        try {
            & $runner Run -Root $allSuiteRoot -Vally $fakeVally -VallyPrefix @() `
                -OutputDirectory (Join-Path $testRoot "all-exit-one-$Scenario")
        } catch {
            $message = $_.Exception.Message
        } finally {
            Remove-Item Env:SKILL_EVAL_FAKE_EXIT_MODE -ErrorAction SilentlyContinue
            Remove-Item Env:SKILL_EVAL_FAKE_EXIT_CODE -ErrorAction SilentlyContinue
        }

        if ($ShouldContinue) {
            Assert-True ([string]::IsNullOrWhiteSpace($message)) (
                "A complete passing primary result set did not continue after Vally exit 1: $message"
            )
            Assert-True ($LASTEXITCODE -eq 0) (
                'A tolerated final-suite Vally exit code remained after successful result assertions.'
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
        $expectedCount = 1
        Assert-True ($runInvocations.Count -eq $expectedCount) (
            "$Scenario invoked $($runInvocations.Count) suites; expected $expectedCount."
        )
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
    Assert-CustomComponentPlan -Scenario 'ZeroThresholdBoundary' -ExpectedRuns 7 `
        -ExpectedThreshold 0 -RunArguments @('--threshold=0')
    Assert-CustomComponentPlan -Scenario 'OneThresholdBoundary' -ExpectedRuns 7 `
        -ExpectedThreshold 1 -RunArguments @('--threshold', '1')

    $componentSpecPath = Join-Path $customComponentDirectory 'eval.vally.yaml'
    $validComponentSpec = Get-Content $componentSpecPath -Raw
    foreach ($invalidThreshold in @('null', '""', '"NaN"', '-0.1', '1.1')) {
        Set-Content $componentSpecPath ($validComponentSpec.Replace('threshold: 0.65', "threshold: $invalidThreshold"))
        $message = ''
        try {
            & $runner Run -Root $customRoot -Suite review-candidate-verifier `
                -Vally $fakeVally -VallyPrefix @() `
                -OutputDirectory (Join-Path $testRoot 'invalid-yaml-threshold')
        } catch {
            $message = $_.Exception.Message
        } finally {
            Set-Content $componentSpecPath $validComponentSpec
        }
        Assert-True ($message -like '*threshold must be between 0 and 1*') (
            "Invalid YAML threshold '$invalidThreshold' was accepted. Error: $message"
        )
    }

    foreach ($case in @(
        @{ Name = 'EmptyThreshold'; Arguments = @('--threshold=') },
        @{ Name = 'WhitespaceThreshold'; Arguments = @('--threshold', ' ') },
        @{ Name = 'InvalidThreshold'; Arguments = @('--threshold=not-a-number') }
    )) {
        $message = ''
        try {
            & $runner Run -Root $customRoot -Suite review-candidate-verifier `
                -Vally $fakeVally -VallyPrefix @() `
                -OutputDirectory (Join-Path $testRoot "invalid-$($case.Name)") `
                @($case.Arguments)
        } catch {
            $message = $_.Exception.Message
        }
        Assert-True ($message -eq '--threshold must be between 0 and 1.') (
            "$($case.Name) was not rejected by the threshold parser. Error: $message"
        )
    }

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
    Remove-Item Env:SKILL_EVAL_FAKE_EXIT_CODE -ErrorAction SilentlyContinue
    Remove-Item Env:SKILL_EVAL_FAKE_RESULTS -ErrorAction SilentlyContinue
    Remove-Item Env:SKILL_EVAL_FAKE_EXIT_MODE -ErrorAction SilentlyContinue
    if ($hadPreviousVally) {
        $env:SKILL_EVAL_VALLY = $previousVally
    } else {
        Remove-Item Env:SKILL_EVAL_VALLY -ErrorAction SilentlyContinue
    }
    Remove-Item -Recurse -Force $testRoot
    Remove-Item -Recurse -Force (Join-Path $repoRoot $relativeOutput) -ErrorAction SilentlyContinue
}
