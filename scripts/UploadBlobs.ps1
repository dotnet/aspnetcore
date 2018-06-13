<#
.SYNOPSIS
Deploys a build to an Azure blob store

.PARAMETER AccountName
The account name for the Azure account

.PARAMETER AccountKey
The account key for the Azure account

.PARAMETER BuildNumber
The build number of the current build

.PARAMETER BaseFeedUrl
The base URI of the package feed (may be different than blobBaseUrl for private-only blobs)

.PARAMETER ContainerName
The container name. Defaults to 'dotnet'

.PARAMETER ArtifactsPath
The path to the build outputs
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    $AccountName,
    [Parameter(Mandatory = $true)]
    $AccountKey,
    [Parameter(Mandatory = $true)]
    $BuildNumber,
    [Parameter(Mandatory = $true)]
    $ArtifactsPath,
    $BaseBlobFeedUrl,
    $ContainerName = 'dotnet'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

Import-Module -Scope Local "$PSScriptRoot/common.psm1"

if (!(Get-Command 'az' -ErrorAction Ignore)) {
    Write-Error 'Missing required command: az. Please install the Azure CLI and ensure it is available on PATH.'
}

$repoRoot = Resolve-Path "$PSScriptRoot/.."

$sleetVersion = '2.3.25'
$sleet = "$repoRoot/.tools/sleet.$sleetVersion/tools/sleet.exe"

if (-not (Test-Path $sleet)) {
    mkdir "$repoRoot/.tools/sleet.$sleetVersion" -ErrorAction Ignore | Out-Null
    $installScriptPath = "$repoRoot/.dotnet/dotnet-install.ps1"
    Invoke-WebRequest -UseBasicParsing -OutFile "$repoRoot/.tools/sleet.$sleetVersion.zip" https://www.nuget.org/api/v2/package/Sleet/$sleetVersion
    Expand-Archive "$repoRoot/.tools/sleet.$sleetVersion.zip" -DestinationPath "$repoRoot/.tools/sleet.$sleetVersion"
}

[xml] $versionProps = Get-Content "$repoRoot/version.props"
$props = $versionProps.Project.PropertyGroup
$VersionPrefix = "$($props.AspNetCoreMajorVersion).$($props.AspNetCoreMinorVersion).$($props.AspNetCorePatchVersion)"

$blobFolder = "$ContainerName/aspnetcore/store/$VersionPrefix-$BuildNumber"
$packagesFolder = "$blobFolder/packages/"

$blobBaseUrl = "https://$AccountName.blob.core.windows.net/$blobFolder"
$packageBlobUrl = "https://$AccountName.blob.core.windows.net/$packagesFolder"

if (-not $BaseBlobFeedUrl) {
    $BaseBlobFeedUrl = "https://$AccountName.blob.core.windows.net/$packagesFolder"
}

$packageGlobPath = "$ArtifactsPath/packages/**/*.nupkg"
$globs = (
    @{
        basePath    = "$ArtifactsPath/lzma"
        pattern     = "*"
        destination = $blobFolder
    },
    @{
        basePath    = "$ArtifactsPath/installers"
        pattern     = "*"
        destination = $blobFolder
    })

$sleetConfigObj = @{
    sources = @(
        @{
            name             = "feed"
            type             = "azure"
            path             = $packageBlobUrl
            baseURI          = $BaseBlobFeedUrl
            container        = $ContainerName
            connectionString = "DefaultEndpointsProtocol=https;AccountName=$AccountName;AccountKey=$AccountKey"
        })
}

$sleetConfig = "$repoRoot/.tools/sleet.json"
$sleetConfigObj | ConvertTo-Json | Set-Content -Path $sleetConfig -Encoding Ascii
if ($PSCmdlet.ShouldProcess("Initialize remote feed in $packageBlobUrl")) {
    Invoke-Block { & $sleet init --config $sleetConfig --verbose }
}

Get-ChildItem -Recurse $packageGlobPath `
    | split-path -parent `
    | select -Unique `
    | % {
    if ($PSCmdlet.ShouldProcess("Push packages in $_ to $packageBlobUrl")) {
        Invoke-Block { & $sleet push --verbose --config $sleetConfig --source feed --force $_ }
    }
}

[string[]] $otherArgs = @()

if ($VerbosePreference) {
    $otherArgs += '--verbose'
}

if ($WhatIfPreference) {
    $otherArgs += '--dryrun'
}

$globs | ForEach-Object {
    $pattern = $_.pattern
    $basePath = $_.basePath
    $destination = $_.destination
    if (!(Get-ChildItem -Recurse "$basePath/$pattern" -ErrorAction Ignore)) {
        Write-Warning "Expected files in $basePath/$pattern but found none"
    }

    Invoke-Block { & az storage blob upload-batch `
            --account-name $AccountName `
            --account-key $AccountKey `
            --verbose `
            --pattern $pattern `
            --destination $destination.TrimEnd('/') `
            --source $basePath `
            --no-progress `
            @otherArgs
    }
}

Write-Host -f green "Done!"
