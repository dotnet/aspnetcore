# Most of the functions in this file require the variables `MaestroApiEndPoint`, 
# `MaestroApiVersion` and `MaestroApiAccessToken` to be globally available.

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

# `tools.ps1` checks $ci to perform some actions. Since the post-build
# scripts don't necessarily execute in the same agent that run the
# build.ps1/sh script this variable isn't automatically set.
$ci = $true
$disableConfigureToolsetImport = $true
. $PSScriptRoot\..\tools.ps1

function Create-MaestroApiRequestHeaders([string]$ContentType = 'application/json') {
  Validate-MaestroVars

  $headers = New-Object 'System.Collections.Generic.Dictionary[[String],[String]]'
  $headers.Add('Accept', $ContentType)
  $headers.Add('Authorization',"Bearer $MaestroApiAccessToken")
  return $headers
}

function Get-MaestroChannel([int]$ChannelId) {
  Validate-MaestroVars

  $apiHeaders = Create-MaestroApiRequestHeaders
  $apiEndpoint = "$MaestroApiEndPoint/api/channels/${ChannelId}?api-version=$MaestroApiVersion"
  
  $result = try { Invoke-WebRequest -Method Get -Uri $apiEndpoint -Headers $apiHeaders | ConvertFrom-Json } catch { Write-Host "Error: $_" }
  return $result
}

function Get-MaestroBuild([int]$BuildId) {
  Validate-MaestroVars

  $apiHeaders = Create-MaestroApiRequestHeaders -AuthToken $MaestroApiAccessToken
  $apiEndpoint = "$MaestroApiEndPoint/api/builds/${BuildId}?api-version=$MaestroApiVersion"

  $result = try { return Invoke-WebRequest -Method Get -Uri $apiEndpoint -Headers $apiHeaders | ConvertFrom-Json } catch { Write-Host "Error: $_" }
  return $result
}

function Get-MaestroSubscriptions([string]$SourceRepository, [int]$ChannelId) {
  Validate-MaestroVars

  $SourceRepository = [System.Web.HttpUtility]::UrlEncode($SourceRepository) 
  $apiHeaders = Create-MaestroApiRequestHeaders -AuthToken $MaestroApiAccessToken
  $apiEndpoint = "$MaestroApiEndPoint/api/subscriptions?sourceRepository=$SourceRepository&channelId=$ChannelId&api-version=$MaestroApiVersion"

  $result = try { Invoke-WebRequest -Method Get -Uri $apiEndpoint -Headers $apiHeaders | ConvertFrom-Json } catch { Write-Host "Error: $_" }
  return $result
}

function Assign-BuildToChannel([int]$BuildId, [int]$ChannelId) {
  Validate-MaestroVars

  $apiHeaders = Create-MaestroApiRequestHeaders -AuthToken $MaestroApiAccessToken
  $apiEndpoint = "$MaestroApiEndPoint/api/channels/${ChannelId}/builds/${BuildId}?api-version=$MaestroApiVersion"
  Invoke-WebRequest -Method Post -Uri $apiEndpoint -Headers $apiHeaders | Out-Null
}

function Trigger-Subscription([string]$SubscriptionId) {
  Validate-MaestroVars

  $apiHeaders = Create-MaestroApiRequestHeaders -AuthToken $MaestroApiAccessToken
  $apiEndpoint = "$MaestroApiEndPoint/api/subscriptions/$SubscriptionId/trigger?api-version=$MaestroApiVersion"
  Invoke-WebRequest -Uri $apiEndpoint -Headers $apiHeaders -Method Post | Out-Null
}

function Validate-MaestroVars {
  try {
    Get-Variable MaestroApiEndPoint | Out-Null
    Get-Variable MaestroApiVersion | Out-Null
    Get-Variable MaestroApiAccessToken | Out-Null

    if (!($MaestroApiEndPoint -Match '^http[s]?://maestro-(int|prod).westus2.cloudapp.azure.com$')) {
      Write-PipelineTelemetryError -Category 'MaestroVars' -Message "MaestroApiEndPoint is not a valid Maestro URL. '$MaestroApiEndPoint'"
      ExitWithExitCode 1  
    }

    if (!($MaestroApiVersion -Match '^[0-9]{4}-[0-9]{2}-[0-9]{2}$')) {
      Write-PipelineTelemetryError -Category 'MaestroVars' -Message "MaestroApiVersion does not match a version string in the format yyyy-MM-DD. '$MaestroApiVersion'"
      ExitWithExitCode 1
    }
  }
  catch {
    Write-PipelineTelemetryError -Category 'MaestroVars' -Message 'Error: Variables `MaestroApiEndPoint`, `MaestroApiVersion` and `MaestroApiAccessToken` are required while using this script.'
    Write-Host $_
    ExitWithExitCode 1
  }
}
