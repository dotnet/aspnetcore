#
# This script requires internal-only access to the code which generates ANCM installers.
#

#requires -version 5
[cmdletbinding()]
param(
    [string]$Configuration = 'Debug',
    [Parameter(Mandatory = $true)]
    [Alias("x86")]
    [string]$Runtime86Zip,
    [Parameter(Mandatory = $true)]
    [Alias("x64")]
    [string]$Runtime64Zip,
    [string]$BuildNumber = 't000',
    [switch]$IsFinalBuild,
    [string]$SignType = '',
    [switch]$clean
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path "$PSScriptRoot/../../../"
Import-Module -Scope Local "$repoRoot/eng/scripts/common.psm1" -Force
$msbuild = Get-MSBuildPath -Prerelease -requires 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64'

$harvestRoot = "$repoRoot/obj/sfx/"
if ($clean) {
    Remove-Item -Recurse -Force $harvestRoot -ErrorAction Ignore | Out-Null
}

New-Item "$harvestRoot/x86", "$harvestRoot/x64" -ItemType Directory -ErrorAction Ignore | Out-Null

if (-not (Test-Path "$harvestRoot/x86/shared/")) {
    Expand-Archive $Runtime86Zip -DestinationPath "$harvestRoot/x86"
}

if (-not (Test-Path "$harvestRoot/x64/shared/")) {
    Expand-Archive $Runtime64Zip -DestinationPath "$harvestRoot/x64"
}

Push-Location $PSScriptRoot
try {
    Invoke-Block { & $msbuild `
            InstallerTasks/InstallerTasks.csproj `
            -nologo `
            -m `
            -v:m `
            -nodeReuse:false `
            -restore `
            -t:Build `
            "-p:Configuration=$Configuration"
    }

    [string[]] $msbuildArgs = @()

    if ($clean) {
        $msbuildArgs += '-t:Clean'
    }

    Invoke-Block { & $msbuild `
            WindowsInstallers.proj `
            -restore `
            -nologo `
            -m `
            -v:m `
            -nodeReuse:false `
            -clp:Summary `
            "-p:SharedFrameworkHarvestRootPath=$repoRoot/obj/sfx/" `
            "-p:Configuration=$Configuration" `
            "-p:BuildNumberSuffix=$BuildNumber" `
            "-p:SignType=$SignType" `
            "-p:IsFinalBuild=$IsFinalBuild" `
            "-bl:$repoRoot/artifacts/logs/installers.msbuild.binlog" `
            '-t:Build' `
            @msbuildArgs
    }
}
finally {
    Pop-Location
}
