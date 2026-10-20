[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $verbosity = 'minimal',
  [bool] $warnAsError = $true,
  [bool] $nodeReuse = $true,
  [bool][Alias('mt')]$msbuildMultiThreaded = $true,
  [switch] $ci,
  [switch] $prepareMachine,
  [switch] $excludePrereleaseVS,
  [string] $msbuildEngine = $null,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$extraArgs
)

. $PSScriptRoot\tools.ps1

try {
  # Node reuse isn't used on CI unless it was explicitly requested via -nodeReuse.
  if ($ci -and -not $PSBoundParameters.ContainsKey('nodeReuse')) {
    $nodeReuse = $false
  }

  # MSBuild's multi-threaded mode isn't run on CI unless it was explicitly requested via -msbuildMultiThreaded.
  if ($ci -and -not $PSBoundParameters.ContainsKey('msbuildMultiThreaded')) {
    $msbuildMultiThreaded = $false
  }

  MSBuild @extraArgs
} 
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'Build' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0