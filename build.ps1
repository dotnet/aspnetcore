#requires -version 4

<#
.SYNOPSIS
Builds this repository.

.DESCRIPTION
This build script installs required tools and runs an MSBuild command on this repository.
This script can be used to invoke various targets, such as targets to produce packages,
build projects, run tests, and generate code.

.PARAMETER RepoPath
The folder to build. Defaults to the folder containing this script. This will be removed soon.

.PARAMETER CI
Sets up CI specific settings and variables.

.PARAMETER Restore
Run restore on projects.

.PARAMETER Build
Compile projects.

.PARAMETER Pack
Produce packages.

.PARAMETER Test
Run tests.

.PARAMETER Sign
Run code signing.

.PARAMETER Projects
A list of projects to build. Globbing patterns are supported, such as "$(pwd)/**/*.csproj"

.PARAMETER All
Build all project types.

.PARAMETER Managed
Build managed projects (C#, F#, VB).

.PARAMETER Native
Build native projects (C++).

.PARAMETER NodeJS
Build NodeJS projects (TypeScript, JS).

.PARAMETER MSBuildArguments
Additional MSBuild arguments to be passed through.

.EXAMPLE
Building both native and managed projects.

    build.ps1 -managed -native

.EXAMPLE
Building a subfolder of code.

    build.ps1 "$(pwd)/src/SomeFolder/**/*.csproj"

.EXAMPLE
Running tests.

    build.ps1 -test

.LINK
Online version: https://github.com/aspnet/AspNetCore/blob/master/docs/BuildFromSource.md
#>
[CmdletBinding(PositionalBinding = $false, DefaultParameterSetName='Groups')]
param(
    # Bootstrapper options
    [Obsolete('This parameter will be removed when we finish https://github.com/aspnet/AspNetCore/issues/4246')]
    [string]$RepoRoot = $PSScriptRoot,

    [switch]$CI,

    # Build lifecycle options
    [switch]$Restore = $True, # Run tests
    [switch]$Build = $True, # Compile
    [switch]$Pack, # Produce packages
    [switch]$Test, # Run tests
    [switch]$Sign, # Code sign

    # Project selection
    [Parameter(ParameterSetName = 'All')]
    [switch]$All,  # Build everything

    # A list of projects which should be built.
    [Parameter(ParameterSetName = 'Projects')]
    [string]$Projects,

    # Build a specified set of project groups
    [Parameter(ParameterSetName = 'Groups')]
    [switch]$Managed,
    [Parameter(ParameterSetName = 'Groups')]
    [switch]$Native,
    [Parameter(ParameterSetName = 'Groups')]
    [switch]$NodeJS,

    # Other lifecycle targets
    [switch]$Help, # Show help

    # Capture the rest
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$MSBuildArguments
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

#
# Functions
#

function Get-KoreBuild {

    if (!(Test-Path $LockFile)) {
        Get-RemoteFile "$ToolsSource/korebuild/channels/$Channel/latest.txt" $LockFile
    }

    $version = Get-Content $LockFile | Where-Object { $_ -like 'version:*' } | Select-Object -first 1
    if (!$version) {
        Write-Error "Failed to parse version from $LockFile. Expected a line that begins with 'version:'"
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

if ($Help) {
    Get-Help $PSCommandPath
    exit 1
}

$RepoRoot = Resolve-Path $RepoRoot
$Channel = 'master'
$ToolsSource = 'https://aspnetcore.blob.core.windows.net/buildtools'
$ConfigFile = Join-Path $PSScriptRoot 'korebuild.json'
$LockFile = Join-Path $PSScriptRoot 'korebuild-lock.txt'

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

$DotNetHome = if ($env:DOTNET_HOME) { $env:DOTNET_HOME } `
    elseif ($CI) { Join-Path $PSScriptRoot '.dotnet' } `
    elseif ($env:USERPROFILE) { Join-Path $env:USERPROFILE '.dotnet'} `
    elseif ($env:HOME) {Join-Path $env:HOME '.dotnet'}`
    else { Join-Path $PSScriptRoot '.dotnet'}

$env:DOTNET_HOME = $DotNetHome

# Execute

$korebuildPath = Get-KoreBuild

# Project selection
if ($All) {
    $MSBuildArguments += '/p:BuildAllProjects=true'
}
elseif ($Projects) {
    if (![System.IO.Path]::IsPathRooted($Projects))
    {
        $Projects = Join-Path (Get-Location) $Projects
    }
    $MSBuildArguments += "/p:Projects=$Projects"
}
else {
    # When adding new sub-group build flags, add them to this check.
    if((-not $Native) -and (-not $Managed) -and (-not $NodeJS)) {
        Write-Warning "No default group of projects was specified, so building the 'managed' subset of projects. Run ``build.cmd -help`` for more details."

        # This goal of this is to pick a sensible default for `build.cmd` with zero arguments.
        # We believe the most common thing our contributors will work on is C#, so if no other build group was picked, build the C# projects.

        $Managed = $true
    }

    $MSBuildArguments += "/p:BuildManaged=$Managed"
    $MSBuildArguments += "/p:BuildNative=$Native"
    $MSBuildArguments += "/p:BuildNodeJS=$NodeJS"
}

# Target selection
$MSBuildArguments += "/p:_RunRestore=$Restore"
$MSBuildArguments += "/p:_RunBuild=$Build"
$MSBuildArguments += "/p:_RunPack=$Pack"
$MSBuildArguments += "/p:_RunTests=$Test"
$MSBuildArguments += "/p:_RunSign=$Sign"

Import-Module -Force -Scope Local (Join-Path $korebuildPath 'KoreBuild.psd1')

try {
    Set-KoreBuildSettings -ToolsSource $ToolsSource -DotNetHome $DotNetHome -RepoPath $RepoRoot -ConfigFile $ConfigFile -CI:$CI
    Invoke-KoreBuildCommand 'default-build' @MSBuildArguments
}
finally {
    Remove-Module 'KoreBuild' -ErrorAction Ignore
}
