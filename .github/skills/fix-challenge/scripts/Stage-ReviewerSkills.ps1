[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string] $Destination
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'ReviewerEvalTools.psm1') -Force

$stagedPath = Copy-SanitizedSkills -Destination $Destination
Write-Host "Staged sanitized skills in $stagedPath"
