param(
  [Parameter(Mandatory=$true)][int] $BuildId,
  [Parameter(Mandatory=$true)][int] $PublishingInfraVersion,
  [Parameter(Mandatory=$true)][string] $AzdoToken,
  [Parameter(Mandatory=$true)][string] $MaestroToken,
  [Parameter(Mandatory=$false)][string] $MaestroApiEndPoint = 'https://maestro.dot.net',
  [Parameter(Mandatory=$true)][string] $WaitPublishingFinish,
  [Parameter(Mandatory=$false)][string] $ArtifactsPublishingAdditionalParameters,
  [Parameter(Mandatory=$false)][string] $SymbolPublishingAdditionalParameters
)

try {
  . $PSScriptRoot\post-build-utils.ps1

  $darc = Get-Darc 

  $optionalParams = [System.Collections.ArrayList]::new()

  if ("" -ne $ArtifactsPublishingAdditionalParameters) {
    $optionalParams.Add("--artifact-publishing-parameters") | Out-Null
    $optionalParams.Add($ArtifactsPublishingAdditionalParameters) | Out-Null
  }

  if ("" -ne $SymbolPublishingAdditionalParameters) {
    $optionalParams.Add("--symbol-publishing-parameters") | Out-Null
    $optionalParams.Add($SymbolPublishingAdditionalParameters) | Out-Null
  }

  if ("false" -eq $WaitPublishingFinish) {
    $optionalParams.Add("--no-wait") | Out-Null
  }

  & $darc add-build-to-channel `
  --id $buildId `
  --publishing-infra-version $PublishingInfraVersion `
  --default-channels `
  --source-branch main `
  --azdev-pat $AzdoToken `
  --bar-uri $MaestroApiEndPoint `
  --password $MaestroToken `
	@optionalParams

  if ($LastExitCode -ne 0) {
    Write-Host "Problems using Darc to promote build ${buildId} to default channels. Stopping execution..."
    exit 1
  }

  Write-Host 'done.'
} 
catch {
  Write-Host $_
  Write-PipelineTelemetryError -Category 'PromoteBuild' -Message "There was an error while trying to publish build '$BuildId' to default channels."
  ExitWithExitCode 1
}
