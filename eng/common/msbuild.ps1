[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $verbosity = 'minimal',
  [bool] $warnAsError = $true,
  [bool] $nodeReuse = $true,
  [switch] $ci,
  [switch] $prepareMachine,
  [switch] $excludePrereleaseVS,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$extraArgs
)

. $PSScriptRoot\tools.ps1

try {
  if ($ci) {
    $nodeReuse = $false
  }

  MSBuild @extraArgs
} 
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'Build' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0