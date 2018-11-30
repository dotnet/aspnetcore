[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $verbosity = "minimal",
  [bool] $warnaserror = $true,
  [bool] $nodereuse = $true,
  [switch] $ci,
  [switch] $prepareMachine,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$extraArgs
)

. $PSScriptRoot\tools.ps1

try {
  InitializeTools
  MSBuild @extraArgs
  ExitWithExitCode $lastExitCode
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
