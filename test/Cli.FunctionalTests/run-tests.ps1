<#
.SYNOPSIS
This script runs the tests in this project on complete build of the .NET Core CLI

.PARAMETER AssetRootUrl
The blob feed for the .NET Core CLI

.PARAMETER RestoreSources
A list of additional NuGet feeds

.PARAMETER SdkVersion
The version of the .NET Core CLI to test. If not specified, the version will be determined automatically if possible.

.PARAMETER PackageVersionsFile
A URL or filepath to a list of package versions
#>
param(
    $AssetRootUrl = 'https://dotnetcli.blob.core.windows.net/dotnet',
    $RestoreSources = 'https://dotnet.myget.org/F/dotnet-core/api/v3/index.json',
    $SdkVersion = $null,
    $PackageVersionsFile = $null
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

$repoRoot = Resolve-Path "$PSScriptRoot/../../"
Import-Module "$repoRoot/scripts/common.psm1" -Scope Local -Force

$AssetRootUrl = $AssetRootUrl.TrimEnd('/')

Push-Location $PSScriptRoot
try {
    New-Item -Type Directory "$PSScriptRoot/obj/" -ErrorAction Ignore | Out-Null

    $pkgPropsFile = $PackageVersionsFile
    if ($PackageVersionsFile -like 'http*') {
        $pkgPropsFile = "$PSScriptRoot/obj/packageversions.props"
        Remove-Item $pkgPropsFile -ErrorAction Ignore
        Invoke-WebRequest -UseBasicParsing $PackageVersionsFile -OutFile $pkgPropsFile
    }

    if (-not $SdkVersion) {
        $cliManifestUrl = "$AssetRootUrl/orchestration-metadata/manifests/cli.xml"
        Write-Host "No SDK version was specified. Attempting to determine the version from $cliManifestUrl"
        $cliXml = "$PSScriptRoot/obj/cli.xml"
        Remove-Item $cliXml -ErrorAction Ignore
        Invoke-WebRequest -UseBasicParsing $cliManifestUrl -OutFile $cliXml
        [xml] $cli = Get-Content $cliXml
        $SdkVersion = $cli.Build.ProductVersion
    }

    Write-Host "SDK: $SdkVersion"

    @{ sdk = @{ version = $SdkVersion } } | ConvertTo-Json | Set-Content "$PSScriptRoot/global.json"

    $dotnetRoot = "$repoRoot/.dotnet"
    $dotnet = "$dotnetRoot/dotnet.exe"

    if (-not (Test-Path "$dotnetRoot/sdk/$SdkVersion/dotnet.dll")) {
        Remote-Item -Recurse -Force $dotnetRoot -ErrorAction Ignore | Out-Null
        $cliUrl = "$AssetRootUrl/Sdk/$SdkVersion/dotnet-sdk-$SdkVersion-win-x64.zip"
        Write-Host "Downloading $cliUrl"
        Invoke-WebRequest -UseBasicParsing $cliUrl -OutFile "$PSScriptRoot/obj/dotnet.zip"
        Expand-Archive "$PSScriptRoot/obj/dotnet.zip" -DestinationPath $dotnetRoot
    }

    # Set a clean test environment
    $env:DOTNET_ROOT = $dotnetRoot
    $env:DOTNET_MULTILEVEL_LOOKUP = 0
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 0
    $env:MSBuildSdksPath = ''
    $env:PATH="$dotnetRoot;$env:PATH"

    Invoke-Block { & $dotnet test `
        --logger "console;verbosity=detailed" `
        --logger "trx;LogFile=$repoRoot/artifacts/logs/e2etests.trx" `
        "-p:DotNetRestoreSources=$RestoreSources" `
        "-p:DotNetPackageVersionPropsPath=$pkgPropsFile" `
        "-bl:$repoRoot/artifacts/logs/e2etests.binlog" }
}
finally {
    Pop-Location
}
