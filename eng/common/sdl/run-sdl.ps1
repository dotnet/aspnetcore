Param(
  [string] $GuardianCliLocation,
  [string] $WorkingDirectory,
  [string] $GdnFolder,
  [string] $UpdateBaseline,
  [string] $GuardianLoggerLevel='Standard'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$disableConfigureToolsetImport = $true
$global:LASTEXITCODE = 0

try {
  # `tools.ps1` checks $ci to perform some actions. Since the SDL
  # scripts don't necessarily execute in the same agent that run the
  # build.ps1/sh script this variable isn't automatically set.
  $ci = $true
  . $PSScriptRoot\..\tools.ps1

  # We store config files in the r directory of .gdn
  $gdnConfigPath = Join-Path $GdnFolder 'r'
  $ValidPath = Test-Path $GuardianCliLocation

  if ($ValidPath -eq $False)
  {
    Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Invalid Guardian CLI Location."
    ExitWithExitCode 1
  }

  $gdnConfigFiles = Get-ChildItem $gdnConfigPath -Recurse -Include '*.gdnconfig'
  Write-Host "Discovered Guardian config files:"
  $gdnConfigFiles | Out-String | Write-Host

  Exec-BlockVerbosely {
    & $GuardianCliLocation run `
      --working-directory $WorkingDirectory `
      --baseline mainbaseline `
      --update-baseline $UpdateBaseline `
      --logger-level $GuardianLoggerLevel `
      --config @gdnConfigFiles
    Exit-IfNZEC "Sdl"
  }
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Force -Category 'Sdl' -Message $_
  ExitWithExitCode 1
}
