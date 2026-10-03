<#
.SYNOPSIS Validate an ai-triage JSON file against triage-schema.json.
.EXAMPLE  pwsh scripts/validate-triage.ps1 /tmp/aspnetcore/triage/12345.json
.NOTES    Exits 0=valid, 1=fixable (retry), 2=fatal. Requires PowerShell 7.5+.
#>
#requires -Version 7.5
param([Parameter(Mandatory, Position = 0)] [string]$Path)
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Path)) { Write-Host "❌ File not found: $Path"; exit 2 }
$schemaPath = Join-Path $PSScriptRoot '../references/triage-schema.json'
if (-not (Test-Path $schemaPath)) { Write-Host "❌ Schema not found: $schemaPath"; exit 2 }

$json = Get-Content $Path -Raw
$triage = $json | ConvertFrom-Json -Depth 50
$errors = @()

# --- Schema validation ---
if (-not ($json | Test-Json -SchemaFile $schemaPath -ErrorVariable schemaErrors -ErrorAction SilentlyContinue)) {
    $errors += $schemaErrors | ForEach-Object {
        $_.Exception.Message -replace '^The JSON is not valid with the schema: ', ''
    } | Sort-Object -Unique
}

# --- Repo check ---
if ($triage.meta.repo -and $triage.meta.repo -ne 'dotnet/aspnetcore') {
    $errors += "meta.repo must be 'dotnet/aspnetcore', got '$($triage.meta.repo)'"
}

# --- codeInvestigation is mandatory for bugs ---
if ($triage.classification.type.value -eq 'bug' -and
    (-not $triage.analysis.codeInvestigation -or $triage.analysis.codeInvestigation.Count -eq 0)) {
    $errors += "Bug issue has no codeInvestigation entries (mandatory for type 'bug')"
}

# --- bugSignals should exist for bugs (warning only) ---
if ($triage.classification.type.value -eq 'bug' -and -not $triage.evidence.bugSignals) {
    Write-Host "⚠️  Warning: Bug issue has no evidence.bugSignals (recommended for bugs)"
}

# --- Absolute path check in codeInvestigation ---
$absPathPattern = '(/Users/|/home/|C:\\Users\\)'
if ($triage.analysis.codeInvestigation) {
    foreach ($ci in $triage.analysis.codeInvestigation) {
        if ($ci.file -match $absPathPattern) {
            $errors += "codeInvestigation file '$($ci.file)' contains absolute path — use relative path from repo root (e.g., 'src/Mvc/...')"
        }
    }
}

# --- actions must be an array ---
if ($triage.output.actions -and $triage.output.actions -isnot [array]) {
    $errors += "output.actions must be an array"
}

if ($errors.Count -eq 0) {
    $number = $triage.meta.number ?? '?'
    $type = $triage.classification.type.value ?? '?'
    $area = $triage.classification.area.value ?? '?'
    Write-Host "✅ $(Split-Path $Path -Leaf) is valid (issue #$number, type: $type, area: $area)"
    exit 0
}
Write-Host "❌ $($errors.Count) validation error(s) in $(Split-Path $Path -Leaf):`n"
$errors | ForEach-Object { Write-Host "  $_" }
exit 1
