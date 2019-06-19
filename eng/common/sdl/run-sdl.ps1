Param(
  [string] $GuardianCliLocation,
  [string] $WorkingDirectory,
  [string] $TargetDirectory,
  [string] $GdnFolder,
  [string[]] $ToolsList,
  [string] $UpdateBaseline,
  [string] $GuardianLoggerLevel="Standard"
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

foreach ($tool in $ToolsList) {
  $gdnConfigFile = Join-Path $gdnConfigPath "$tool-configure.gdnconfig"
  $config = $False
  Write-Host $tool
  # We have to manually configure tools that run on source to look at the source directory only
  if ($tool -eq "credscan") {
    Write-Host "$GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args `" TargetDirectory : $TargetDirectory `""
    & $GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args " TargetDirectory : $TargetDirectory "
    if ($LASTEXITCODE -ne 0) {
      Write-Host "Guardian configure for $tool failed with exit code $LASTEXITCODE."
      exit $LASTEXITCODE
    }
    $config = $True
  }
  if ($tool -eq "policheck") {
    Write-Host "$GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args `" Target : $TargetDirectory `""
    & $GuardianCliLocation configure --working-directory $WorkingDirectory --tool $tool --output-path $gdnConfigFile --logger-level $GuardianLoggerLevel --noninteractive --force --args " Target : $TargetDirectory "
    if ($LASTEXITCODE -ne 0) {
      Write-Host "Guardian configure for $tool failed with exit code $LASTEXITCODE."
      exit $LASTEXITCODE
    }
    $config = $True
  }

  Write-Host "$GuardianCliLocation run --working-directory $WorkingDirectory --tool $tool --baseline mainbaseline --update-baseline $UpdateBaseline --logger-level $GuardianLoggerLevel --config $gdnConfigFile $config"
  if ($config) {
    & $GuardianCliLocation run --working-directory $WorkingDirectory --tool $tool --baseline mainbaseline --update-baseline $UpdateBaseline --logger-level $GuardianLoggerLevel --config $gdnConfigFile
    if ($LASTEXITCODE -ne 0) {
      Write-Host "Guardian run for $tool using $gdnConfigFile failed with exit code $LASTEXITCODE."
      exit $LASTEXITCODE
    }
  } else {
    & $GuardianCliLocation run --working-directory $WorkingDirectory --tool $tool --baseline mainbaseline --update-baseline $UpdateBaseline --logger-level $GuardianLoggerLevel
    if ($LASTEXITCODE -ne 0) {
      Write-Host "Guardian run for $tool failed with exit code $LASTEXITCODE."
      exit $LASTEXITCODE
    }
  }
}

