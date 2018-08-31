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

.PARAMETER ProdConManifestUrl
The prodcon build.xml file

.PARAMETER ProcConChannel
The prodcon channel to use if a build.xml file isn't set.
#>

param(
    [switch]$ci,
    $AssetRootUrl = $env:PB_AccessRootUrl,
    $AccessTokenSuffix = $env:PB_AccessTokenSuffix,
    $RestoreSources = $env:PB_RestoreSources,
    $ProdConManifestUrl,
    $ProcConChannel = 'release/2.2'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

$repoRoot = Resolve-Path "$PSScriptRoot/../../"
Import-Module "$repoRoot/scripts/common.psm1" -Scope Local -Force

Push-Location $PSScriptRoot
try {
    New-Item -Type Directory "$PSScriptRoot/obj/" -ErrorAction Ignore | Out-Null
    $sdkVersion = ''

    if (-not $ci -or $ProdConManifestUrl) {

        if (-not $ProdConManifestUrl) {
            Write-Host -ForegroundColor Magenta "Running tests for the latest ProdCon build"
            $ProdConManifestUrl = "https://raw.githubusercontent.com/dotnet/versions/master/build-info/dotnet/product/cli/$ProcConChannel/build.xml"
        }

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
        [xml] $cli = Invoke-RestMethod "$AssetRootUrl/orchestration-metadata/manifests/cli.xml${AccessTokenSuffix}"
        $sdkVersion = $cli.Build.ProductVersion
    }

    Write-Host "sdkVersion:      $sdkVersion"
    Write-Host "AssetRootUrl:    $AssetRootUrl"
    Write-Host "RestoreSources:  $RestoreSources"

    @{ sdk = @{ version = $sdkVersion } } | ConvertTo-Json | Set-Content "$PSScriptRoot/global.json"

    $dotnetRoot = "$repoRoot/.dotnet"
    $dotnet = "$dotnetRoot/dotnet.exe"

    if (-not (Test-Path "$dotnetRoot/sdk/$sdkVersion/dotnet.dll")) {
        Remove-Item -Recurse -Force $dotnetRoot -ErrorAction Ignore | Out-Null
        $cliUrl = "$AssetRootUrl/Sdk/$sdkVersion/dotnet-sdk-$sdkVersion-win-x64.zip"
        Write-Host "Downloading $cliUrl"
        Invoke-WebRequest -UseBasicParsing "${cliUrl}${AccessTokenSuffix}" -OutFile "$PSScriptRoot/obj/dotnet.zip"
        Expand-Archive "$PSScriptRoot/obj/dotnet.zip" -DestinationPath $dotnetRoot
    }

    # Set a clean test environment
    $env:DOTNET_ROOT = $dotnetRoot
    $env:DOTNET_MULTILEVEL_LOOKUP = 0
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 0
    $env:MSBuildSdksPath = ''
    $env:PATH = "$dotnetRoot;$env:PATH"

    # Required by the tests. It is assumed packages on this feed will end up on nuget.org
    $env:NUGET_PACKAGE_SOURCE = $RestoreSources

    Invoke-Block { & $dotnet test `
            --logger "console;verbosity=detailed" `
            --logger "trx;LogFileName=$repoRoot/artifacts/logs/e2etests.trx" `
            "-p:DotNetRestoreSources=$RestoreSources" `
            "-bl:$repoRoot/artifacts/logs/e2etests.binlog" }
}
finally {
    Pop-Location
}
