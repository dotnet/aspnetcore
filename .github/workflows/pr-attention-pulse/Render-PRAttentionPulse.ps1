#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$InputPath,
    [Parameter(Mandatory)][string]$OutputPath,
    [Parameter(Mandatory)][string]$SnapshotContextPath,
    [string]$Repository,
    [string]$ServerUrl,
    [string]$RunId,
    [string]$RunAttempt,
    [string]$GeneratedAt = [datetime]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", [Globalization.CultureInfo]::InvariantCulture)
)

$ErrorActionPreference = "Stop"
Import-Module -Scope Local -Force (Join-Path $PSScriptRoot "PRAttentionPulseContract.psm1")

$snapshotInput = Read-PulseSnapshotInput -InputPath $InputPath
$snapshot = New-PulseSnapshotContext -Repository $Repository -ServerUrl $ServerUrl -RunId $RunId `
    -RunAttempt $RunAttempt -GeneratedAt $GeneratedAt -InputSha256 $snapshotInput.Sha256
$body = ConvertTo-PRAttentionPulseBody -Pulse $snapshotInput.Pulse -SnapshotContext $snapshot
[IO.File]::WriteAllText($OutputPath, $body, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText($SnapshotContextPath, ($snapshot | ConvertTo-Json -Compress), [Text.UTF8Encoding]::new($false))
