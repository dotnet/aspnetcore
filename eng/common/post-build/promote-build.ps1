param(
  [Parameter(Mandatory=$true)][int] $BuildId,
  [Parameter(Mandatory=$true)][int] $ChannelId,
  [Parameter(Mandatory=$true)][string] $MaestroApiAccessToken,
  [Parameter(Mandatory=$false)][string] $MaestroApiEndPoint = "https://maestro-prod.westus2.cloudapp.azure.com",
  [Parameter(Mandatory=$false)][string] $MaestroApiVersion = "2019-01-16"
)

. $PSScriptRoot\post-build-utils.ps1

try {
  # Check that the channel we are going to promote the build to exist
  $channelInfo = Get-MaestroChannel -ChannelId $ChannelId

  if (!$channelInfo) {
    Write-Host "Channel with BAR ID $ChannelId was not found in BAR!"
    ExitWithExitCode 1
  }

  # Get info about which channels the build has already been promoted to
  $buildInfo = Get-MaestroBuild -BuildId $BuildId
  
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

  Write-Host "Promoting build '$BuildId' to channel '$ChannelId'."

  Assign-BuildToChannel -BuildId $BuildId -ChannelId $ChannelId

  Write-Host "done."
} 
catch {
  Write-Host "There was an error while trying to promote build '$BuildId' to channel '$ChannelId'"
  Write-Host $_
  Write-Host $_.ScriptStackTrace
}
