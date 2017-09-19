#!/usr/bin/env powershell
#requires -version 4

<#
.SYNOPSIS
Build this repository

.DESCRIPTION
Downloads korebuild if required. Then builds the repository.

.PARAMETER Path
The folder to build. Defaults to the folder containing this script.

.PARAMETER Channel
The channel of KoreBuild to download. Overrides the value from the config file.

.PARAMETER DotNetHome
The directory where .NET Core tools will be stored.

.PARAMETER ToolsSource
The base url where build tools can be downloaded. Overrides the value from the config file.

.PARAMETER Update
Updates KoreBuild to the latest version even if a lock file is present.

.PARAMETER ConfigFile
The path to the configuration file that stores values. Defaults to version.props.

.PARAMETER MSBuildArgs
Arguments to be passed to MSBuild

.NOTES
This function will create a file $PSScriptRoot/korebuild-lock.txt. This lock file can be committed to source, but does not have to be.
When the lockfile is not present, KoreBuild will create one using latest available version from $Channel.

The $ConfigFile is expected to be an JSON file. It is optional, and the configuration values in it are optional as well. Any options set
in the file are overridden by command line parameters.

.EXAMPLE
Example config file:
```json
{
  "$schema": "https://raw.githubusercontent.com/aspnet/BuildTools/dev/tools/korebuild.schema.json",
  "channel": "dev",
  "toolsSource": "https://aspnetcore.blob.core.windows.net/buildtools"
}
```
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string]$Path = $PSScriptRoot,
    [Alias('c')]
    [string]$Channel,
    [Alias('d')]
    [string]$DotNetHome,
    [Alias('s')]
    [string]$ToolsSource,
    [Alias('u')]
    [switch]$Update,
    [string]$ConfigFile = $null,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$MSBuildArgs
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

#
# Functions
#

function Get-KoreBuild {

    $lockFile = Join-Path $Path 'korebuild-lock.txt'

    if (!(Test-Path $lockFile) -or $Update) {
        Get-RemoteFile "$ToolsSource/korebuild/channels/$Channel/latest.txt" $lockFile
    }

    $version = Get-Content $lockFile | Where-Object { $_ -like 'version:*' } | Select-Object -first 1
    if (!$version) {
        Write-Error "Failed to parse version from $lockFile. Expected a line that begins with 'version:'"
    }
    $version = $version.TrimStart('version:').Trim()
    $korebuildPath = Join-Paths $DotNetHome ('buildtools', 'korebuild', $version)

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
            remove-item -Recurse -Force $korebuildPath -ErrorAction Ignore
            throw
        }
        finally {
            remove-item $tmpfile -ErrorAction Ignore
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
        elseif ($env:USERPROFILE) { Join-Path $env:USERPROFILE '.dotnet'} `
        elseif ($env:HOME) {Join-Path $env:HOME '.dotnet'}`
        else { Join-Path $PSScriptRoot '.dotnet'}
}

if (!$Channel) { $Channel = 'dev' }
if (!$ToolsSource) { $ToolsSource = 'https://aspnetcore.blob.core.windows.net/buildtools' }

# Execute

$korebuildPath = Get-KoreBuild
Import-Module -Force -Scope Local (Join-Path $korebuildPath 'KoreBuild.psd1')

try {
    Install-Tools $ToolsSource $DotNetHome
    Invoke-RepositoryBuild $Path @MSBuildArgs
}
finally {
    Remove-Module 'KoreBuild' -ErrorAction Ignore
}
