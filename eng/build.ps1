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

.PARAMETER PrepareMachine
In CI, Turns on machine preparation/clean up code that changes the machine state (e.g. kills build processes).

.PARAMETER NativeToolsOnMachine
Turns on native tooling handling. On CI machines, promotes native tools listed in global.json to the path.

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

.PARAMETER OnlyBuildRepoTasks
Only build eng/tools/RepoTasks/ and nothing else.

.PARAMETER Pack
Produce packages.

.PARAMETER Test
Run tests.

.PARAMETER Sign
Run code signing.

.PARAMETER Publish
Run publishing.

.PARAMETER Configuration
Debug or Release

.PARAMETER Architecture
The CPU architecture to build for (x64, x86, arm64). Default=x64

.PARAMETER Projects
A list of projects to build. Globbing patterns are supported, such as "$(pwd)/**/*.csproj"

.PARAMETER All
Build all project types.

.PARAMETER BuildManaged
Build managed projects (C#, F#, VB).
You can also use -NoBuildManaged to suppress this project type.

.PARAMETER BuildNative
Build native projects (C++).
This is the default but useful when you want to build _only_ native projects.
You can use -NoBuildNative to suppress this project type.

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

.PARAMETER ExcludeCIBinarylog
Don't output binary log by default in CI builds (short: -nobl).

.PARAMETER Verbosity
MSBuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]

.PARAMETER MSBuildArguments
Additional MSBuild arguments to be passed through.

.PARAMETER RuntimeSourceFeed
Additional feed that can be used when downloading .NET runtimes and SDKs

.PARAMETER RuntimeSourceFeedKey
Key for feed that can be used when downloading .NET runtimes and SDKs

.EXAMPLE
Building both native and managed projects.

    build.ps1

or

    build.ps1 -BuildManaged

or

    build.ps1 -BuildManaged -BuildNative

.EXAMPLE
Build only native projects.

    build.ps1 -BuildNative

.EXAMPLE
Building a subfolder of code.

    build.ps1 -projects "$(pwd)/src/SomeFolder/**/*.csproj"

.EXAMPLE
Running tests.

    build.ps1 -test

.LINK
Online version: https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md
#>
[CmdletBinding(PositionalBinding = $false, DefaultParameterSetName='Groups')]
param(
    [switch]$CI,
    [switch]$PrepareMachine,
    [switch]$NativeToolsOnMachine,

    # Build lifecycle options
    [switch]$Restore,
    [switch]$NoRestore, # Suppress restore
    [switch]$NoBuild, # Suppress compiling
    [switch]$NoBuildDeps, # Suppress project to project dependencies
    [switch]$Pack, # Produce packages
    [switch]$Test, # Run tests
    [switch]$Sign, # Code sign
    [switch]$Publish, # Run arcade publishing

    [Alias('c')]
    [ValidateSet('Debug', 'Release')]
    $Configuration,

    [ValidateSet('x64', 'x86', 'arm64')]
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
    [switch]$OnlyBuildRepoTasks,

    # Diagnostics
    [Alias('bl')]
    [switch]$BinaryLog,
    [Alias('nobl')]
    [switch]$ExcludeCIBinarylog,
    [Alias('v')]
    [string]$Verbosity = 'minimal',
    [switch]$DumpProcesses, # Capture all running processes and dump them to a file.

    # Other lifecycle targets
    [switch]$Help, # Show help

    # Optional arguments that enable downloading an internal
    # runtime or runtime from a non-default location
    [Alias('DotNetRuntimeSourceFeed')]
    [string]$RuntimeSourceFeed,
    [Alias('DotNetRuntimeSourceFeedKey')]
    [string]$RuntimeSourceFeedKey,

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
    Start-Job -Name DumpProcesses -FilePath $PSScriptRoot\scripts\dump_process.ps1 -ArgumentList $PSScriptRoot
}

# Project selection
if ($Projects) {
    if (![System.IO.Path]::IsPathRooted($Projects))
    {
        $Projects = Join-Path (Get-Location) $Projects
    }
}
# When adding new sub-group build flags, add them to this check.
elseif (-not ($All -or $BuildNative -or $BuildManaged -or $BuildNodeJS -or $BuildInstallers -or $BuildJava -or $OnlyBuildRepoTasks)) {
    Write-Warning "No default group of projects was specified, so building the managed and native projects and their dependencies. Run ``build.cmd -help`` for more details."

    # The goal of this is to pick a sensible default for `build.cmd` with zero arguments.
    $BuildManaged = $true
}

if ($BuildManaged -or ($All -and (-not $NoBuildManaged))) {
    if (-not ($BuildNodeJS -or $NoBuildNodeJS)) {
        $node = Get-Command node -ErrorAction Ignore -CommandType Application

        if ($node) {
            $nodeHome = Split-Path -Parent (Split-Path -Parent $node.Path)
            Write-Host -f Magenta "Building of C# project is enabled and has dependencies on NodeJS projects. Building of NodeJS projects is enabled since node is detected in $nodeHome."
            Write-Host -f Magenta "Note that if you are running Source Build, building NodeJS projects will be disabled later on."
            $BuildNodeJS = $true
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

if ($NoBuildDeps) { $MSBuildArguments += "/p:BuildProjectReferences=false" }

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
if (-not $RunBuild) { $MSBuildArguments += "/p:NoBuild=true" }
$MSBuildArguments += "/p:Pack=$Pack"
$MSBuildArguments += "/p:Test=$Test"
$MSBuildArguments += "/p:Sign=$Sign"
$MSBuildArguments += "/p:Publish=$Publish"

$MSBuildArguments += "/p:TargetArchitecture=$Architecture"
$MSBuildArguments += "/p:TargetOsName=win"

if (-not $Configuration) {
    $Configuration = if ($CI) { 'Release' } else { 'Debug' }
}
$MSBuildArguments += "/p:Configuration=$Configuration"

[string[]]$ToolsetBuildArguments = @()
if ($RuntimeSourceFeed -or $RuntimeSourceFeedKey) {
    $runtimeFeedArg = "/p:DotNetRuntimeSourceFeed=$RuntimeSourceFeed"
    $runtimeFeedKeyArg = "/p:DotNetRuntimeSourceFeedKey=$RuntimeSourceFeedKey"
    $MSBuildArguments += $runtimeFeedArg
    $MSBuildArguments += $runtimeFeedKeyArg
    $ToolsetBuildArguments += $runtimeFeedArg
    $ToolsetBuildArguments += $runtimeFeedKeyArg
}

# Split build categories between dotnet msbuild and desktop msbuild. Use desktop msbuild as little as possible.
[string[]]$dotnetBuildArguments = $MSBuildArguments
if ($All) { $dotnetBuildArguments += '/p:BuildAllProjects=true' }
if ($Projects) {
    if ($BuildNative) {
        $MSBuildArguments += "/p:ProjectToBuild=$Projects"
    } else {
        $dotnetBuildArguments += "/p:ProjectToBuild=$Projects"
    }
}

if ($NoBuildInstallers) { $MSBuildArguments += "/p:BuildInstallers=false"; $BuildInstallers = $false }
if ($BuildInstallers) { $MSBuildArguments += "/p:BuildInstallers=true" }

# Build native projects by default unless -NoBuildNative was specified.
$specifiedBuildNative = $BuildNative
$BuildNative = $true
if ($NoBuildNative) { $MSBuildArguments += "/p:BuildNative=false"; $BuildNative = $false }
if ($BuildNative) { $MSBuildArguments += "/p:BuildNative=true"}

if ($NoBuildJava) { $dotnetBuildArguments += "/p:BuildJava=false"; $BuildJava = $false }
if ($BuildJava) { $dotnetBuildArguments += "/p:BuildJava=true" }
if ($NoBuildManaged) { $dotnetBuildArguments += "/p:BuildManaged=false"; $BuildManaged = $false }
if ($BuildManaged) { $dotnetBuildArguments += "/p:BuildManaged=true" }
if ($NoBuildNodeJS) { $dotnetBuildArguments += "/p:BuildNodeJSUnlessSourcebuild=false"; $BuildNodeJS = $false }
if ($BuildNodeJS) { $dotnetBuildArguments += "/p:BuildNodeJSUnlessSourcebuild=true" }

# Don't bother with two builds if just one will build everything. Ignore super-weird cases like
# "-Projects ... -NoBuildJava -NoBuildManaged -NoBuildNodeJS". An empty `./build.ps1` command will build both
# managed and native projects.
$performDesktopBuild = $BuildInstallers -or $BuildNative
$performDotnetBuild = $BuildJava -or $BuildManaged -or $BuildNodeJS -or `
    ($All -and -not ($NoBuildJava -and $NoBuildManaged -and $NoBuildNodeJS)) -or `
    ($Projects -and -not ($BuildInstallers -or $specifiedBuildNative))

# Initialize global variables need to be set before the import of Arcade is imported
$restore = $RunRestore

# Though VS Code may indicate $nodeReuse and $msbuildEngine are unused, tools.ps1 uses them.

# Disable node reuse - Workaround perpetual issues in node reuse and custom task assemblies
$nodeReuse = $false
$env:MSBUILDDISABLENODEREUSE=1

# Use `dotnet msbuild` by default
$msbuildEngine = 'dotnet'

# Ensure passing neither -bl nor -nobl on CI avoids errors in tools.ps1. This is needed because both parameters are
# $false by default i.e. they always exist. (We currently avoid binary logs but that is made visible in the YAML.)
if ($CI -and -not $excludeCIBinarylog) {
    $binaryLog = $true
}

# tools.ps1 corrupts global state, so reset these values in case they carried over from a previous build
Remove-Item variable:global:_BuildTool -ea Ignore
Remove-Item variable:global:_DotNetInstallDir -ea Ignore
Remove-Item variable:global:_ToolsetBuildProj -ea Ignore
Remove-Item variable:global:_MSBuildExe -ea Ignore

# tools.ps1 expects the remaining arguments to be available via the $properties string array variable
# TODO: Remove when https://github.com/dotnet/source-build/issues/4337 is implemented.
[string[]] $properties = $MSBuildArguments

# Import Arcade
. "$PSScriptRoot/common/tools.ps1"

function LocateJava {
    $foundJdk = $false
    $javac = Get-Command javac -ErrorAction Ignore -CommandType Application
    $localJdkPath = "$PSScriptRoot\..\.tools\jdk\win-x64\"
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
        Write-Error "Could not find the JDK. Either run $PSScriptRoot\scripts\InstallJdk.ps1 to install for this repo, or install the JDK globally on your machine (see $PSScriptRoot\..\docs\BuildFromSource.md for details)."
    }
}

# Add default .binlog location if not already on the command line. tools.ps1 does not handle this; it just checks
# $BinaryLog, $CI and $ExcludeCIBinarylog values for an error case. But tools.ps1 provides a nice function to help.
if ($BinaryLog) {
    $bl = GetMSBuildBinaryLogCommandLineArgument($MSBuildArguments)
    if (-not $bl) {
        $dotnetBuildArguments += "/bl:" + (Join-Path $LogDir "Build.binlog")
        $MSBuildArguments += "/bl:" + (Join-Path $LogDir "Build.native.binlog")
        $ToolsetBuildArguments += "/bl:" + (Join-Path $LogDir "Build.repotasks.binlog")
    } else {
        # Use a different binary log path when running desktop msbuild if doing both builds.
        if ($performDesktopBuild -and $performDotnetBuild) {
            $MSBuildArguments += "/bl:" + [System.IO.Path]::ChangeExtension($bl, "native.binlog")
        }

        $ToolsetBuildArguments += "/bl:" + [System.IO.Path]::ChangeExtension($bl, "repotasks.binlog")
    }
} elseif ($CI) {
    # Ensure the artifacts/log directory isn't empty to avoid warnings.
    New-Item (Join-Path $LogDir "empty.log") -ItemType File -ErrorAction SilentlyContinue >$null
}

# Capture MSBuild crash logs
$env:MSBUILDDEBUGPATH = $LogDir

$local:exit_code = $null
try {
    # Set this global property so Arcade will always initialize the toolset. The error message you get when you build on a clean machine
    # with -norestore is not obvious about what to do to fix it. As initialization takes very little time, we think always initializing
    # the toolset is a better default behavior.
    $tmpRestore = $restore
    $restore = $true

    # Initialize the native tools before locating java.
    if ($NativeToolsOnMachine) {
        $env:NativeToolsOnMachine=$true
        # Do not promote native tools except in cases where -NativeToolsOnMachine is passed.
        # Currently the JDK is laid out in an incorrect pattern: https://github.com/dotnet/dnceng/issues/2185
        InitializeNativeTools
    }

    # Locate java, now that we may have java available after initializing native tools.
    LocateJava

    $toolsetBuildProj = InitializeToolset

    $restore = $tmpRestore

    if ($ci) {
        $global:VerbosePreference = 'Continue'
    }

    if (-not $NoBuildRepoTasks) {
        Write-Host

        MSBuild $toolsetBuildProj `
            /p:RepoRoot=$RepoRoot `
            /p:Projects=$EngRoot\tools\RepoTasks\RepoTasks.csproj `
            /p:Configuration=Release `
            /p:Restore=$RunRestore `
            /p:Build=true `
            /clp:NoSummary `
            @ToolsetBuildArguments
    }

    if (-not $OnlyBuildRepoTasks) {
        if ($performDesktopBuild) {
            Write-Host
            Remove-Item variable:global:_BuildTool -ErrorAction Ignore
            $msbuildEngine = 'vs'

            MSBuild $toolsetBuildProj /p:RepoRoot=$RepoRoot @MSBuildArguments
        }

        if ($performDotnetBuild) {
            Write-Host
            Remove-Item variable:global:_BuildTool -ErrorAction Ignore
            $msbuildEngine = 'dotnet'

            MSBuild $toolsetBuildProj /p:RepoRoot=$RepoRoot @dotnetBuildArguments
        }
    }
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
}

ExitWithExitCode $exit_code
