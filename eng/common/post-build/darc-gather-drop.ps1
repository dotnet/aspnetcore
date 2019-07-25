param(
  [Parameter(Mandatory=$true)][string] $BarBuildId,             # ID of the build which assets should be downloaded
  [Parameter(Mandatory=$true)][string] $MaestroAccessToken,     # Token used to access Maestro API
  [Parameter(Mandatory=$true)][string] $DropLocation            # Where the assets should be downloaded to
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

. $PSScriptRoot\..\tools.ps1

try {
  Write-Host "Installing DARC ..."

  . $PSScriptRoot\..\darc-init.ps1
  $exitCode = $LASTEXITCODE

  if ($exitCode -ne 0) {
    Write-PipelineTaskError "Something failed while running 'darc-init.ps1'. Check for errors above. Exiting now..."
    ExitWithExitCode $exitCode
  }

  darc gather-drop --non-shipping `
    --continue-on-error `
    --id $BarBuildId `
    --output-dir $DropLocation `
    --bar-uri https://maestro-prod.westus2.cloudapp.azure.com/ `
    --password $MaestroAccessToken `
    --latest-location
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
