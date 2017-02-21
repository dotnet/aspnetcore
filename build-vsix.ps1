#requires -version 4
param(
    [string]$MSBuildPath,
    [string]$Configuration='Debug',
    [string]$VsixName='Microsoft.VisualStudio.RazorExtension.vsix'
)
$ErrorActionPreference='Stop'
########################
#  Helpers
########################

function exec($cmd) {
    $cmdName = [IO.Path]::GetFileName($cmd)
    Write-Host -ForegroundColor Cyan "> $cmdName $args"
    & $cmd @args
    $exitCode = $LASTEXITCODE
    if($exitCode -ne 0) {
        throw "'$cmdName $args' failed with exit code: $exitCode"
    }
}

function Get-BuildConfiguration {
    if ($env:Configuration) {
        return $env:Configuration
    }
    return $Configuration
}

function Get-MSBuildPath {
    if ($MSBuildPath) {
        return $MSBuildPath
    }

    $vsDir=$ENV:VSINSTALLDIR
    if (!($vsDir)) {
        $vsDir = Join-Path ${ENV:ProgramFiles(x86)} 'Microsoft Visual Studio/2017*/*'
    }

    $msbuild = Get-ChildItem $(Join-Path $vsDir 'MSBuild/15.0/bin/MSBuild.exe')

    if (!($msbuild) -or ($msbuild | Measure-Object).Count -ne 1) {
        throw "MSBuild 15.0 could not be located automatically in '$vsDir'. Use -MSBuildPath to specify its location."
    }

    return $msbuild
}

########################
#  Variables
########################

$intermediateDir = Join-Path $PSScriptRoot 'obj'
$nugetExePath = Join-Path $intermediateDir 'nuget.exe'
$artifactsDir = Join-Path $PSScriptRoot 'artifacts'
$buildDir = Join-Path $artifactsDir 'build'
$vsixPath = Join-Path $buildDir $VsixName
$msbuildDir = Join-Path $artifactsDir 'msbuild'
$logPath = Join-Path $msbuildDir 'vsix-msbuild.log'
$vsixCsproj = Join-Path $PSScriptRoot 'tooling/Microsoft.VisualStudio.RazorExtension/Microsoft.VisualStudio.RazorExtension.csproj'
$msbuild = Get-MSBuildPath
$config = Get-BuildConfiguration

########################
#  Main
########################

exec .\build.ps1 initialize

mkdir $buildDir -ErrorAction Ignore | out-null
mkdir $msbuildDir -ErrorAction Ignore | out-null
mkdir $intermediateDir -ErrorAction Ignore | out-null

if (!(Test-Path $nugetExePath))
{
    Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/v4.0.0-rc4/NuGet.exe" -OutFile "$nugetExePath"
}

exec $nugetExePath restore $vsixCsproj -SolutionDirectory $PSScriptRoot -Verbosity quiet
exec $msbuild $vsixCsproj `
    /v:m `
    /p:DeployExtension=false `
    /fl "/flp:v=D;LogFile=$logPath" `
    /p:TargetVsixContainer=$vsixPath `
    /p:Configuration=$config
