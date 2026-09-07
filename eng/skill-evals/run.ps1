#requires -Version 7.0
param(
    [Parameter(Position = 0)]
    [ValidateSet('Validate', 'Test', 'Lint', 'Run')]
    [string]$Action = 'Validate',

    [string]$Eval,

    [string]$Experiment,

    [string]$OutputDirectory,

    [string]$Root,

    [string]$Vally = 'npx',

    [string[]]$VallyPrefix = @(
        '--yes',
        '--registry=https://packagefeedproxy.microsoft.io/npm/',
        '@microsoft/vally-cli@0.14.0'
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

    Invoke-VallyIsolated @(
        'experiment',
        'run', $ExperimentPath,
        '--compare',
        '--dry-run',
        '--output-dir', 'plan-results'
    )
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

    $standardSpecs = @(
        Get-ChildItem $evalRoot -Directory -ErrorAction SilentlyContinue |
            ForEach-Object { Join-Path $_.FullName 'eval.vally.yaml' } |
            Where-Object { Test-Path $_ -PathType Leaf }
    )
    if ($standardSpecs.Count -eq 0) {
        $errors.Add("No standard */eval.vally.yaml specs were found under '$evalRoot'.")
    }
    foreach ($spec in $standardSpecs) {
        $skillName = Split-Path (Split-Path $spec -Parent) -Leaf
        $skillFile = Join-Path $runtimeRoot "$skillName/SKILL.md"
        if (-not (Test-Path $skillFile -PathType Leaf)) {
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
    foreach ($spec in Get-ChildItem $evalRoot -Recurse -File -Filter '*.vally.yaml') {
        $relative = [IO.Path]::GetRelativePath($repoRoot, $spec.FullName).Replace('\', '/')
        if (-not $tracked.Contains($relative)) {
            $errors.Add("$relative is not tracked by git and will not exist in CI.")
        }
    }

    foreach ($fixtures in Get-ChildItem $evalRoot -Recurse -Directory |
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
        if ($hasExplicitExperiment) {
            Invoke-VallyExperimentDryRun $selectedExperiment
        } else {
            Invoke-VallyExperimentDryRun $standardExperiment
            Invoke-VallyExperimentDryRun $smokeExperiment
        }
    }
    'Test' {
        & (Join-Path $PSScriptRoot 'test_validate.ps1')
        & (Join-Path $PSScriptRoot 'test_run.ps1')
        & (Join-Path $PSScriptRoot 'test_assert_results.ps1')
        & (Join-Path $PSScriptRoot 'test_stage_run.ps1')
        & (Join-Path $PSScriptRoot 'test_workflow.ps1')
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

        if (-not $resolvedEval -or (Split-Path $resolvedEval -Leaf) -eq 'eval.vally.yaml') {
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
        Invoke-VallyIsolated $vallyArguments
    }
}
