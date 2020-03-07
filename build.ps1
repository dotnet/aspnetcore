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

.PARAMETER NoBuildDeps
Do not build project-to-project references and only build the specified project.

.PARAMETER NoBuildRepoTasks
Skip building eng/tools/RepoTasks/

.PARAMETER Pack
Produce packages.

.PARAMETER Test
Run tests.

.PARAMETER Sign
Run code signing.

.PARAMETER Configuration
Debug or Release

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

.PARAMETER BinaryLog
Enable the binary logger

.PARAMETER Verbosity
MSBuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]

.PARAMETER MSBuildArguments
Additional MSBuild arguments to be passed through.

.PARAMETER DotNetRuntimeSourceFeed
Additional feed that can be used when downloading .NET runtimes

.PARAMETER DotNetRuntimeSourceFeedKey
Key for feed that can be used when downloading .NET runtimes

.EXAMPLE
Building both native and managed projects.

    build.ps1 -BuildManaged -BuildNative

.EXAMPLE
Building a subfolder of code.

    build.ps1 -projects "$(pwd)/src/SomeFolder/**/*.csproj"

.EXAMPLE
Running tests.

    build.ps1 -test

.LINK
Online version: https://github.com/dotnet/aspnetcore/blob/master/docs/BuildFromSource.md
#>
[CmdletBinding(PositionalBinding = $false, DefaultParameterSetName='Groups')]
param(
    [switch]$CI,

    # Build lifecycle options
    [switch]$Restore,
    [switch]$NoRestore, # Suppress restore
    [switch]$NoBuild, # Suppress compiling
    [switch]$NoBuildDeps, # Suppress project to project dependencies
    [switch]$Pack, # Produce packages
    [switch]$Test, # Run tests
    [switch]$Sign, # Code sign

    [Alias('c')]
    [ValidateSet('Debug', 'Release')]
    $Configuration,

    [ValidateSet('x64', 'x86', 'arm', 'arm64')]
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

    [switch]$NoBuildRepoTasks,

    # By default, Windows builds will use MSBuild.exe. Passing this will force the build to run on
    # dotnet.exe instead, which may cause issues if you invoke build on a project unsupported by
    # MSBuild for .NET Core
    [switch]$ForceCoreMsbuild,

    # Diagnostics
    [Alias('bl')]
    [switch]$BinaryLog,
    [Alias('v')]
    [string]$Verbosity = 'minimal',
    [switch]$DumpProcesses, # Capture all running processes and dump them to a file.

    # Other lifecycle targets
    [switch]$Help, # Show help

    # Optional arguments that enable downloading an internal
    # runtime or runtime from a non-default location
    [string]$DotNetRuntimeSourceFeed,
    [string]$DotNetRuntimeSourceFeedKey,

    # Capture the rest
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$MSBuildArguments
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

if ($Help) {
    Get-Help $PSCommandPath
    exit 1
}

if ($DumpProcesses -or $CI) {
    # Dump running processes
    Start-Job -Name DumpProcesses -FilePath $PSScriptRoot\eng\scripts\dump_process.ps1 -ArgumentList $PSScriptRoot
}

# Project selection
if ($All) {
    $MSBuildArguments += '/p:BuildAllProjects=true'
}
elseif ($Projects) {
    if (![System.IO.Path]::IsPathRooted($Projects))
    {
        $Projects = Join-Path (Get-Location) $Projects
    }
    $MSBuildArguments += "/p:ProjectToBuild=$Projects"
}
# When adding new sub-group build flags, add them to this check.
elseif((-not $BuildNative) -and (-not $BuildManaged) -and (-not $BuildNodeJS) -and (-not $BuildInstallers) -and (-not $BuildJava)) {
    Write-Warning "No default group of projects was specified, so building the 'managed' and its dependent subsets of projects. Run ``build.cmd -help`` for more details."

    # This goal of this is to pick a sensible default for `build.cmd` with zero arguments.
    # Now that we support subfolder invokations of build.cmd, we will be pushing to have build.cmd build everything (-all) by default

    $BuildManaged = $true
}

if ($BuildManaged -or ($All -and (-not $NoBuildManaged))) {
    if ((-not $BuildNodeJS) -and (-not $NoBuildNodeJS)) {
        $node = Get-Command node -ErrorAction Ignore -CommandType Application

        if ($node) {
            $nodeHome = Split-Path -Parent (Split-Path -Parent $node.Path)
            Write-Host -f Magenta "Building of C# project is enabled and has dependencies on NodeJS projects. Building of NodeJS projects is enabled since node is detected in $nodeHome."
        }
        else {
            Write-Host -f Magenta "Building of NodeJS projects is disabled since node is not detected on Path and no BuildNodeJs or NoBuildNodeJs setting is set explicitly."
            $NoBuildNodeJS = $true
        }
    }

    if ($NoBuildNodeJS){
        Write-Warning "Some managed projects depend on NodeJS projects. Building NodeJS is disabled so the managed projects will fallback to using the output from previous builds. The output may not be correct or up to date."
    }
}

if ($BuildInstallers) { $MSBuildArguments += "/p:BuildInstallers=true" }
if ($BuildManaged) { $MSBuildArguments += "/p:BuildManaged=true" }
if ($BuildNative) { $MSBuildArguments += "/p:BuildNative=true" }
if ($BuildNodeJS) { $MSBuildArguments += "/p:BuildNodeJS=true" }
if ($BuildJava) { $MSBuildArguments += "/p:BuildJava=true" }

if ($NoBuildDeps) { $MSBuildArguments += "/p:BuildProjectReferences=false" }

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
$MSBuildArguments += "/p:Restore=$RunRestore"
$MSBuildArguments += "/p:Build=$RunBuild"
if (-not $RunBuild) {
    $MSBuildArguments += "/p:NoBuild=true"
}
$MSBuildArguments += "/p:Pack=$Pack"
$MSBuildArguments += "/p:Test=$Test"
$MSBuildArguments += "/p:Sign=$Sign"

$MSBuildArguments += "/p:TargetArchitecture=$Architecture"
$MSBuildArguments += "/p:TargetOsName=win"

if (-not $Configuration) {
    $Configuration = if ($CI) { 'Release' } else { 'Debug' }
}
$MSBuildArguments += "/p:Configuration=$Configuration"

[string[]]$ToolsetBuildArguments = @()
if ($DotNetRuntimeSourceFeed -or $DotNetRuntimeSourceFeedKey) {
    $runtimeFeedArg = "/p:DotNetRuntimeSourceFeed=$DotNetRuntimeSourceFeed"
    $runtimeFeedKeyArg = "/p:DotNetRuntimeSourceFeedKey=$DotNetRuntimeSourceFeedKey"
    $MSBuildArguments += $runtimeFeedArg
    $MSBuildArguments += $runtimeFeedKeyArg
    $ToolsetBuildArguments += $runtimeFeedArg
    $ToolsetBuildArguments += $runtimeFeedKeyArg
}

$foundJdk = $false
$javac = Get-Command javac -ErrorAction Ignore -CommandType Application
$localJdkPath = "$PSScriptRoot\.tools\jdk\win-x64\"
if (Test-Path "$localJdkPath\bin\javac.exe") {
    $foundJdk = $true
    Write-Host -f Magenta "Detected JDK in $localJdkPath (via local repo convention)"
    $env:JAVA_HOME = $localJdkPath
}
elseif ($env:JAVA_HOME) {
    if (-not (Test-Path "${env:JAVA_HOME}\bin\javac.exe")) {
        Write-Error "The environment variable JAVA_HOME was set, but ${env:JAVA_HOME}\bin\javac.exe does not exist. Remove JAVA_HOME or update it to the correct location for the JDK. See https://www.bing.com/search?q=java_home for details."
    }
    else {
        Write-Host -f Magenta "Detected JDK in ${env:JAVA_HOME} (via JAVA_HOME)"
        $foundJdk = $true
    }
}
elseif ($javac) {
    $foundJdk = $true
    $javaHome = Split-Path -Parent (Split-Path -Parent $javac.Path)
    $env:JAVA_HOME = $javaHome
    Write-Host -f Magenta "Detected JDK in $javaHome (via PATH)"
}
else {
    try {
        $jdkRegistryKeys = @(
            "HKLM:\SOFTWARE\JavaSoft\JDK",  # for JDK 10+
            "HKLM:\SOFTWARE\JavaSoft\Java Development Kit"  # fallback for JDK 8
        )
        $jdkRegistryKey = $jdkRegistryKeys | Where-Object { Test-Path $_ } | Select-Object -First 1
        if ($jdkRegistryKey) {
            $jdkVersion = (Get-Item $jdkRegistryKey | Get-ItemProperty -name CurrentVersion).CurrentVersion
            $javaHome = (Get-Item $jdkRegistryKey\$jdkVersion | Get-ItemProperty -Name JavaHome).JavaHome
            if (Test-Path "${javaHome}\bin\javac.exe") {
                $env:JAVA_HOME = $javaHome
                Write-Host -f Magenta "Detected JDK $jdkVersion in $env:JAVA_HOME (via registry)"
                $foundJdk = $true
            }
        }
    }
    catch {
        Write-Verbose "Failed to detect Java: $_"
    }
}

if ($env:PATH -notlike "*${env:JAVA_HOME}*") {
    $env:PATH = "$(Join-Path $env:JAVA_HOME bin);${env:PATH}"
}

if (-not $foundJdk -and $RunBuild -and ($All -or $BuildJava) -and -not $NoBuildJava) {
    Write-Error "Could not find the JDK. Either run $PSScriptRoot\eng\scripts\InstallJdk.ps1 to install for this repo, or install the JDK globally on your machine (see $PSScriptRoot\docs\BuildFromSource.md for details)."
}

# Initialize global variables need to be set before the import of Arcade is imported
$restore = $RunRestore

# Though VS Code may indicate $nodeReuse, $warnAsError and $msbuildEngine are unused, tools.ps1 uses them.

# Disable node reuse - Workaround perpetual issues in node reuse and custom task assemblies
$nodeReuse = $false
$env:MSBUILDDISABLENODEREUSE=1

# Our build often has warnings that we can't fix, like "MSB3026: Could not copy" due to race
# conditions in building C++
# Fixing this is tracked by https://github.com/dotnet/aspnetcore-internal/issues/601
$warnAsError = $false

if ($ForceCoreMsbuild) {
    $msbuildEngine = 'dotnet'
}

# Workaround Arcade check which asserts BinaryLog is true on CI.
# We always use binlogs on CI, but we customize the name of the log file
$tmpBinaryLog = $BinaryLog
if ($CI) {
    $BinaryLog = $true
}

# tools.ps1 corrupts global state, so reset these values in case they carried over from a previous build
Remove-Item variable:global:_BuildTool -ea Ignore
Remove-Item variable:global:_DotNetInstallDir -ea Ignore
Remove-Item variable:global:_ToolsetBuildProj -ea Ignore
Remove-Item variable:global:_MSBuildExe -ea Ignore

# Import Arcade
. "$PSScriptRoot/eng/common/tools.ps1"

if ($tmpBinaryLog) {
    $MSBuildArguments += "/bl:$LogDir/Build.binlog"
}

# Capture MSBuild crash logs
$env:MSBUILDDEBUGPATH = $LogDir

$local:exit_code = $null
try {
    # Import custom tools configuration, if present in the repo.
    # Note: Import in global scope so that the script set top-level variables without qualification.
    $configureToolsetScript = Join-Path $EngRoot "configure-toolset.ps1"
    if (Test-Path $configureToolsetScript) {
      . $configureToolsetScript
    }

    # Set this global property so Arcade will always initialize the toolset. The error message you get when you build on a clean machine
    # with -norestore is not obvious about what to do to fix it. As initialization takes very little time, we think always initializing
    # the toolset is a better default behavior.
    $tmpRestore = $restore
    $restore = $true

    $toolsetBuildProj = InitializeToolset

    $restore = $tmpRestore

    if ($ci) {
        $global:VerbosePreference = 'Continue'
    }

    if (-not $NoBuildRepoTasks) {
        MSBuild $toolsetBuildProj `
            /p:RepoRoot=$RepoRoot `
            /p:Projects=$EngRoot\tools\RepoTasks\RepoTasks.csproj `
            /p:Configuration=Release `
            /p:Restore=$RunRestore `
            /p:Build=true `
            /clp:NoSummary `
            @ToolsetBuildArguments
    }

    MSBuild $toolsetBuildProj `
        /p:RepoRoot=$RepoRoot `
        @MSBuildArguments
}
catch {
    Write-Host $_.ScriptStackTrace
    Write-PipelineTaskError -Message $_
    $exit_code = 1
}
finally {
    if (! $exit_code) {
        $exit_code = $LASTEXITCODE
    }

    # tools.ps1 corrupts global state, so reset these values so they don't carry between invocations of build.ps1
    Remove-Item variable:global:_BuildTool -ea Ignore
    Remove-Item variable:global:_DotNetInstallDir -ea Ignore
    Remove-Item variable:global:_ToolsetBuildProj -ea Ignore
    Remove-Item variable:global:_MSBuildExe -ea Ignore

    if ($DumpProcesses -or $ci) {
        Stop-Job -Name DumpProcesses
        Remove-Job -Name DumpProcesses
    }

    if ($ci) {
        & "$PSScriptRoot/eng/scripts/KillProcesses.ps1"
    }
}

ExitWithExitCode $exit_code
