Param(
  [string] $GuardianCliLocation,
  [string] $Repository,
  [string] $BranchName='master',
  [string] $WorkingDirectory,
  [string] $GuardianLoggerLevel='Standard'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$disableConfigureToolsetImport = $true
$global:LASTEXITCODE = 0

# `tools.ps1` checks $ci to perform some actions. Since the SDL
# scripts don't necessarily execute in the same agent that run the
# build.ps1/sh script this variable isn't automatically set.
$ci = $true
. $PSScriptRoot\..\tools.ps1

# Don't display the console progress UI - it's a huge perf hit
$ProgressPreference = 'SilentlyContinue'

Add-Type -AssemblyName System.IO.Compression.FileSystem

try {
  # if the folder does not exist, we'll do a guardian init and push it to the remote repository
  Write-Host 'Initializing Guardian...'
  Write-Host "$GuardianCliLocation init --working-directory $WorkingDirectory --logger-level $GuardianLoggerLevel"
  & $GuardianCliLocation init --working-directory $WorkingDirectory --logger-level $GuardianLoggerLevel
  if ($LASTEXITCODE -ne 0) {
    Write-PipelineTelemetryError -Force -Category 'Build' -Message "Guardian init failed with exit code $LASTEXITCODE."
    ExitWithExitCode $LASTEXITCODE
  }
  # We create the mainbaseline so it can be edited later
  Write-Host "$GuardianCliLocation baseline --working-directory $WorkingDirectory --name mainbaseline"
  & $GuardianCliLocation baseline --working-directory $WorkingDirectory --name mainbaseline
  if ($LASTEXITCODE -ne 0) {
    Write-PipelineTelemetryError -Force -Category 'Build' -Message "Guardian baseline failed with exit code $LASTEXITCODE."
    ExitWithExitCode $LASTEXITCODE
  }
  ExitWithExitCode 0
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Force -Category 'Sdl' -Message $_
  ExitWithExitCode 1
}
