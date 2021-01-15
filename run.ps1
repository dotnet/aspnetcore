#!/usr/bin/env powershell
#requires -version 4

<#
.SYNOPSIS
Executes KoreBuild commands.

.DESCRIPTION
Downloads korebuild if required. Then executes the KoreBuild command. To see available commands, execute with `-Command help`.

.PARAMETER Command
The KoreBuild command to run.

.PARAMETER Path
The folder to build. Defaults to the folder containing this script.

.PARAMETER LockFile
The path to the korebuild-lock.txt file. Defaults to $Path/korebuild-lock.txt

.PARAMETER Channel
The channel of KoreBuild to download. Overrides the value from the config file.

.PARAMETER DotNetHome
The directory where .NET Core tools will be stored.

.PARAMETER ToolsSource
The base url where build tools can be downloaded. Overrides the value from the config file.

.PARAMETER Update
Updates KoreBuild to the latest version even if a lock file is present.

.PARAMETER Reinstall
Re-installs KoreBuild

.PARAMETER ConfigFile
The path to the configuration file that stores values. Defaults to korebuild.json.

.PARAMETER CI
Sets up CI specific settings and variables.

.PARAMETER Projects
A list of projects to build. (Must be an absolute path.) Globbing patterns are supported, such as "$(pwd)/**/*.csproj"

.PARAMETER PackageVersionPropsUrl
(optional) the url of the package versions props path containing dependency versions.

.PARAMETER AssetRootUrl
(optional) the base url for acquiring build assets from an orchestrated build

.PARAMETER AccessTokenSuffix
(optional) the query string to append to any blob store access for PackageVersionPropsUrl, if any.

.PARAMETER RestoreSources
(optional) Semi-colon delimited list of additional NuGet feeds to use as part of restore.

.PARAMETER ProductBuildId
(optional) The product build ID for correlation with orchestrated builds.

.PARAMETER MSBuildArguments
Additional MSBuild arguments to be passed through.

.NOTES
This function will create a file $PSScriptRoot/korebuild-lock.txt. This lock file can be committed to source, but does not have to be.
When the lockfile is not present, KoreBuild will create one using latest available version from $Channel.

The $ConfigFile is expected to be an JSON file. It is optional, and the configuration values in it are optional as well. Any options set
in the file are overridden by command line parameters.

.EXAMPLE
Example config file:
```json
{
  "$schema": "https://raw.githubusercontent.com/aspnet/BuildTools/master/tools/korebuild.schema.json",
  "channel": "master",
  "toolsSource": "https://aspnetcore.blob.core.windows.net/buildtools"
}
```
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(Mandatory=$true, Position = 0)]
    [string]$Command,
    [string]$Path = $PSScriptRoot,
    [string]$LockFile,
    [Alias('c')]
    [string]$Channel,
    [Alias('d')]
    [string]$DotNetHome,
    [Alias('s')]
    [string]$ToolsSource,
    [Alias('u')]
    [switch]$Update,
    [switch]$Reinstall,
    [string]$ConfigFile = $null,
    [switch]$CI,
    [string]$Projects,
    [string]$PackageVersionPropsUrl = $env:PB_PackageVersionPropsUrl,
    [string]$AccessTokenSuffix = $env:PB_AccessTokenSuffix,
    [string]$RestoreSources = ${env:PB_RestoreSource},
    [string]$AssetRootUrl = ${env:PB_AssetRootUrl},
    [string]$ProductBuildId = ${env:ProductBuildId},
    [string]$PublishBlobFeedUrl = ${env:PB_PublishBlobFeedUrl},
    [string]$PublishType = ${env:PB_PublishType},
    [string]$SkipTests = ${env:PB_SkipTests},
    [string]$IsFinalBuild = ${env:PB_IsFinalBuild},
    [string]$SignType = ${env:PB_SignType},
    [string]$PublishBlobFeedKey = ${env:PB_PublishBlobFeedKey},
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$MSBuildArguments
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

#
# Functions
#

function Get-KoreBuild {

    if (!(Test-Path $LockFile) -or $Update) {
        Get-RemoteFile "$ToolsSource/korebuild/channels/$Channel/latest.txt" $LockFile
    }

    $version = Get-Content $LockFile | Where-Object { $_ -like 'version:*' } | Select-Object -first 1
    if (!$version) {
        Write-Error "Failed to parse version from $LockFile. Expected a line that begins with 'version:'"
    }
    $version = $version.TrimStart('version:').Trim()
    $korebuildPath = Join-Paths $DotNetHome ('buildtools', 'korebuild', $version)

    if ($Reinstall -and (Test-Path $korebuildPath)) {
        Remove-Item -Force -Recurse $korebuildPath
    }

    if (!(Test-Path $korebuildPath)) {
        Write-Host -ForegroundColor Magenta "Downloading KoreBuild $version"
        New-Item -ItemType Directory -Path $korebuildPath | Out-Null
        $remotePath = "$ToolsSource/korebuild/artifacts/$version/korebuild.$version.zip"

        try {
            $tmpfile = Join-Path ([IO.Path]::GetTempPath()) "KoreBuild-$([guid]::NewGuid()).zip"
            Get-RemoteFile $remotePath $tmpfile
            if (Get-Command -Name 'Expand-Archive' -ErrorAction Ignore) {
                # Use built-in commands where possible as they are cross-plat compatible
                Expand-Archive -Path $tmpfile -DestinationPath $korebuildPath
            }
            else {
                # Fallback to old approach for old installations of PowerShell
                Add-Type -AssemblyName System.IO.Compression.FileSystem
                [System.IO.Compression.ZipFile]::ExtractToDirectory($tmpfile, $korebuildPath)
            }
        }
        catch {
            Remove-Item -Recurse -Force $korebuildPath -ErrorAction Ignore
            throw
        }
        finally {
            Remove-Item $tmpfile -ErrorAction Ignore
        }
    }

    return $korebuildPath
}

function Join-Paths([string]$path, [string[]]$childPaths) {
    $childPaths | ForEach-Object { $path = Join-Path $path $_ }
    return $path
}

function Get-RemoteFile([string]$RemotePath, [string]$LocalPath) {
    if ($RemotePath -notlike 'http*') {
        Copy-Item $RemotePath $LocalPath
        return
    }

    $retries = 10
    while ($retries -gt 0) {
        $retries -= 1
        try {
            $ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138
            Invoke-WebRequest -UseBasicParsing -Uri $RemotePath -OutFile $LocalPath
            return
        }
        catch {
            Write-Verbose "Request failed. $retries retries remaining"
        }
    }

    Write-Error "Download failed: '$RemotePath'."
}

#
# Main
#

# Load configuration or set defaults

$Path = Resolve-Path $Path
if (!$ConfigFile) { $ConfigFile = Join-Path $Path 'korebuild.json' }

if (Test-Path $ConfigFile) {
    try {
        $config = Get-Content -Raw -Encoding UTF8 -Path $ConfigFile | ConvertFrom-Json
        if ($config) {
            if (!($Channel) -and (Get-Member -Name 'channel' -InputObject $config)) { [string] $Channel = $config.channel }
            if (!($ToolsSource) -and (Get-Member -Name 'toolsSource' -InputObject $config)) { [string] $ToolsSource = $config.toolsSource}
        }
    } catch {
        Write-Warning "$ConfigFile could not be read. Its settings will be ignored."
        Write-Warning $Error[0]
    }
}

if (!$DotNetHome) {
    $DotNetHome = if ($env:DOTNET_HOME) { $env:DOTNET_HOME } `
        elseif ($CI) { Join-Path $PSScriptRoot '.dotnet' } `
        elseif ($env:USERPROFILE) { Join-Path $env:USERPROFILE '.dotnet'} `
        elseif ($env:HOME) {Join-Path $env:HOME '.dotnet'}`
        else { Join-Path $PSScriptRoot '.dotnet'}
}

if (!$LockFile) { $LockFile = Join-Path $Path 'korebuild-lock.txt' }
if (!$Channel) { $Channel = 'master' }
if (!$ToolsSource) { $ToolsSource = 'https://aspnetcore.blob.core.windows.net/buildtools' }

[string[]] $ProdConArgs = @()

if ($Projects) {
    $MSBuildArguments += "-p:Projects=$Projects"
    $MSBuildArguments += "-p:_ProjectsOnly=true"
}

# PipeBuild parameters

if ($PackageVersionPropsUrl) {
    $IntermediateDir = Join-Path $PSScriptRoot 'obj'
    $PropsFilePath = Join-Path $IntermediateDir 'external-dependencies.props'
    New-Item -ItemType Directory $IntermediateDir -ErrorAction Ignore | Out-Null
    Get-RemoteFile "${PackageVersionPropsUrl}${AccessTokenSuffix}" $PropsFilePath
    $ProdConArgs += "-p:DotNetPackageVersionPropsPath=$PropsFilePath"
}

if ($AccessTokenSuffix) {
    $ProdConArgs += "-p:DotNetAssetRootAccessTokenSuffix=$AccessTokenSuffix"
}

if ($AssetRootUrl) {
    $ProdConArgs += "-p:DotNetAssetRootUrl=$AssetRootUrl"
}

if ($RestoreSources) {
    $ProdConArgs += "-p:DotNetAdditionalRestoreSources=$RestoreSources"
}

if ($ProductBuildId) {
    $ProdConArgs += "-p:DotNetProductBuildId=$ProductBuildId"
}

if ($PublishBlobFeedUrl) {
    $ProdConArgs += "-p:PublishBlobFeedUrl=$PublishBlobFeedUrl"
}

if ($PublishType) {
    $ProdConArgs += "-p:PublishType=$PublishType"
}

if ($SkipTests) {
    $ProdConArgs += "-p:SkipTests=$SkipTests"
}

if ($IsFinalBuild) {
    $ProdConArgs += "-p:IsFinalBuild=$IsFinalBuild"
}

if ($SignType) {
    $ProdConArgs += "-p:SignType=$SignType"
}

if ($PublishBlobFeedKey) {
    $ProdConArgs += "-p:PublishBlobFeedKey=$PublishBlobFeedKey"
}

# Execute

$korebuildPath = Get-KoreBuild
Import-Module -Force -Scope Local (Join-Path $korebuildPath 'KoreBuild.psd1')

try {
    Set-KoreBuildSettings -ToolsSource $ToolsSource -DotNetHome $DotNetHome -RepoPath $Path -ConfigFile $ConfigFile -CI:$CI
    Invoke-KoreBuildCommand $Command @ProdConArgs @MSBuildArguments
}
finally {
    Remove-Module 'KoreBuild' -ErrorAction Ignore
}
