#requires -version 5
<#
.SYNOPSIS
    Verifies that the dependency graph in eng/AffectedProjectAreas.json is up to date.

.DESCRIPTION
    Scans all production .csproj files to find cross-area dependencies (via <Reference> and
    <ProjectReference> elements), then compares them against the declared graph in
    eng/AffectedProjectAreas.json. Reports errors if:
    - An area has an actual dependency not declared in the graph (missing dependency)
    - An area in the graph lists a dependency that no longer exists (stale dependency)

.PARAMETER ci
    When set, outputs Azure DevOps error annotations.
#>
param(
    [switch]$ci
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

$repoRoot = Resolve-Path "$PSScriptRoot/../.."
$graphPath = Join-Path $repoRoot "eng/AffectedProjectAreas.json"
$projectRefsPath = Join-Path $repoRoot "eng/ProjectReferences.props"

[string[]] $errors = @()

function LogError {
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$message
    )
    if ($env:TF_BUILD) {
        Write-Host "##vso[task.logissue type=error]$message"
    }
    Write-Host -f Red "error: $message"
    $script:errors += $message
}

# Step 1: Parse ProjectReferences.props to build assembly name -> area map
Write-Host "Parsing $projectRefsPath"
$projectRefsContent = Get-Content -Raw $projectRefsPath
$assemblyToArea = @{}
$providerMatches = [regex]::Matches($projectRefsContent, 'Include="([^"]+)"\s+ProjectPath="\$\(RepoRoot\)src[\\/]([^\\/]+)')
foreach ($match in $providerMatches) {
    $assemblyName = $match.Groups[1].Value
    $area = $match.Groups[2].Value
    $assemblyToArea[$assemblyName] = $area
}
Write-Host "  Mapped $($assemblyToArea.Count) assemblies to areas"

# Step 2: Find all production .csproj files (under src/**/src/*.csproj pattern)
$srcRoot = Join-Path $repoRoot "src"
$allCsprojs = Get-ChildItem -Recurse -Filter "*.csproj" -Path $srcRoot |
    Where-Object {
        $_.FullName -NotLike '*\node_modules\*' -and
        $_.FullName -NotLike '*\bin\*' -and
        $_.FullName -NotLike '*\obj\*' -and
        $_.FullName -NotLike '*\submodules\*' -and
        $_.FullName -NotLike '*\testassets\*' -and
        $_.FullName -NotLike '*\samples\*' -and
        $_.FullName -NotLike '*\perf\*' -and
        $_.FullName -NotLike '*\benchmarkapps\*'
    }

# Filter to production projects (those under a 'src' subdirectory)
$srcProjects = $allCsprojs | Where-Object {
    $relativePath = $_.FullName.Substring($srcRoot.Length + 1).Replace('\', '/')
    $parts = $relativePath.Split('/')
    # Check if any intermediate directory is named 'src'
    $hasSrcDir = $false
    for ($i = 1; $i -lt $parts.Length - 1; $i++) {
        if ($parts[$i] -eq 'src') {
            $hasSrcDir = $true
            break
        }
    }
    $hasSrcDir
}

Write-Host "  Found $($srcProjects.Count) production source projects"

# Step 3: Scan for cross-area dependencies
function Get-Area([string]$filePath) {
    $relative = $filePath.Substring($srcRoot.Length + 1).Replace('\', '/')
    return $relative.Split('/')[0]
}

$actualDeps = @{}
$refRegex = [regex]::new('<Reference\s+Include="([^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$projRefRegex = [regex]::new('<ProjectReference\s+Include="([^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

foreach ($csproj in $srcProjects) {
    $area = Get-Area $csproj.FullName
    $content = Get-Content -Raw $csproj.FullName

    # Check <Reference Include="AssemblyName">
    foreach ($match in $refRegex.Matches($content)) {
        $assemblyName = $match.Groups[1].Value
        if ($assemblyToArea.ContainsKey($assemblyName)) {
            $refArea = $assemblyToArea[$assemblyName]
            if ($refArea -ne $area) {
                if (-not $actualDeps.ContainsKey($area)) {
                    $actualDeps[$area] = [System.Collections.Generic.HashSet[string]]::new()
                }
                [void]$actualDeps[$area].Add($refArea)
            }
        }
    }

    # Check <ProjectReference Include="...">
    foreach ($match in $projRefRegex.Matches($content)) {
        $refPath = $match.Groups[1].Value.Replace('/', '\')
        $absRef = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($csproj.DirectoryName, $refPath))
        if ($absRef.StartsWith($srcRoot)) {
            $refArea = Get-Area $absRef
            if ($refArea -ne $area) {
                if (-not $actualDeps.ContainsKey($area)) {
                    $actualDeps[$area] = [System.Collections.Generic.HashSet[string]]::new()
                }
                [void]$actualDeps[$area].Add($refArea)
            }
        }
    }
}

# Step 4: Load declared graph
Write-Host "Loading dependency graph from $graphPath"
$graph = Get-Content -Raw $graphPath | ConvertFrom-Json
$declaredDeps = @{}
foreach ($area in $graph.PSObject.Properties.Name) {
    if ($area -eq '_comment') { continue }
    $deps = @($graph.$area)
    if ($deps.Count -eq 0) {
        $declaredDeps[$area] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    } else {
        $declaredDeps[$area] = [System.Collections.Generic.HashSet[string]]::new([string[]]$deps, [System.StringComparer]::OrdinalIgnoreCase)
    }
}

# Step 5: Compare actual vs declared
Write-Host "Comparing actual dependencies with declared graph..."

# Check for missing dependencies (in actual but not declared)
foreach ($area in $actualDeps.Keys | Sort-Object) {
    $actual = $actualDeps[$area]
    $declared = $null
    if ($declaredDeps.ContainsKey($area)) {
        $declared = $declaredDeps[$area]
    }

    foreach ($dep in $actual | Sort-Object) {
        if ($null -eq $declared -or -not $declared.Contains($dep)) {
            LogError "Area '$area' depends on '$dep' but this is not declared in eng/AffectedProjectAreas.json. Please add '$dep' to the dependency list for '$area' and run the verification again."
        }
    }
}

# Check for stale dependencies (in declared but not actual)
foreach ($area in $declaredDeps.Keys | Sort-Object) {
    if ($area -eq '_comment') { continue }
    $declared = $declaredDeps[$area]
    $actual = $null
    if ($actualDeps.ContainsKey($area)) {
        $actual = $actualDeps[$area]
    }

    foreach ($dep in $declared | Sort-Object) {
        if ($null -eq $actual -or -not $actual.Contains($dep)) {
            LogError "Area '$area' declares a dependency on '$dep' in eng/AffectedProjectAreas.json but no such dependency was found in production source projects. Remove '$dep' from the dependency list for '$area'."
        }
    }
}

# Summary
Write-Host ""
Write-Host "Summary:"
Write-Host "  $($errors.Count) error(s)"

if ($errors.Count -gt 0) {
    Write-Host ""
    foreach ($err in $errors) {
        Write-Host -f Red "  $err"
    }
    Write-Host ""
    Write-Host "To fix these errors, update eng/AffectedProjectAreas.json to match the actual cross-area"
    Write-Host "dependencies in production source projects. You can run eng/scripts/GenerateAffectedProjectAreas.ps1"
    Write-Host "to regenerate the file automatically."
    exit 1
}
else {
    Write-Host "  Dependency graph is up to date."
}
