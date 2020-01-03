param(
  [string] $token
)

. $PSScriptRoot\pipeline-logging-functions.ps1

Write-PipelineSetVariable -Name 'VSS_NUGET_ACCESSTOKEN' -Value $token
Write-PipelineSetVariable -Name 'VSS_NUGET_URI_PREFIXES' -Value 'https://dnceng.pkgs.visualstudio.com/;https://pkgs.dev.azure.com/dnceng/;https://devdiv.pkgs.visualstudio.com/;https://pkgs.dev.azure.com/devdiv/'
