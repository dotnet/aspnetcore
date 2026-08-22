[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string[]] $Path
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'ReviewerEvalTools.psm1') -Force

$Path = @($Path | ForEach-Object { $_ -split ',' } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($Path.Count -eq 0)
{
    throw 'at least one eval path is required'
}

$result = Test-EvalSuites -Paths $Path
$result.Warnings | ForEach-Object { Write-Warning $_ }
if ($result.Errors.Count -gt 0)
{
    $result.Errors | ForEach-Object { Write-Error $_ }
    exit 1
}

$result.Summary | ConvertTo-Json -Depth 10
