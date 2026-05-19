param(
  [Parameter(Mandatory = $true)]
  [ValidateNotNullOrEmpty()]
  [string]$ProcDumpOutputPath
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

function Warn {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Message
  )

  Write-Warning "$Message"
  if ((Test-Path env:TF_BUILD) -and $env:TF_BUILD) {
    Write-Host "##vso[task.logissue type=warning]$Message"
  }
}

$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$procDumpOutputPath = Join-Path $repoRoot $ProcDumpOutputPath

if ((Test-Path env:SYSTEM_DEFAULTWORKINGDIRECTORY) -and $env:SYSTEM_DEFAULTWORKINGDIRECTORY) {
  if (Test-Path env:SYSTEM_PHASENAME) { $jobName = "$env:SYSTEM_PHASENAME" } else { $jobName = "$env:AGENT_OS" }
  $artifactName = "${jobName}_Dumps"
  $wd = $env:SYSTEM_DEFAULTWORKINGDIRECTORY
} else {
  $artifactName = "Artifacts_Dumps"
  $wd = "$PWD"
}

[string[]]$files = Get-ChildItem "$wd\dotnet-*.core"
if (Test-Path "$procDumpOutputPath") {
  $files += Get-ChildItem "${procDumpOutputPath}*.dmp"
}

if ($null -eq $files -or 0 -eq $files.Count) {
  Warn "No core files found."
} else {
  foreach ($file in $files) {
    Write-Host "##vso[artifact.upload containerfolder=$artifactName;artifactname=$artifactName]$file"
  }
}
