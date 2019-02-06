#requires -version 5

<#
.SYNOPSIS
Builds this repository.

.DESCRIPTION
This build script installs required tools and runs an MSBuild command on this repository.
This script can be used to invoke various targets, such as targets to produce packages,
build projects, run tests, and generate code.

.PARAMETER CI
Sets up CI specific settings and variables.

.PARAMETER Restore
Run restore.

.PARAMETER NoRestore
Suppress running restore on projects.

.PARAMETER NoBuild
Suppress re-compile projects. (Implies -NoRestore)

.PARAMETER Pack
Produce packages.

.PARAMETER Test
Run tests.

.PARAMETER Sign
Run code signing.

.PARAMETER Architecture
The CPU architecture to build for (x64, x86, arm). Default=x64

.PARAMETER Projects
A list of projects to build. Globbing patterns are supported, such as "$(pwd)/**/*.csproj"

.PARAMETER All
Build all project types.

.PARAMETER BuildManaged
Build managed projects (C#, F#, VB).
You can also use -NoBuildManaged to suppress this project type.

.PARAMETER BuildNative
Build native projects (C++).
You can also use -NoBuildNative to suppress this project type.

.PARAMETER BuildNodeJS
Build NodeJS projects (TypeScript, JS).
You can also use -NoBuildNodeJS to suppress this project type.

.PARAMETER BuildJava
Build Java projects.
You can also use -NoBuildJava to suppress this project type.

.PARAMETER BuildInstallers
Build Windows Installers. Required .NET 3.5 to be installed (WiX toolset requirement).
You can also use -NoBuildInstallers to suppress this project type.

.PARAMETER MSBuildArguments
Additional MSBuild arguments to be passed through.

.EXAMPLE
Building both native and managed projects.

    build.ps1 -BuildManaged -BuildNative

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
    [switch]$CI,

    # Build lifecycle options
    [switch]$Restore,
    [switch]$NoRestore, # Suppress restore
    [switch]$NoBuild, # Suppress compiling
    [switch]$Pack, # Produce packages
    [switch]$Test, # Run tests
    [switch]$Sign, # Code sign

    [ValidateSet('x64', 'x86', 'arm')]
    $Architecture = 'x64',

    # A list of projects which should be built.
    [string]$Projects,

    # Project selection
    [switch]$All,  # Build everything

    # Build a specified set of project groups
    [switch]$BuildManaged,
    [switch]$BuildNative,
    [switch]$BuildNodeJS,
    [switch]$BuildJava,
    [switch]$BuildInstallers,

    # Inverse of the previous switches because specifying '-switch:$false' is not intuitive for most command line users
    [switch]$NoBuildManaged,
    [switch]$NoBuildNative,
    [switch]$NoBuildNodeJS,
    [switch]$NoBuildJava,
    [switch]$NoBuildInstallers,

    # By default, Windows builds will use MSBuild.exe. Passing this will force the build to run on
    # dotnet.exe instead, which may cause issues if you invoke build on a project unsupported by
    # MSBuild for .NET Core
    [switch]$ForceCoreMsbuild,

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

if ($Help) {
    Get-Help $PSCommandPath
    exit 1
}

$Channel = 'master'
$ToolsSource = 'https://aspnetcore.blob.core.windows.net/buildtools'
$ConfigFile = Join-Path $PSScriptRoot 'korebuild.json'
$LockFile = Join-Path $PSScriptRoot 'korebuild-lock.txt'

if (Test-Path $ConfigFile) {
    try {
        $config = Get-Content -Raw -Encoding UTF8 -Path $ConfigFile | ConvertFrom-Json
        if ($config) {
            if (Get-Member -Name 'channel' -InputObject $config) { [string] $Channel = $config.channel }
            if (Get-Member -Name 'toolsSource' -InputObject $config) { [string] $ToolsSource = $config.toolsSource}
        }
    } catch {
        Write-Warning "$ConfigFile could not be read. Its settings will be ignored."
        Write-Warning $Error[0]
    }
}

$DotNetHome = Join-Path $PSScriptRoot '.dotnet'
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
# When adding new sub-group build flags, add them to this check.
elseif((-not $BuildNative) -and (-not $BuildManaged) -and (-not $BuildNodeJS) -and (-not $BuildInstallers) -and (-not $BuildJava)) {
    Write-Warning "No default group of projects was specified, so building the 'managed' and 'native' subsets of projects. Run ``build.cmd -help`` for more details."

    # This goal of this is to pick a sensible default for `build.cmd` with zero arguments.
    # Now that we support subfolder invokations of build.cmd, we will be pushing to have build.cmd build everything (-all) by default

    $BuildManaged = $true
    $BuildNative = $true
}

if ($BuildInstallers) { $MSBuildArguments += "/p:BuildInstallers=true" }
if ($BuildManaged) { $MSBuildArguments += "/p:BuildManaged=true" }
if ($BuildNative) { $MSBuildArguments += "/p:BuildNative=true" }
if ($BuildNodeJS) { $MSBuildArguments += "/p:BuildNodeJS=true" }
if ($BuildJava) { $MSBuildArguments += "/p:BuildJava=true" }

if ($NoBuildInstallers) { $MSBuildArguments += "/p:BuildInstallers=false" }
if ($NoBuildManaged) { $MSBuildArguments += "/p:BuildManaged=false" }
if ($NoBuildNative) { $MSBuildArguments += "/p:BuildNative=false" }
if ($NoBuildNodeJS) { $MSBuildArguments += "/p:BuildNodeJS=false" }
if ($NoBuildJava) { $MSBuildArguments += "/p:BuildJava=false" }

$RunBuild = if ($NoBuild) { $false } else { $true }

# Run restore by default unless -NoRestore is set.
# -NoBuild implies -NoRestore, unless -Restore is explicitly set (as in restore.cmd)
$RunRestore = if ($NoRestore) { $false }
    elseif ($Restore) { $true }
    elseif ($NoBuild) { $false }
    else { $true }

# Target selection
$MSBuildArguments += "/p:_RunRestore=$RunRestore"
$MSBuildArguments += "/p:_RunBuild=$RunBuild"
$MSBuildArguments += "/p:_RunPack=$Pack"
$MSBuildArguments += "/p:_RunTests=$Test"
$MSBuildArguments += "/p:_RunSign=$Sign"

$MSBuildArguments += "/p:TargetArchitecture=$Architecture"
$MSBuildArguments += "/p:TargetOsName=win"

Import-Module -Force -Scope Local (Join-Path $korebuildPath 'KoreBuild.psd1')

try {
    Set-KoreBuildSettings -ToolsSource $ToolsSource -DotNetHome $DotNetHome -RepoPath $PSScriptRoot -ConfigFile $ConfigFile -CI:$CI
    if ($ForceCoreMsbuild) {
        $global:KoreBuildSettings.MSBuildType = 'core'
    }
    Invoke-KoreBuildCommand 'default-build' @MSBuildArguments
}
finally {
    Remove-Module 'KoreBuild' -ErrorAction Ignore
    Remove-Item env:DOTNET_HOME
}
