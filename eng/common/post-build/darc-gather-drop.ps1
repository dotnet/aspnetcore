param(
  [Parameter(Mandatory=$true)][int] $BarBuildId,                # ID of the build which assets should be downloaded
  [Parameter(Mandatory=$true)][string] $DropLocation,           # Where the assets should be downloaded to
  [Parameter(Mandatory=$true)][string] $MaestroApiAccessToken,  # Token used to access Maestro API
  [Parameter(Mandatory=$false)][string] $MaestroApiEndPoint = "https://maestro-prod.westus2.cloudapp.azure.com",     # Maestro API URL
  [Parameter(Mandatory=$false)][string] $MaestroApiVersion = "2019-01-16"                                            # Version of Maestro API to use
)

. $PSScriptRoot\post-build-utils.ps1

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
    --bar-uri $MaestroApiEndpoint `
    --password $MaestroApiAccessToken `
    --latest-location
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
