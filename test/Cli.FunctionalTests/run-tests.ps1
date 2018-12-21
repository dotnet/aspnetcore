<#
.SYNOPSIS
This script runs the tests in this project on complete build of the .NET Core CLI

.PARAMETER ci
This is a CI build

.PARAMETER AccessTokenSuffix
The access token for Azure blobs

.PARAMETER AssetRootUrl
The blob feed for the .NET Core CLI. If not specified, it will determined automatically if possible.

.PARAMETER RestoreSources
A list of additional NuGet feeds.  If not specified, it will determined automatically if possible.

.PARAMETER TestRuntimeIdentifier
Filter the tests by which RID they publish for. If empty (default), tests are run for
* none (portable)
* osx-x64
* linux-x64
* win-x64

.PARAMETER HostRid
The RID of the platform running the tests. (Determined automatically if possible)

.PARAMETER ProdConManifestUrl
The prodcon build.xml file

.PARAMETER ProdConChannel
The prodcon channel to use if a build.xml file isn't set.

.PARAMETER AdditionalRestoreSources
A pipe-separated list of extra NuGet feeds.  Required for builds which need to restore from multiple feeds.
#>

param(
    [switch]$ci,
    $AssetRootUrl = $env:PB_ASSETROOTURL,
    $AccessTokenSuffix = $env:PB_ACCESSTOKENSUFFIX,
    $RestoreSources = $env:PB_RESTORESOURCE,
    [ValidateSet('none', 'osx-x64', 'linux-x64', 'win-x64')]
    $TestRuntimeIdentifier,
    $HostRid,
    $ProdConManifestUrl,
    $ProdConChannel = 'master',
    $AdditionalRestoreSources
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

$repoRoot = Resolve-Path "$PSScriptRoot/../../"
Import-Module "$repoRoot/eng/scripts/common.psm1" -Scope Local -Force

# This ID corresponds to the ProdCon build number
Write-Host "ProductBuildId:  $env:PRODUCTBUILDID"

if (-not $HostRid) {
    if (Test-Path Variable:/IsCoreCLR) {
        $HostRid = if ($IsWindows) { 'win-x64' } `
            elseif ($IsLinux) { 'linux-x64' } `
            elseif ($IsMacOS) { 'osx-x64' }
    }
    else {
        $HostRid = 'win-x64'
    }
}

if (-not $HostRid) {
    throw 'Could not determine which platform this script is running on. Add -HostRid $rid where $rid = the .NET Core SDK to install'
}

switch ($HostRid) {
    'win-x64' {
        $dotnetFileName = 'dotnet.exe'
        $archiveExt = '.zip'
    }
    default {
        $dotnetFileName = 'dotnet'
        $archiveExt = '.tar.gz'
    }
}

Push-Location $PSScriptRoot
try {
    New-Item -Type Directory "$PSScriptRoot/obj/" -ErrorAction Ignore | Out-Null
    $sdkVersion = ''

    if (-not $ci -or $ProdConManifestUrl) {
        # Workaround for pwsh 6 dumping progress info
        $ProgressPreference = 'SilentlyContinue'

        if (-not $ProdConManifestUrl) {
            Write-Host -ForegroundColor Magenta "Running tests for the latest ProdCon build"
            $ProdConManifestUrl = "https://raw.githubusercontent.com/dotnet/versions/master/build-info/dotnet/product/cli/$ProdConChannel/build.xml"
        }

        Write-Host "ProdConManifestUrl:    $ProdConManifestUrl"

        [xml] $prodConManifest = Invoke-RestMethod $ProdConManifestUrl

        $RestoreSources = $prodConManifest.OrchestratedBuild.Endpoint `
            | ? { $_.Type -eq 'BlobFeed' } `
            | select -first 1 -ExpandProperty Url

        $AssetRootUrl = $RestoreSources -replace '/index.json', '/assets'

        $sdkVersion = $prodConManifest.OrchestratedBuild.Build `
            | ? { $_.Name -eq 'cli' } `
            | select -first 1 -ExpandProperty ProductVersion
    }
    else {
        if (-not $AssetRootUrl) {
            Write-Error "Missing required parameter: AssetRootUrl"
        }
        $AssetRootUrl = $AssetRootUrl.TrimEnd('/')
        $cliMetadataUrl = "$AssetRootUrl/orchestration-metadata/manifests/cli.xml${AccessTokenSuffix}"
        Write-Host "CliMetadataUrl:  $cliMetadataUrl"
        [xml] $cli = Invoke-RestMethod $cliMetadataUrl
        $sdkVersion = $cli.Build.ProductVersion
    }

    if ($AdditionalRestoreSources) {
        $RestoreSources += "|$AdditionalRestoreSources"
    }

    Write-Host "sdkVersion:      $sdkVersion"
    Write-Host "AssetRootUrl:    $AssetRootUrl"
    Write-Host "RestoreSources:  $RestoreSources"

    @{ sdk = @{ version = $sdkVersion } } | ConvertTo-Json | Set-Content "$PSScriptRoot/global.json"

    $dotnetRoot = "$repoRoot/.dotnet"
    $dotnet = "$dotnetRoot/$dotnetFileName"

    if (-not (Test-Path "$dotnetRoot/sdk/$sdkVersion/dotnet.dll")) {
        Remove-Item -Recurse -Force $dotnetRoot -ErrorAction Ignore | Out-Null
        $cliUrl = "$AssetRootUrl/Sdk/$sdkVersion/dotnet-sdk-$sdkVersion-$HostRid$archiveExt"
        $cliArchiveFile = "$PSScriptRoot/obj/dotnet$archiveExt"
        Write-Host "Downloading $cliUrl"
        Invoke-WebRequest -UseBasicParsing "${cliUrl}${AccessTokenSuffix}" -OutFile $cliArchiveFile
        if ($archiveExt -eq '.zip') {
            Expand-Archive $cliArchiveFile -DestinationPath $dotnetRoot
        }
        else {
            New-Item -Type Directory $dotnetRoot -ErrorAction Ignore | Out-Null
            Invoke-Block { & tar xzf $cliArchiveFile -C $dotnetRoot }
        }
    }

    # Set a clean test environment
    $env:DOTNET_ROOT = $dotnetRoot
    $env:DOTNET_MULTILEVEL_LOOKUP = 0
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 0
    $env:MSBUILDSDKSPATH = ''
    $env:PATH = "$dotnetRoot;$env:PATH"

    # Required by the tests. It is assumed packages on this feed will end up on nuget.org
    $env:NUGET_PACKAGE_SOURCE = $RestoreSources

    [string[]] $filterArgs = @()

    if ($TestRuntimeIdentifier) {
        $filterArgs += '--filter',"rid: $TestRuntimeIdentifier"
    }

    Invoke-Block { & $dotnet test `
            --logger "console;verbosity=detailed" `
            --logger "trx;LogFileName=$repoRoot/artifacts/logs/e2etests.trx" `
            "-bl:$repoRoot/artifacts/logs/e2etests.binlog" `
            @filterArgs }
}
finally {
    Pop-Location
}
