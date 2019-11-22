[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $verbosity = "minimal",
  [string] $architecture = "",
  [string] $version = "Latest",
  [string] $runtime = "dotnet"
)

. $PSScriptRoot\tools.ps1

$dotnetRoot = Join-Path $RepoRoot ".dotnet"

$installdir = $dotnetRoot
try {
    if ($architecture -and $architecture.Trim() -eq "x86") {
        $installdir = Join-Path $installdir "x86"
    }
   InstallDotNet $installdir $version $architecture $runtime $true
} 
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}

ExitWithExitCode 0
