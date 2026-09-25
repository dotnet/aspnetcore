<#
.SYNOPSIS Persist a validated triage JSON to artifacts/ai/triage/.
.EXAMPLE  pwsh scripts/persist-triage.ps1 /tmp/aspnetcore/triage/12345.json
.NOTES    Validates then copies to artifacts/ai/triage/{number}.json. No git push.
#>
#requires -Version 7.5
param([Parameter(Mandatory, Position = 0)] [string]$Path)
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Path)) { Write-Host "❌ Source file not found: $Path"; exit 2 }

# Validate before persisting
$ValidateScript = Join-Path $PSScriptRoot 'validate-triage.ps1'
Write-Host "Validating $Path …"
& $ValidateScript $Path
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Validation failed (exit $LASTEXITCODE). File not persisted."
    exit 1
}

# Read the JSON to get the issue number
$triage = Get-Content $Path -Raw | ConvertFrom-Json -Depth 50
$number = $triage.meta.number
if (-not $number) { Write-Host "❌ Cannot read meta.number from $Path"; exit 2 }

# Determine repo root (where this script lives under .github/skills/issue-triage/scripts/)
$repoRoot = Split-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent) -Parent

$outputDir = Join-Path $repoRoot 'artifacts' 'ai' 'triage'
$outputPath = Join-Path $outputDir "$number.json"

# Create directory if needed
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Copy-Item -Path $Path -Destination $outputPath -Force
Write-Host "✅ Triage persisted: artifacts/ai/triage/$number.json"
Write-Host "   (No git push — file is ready for human review)"
