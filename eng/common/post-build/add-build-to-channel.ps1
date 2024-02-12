param(
  [Parameter(Mandatory=$true)][int] $BuildId,
  [Parameter(Mandatory=$true)][int] $ChannelId,
  [Parameter(Mandatory=$true)][string] $MaestroApiAccessToken,
  [Parameter(Mandatory=$false)][string] $MaestroApiEndPoint = 'https://maestro.dot.net',
  [Parameter(Mandatory=$false)][string] $MaestroApiVersion = '2019-01-16'
)

try {
  . $PSScriptRoot\post-build-utils.ps1

  # Check that the channel we are going to promote the build to exist
  $channelInfo = Get-MaestroChannel -ChannelId $ChannelId

  if (!$channelInfo) {
    Write-PipelineTelemetryCategory -Category 'PromoteBuild' -Message "Channel with BAR ID $ChannelId was not found in BAR!"
    ExitWithExitCode 1
  }

  # Get info about which channel(s) the build has already been promoted to
  $buildInfo = Get-MaestroBuild -BuildId $BuildId
  
  if (!$buildInfo) {
    Write-PipelineTelemetryError -Category 'PromoteBuild' -Message "Build with BAR ID $BuildId was not found in BAR!"
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

  Write-Host 'done.'
} 
catch {
  Write-Host $_
  Write-PipelineTelemetryError -Category 'PromoteBuild' -Message "There was an error while trying to promote build '$BuildId' to channel '$ChannelId'"
  ExitWithExitCode 1
}
