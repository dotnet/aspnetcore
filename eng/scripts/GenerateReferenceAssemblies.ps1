param(
    [switch]$ci
)
$ErrorActionPreference = 'stop'

$repoRoot = Resolve-Path "$PSScriptRoot/../.."

& "$repoRoot\build.ps1" -ci:$ci -BuildManaged -NoBuildNodeJS /t:GenerateReferenceSources
