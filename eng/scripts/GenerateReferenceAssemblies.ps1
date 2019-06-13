param(
    [switch]$ci
)
$ErrorActionPreference = 'stop'

$repoRoot = Resolve-Path "$PSScriptRoot/../.."

& "$repoRoot\build.ps1" -ci:$ci -nobuild -BuildManaged -NoBuildNodeJS -projects "$repoRoot/eng/CodeGen.proj" /p:GenerateReferenceSources=true
