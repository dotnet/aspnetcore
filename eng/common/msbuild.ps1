[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $verbosity = 'minimal',
  [bool] $warnAsError = $true,
  [bool] $nodeReuse = $true,
  [switch] $ci,
  [switch] $prepareMachine,
  [switch] $excludePrereleaseVS,
  [string] $msbuildEngine = $null,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$extraArgs
)

. $PSScriptRoot\tools.ps1

try {
  if ($ci) {
    # Disable node reuse on CI unless explicitly opted in via MSBUILD_NODEREUSE_ENABLED.
    # Internal testing only; this env var will be replaced with a switch (https://github.com/dotnet/arcade/issues/17013) and must not be depended on.
    if ($env:MSBUILD_NODEREUSE_ENABLED -ne "1") {
      $nodeReuse = $false
    }
  }

  MSBuild @extraArgs
} 
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'Build' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0