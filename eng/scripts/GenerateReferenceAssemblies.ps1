param(
    [switch]$ci
)
$ErrorActionPreference = 'stop'

$repoRoot = Resolve-Path "$PSScriptRoot/../.."

& "$repoRoot\eng\common\msbuild.ps1" -ci:$ci "$repoRoot/eng/CodeGen.proj" `
    /p:BuildNodeJs=false
    /t:GenerateReferenceSources `
    /bl:artifacts/log/genrefassemblies.binlog
