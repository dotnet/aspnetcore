#
# This script requires internal-only access to the code which generates ANCM installers.
#

#requires -version 5
[cmdletbinding()]
param(
    [string]$Configuration = 'Debug',
    [string]$BuildNumber = 't000',
    [string]$PackageVersionPropsUrl = $env:PB_PackageVersionPropsUrl,
    [string]$AccessTokenSuffix = $null,
    [switch]$clean
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path "$PSScriptRoot/../../../"
Import-Module -Scope Local "$repoRoot/scripts/common.psm1" -Force
$msbuild = Get-MSBuildPath -Prerelease -requires 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64'

$harvestRoot = "$repoRoot/obj/sfx/"
$sharedFxDepsRoot = "$repoRoot/.deps/fx/"
if ($clean) {
    Remove-Item -Recurse -Force $harvestRoot -ErrorAction Ignore | Out-Null
}

New-Item "$harvestRoot/x86", "$harvestRoot/x64" -ItemType Directory -ErrorAction Ignore | Out-Null

Push-Location $PSScriptRoot
try {
    Invoke-Block { & $msbuild `
            tasks/InstallerTasks.csproj `
            -nologo `
            -m `
            -v:m `
            -nodeReuse:false `
            -restore `
            -t:Build `
            "-p:Configuration=$Configuration"
    }

    [string[]] $msbuildArgs = @()

    # PipeBuild parameters
    $msbuildArgs += "-p:SignType=${env:PB_SignType}"
    $msbuildArgs += "-p:DotNetAssetRootUrl=${env:PB_AssetRootUrl}"
    $msbuildArgs += "-p:IsFinalBuild=${env:PB_IsFinalBuild}"

    if ($clean) {
        $msbuildArgs += '-t:Clean'
    }

    if ($AccessTokenSuffix) {
        $msbuildArgs += "-p:DotNetAccessTokenSuffix=$AccessTokenSuffix"
    }

    if ($PackageVersionPropsUrl) {
        $IntermediateDir = Join-Path $PSScriptRoot 'obj'
        $PropsFilePath = Join-Path $IntermediateDir 'external-dependencies.props'
        New-Item -ItemType Directory $IntermediateDir -ErrorAction Ignore | Out-Null
        Get-RemoteFile "${PackageVersionPropsUrl}${AccessTokenSuffix}" $PropsFilePath
        $msbuildArgs += "-p:DotNetPackageVersionPropsPath=$PropsFilePath"
    }

    $msbuildArgs += '-t:Build'

    Invoke-Block { & $msbuild `
            WindowsInstallers.proj `
            -restore `
            -nologo `
            -m `
            -v:m `
            -nodeReuse:false `
            -clp:Summary `
            "-p:SharedFrameworkHarvestRootPath=$repoRoot/obj/sfx/" `
            "-p:SharedFxDepsRoot=$sharedFxDepsRoot" `
            "-p:Configuration=$Configuration" `
            "-p:BuildNumber=$BuildNumber" `
            "-bl:$repoRoot/artifacts/logs/installers.msbuild.binlog" `
            @msbuildArgs
    }
}
finally {
    Pop-Location
}
