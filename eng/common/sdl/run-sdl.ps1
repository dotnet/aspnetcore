Param(
  [string] $GuardianCliLocation,
  [string] $WorkingDirectory,
  [string] $TargetDirectory,
  [string] $GdnFolder,
  [string[]] $ToolsList,
  [string] $UpdateBaseline,
  [string] $GuardianLoggerLevel="Standard",
  [string[]] $CrScanAdditionalRunConfigParams,
  [string[]] $PoliCheckAdditionalRunConfigParams
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0
$LASTEXITCODE = 0

# We store config files in the r directory of .gdn
Write-Host $ToolsList
$gdnConfigPath = Join-Path $GdnFolder "r"
$ValidPath = Test-Path $GuardianCliLocation

if ($ValidPath -eq $False)
{
  Write-Host "Invalid Guardian CLI Location."
  exit 1
}

$configParam = @("--config")

foreach ($tool in $ToolsList) {
  $gdnConfigFile = Join-Path $gdnConfigPath "$tool-configure.gdnconfig"
  Write-Host $tool
  # We have to manually configure tools that run on source to look at the source directory only
  if ($tool -eq "credscan") {
    Write-Host "$GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args `" TargetDirectory < $TargetDirectory `" `" OutputType < pre `" $(If ($CrScanAdditionalRunConfigParams) {$CrScanAdditionalRunConfigParams})"
    & $GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args " TargetDirectory < $TargetDirectory " "OutputType < pre" $(If ($CrScanAdditionalRunConfigParams) {$CrScanAdditionalRunConfigParams})
    if ($LASTEXITCODE -ne 0) {
      Write-Host "Guardian configure for $tool failed with exit code $LASTEXITCODE."
      exit $LASTEXITCODE
    }
  }
  if ($tool -eq "policheck") {
    Write-Host "$GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args `" Target < $TargetDirectory `" $(If ($PoliCheckAdditionalRunConfigParams) {$PoliCheckAdditionalRunConfigParams})"
    & $GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args " Target < $TargetDirectory " $(If ($PoliCheckAdditionalRunConfigParams) {$PoliCheckAdditionalRunConfigParams})
    if ($LASTEXITCODE -ne 0) {
      Write-Host "Guardian configure for $tool failed with exit code $LASTEXITCODE."
      exit $LASTEXITCODE
    }
  }

  $configParam+=$gdnConfigFile
}

Write-Host "$GuardianCliLocation run --working-directory $WorkingDirectory --baseline mainbaseline --update-baseline $UpdateBaseline --logger-level $GuardianLoggerLevel $configParam"
& $GuardianCliLocation run --working-directory $WorkingDirectory --tool $tool --baseline mainbaseline --update-baseline $UpdateBaseline --logger-level $GuardianLoggerLevel $configParam
if ($LASTEXITCODE -ne 0) {
  Write-Host "Guardian run for $ToolsList using $configParam failed with exit code $LASTEXITCODE."
  exit $LASTEXITCODE
}
