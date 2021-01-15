[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $verbosity = 'minimal',
  [string] $architecture = '',
  [string] $version = 'Latest',
  [string] $runtime = 'dotnet',
  [string] $RuntimeSourceFeed = '',
  [string] $RuntimeSourceFeedKey = ''
)

. $PSScriptRoot\tools.ps1

$dotnetRoot = Join-Path $RepoRoot '.dotnet'

$installdir = $dotnetRoot
try {
    if ($architecture -and $architecture.Trim() -eq 'x86') {
        $installdir = Join-Path $installdir 'x86'
    }
    InstallDotNet $installdir $version $architecture $runtime $true -RuntimeSourceFeed $RuntimeSourceFeed -RuntimeSourceFeedKey $RuntimeSourceFeedKey
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'InitializeToolset' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0
