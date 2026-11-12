#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)]
    [string]$TrustedRoot,

    [Parameter(Mandatory = $true)]
    [string]$CandidateRoot,

    [Parameter(Mandatory = $true)]
    [string]$EvalName,

    [Parameter(Mandatory = $true)]
    [string]$Destination
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

# Matches the "yaml" package version resolved by evaluation-tools/package-lock.json
# (a transitive dependency of the pinned @microsoft/vally-cli). Using its bundled
# CLI gives Assert-EvalSpecIsRestricted a real YAML parse instead of a line scan,
# so flow mappings, quoted keys, and block scalars are handled correctly.
$script:yamlCliVersion = '2.9.1'

function Resolve-Directory {
    param(
        [string]$Path,
        [string]$Description
    )

    if (-not (Test-Path $Path -PathType Container)) {
        throw "Missing $Description directory at '$Path'."
    }

    return (Resolve-Path $Path).Path
}

function Test-IsLink {
    param([IO.FileSystemInfo]$Item)

    return $null -ne $Item.LinkType -or (
        ($Item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0
    )
}

function Assert-PathWithinRoot {
    param(
        [string]$Root,
        [string]$Path,
        [string]$Description
    )

    $relative = [IO.Path]::GetRelativePath($Root, $Path)
    if ([IO.Path]::IsPathRooted($relative) -or
        $relative -eq '..' -or
        $relative.StartsWith("../", [StringComparison]::Ordinal) -or
        $relative.StartsWith("..\", [StringComparison]::Ordinal)) {
        throw "$Description escapes its expected root: '$Path'."
    }
}

function Assert-TreeIsSafe {
    param(
        [string]$Path,
        [string]$ExpectedRoot,
        [string]$Description
    )

    Assert-PathWithinRoot $ExpectedRoot $Path $Description
    $relativePath = [IO.Path]::GetRelativePath($ExpectedRoot, $Path)
    $currentPath = $ExpectedRoot
    foreach ($segment in $relativePath.Split(
        [char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar),
        [StringSplitOptions]::RemoveEmptyEntries
    )) {
        $currentPath = Join-Path $currentPath $segment
        $pathItem = Get-Item $currentPath -Force
        if (Test-IsLink $pathItem) {
            throw "$Description '$($pathItem.FullName)' is a symlink or reparse point."
        }
    }

    $rootItem = Get-Item $Path -Force
    if (-not $rootItem.PSIsContainer) {
        return
    }

    foreach ($item in Get-ChildItem $rootItem.FullName -Recurse -Force) {
        Assert-PathWithinRoot $ExpectedRoot $item.FullName $Description
        if (Test-IsLink $item) {
            throw "$Description '$($item.FullName)' is a symlink or reparse point."
        }
    }
}

function ConvertFrom-EvalSpecYaml {
    param([string]$Path)

    $yamlText = Get-Content $Path -Raw
    $jsonText = $yamlText | & npx `
        --yes `
        --registry=https://packagefeedproxy.microsoft.io/npm/ `
        "yaml@$script:yamlCliVersion" `
        --json --single --strict
    if ($LASTEXITCODE -ne 0) {
        throw "Could not parse '$Path' as a single, strict YAML document."
    }
    if ([string]::IsNullOrWhiteSpace($jsonText)) {
        return $null
    }
    return $jsonText | ConvertFrom-Json -AsHashtable -Depth 100
}

function Find-ForbiddenEvalField {
    param(
        $Node,
        [string]$DispositionMode
    )

    if ($Node -is [System.Collections.IDictionary]) {
        foreach ($environmentKey in 'environment', 'agent_environment', 'grading_environment') {
            $environmentBlock = $Node[$environmentKey]
            if ($environmentBlock -is [System.Collections.IDictionary]) {
                if ($environmentBlock.Contains('commands')) {
                    return "declares a forbidden '$environmentKey.commands' field; staged fixture binaries are made executable automatically."
                }
                if ($environmentBlock.Contains('mcpServers')) {
                    return "declares a forbidden '$environmentKey.mcpServers' field; per-eval MCP servers are not allowed."
                }
                if ($DispositionMode -and $environmentKey -eq 'grading_environment' -and
                    $environmentBlock.Contains('files')) {
                    $files = @($environmentBlock['files'])
                    if ($files.Count -ne 1 -or $files[0] -isnot [Collections.IDictionary] -or
                        $files[0]['src'] -cne '../grade_disposition.mjs' -or
                        $files[0]['dest'] -cne 'grade_disposition.mjs') {
                        return "declares forbidden grading_environment.files; only '../grade_disposition.mjs' to 'grade_disposition.mjs' is allowed."
                    }
                }
            }
        }

        if ($DispositionMode -and $Node['type'] -ceq 'program') {
            $config = $Node['config']
            if ($config -isnot [Collections.IDictionary] -or $config.Count -ne 3 -or
                $config['program'] -cne 'node' -or
                $config['args'] -isnot [Collections.IEnumerable] -or $config['args'] -is [string] -or
                @($config['args']).Count -ne 1 -or $config['args'][0] -cne 'grade_disposition.mjs' -or
                $config['env'] -isnot [Collections.IDictionary] -or $config['env'].Count -ne 1 -or
                $config['env']['DISPOSITION_MODE'] -cne $DispositionMode) {
                return "declares a forbidden program grader; use only node grade_disposition.mjs with DISPOSITION_MODE=$DispositionMode."
            }
        }

        $executorBlock = $Node['executor']
        if ($executorBlock -is [System.Collections.IDictionary]) {
            $executorConfig = $executorBlock['config']
            if ($executorConfig -is [System.Collections.IDictionary] -and
                $executorConfig.Contains('provider')) {
                return "declares a forbidden 'executor.config.provider' override."
            }
        }

        foreach ($value in $Node.Values) {
            $found = Find-ForbiddenEvalField $value $DispositionMode
            if ($found) {
                return $found
            }
        }
    } elseif ($Node -is [System.Collections.IEnumerable] -and $Node -isnot [string]) {
        foreach ($item in $Node) {
            $found = Find-ForbiddenEvalField $item $DispositionMode
            if ($found) {
                return $found
            }
        }
    }

    return $null
}

function Assert-EvalSpecIsRestricted {
    param(
        [string]$Path,
        [string]$Description
    )

    $parsed = ConvertFrom-EvalSpecYaml $Path
    $dispositionMode = switch ($EvalName) {
        'review-pull-request' { 'review' }
        'review-candidate-verifier' { 'component' }
    }
    $violation = Find-ForbiddenEvalField $parsed $dispositionMode
    if ($violation) {
        throw "$Description $violation"
    }
    if ($dispositionMode) {
        if ($parsed -isnot [Collections.IDictionary] -or
            $parsed['grading_environment'] -isnot [Collections.IDictionary] -or
            -not $parsed['grading_environment'].Contains('files')) {
            throw "$Description must declare the trusted grading_environment.files mapping."
        }
        foreach ($stimulus in @($parsed['stimuli'])) {
            $primary = @($stimulus['graders'] | Where-Object {
                $_ -is [Collections.IDictionary] -and
                $_['name'] -ceq "disposition-$($stimulus['name'])"
            })
            if ($primary.Count -ne 1 -or $primary[0]['type'] -cne 'program') {
                throw "$Description must declare exactly one primary disposition program grader for '$($stimulus['name'])'."
            }
        }
    }
}

function Test-PathWithinRoot {
    param(
        [string]$Root,
        [string]$Path
    )

    $relative = [IO.Path]::GetRelativePath($Root, $Path)
    return -not [IO.Path]::IsPathRooted($relative) -and
        $relative -ne '..' -and
        -not $relative.StartsWith("../", [StringComparison]::Ordinal) -and
        -not $relative.StartsWith("..\", [StringComparison]::Ordinal)
}

if ($EvalName -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$' -or
    $EvalName -eq '.' -or
    $EvalName.Contains('..', [StringComparison]::Ordinal)) {
    throw "Invalid eval name '$EvalName'."
}

$trustedRootPath = Resolve-Directory $TrustedRoot 'trusted control-plane'
$candidateRootPath = Resolve-Directory $CandidateRoot 'candidate'
$destinationPath = [IO.Path]::GetFullPath($Destination)
if (Test-Path $destinationPath) {
    throw "Staging destination '$destinationPath' already exists."
}
if ((Test-PathWithinRoot $trustedRootPath $destinationPath) -or
    (Test-PathWithinRoot $candidateRootPath $destinationPath)) {
    throw "Staging destination '$destinationPath' must be outside both source trees."
}

$trustedFiles = @(
    'eng/skill-evals/run.ps1'
    'eng/skill-evals/assert_results.ps1'
    'eng/skill-evals/skills-vs-baseline.experiment.yaml'
    'eng/skill-evals/skills-smoke.experiment.yaml'
)
if ($EvalName -in @('review-pull-request', 'review-candidate-verifier')) {
    $trustedFiles += 'eng/skill-evals/grade_disposition.mjs'
    if ($EvalName -eq 'review-pull-request') {
        $trustedFiles += @(
            'eng/skill-evals/review-vs-baseline.experiment.yaml'
            'eng/skill-evals/review-smoke.experiment.yaml'
        )
    }
}
$candidateSkill = Join-Path $candidateRootPath ".github/skills/$EvalName"
$candidateEvalRoot = Join-Path $candidateRootPath "eng/skill-evals/$EvalName"
$candidateEval = Join-Path $candidateEvalRoot 'eval.vally.yaml'
$candidateFixtures = Join-Path $candidateEvalRoot 'fixtures'

if ($EvalName -ne 'review-candidate-verifier' -and
    -not (Test-Path $candidateSkill -PathType Container)) {
    throw "Missing candidate skill directory at '$candidateSkill'."
}
if ($EvalName -ne 'review-candidate-verifier') {
    Assert-TreeIsSafe $candidateSkill $candidateRootPath "Candidate skill '$EvalName'"
}
if (-not (Test-Path $candidateEval -PathType Leaf)) {
    throw "Missing candidate eval specification at '$candidateEval'."
}
Assert-TreeIsSafe $candidateEval $candidateRootPath "Candidate eval '$EvalName'"
Assert-EvalSpecIsRestricted $candidateEval "Candidate eval '$EvalName'"
if (Test-Path $candidateFixtures) {
    if (-not (Test-Path $candidateFixtures -PathType Container)) {
        throw "Candidate fixtures path '$candidateFixtures' is not a directory."
    }
    Assert-TreeIsSafe $candidateFixtures $candidateRootPath "Candidate fixtures '$EvalName'"
}

foreach ($relativePath in $trustedFiles) {
    $source = Join-Path $trustedRootPath $relativePath
    if (-not (Test-Path $source -PathType Leaf)) {
        throw "Missing trusted control-plane file '$relativePath'."
    }
    Assert-TreeIsSafe $source $trustedRootPath "Trusted control-plane file '$relativePath'"
}

New-Item -ItemType Directory -Path $destinationPath | Out-Null
try {
    foreach ($relativePath in $trustedFiles) {
        $target = Join-Path $destinationPath $relativePath
        New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force |
            Out-Null
        Copy-Item (Join-Path $trustedRootPath $relativePath) $target
    }

    if ($EvalName -ne 'review-candidate-verifier') {
        $skillTarget = Join-Path $destinationPath ".github/skills/$EvalName"
        New-Item -ItemType Directory -Path (Split-Path $skillTarget -Parent) -Force |
            Out-Null
        Copy-Item $candidateSkill $skillTarget -Recurse -Force
    }

    $evalTargetRoot = Join-Path $destinationPath "eng/skill-evals/$EvalName"
    New-Item -ItemType Directory -Path $evalTargetRoot -Force | Out-Null
    Copy-Item $candidateEval (Join-Path $evalTargetRoot 'eval.vally.yaml')
    if (Test-Path $candidateFixtures -PathType Container) {
        $fixturesTarget = Join-Path $evalTargetRoot 'fixtures'
        Copy-Item $candidateFixtures $fixturesTarget -Recurse -Force
        if (-not $IsWindows) {
            Get-ChildItem $fixturesTarget -Recurse -File -Force | ForEach-Object {
                chmod +x $_.FullName
            }
        }
    }

    if ($EvalName -in @('review-pull-request', 'review-candidate-verifier')) {
        $reviewSupportFiles = @(
            '.github/agents/review-candidate-verifier.agent.md'
        )
        if ($EvalName -eq 'review-pull-request') {
            $reviewSupportFiles += @(
                '.github/agents/review-guide-worker.agent.md'
                'docs/CrossCuttingGuidance.md'
                'docs/BlazorComponentsGuidance.md'
            )
        }
        foreach ($relativePath in $reviewSupportFiles) {
            $sourcePath = Join-Path $candidateRootPath $relativePath
            if (-not (Test-Path $sourcePath -PathType Leaf)) {
                throw "Missing candidate support file '$relativePath'."
            }
            Assert-TreeIsSafe $sourcePath $candidateRootPath "Candidate support file '$relativePath'"
            $targetPath = Join-Path $destinationPath $relativePath
            New-Item -ItemType Directory -Path (Split-Path $targetPath -Parent) -Force |
                Out-Null
            Copy-Item $sourcePath $targetPath -Force
        }
    }

    Assert-TreeIsSafe $destinationPath $destinationPath 'Staged evaluation tree'
} catch {
    Remove-Item $destinationPath -Recurse -Force -ErrorAction SilentlyContinue
    throw
}

Write-Host "Staged trusted control plane with exact candidate data for '$EvalName'."
