#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$InputPath,
    [Parameter(Mandatory)][string]$OutputPath
)

$ErrorActionPreference = "Stop"
Import-Module -Scope Local -Force (Join-Path $PSScriptRoot "PRAttentionPulseContract.psm1")

$pulse = Get-Content -LiteralPath $InputPath -Raw | ConvertFrom-Json -Depth 100
$body = ConvertTo-PRAttentionPulseBody -Pulse $pulse
[IO.File]::WriteAllText($OutputPath, $body, [Text.UTF8Encoding]::new($false))
