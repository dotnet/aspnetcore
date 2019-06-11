param(
    [switch]$ci
)
$ErrorActionPreference = 'stop'

$repoRoot = Resolve-Path "$PSScriptRoot/../.."

& "$repoRoot\build.ps1" -ci:$ci -NoRestore -all -projects "$repoRoot/eng/CodeGen.proj" /p:GenerateProjectList=true
