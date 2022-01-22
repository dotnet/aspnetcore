Param(
  [string] $GuardianCliLocation,
  [string] $WorkingDirectory,
  [string] $TargetDirectory,
  [string] $GdnFolder,
  [string[]] $ToolsList,
  [string] $UpdateBaseline,
  [string] $GuardianLoggerLevel='Standard',
  [string[]] $CrScanAdditionalRunConfigParams,
  [string[]] $PoliCheckAdditionalRunConfigParams
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$disableConfigureToolsetImport = $true
$LASTEXITCODE = 0

try {
  # `tools.ps1` checks $ci to perform some actions. Since the SDL
  # scripts don't necessarily execute in the same agent that run the
  # build.ps1/sh script this variable isn't automatically set.
  $ci = $true
  . $PSScriptRoot\..\tools.ps1

  # We store config files in the r directory of .gdn
  Write-Host $ToolsList
  $gdnConfigPath = Join-Path $GdnFolder 'r'
  $ValidPath = Test-Path $GuardianCliLocation

  if ($ValidPath -eq $False)
  {
    Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Invalid Guardian CLI Location."
    ExitWithExitCode 1
  }

  $configParam = @('--config')

  foreach ($tool in $ToolsList) {
    $gdnConfigFile = Join-Path $gdnConfigPath "$tool-configure.gdnconfig"
    Write-Host $tool
    # We have to manually configure tools that run on source to look at the source directory only
    if ($tool -eq 'credscan') {
      Write-Host "$GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args `" TargetDirectory < $TargetDirectory `" `" OutputType < pre `" $(If ($CrScanAdditionalRunConfigParams) {$CrScanAdditionalRunConfigParams})"
      & $GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args " TargetDirectory < $TargetDirectory " "OutputType < pre" $(If ($CrScanAdditionalRunConfigParams) {$CrScanAdditionalRunConfigParams})
      if ($LASTEXITCODE -ne 0) {
        Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Guardian configure for $tool failed with exit code $LASTEXITCODE."
        ExitWithExitCode $LASTEXITCODE
      }
    }
    if ($tool -eq 'policheck') {
      Write-Host "$GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args `" Target < $TargetDirectory `" $(If ($PoliCheckAdditionalRunConfigParams) {$PoliCheckAdditionalRunConfigParams})"
      & $GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args " Target < $TargetDirectory " $(If ($PoliCheckAdditionalRunConfigParams) {$PoliCheckAdditionalRunConfigParams})
      if ($LASTEXITCODE -ne 0) {
        Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Guardian configure for $tool failed with exit code $LASTEXITCODE."
        ExitWithExitCode $LASTEXITCODE
      }
    }

    $configParam+=$gdnConfigFile
  }

  Write-Host "$GuardianCliLocation run --working-directory $WorkingDirectory --baseline mainbaseline --update-baseline $UpdateBaseline --logger-level $GuardianLoggerLevel $configParam"
  & $GuardianCliLocation run --working-directory $WorkingDirectory --tool $tool --baseline mainbaseline --update-baseline $UpdateBaseline --logger-level $GuardianLoggerLevel $configParam
  if ($LASTEXITCODE -ne 0) {
    Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Guardian run for $ToolsList using $configParam failed with exit code $LASTEXITCODE."
    ExitWithExitCode $LASTEXITCODE
  }
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Force -Category 'Sdl' -Message $_
  ExitWithExitCode 1
}
