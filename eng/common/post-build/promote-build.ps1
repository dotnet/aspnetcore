param(
  [Parameter(Mandatory=$true)][int] $BuildId,
  [Parameter(Mandatory=$true)][int] $ChannelId,
  [Parameter(Mandatory=$true)][string] $BarToken,
  [string] $MaestroEndpoint = "https://maestro-prod.westus2.cloudapp.azure.com",
  [string] $ApiVersion = "2019-01-16"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

. $PSScriptRoot\..\tools.ps1

function Get-Headers([string]$accept, [string]$barToken) {
  $headers = New-Object 'System.Collections.Generic.Dictionary[[String],[String]]'
  $headers.Add('Accept',$accept)
  $headers.Add('Authorization',"Bearer $barToken")
  return $headers
}

try {
  $maestroHeaders = Get-Headers 'application/json' $BarToken

  # Get info about which channels the build has already been promoted to
  $getBuildApiEndpoint = "$MaestroEndpoint/api/builds/${BuildId}?api-version=$ApiVersion"
  $buildInfo = Invoke-WebRequest -Method Get -Uri $getBuildApiEndpoint -Headers $maestroHeaders | ConvertFrom-Json

  if (!$buildInfo) {
    Write-Host "Build with BAR ID $BuildId was not found in BAR!"
    ExitWithExitCode 1
  }

  # Find whether the build is already assigned to the channel or not
  if ($buildInfo.channels) {
    foreach ($channel in $buildInfo.channels) {
      if ($channel.Id -eq $ChannelId) {
        Write-Host "The build with BAR ID $BuildId is already on channel $ChannelId!"
        ExitWithExitCode 0
      }
    }
  }

  Write-Host "Build not present in channel $ChannelId. Promoting build ... "

  $promoteBuildApiEndpoint = "$maestroEndpoint/api/channels/${ChannelId}/builds/${BuildId}?api-version=$ApiVersion"
  Invoke-WebRequest -Method Post -Uri $promoteBuildApiEndpoint -Headers $maestroHeaders
  Write-Host "done."
} 
catch {
  Write-Host "There was an error while trying to promote build '$BuildId' to channel '$ChannelId'"
  Write-Host $_
  Write-Host $_.ScriptStackTrace
}
