#requires -version 5
[cmdletbinding()]
param(
    [switch]$ci,
    [Alias("x86")]
    [string]$sharedfx86harvestroot,
    [Alias("x64")]
    [string]$sharedfx64harvestroot,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AdditionalArgs
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path "$PSScriptRoot/../../../"
Import-Module -Scope Local "$repoRoot/eng/scripts/common.psm1" -Force

$harvestRoot = "$repoRoot/obj/sfx/"
if ($clean) {
    Remove-Item -Recurse -Force $harvestRoot -ErrorAction Ignore | Out-Null
}

# TODO: harvest shared frameworks from a project reference

New-Item "$harvestRoot/x86", "$harvestRoot/x64" -ItemType Directory -ErrorAction Ignore | Out-Null

[string[]] $msbuildargs = @()
if (-not $sharedfx86harvestroot) {
    $msbuildargs += "-p:SharedFrameworkX86HarvestRootPath=$sharedfx86harvestroot"
}

if (-not $sharedfx64harvestroot) {
    $msbuildargs += "-p:SharedFrameworkX64HarvestRootPath=$sharedfx64harvestroot"
}

Push-Location $PSScriptRoot
try {
    & $repoRoot/eng/build.ps1 `
            -ci:$ci `
            -sign `
            -BuildInstallers `
            "-bl:$repoRoot/artifacts/log/installers.msbuild.binlog" `
            @msbuildargs `
            @AdditionalArgs
}
finally {
    Pop-Location
}
