[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $configuration = "Debug",
  [string] $projects = "",
  [string] $verbosity = "minimal",
  [string] $msbuildEngine = $null,
  [bool] $warnaserror = $true,
  [bool] $nodereuse = $true,
  [switch] $execute,
  [switch] $restore,
  [switch] $deployDeps,
  [switch] $build,
  [switch] $rebuild,
  [switch] $deploy,
  [switch] $test,
  [switch] $integrationTest,
  [switch] $performanceTest,
  [switch] $sign,
  [switch] $pack,
  [switch] $publish,
  [switch] $publishBuildAssets,
  [switch] $ci,
  [switch] $prepareMachine,
  [switch] $help,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

. $PSScriptRoot\tools.ps1

function Print-Usage() {
    Write-Host "Common settings:"
    Write-Host "  -configuration <value>  Build configuration Debug, Release"
    Write-Host "  -verbosity <value>      Msbuild verbosity (q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic])"
    Write-Host "  -help                   Print help and exit"
    Write-Host ""

    Write-Host "Actions:"
    Write-Host "  -restore                Restore dependencies"
    Write-Host "  -build                  Build solution"
    Write-Host "  -rebuild                Rebuild solution"
    Write-Host "  -deploy                 Deploy built VSIXes"
    Write-Host "  -deployDeps             Deploy dependencies (e.g. VSIXes for integration tests)"
    Write-Host "  -test                   Run all unit tests in the solution"
    Write-Host "  -pack                   Package build outputs into NuGet packages and Willow components"
    Write-Host "  -integrationTest        Run all integration tests in the solution"
    Write-Host "  -performanceTest        Run all performance tests in the solution"
    Write-Host "  -sign                   Sign build outputs"
    Write-Host "  -publish                Publish artifacts (e.g. symbols)"
    Write-Host "  -publishBuildAssets        Push assets to BAR"
    Write-Host ""

    Write-Host "Advanced settings:"
    Write-Host "  -projects <value>       Semi-colon delimited list of sln/proj's to build. Globbing is supported (*.sln)"
    Write-Host "  -ci                     Set when running on CI server"
    Write-Host "  -prepareMachine         Prepare machine for CI run"
    Write-Host "  -msbuildEngine <value>  Msbuild engine to use to run build ('dotnet', 'vs', or unspecified)."
    Write-Host ""
    Write-Host "Command line arguments not listed above are passed thru to msbuild."
    Write-Host "The above arguments can be shortened as much as to be unambiguous (e.g. -co for configuration, -t for test, etc.)."
}

if ($help -or (($properties -ne $null) -and ($properties.Contains("/help") -or $properties.Contains("/?")))) {
  Print-Usage
  exit 0
}

try {
  if ($projects -eq "") {
    $projects = Join-Path $RepoRoot "*.sln"
  }

  InitializeTools

  $BuildLog = Join-Path $LogDir "Build.binlog"

  MSBuild $ToolsetBuildProj `
    /bl:$BuildLog `
    /p:Configuration=$configuration `
    /p:Projects=$projects `
    /p:RepoRoot=$RepoRoot `
    /p:Restore=$restore `
    /p:DeployDeps=$deployDeps `
    /p:Build=$build `
    /p:Rebuild=$rebuild `
    /p:Deploy=$deploy `
    /p:Test=$test `
    /p:Pack=$pack `
    /p:IntegrationTest=$integrationTest `
    /p:PerformanceTest=$performanceTest `
    /p:Sign=$sign `
    /p:Publish=$publish `
    /p:Execute=$execute `
    /p:ContinuousIntegrationBuild=$ci `
    @properties

  if ($lastExitCode -ne 0) {
    Write-Host "Build Failed (exit code '$lastExitCode'). See log: $BuildLog" -ForegroundColor Red
    ExitWithExitCode $lastExitCode
  }

  ExitWithExitCode $lastExitCode
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
