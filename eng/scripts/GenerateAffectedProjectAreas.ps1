#requires -version 5
<#
.SYNOPSIS
    Regenerates eng/AffectedProjectAreas.json from actual project references.

.DESCRIPTION
    Scans all production .csproj files to discover cross-area dependencies and writes the
    dependency graph to eng/AffectedProjectAreas.json.
#>
param(
    [switch]$ci
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

$repoRoot = Resolve-Path "$PSScriptRoot/../.."
$graphPath = Join-Path $repoRoot "eng/AffectedProjectAreas.json"
$projectRefsPath = Join-Path $repoRoot "eng/ProjectReferences.props"

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

# Step 2: Find all production .csproj files
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

$srcProjects = $allCsprojs | Where-Object {
    $relativePath = $_.FullName.Substring($srcRoot.Length + 1).Replace('\', '/')
    $parts = $relativePath.Split('/')
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

$areaDeps = @{}
$refRegex = [regex]::new('<Reference\s+Include="([^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$projRefRegex = [regex]::new('<ProjectReference\s+Include="([^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

foreach ($csproj in $srcProjects) {
    $area = Get-Area $csproj.FullName
    $content = Get-Content -Raw $csproj.FullName

    foreach ($match in $refRegex.Matches($content)) {
        $assemblyName = $match.Groups[1].Value
        if ($assemblyToArea.ContainsKey($assemblyName)) {
            $refArea = $assemblyToArea[$assemblyName]
            if ($refArea -ne $area) {
                if (-not $areaDeps.ContainsKey($area)) {
                    $areaDeps[$area] = [System.Collections.Generic.HashSet[string]]::new()
                }
                [void]$areaDeps[$area].Add($refArea)
            }
        }
    }

    foreach ($match in $projRefRegex.Matches($content)) {
        $refPath = $match.Groups[1].Value.Replace('/', '\')
        $absRef = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($csproj.DirectoryName, $refPath))
        if ($absRef.StartsWith($srcRoot)) {
            $refArea = Get-Area $absRef
            if ($refArea -ne $area) {
                if (-not $areaDeps.ContainsKey($area)) {
                    $areaDeps[$area] = [System.Collections.Generic.HashSet[string]]::new()
                }
                [void]$areaDeps[$area].Add($refArea)
            }
        }
    }
}

# Step 4: Build the graph JSON
$srcDirs = Get-ChildItem -Directory $srcRoot | Select-Object -ExpandProperty Name | Sort-Object

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('{')
$lines.Add('  "_comment": "This file defines the dependency graph between top-level src/ areas. Each area maps to the list of other areas it directly depends on (forward dependencies). This is used by GetAffectedTestAreas.ps1 to compute which areas need testing when files change, and by VerifyDependencyGraph.ps1 to ensure the graph stays current. Run eng/scripts/GenerateAffectedProjectAreas.ps1 to regenerate.",')
$lines.Add('')

for ($i = 0; $i -lt $srcDirs.Count; $i++) {
    $area = $srcDirs[$i]
    $deps = @()
    if ($areaDeps.ContainsKey($area)) {
        $deps = $areaDeps[$area] | Sort-Object
    }

    $depsJson = ($deps | ForEach-Object { "`"$_`"" }) -join ', '
    $comma = if ($i -lt $srcDirs.Count - 1) { ',' } else { '' }
    $lines.Add("  `"$area`": [$depsJson]$comma")
}

$lines.Add('}')

$content = $lines -join "`n"
Set-Content -Path $graphPath -Value $content -NoNewline -Encoding UTF8

Write-Host ""
Write-Host "Generated $graphPath"
