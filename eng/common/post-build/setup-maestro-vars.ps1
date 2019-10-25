param(
  [Parameter(Mandatory=$true)][string] $ReleaseConfigsPath            # Full path to ReleaseConfigs.txt asset
)

. $PSScriptRoot\post-build-utils.ps1

try {
  $Content = Get-Content $ReleaseConfigsPath

  $BarId = $Content | Select -Index 0

  $Channels = ""            
  $Content | Select -Index 1 | ForEach-Object { $Channels += "$_ ," }

  $IsStableBuild = $Content | Select -Index 2

  Write-PipelineSetVariable -Name 'BARBuildId' -Value $BarId
  Write-PipelineSetVariable -Name 'InitialChannels' -Value "$Channels"
  Write-PipelineSetVariable -Name 'IsStableBuild' -Value $IsStableBuild
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}