#requires -Version 7.0
param(
    [Parameter(Position = 0)]
    [ValidateSet('Validate', 'Test', 'Lint', 'Run')]
    [string]$Action = 'Validate',

    [string]$Eval,

    [string]$Suite,

    [string]$Experiment,

    [string]$OutputDirectory,

    [string]$Root,

    [string]$Vally = 'npx',

    [string[]]$VallyPrefix = @(
        '--yes',
        '--registry=https://packagefeedproxy.microsoft.io/npm/',
        '@microsoft/vally-cli@0.19.0'
    ),

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

if (-not $PSBoundParameters.ContainsKey('Vally')) {
    $installedVally = if ($IsWindows) {
        Join-Path $PSScriptRoot 'evaluation-tools/node_modules/.bin/vally.cmd'
    } else {
        Join-Path $PSScriptRoot 'evaluation-tools/node_modules/.bin/vally'
    }
    if (-not [string]::IsNullOrWhiteSpace($env:SKILL_EVAL_VALLY)) {
        $Vally = $env:SKILL_EVAL_VALLY
        if (-not $PSBoundParameters.ContainsKey('VallyPrefix')) {
            $VallyPrefix = @()
        }
    } elseif (Test-Path $installedVally -PathType Leaf) {
        $Vally = $installedVally
        if (-not $PSBoundParameters.ContainsKey('VallyPrefix')) {
            $VallyPrefix = @()
        }
    } elseif ($Action -eq 'Run') {
        throw 'Run requires the pinned evaluation tools. Run npm ci --prefix eng/skill-evals/evaluation-tools first.'
    }
}

$repoRoot = if ($Root) {
    (Resolve-Path $Root).Path
} else {
    (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
}
$evalRoot = Join-Path $repoRoot 'eng/skill-evals'
if ($Suite) {
    if ($Suite -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$' -or $Suite.Contains('..')) {
        throw "Invalid suite name '$Suite'."
    }
    if ($Eval) {
        throw 'Specify either -Suite or -Eval, not both.'
    }
    $Eval = Join-Path $evalRoot "$Suite/eval.vally.yaml"
    if (-not (Test-Path $Eval -PathType Leaf)) {
        throw "Missing suite specification at '$Eval'."
    }
}
$runtimeRoot = Join-Path $repoRoot '.github/skills'
$standardExperiment = Join-Path $evalRoot 'skills-vs-baseline.experiment.yaml'
$smokeExperiment = Join-Path $evalRoot 'skills-smoke.experiment.yaml'
$hasExplicitExperiment = -not [string]::IsNullOrWhiteSpace($Experiment)
$selectedExperiment = if ($hasExplicitExperiment) {
    $candidate = if ([IO.Path]::IsPathRooted($Experiment)) {
        $Experiment
    } else {
        Join-Path $repoRoot $Experiment
    }
    (Resolve-Path $candidate).Path
} else {
    $standardExperiment
}
$script:vallyInitialized = $false
$reviewExperiment = Join-Path $evalRoot 'review-vs-baseline.experiment.yaml'
$reviewSmokeExperiment = Join-Path $evalRoot 'review-smoke.experiment.yaml'
$reviewEval = Join-Path $evalRoot 'review-pull-request/eval.vally.yaml'

function Resolve-Eval {
    if (-not $Eval) {
        return $null
    }

    $candidate = if ([IO.Path]::IsPathRooted($Eval)) {
        $Eval
    } else {
        Join-Path $repoRoot $Eval
    }
    return (Resolve-Path $candidate).Path
}

function Get-EvalSpecs {
    $resolvedEval = Resolve-Eval
    if ($resolvedEval) {
        return @($resolvedEval)
    }
    return @(
        Get-ChildItem $evalRoot -Recurse -File -Filter '*.vally.yaml' |
            ForEach-Object FullName
    )
}

if ($Eval -and -not $Suite) {
    $resolvedSelection = Resolve-Eval
    $relativeSelection = [IO.Path]::GetRelativePath($evalRoot, $resolvedSelection).Replace('\', '/')
    if ($relativeSelection -match '^[^/]+/eval\.vally\.yaml$') {
        $Suite = Split-Path (Split-Path $resolvedSelection -Parent) -Leaf
    }
}

function Invoke-Vally {
    param([string[]]$VallyArguments)

    $command = Get-Command $Vally -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "Vally command '$Vally' was not found."
    }

    if (-not $script:vallyInitialized) {
        $global:LASTEXITCODE = 0
        $versionOutput = & $Vally @VallyPrefix --version
        $versionSucceeded = $?
        $versionExitCode = $LASTEXITCODE
        if (-not $versionSucceeded -or $versionExitCode -ne 0) {
            throw "Vally version probe failed with exit code $versionExitCode."
        }

        $identity = ($versionOutput | Out-String).Trim()
        $commandPath = if ($command.Path) { $command.Path } else { $Vally }
        $invocation = (@($commandPath) + $VallyPrefix) -join ' '
        Write-Host "Using Vally: $invocation (reported version: $identity)"
        $script:vallyInitialized = $true
    }

    $global:LASTEXITCODE = 0
    & $Vally @VallyPrefix @VallyArguments
    $invocationSucceeded = $?
    $invocationExitCode = $LASTEXITCODE
    if (-not $invocationSucceeded -or $invocationExitCode -ne 0) {
        if ($invocationExitCode -eq 0) {
            $invocationExitCode = 1
        }
        exit $invocationExitCode
    }
}

function Invoke-VallyLint {
    $specs = @(Get-EvalSpecs)
    if ($specs.Count -eq 0) {
        throw "No Vally eval specifications were found under '$evalRoot'."
    }

    foreach ($spec in $specs) {
        $vallyArguments = @('lint', '--strict', $runtimeRoot, '--eval-spec', $spec)
        $vallyArguments += $Arguments
        Invoke-Vally $vallyArguments
    }
}

function Invoke-VallyIsolated {
    param([string[]]$VallyArguments)

    $workDirectory = Join-Path ([IO.Path]::GetTempPath()) (
        'aspnetcore-skill-evals-' + [guid]::NewGuid().ToString('N')
    )
    New-Item -ItemType Directory -Path $workDirectory | Out-Null
    try {
        Push-Location $workDirectory
        Invoke-Vally $VallyArguments
    } finally {
        Pop-Location
        Remove-Item -Recurse -Force $workDirectory
    }
}

function Invoke-VallyExperimentDryRun {
    param([string]$ExperimentPath)

    $vallyArguments = @(
        'experiment',
        'run', $ExperimentPath,
        '--compare',
        '--dry-run',
        '--output-dir', 'plan-results'
    )
    if ($Suite) {
        $vallyArguments += @('--eval-filter', "$Suite/eval.vally.yaml")
    }
    Invoke-VallyIsolated $vallyArguments
}

function Get-TrackedFiles {
    $global:LASTEXITCODE = 0
    $files = @(& git -C $repoRoot -c core.quotepath=false ls-files)
    if ($LASTEXITCODE -ne 0) {
        throw "Could not read tracked files from '$repoRoot'."
    }
    return [Collections.Generic.HashSet[string]]::new(
        [string[]]($files | ForEach-Object { $_.Replace('\', '/') })
    )
}

function Test-IsLink {
    param([IO.FileSystemInfo]$Item)

    return $null -ne $Item.LinkType -or (
        ($Item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0
    )
}

function Get-LayoutErrors {
    $errors = [Collections.Generic.List[string]]::new()
    if (-not (Test-Path $standardExperiment -PathType Leaf)) {
        $errors.Add("Missing standard experiment: $standardExperiment")
    }
    if (-not (Test-Path $smokeExperiment -PathType Leaf)) {
        $errors.Add("Missing smoke experiment: $smokeExperiment")
    }

    $layoutEvalRoot = if ($Suite) { Join-Path $evalRoot $Suite } else { $evalRoot }
    $standardSpecs = @(
        if ($Suite) {
            $Eval
        } else {
            Get-ChildItem $evalRoot -Directory -ErrorAction SilentlyContinue |
                ForEach-Object { Join-Path $_.FullName 'eval.vally.yaml' } |
                Where-Object { Test-Path $_ -PathType Leaf }
        }
    )
    if ($standardSpecs.Count -eq 0) {
        $errors.Add("No standard */eval.vally.yaml specs were found under '$evalRoot'.")
    }
    foreach ($spec in $standardSpecs) {
        $skillName = Split-Path (Split-Path $spec -Parent) -Leaf
        $skillFile = Join-Path $runtimeRoot "$skillName/SKILL.md"
        if ($skillName -ne 'review-candidate-verifier' -and
            -not (Test-Path $skillFile -PathType Leaf)) {
            $errors.Add("$spec has no matching runtime skill at '$skillFile'.")
        }
    }

    if (Test-Path $runtimeRoot -PathType Container) {
        foreach ($spec in Get-ChildItem $runtimeRoot -Recurse -File -Filter '*.vally.yaml') {
            $errors.Add("$($spec.FullName) is eval-only and belongs under '$evalRoot'.")
        }
        foreach ($directory in Get-ChildItem $runtimeRoot -Recurse -Directory) {
            if ($directory.Name -eq 'evals') {
                $errors.Add("$($directory.FullName) is eval-only and belongs under '$evalRoot'.")
            }
        }
    }

    $tracked = Get-TrackedFiles
    foreach ($spec in Get-ChildItem $layoutEvalRoot -Recurse -File -Filter '*.vally.yaml') {
        $relative = [IO.Path]::GetRelativePath($repoRoot, $spec.FullName).Replace('\', '/')
        if (-not $tracked.Contains($relative)) {
            $errors.Add("$relative is not tracked by git and will not exist in CI.")
        }
    }

    foreach ($fixtures in Get-ChildItem $layoutEvalRoot -Recurse -Directory |
        Where-Object Name -eq 'fixtures') {
        $items = @(Get-ChildItem $fixtures.FullName -Recurse -Force)
        $fixtureFiles = @($items | Where-Object { -not $_.PSIsContainer })
        if ($fixtureFiles.Count -eq 0) {
            $errors.Add("$($fixtures.FullName) is empty and cannot be represented in git.")
            continue
        }
        foreach ($item in @($fixtures) + $items) {
            if (Test-IsLink $item) {
                $errors.Add("$($item.FullName) is a symlink; eval fixtures must be self-contained.")
            }
            if (-not $item.PSIsContainer) {
                $relative = [IO.Path]::GetRelativePath(
                    $repoRoot,
                    $item.FullName
                ).Replace('\', '/')
                if (-not $tracked.Contains($relative)) {
                    $errors.Add("$relative is not tracked by git and will not exist in CI.")
                }
            }
        }
    }
    return $errors
}

function Invoke-LayoutValidation {
    $errors = @(Get-LayoutErrors)
    if ($errors.Count -gt 0) {
        throw "Skill-eval layout validation failed:`n  - $($errors -join "`n  - ")"
    }
    Write-Host 'Skill-eval layout validation passed.'
}

switch ($Action) {
    'Validate' {
        Invoke-LayoutValidation
        Invoke-VallyLint
        if ($Suite -eq 'review-candidate-verifier') {
            # Component specs have no skill treatment. Lint validates their schema.
        } elseif ($hasExplicitExperiment) {
            Invoke-VallyExperimentDryRun $selectedExperiment
        } elseif ($Suite -eq 'review-pull-request' -and (Test-Path $reviewEval)) {
            Invoke-VallyExperimentDryRun $reviewExperiment
            Invoke-VallyExperimentDryRun $reviewSmokeExperiment
        } else {
            Invoke-VallyExperimentDryRun $standardExperiment
            Invoke-VallyExperimentDryRun $smokeExperiment
            if (Test-Path $reviewEval) {
                Invoke-VallyExperimentDryRun $reviewExperiment
                Invoke-VallyExperimentDryRun $reviewSmokeExperiment
            }
        }
    }
    'Test' {
        & (Join-Path $PSScriptRoot 'test_validate.ps1')
        & (Join-Path $PSScriptRoot 'test_run.ps1')
        & (Join-Path $PSScriptRoot 'test_assert_results.ps1')
        & (Join-Path $PSScriptRoot 'test_stage_run.ps1')
        & (Join-Path $PSScriptRoot 'test_workflow.ps1')
        if (-not $Suite -or $Suite -in @('review-pull-request', 'review-candidate-verifier')) {
            & (Join-Path $PSScriptRoot 'tests/test_review_fixtures.ps1')
        }
        if (Test-Path (Join-Path $PSScriptRoot 'test_disposition.mjs')) {
            & node --test (Join-Path $PSScriptRoot 'test_disposition.mjs')
            if ($LASTEXITCODE -ne 0) {
                throw 'Disposition grader self-test failed.'
            }
        }
    }
    'Lint' {
        Invoke-VallyLint
    }
    'Run' {
        $resolvedEval = Resolve-Eval
        $output = if ($OutputDirectory) {
            if ([IO.Path]::IsPathRooted($OutputDirectory)) {
                $OutputDirectory
            } else {
                Join-Path $repoRoot $OutputDirectory
            }
        } else {
            Join-Path $repoRoot 'artifacts/skill-evals'
        }

        if (-not $resolvedEval -and -not $hasExplicitExperiment) {
            foreach ($directory in Get-ChildItem $evalRoot -Directory) {
                if (Test-Path (Join-Path $directory.FullName 'eval.vally.yaml')) {
                    $suiteOutput = Join-Path $output $directory.Name
                    & $PSCommandPath Run -Root $repoRoot -Suite $directory.Name `
                        -OutputDirectory $suiteOutput `
                        -Vally $Vally -VallyPrefix $VallyPrefix @Arguments
                    if (-not $? -or $LASTEXITCODE -ne 0) {
                        if ($LASTEXITCODE -ne 1) {
                            throw "The '$($directory.Name)' suite run failed."
                        }
                        & (Join-Path $PSScriptRoot 'assert_results.ps1') -OutputDirectory $suiteOutput
                        $global:LASTEXITCODE = 0
                        Write-Warning "Vally exited with code 1 for '$($directory.Name)'. Complete primary trials passed the applicable result-set checks."
                    }
                }
            }
            break
        }
        $selectedSuite = if ($resolvedEval) {
            Split-Path (Split-Path $resolvedEval -Parent) -Leaf
        }
        if ($selectedSuite -eq 'review-pull-request' -and (Test-Path $reviewExperiment)) {
            $selectedExperiment = if ($selectedExperiment -eq $smokeExperiment) {
                $reviewSmokeExperiment
            } elseif (-not $hasExplicitExperiment) {
                $reviewExperiment
            } else {
                $selectedExperiment
            }
        }
        if ($selectedSuite -eq 'review-candidate-verifier') {
            $componentJson = Get-Content $resolvedEval -Raw | & npx `
                --yes --registry=https://packagefeedproxy.microsoft.io/npm/ `
                yaml@2.9.1 --json --single --strict
            if ($LASTEXITCODE -ne 0) {
                throw "Could not parse '$resolvedEval' as a single, strict YAML document."
            }
            $componentSpec = $componentJson | ConvertFrom-Json -AsHashtable -Depth 100
            $componentDefaults = $componentSpec['defaults']
            $componentScoring = $componentSpec['scoring']
            $runs = if ($selectedExperiment -in @($smokeExperiment, $reviewSmokeExperiment)) {
                1
            } elseif ($componentDefaults -is [Collections.IDictionary] -and $null -ne $componentDefaults['runs']) {
                [int]$componentDefaults['runs']
            } else {
                1
            }
            $threshold = if ($componentScoring -is [Collections.IDictionary] -and $null -ne $componentScoring['threshold']) {
                [double]$componentScoring['threshold']
            } else {
                1
            }
            for ($index = 0; $Arguments -and $index -lt $Arguments.Count; $index++) {
                if ($Arguments[$index] -match '^--(runs|threshold)(?:=(.*))?$') {
                    $option = $Matches[1]
                    $value = if ($Arguments[$index].Contains('=')) {
                        $Matches[2]
                    } else {
                        if (++$index -ge $Arguments.Count) {
                            throw "Missing value for --$option."
                        }
                        $Arguments[$index]
                    }
                    if ($option -eq 'runs') {
                        $parsedRuns = 0
                        if (-not [int]::TryParse($value, [ref]$parsedRuns) -or $parsedRuns -lt 1) {
                            throw '--runs must be a positive integer.'
                        }
                        $runs = $parsedRuns
                    } else {
                        $parsedThreshold = 0.0
                        if ([string]::IsNullOrWhiteSpace($value) -or
                            -not [double]::TryParse(
                                $value,
                                [Globalization.NumberStyles]::Float,
                                [Globalization.CultureInfo]::InvariantCulture,
                                [ref]$parsedThreshold
                            ) -or
                            -not [double]::IsFinite($parsedThreshold) -or
                            $parsedThreshold -lt 0 -or $parsedThreshold -gt 1) {
                            throw '--threshold must be between 0 and 1.'
                        }
                        $threshold = $parsedThreshold
                    }
                }
            }
            $plannedStimulusNames = @($componentSpec['stimuli'] | ForEach-Object { $_['name'] })
            $vallyArguments = @(
                'eval', '--eval-spec', $resolvedEval,
                '--runs', $runs.ToString(),
                '--output-dir', $output
            )
        } elseif (-not $resolvedEval -or (Split-Path $resolvedEval -Leaf) -eq 'eval.vally.yaml') {
            $vallyArguments = @(
                'experiment',
                'run', $selectedExperiment,
                '--compare',
                '--output-dir', $output
            )
            if ($resolvedEval) {
                $relativeEval = [IO.Path]::GetRelativePath(
                    $evalRoot,
                    $resolvedEval
                ).Replace('\', '/')
                $vallyArguments += @('--eval-filter', $relativeEval)
            }
        } else {
            $vallyArguments = @(
                'eval',
                '--eval-spec', $resolvedEval,
                '--output-dir', $output
            )
        }

        $vallyArguments += $Arguments
        $existingRunDirectoryNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        if ($selectedSuite -eq 'review-candidate-verifier' -and (Test-Path $output -PathType Container)) {
            foreach ($directory in Get-ChildItem $output -Directory -ErrorAction SilentlyContinue) {
                [void]$existingRunDirectoryNames.Add($directory.Name)
            }
        }
        $invocationCompleted = $false
        try {
            Invoke-VallyIsolated $vallyArguments
            $invocationCompleted = $true
        } finally {
            if ($selectedSuite -eq 'review-candidate-verifier') {
                $runDirectories = @(
                    Get-ChildItem $output -Directory -ErrorAction SilentlyContinue |
                        Where-Object { -not $existingRunDirectoryNames.Contains($_.Name) }
                )
                if ($runDirectories.Count -eq 1 -and
                    (Test-Path (Join-Path $runDirectories[0].FullName 'results.jsonl'))) {
                    @{
                        evals = @(@{
                            variant = 'current'
                            plannedStimulusNames = $plannedStimulusNames
                            plannedStimulusCount = $plannedStimulusNames.Count
                            runs = $runs
                            threshold = $threshold
                        })
                    } | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $runDirectories[0].FullName 'plan-snapshot.json')
                } elseif ($invocationCompleted) {
                    throw 'Expected one completed component output directory with results.jsonl.'
                }
            }
        }
    }
}
