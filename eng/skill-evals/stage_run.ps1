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
    param($Node)

    if ($Node -is [System.Collections.IDictionary]) {
        foreach ($environmentKey in 'environment', 'agent_environment') {
            $environmentBlock = $Node[$environmentKey]
            if ($environmentBlock -is [System.Collections.IDictionary]) {
                if ($environmentBlock.Contains('commands')) {
                    return "declares a forbidden '$environmentKey.commands' field; staged fixture binaries are made executable automatically."
                }
                if ($environmentBlock.Contains('mcpServers')) {
                    return "declares a forbidden '$environmentKey.mcpServers' field; per-eval MCP servers are not allowed."
                }
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
            $found = Find-ForbiddenEvalField $value
            if ($found) {
                return $found
            }
        }
    } elseif ($Node -is [System.Collections.IEnumerable] -and $Node -isnot [string]) {
        foreach ($item in $Node) {
            $found = Find-ForbiddenEvalField $item
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
    $violation = Find-ForbiddenEvalField $parsed
    if ($violation) {
        throw "$Description $violation"
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
$candidateSkill = Join-Path $candidateRootPath ".github/skills/$EvalName"
$candidateEvalRoot = Join-Path $candidateRootPath "eng/skill-evals/$EvalName"
$candidateEval = Join-Path $candidateEvalRoot 'eval.vally.yaml'
$candidateFixtures = Join-Path $candidateEvalRoot 'fixtures'

if (-not (Test-Path $candidateSkill -PathType Container)) {
    throw "Missing candidate skill directory at '$candidateSkill'."
}
Assert-TreeIsSafe $candidateSkill $candidateRootPath "Candidate skill '$EvalName'"
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

    $skillTarget = Join-Path $destinationPath ".github/skills/$EvalName"
    New-Item -ItemType Directory -Path (Split-Path $skillTarget -Parent) -Force |
        Out-Null
    Copy-Item $candidateSkill $skillTarget -Recurse -Force

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

    Assert-TreeIsSafe $destinationPath $destinationPath 'Staged evaluation tree'
} catch {
    Remove-Item $destinationPath -Recurse -Force -ErrorAction SilentlyContinue
    throw
}

Write-Host "Staged trusted control plane with exact candidate data for '$EvalName'."
