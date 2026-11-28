#!/usr/bin/env pwsh
#Requires -Version 7.0
<#
.SYNOPSIS
    Persists a validated repro JSON file to artifacts/ai/repro/.

.DESCRIPTION
    Validates the JSON file (using validate-repro.ps1), then copies it to
    artifacts/ai/repro/{number}.json in the repository root.
    No git operations are performed.

.PARAMETER Path
    Path to the repro JSON file to persist.

.PARAMETER Force
    Overwrite an existing file without prompting.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory, Position = 0)]
    [string] $Path,

    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Locate repository root (4 levels up: scripts → issue-repro → skills → .github → repo) ──
$ScriptDir  = $PSScriptRoot
$RepoRoot   = (Resolve-Path (Join-Path $ScriptDir '..\..\..\..')).Path

$ArtifactDir = Join-Path $RepoRoot 'artifacts' 'ai' 'repro'

# ── Validate first ────────────────────────────────────────────────────────────
$ValidateScript = Join-Path $ScriptDir 'validate-repro.ps1'
Write-Host "Validating $Path …"
& $ValidateScript $Path
if ($LASTEXITCODE -ne 0) {
    Write-Error "Validation failed (exit $LASTEXITCODE). File not persisted."
    exit 1
}

# ── Load JSON to get issue number ─────────────────────────────────────────────
$json = Get-Content $Path -Raw | ConvertFrom-Json -Depth 20
$Number = $json.meta.number
if (-not $Number) {
    Write-Error "Cannot determine issue number from meta.number"
    exit 1
}

# ── Ensure artifact directory exists ─────────────────────────────────────────
if (-not (Test-Path $ArtifactDir)) {
    New-Item -ItemType Directory -Path $ArtifactDir -Force | Out-Null
    Write-Host "Created $ArtifactDir"
}

# ── Copy file ─────────────────────────────────────────────────────────────────
$Destination = Join-Path $ArtifactDir "$Number.json"

if ((Test-Path $Destination) -and -not $Force) {
    Write-Warning "File already exists — overwriting: $Destination (use -Force to suppress this warning)"
}

Copy-Item -Path $Path -Destination $Destination -Force
Write-Host "Persisted repro: $Destination" -ForegroundColor Green
Write-Host ""
Write-Host "Artifact path (relative): artifacts/ai/repro/$Number.json"
Write-Host ""
Write-Host "NOTE: No git push performed. The artifact is local only."
