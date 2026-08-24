#requires -Version 7.0
param(
    [Parameter(Position = 0)]
    [ValidateSet('Validate', 'Test', 'Lint', 'Run', 'RunAgent')]
    [string]$Action = 'Validate',

    [string]$Eval,

    [string]$OutputDirectory,

    [string]$Root,

    [string]$Vally = 'npx',

    [string[]]$VallyPrefix = @(
        '--yes',
        '--registry=https://packagefeedproxy.microsoft.io/npm/',
        '@microsoft/vally-cli@0.13.0'
    ),

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$repoRoot = if ($Root) {
    (Resolve-Path $Root).Path
} else {
    (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
}
$evalRoot = Join-Path $repoRoot 'eng/skill-evals'
$runtimeRoot = Join-Path $repoRoot '.github/skills'
$experiment = Join-Path $evalRoot 'skills-vs-baseline.experiment.yaml'
$readinessEvalRoot = Join-Path $evalRoot 'blazor-component-readiness'
$readinessRepresentative = Join-Path $readinessEvalRoot 'representative.vally.yaml'
$readinessRegression = Join-Path $readinessEvalRoot 'regression.vally.yaml'
$readinessExecutor = Join-Path $readinessEvalRoot 'copilot-agent-executor.mjs'
$readinessExecutorTest = Join-Path $readinessEvalRoot 'copilot-agent-executor.test.mjs'
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
    Invoke-VallyIsolated @(
        'experiment',
        'run', $experiment,
        '--compare',
        '--dry-run',
        '--output-dir', 'plan-results'
    )
}

function Resolve-OutputDirectory {
    if ($OutputDirectory) {
        if ([IO.Path]::IsPathRooted($OutputDirectory)) {
            return $OutputDirectory
        }
        return Join-Path $repoRoot $OutputDirectory
    }

    return Join-Path $repoRoot 'artifacts/skill-evals'
}

function Test-IsReadinessAgentEval {
    param([string]$Path)

    return [string]::Equals(
        $Path,
        $readinessRepresentative,
        [StringComparison]::OrdinalIgnoreCase
    ) -or [string]::Equals(
        $Path,
        $readinessRegression,
        [StringComparison]::OrdinalIgnoreCase
    )
}

function Assert-AgentArgumentsSafe {
    foreach ($reserved in @(
        '-e',
        '--backend',
        '--eval-spec',
        '--executor',
        '--executor-plugin',
        '--skill-dir',
        '--skip-validate',
        '--work-dir',
        '--workspace'
    )) {
        if ($Arguments -contains $reserved -or
            @($Arguments | Where-Object { $_.StartsWith("$reserved=") }).Count -gt 0) {
            throw "RunAgent owns $reserved to preserve exact agent binding and workspace isolation."
        }
    }
}

function Invoke-ReadinessAgentEval {
    $resolvedEval = Resolve-Eval
    if (-not $resolvedEval) {
        $resolvedEval = (Resolve-Path $readinessRepresentative).Path
    }
    if (-not (Test-IsReadinessAgentEval $resolvedEval)) {
        throw 'RunAgent only supports the component-readiness representative and regression suites.'
    }

    Assert-AgentArgumentsSafe
    $output = Resolve-OutputDirectory
    $workDirectory = Join-Path ([IO.Path]::GetTempPath()) (
        'aspnetcore-readiness-agent-evals-' + [guid]::NewGuid().ToString('N')
    )
    $workspace = Join-Path $workDirectory 'workspaces'
    New-Item -ItemType Directory -Path $workDirectory | Out-Null
    try {
        Push-Location $workDirectory
        $vallyArguments = @(
            'eval',
            '--eval-spec', $resolvedEval,
            '--executor-plugin', $readinessExecutor,
            '--executor', 'blazor-component-readiness-agent',
            '--workspace', $workspace,
            '--workers', '1',
            '--output-dir', $output
        )
        $vallyArguments += $Arguments
        Invoke-Vally $vallyArguments
    } finally {
        Pop-Location
        Remove-Item -Recurse -Force $workDirectory
    }
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
    if (-not (Test-Path $experiment -PathType Leaf)) {
        $errors.Add("Missing standard experiment: $experiment")
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
        Invoke-VallyExperimentDryRun
    }
    'Test' {
        & (Join-Path $PSScriptRoot 'test_validate.ps1')
        & (Join-Path $PSScriptRoot 'test_run.ps1')
        $global:LASTEXITCODE = 0
        & node --test $readinessExecutorTest
        if (-not $? -or $LASTEXITCODE -ne 0) {
            throw "Component-readiness custom-agent executor tests failed."
        }
    }
    'Lint' {
        Invoke-VallyLint
    }
    'Run' {
        $resolvedEval = Resolve-Eval
        if ($resolvedEval -and (Test-IsReadinessAgentEval $resolvedEval)) {
            throw 'Use RunAgent for component-readiness custom-agent suites.'
        }
        $output = Resolve-OutputDirectory

        if (-not $resolvedEval -or (Split-Path $resolvedEval -Leaf) -eq 'eval.vally.yaml') {
            $vallyArguments = @(
                'experiment',
                'run', $experiment,
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
    'RunAgent' {
        Invoke-ReadinessAgentEval
    }
}
