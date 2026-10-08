param(
    [string]$LogOutputPath = "artifacts/log/node-crash/"
)

$ErrorActionPreference = 'Continue'
Set-StrictMode -Version 2

$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$logOutputPath = Join-Path $repoRoot $LogOutputPath
$startFile = Join-Path $logOutputPath "start-utc.txt"
$summaryFile = Join-Path $logOutputPath "summary.txt"
$eventLogFile = Join-Path $logOutputPath "windows-application-events.txt"
$fallbackNpmLogPath = Join-Path $env:LOCALAPPDATA "npm-cache\_logs"
$collectedNpmLogPath = Join-Path $logOutputPath "npm-cache"

if ($env:OS -eq "Windows_NT")
{
    Remove-Item "HKCU:\Software\Microsoft\Windows\Windows Error Reporting\LocalDumps\node.exe" -Recurse -Force -ErrorAction SilentlyContinue
}

if ($env:AGENT_JOBSTATUS -eq "Succeeded")
{
    Remove-Item $logOutputPath -Recurse -Force -ErrorAction SilentlyContinue
    exit 0
}

New-Item -ItemType Directory -Force $logOutputPath | Out-Null

@(
    "Collected: $((Get-Date).ToUniversalTime().ToString('O'))"
    "Job status: $env:AGENT_JOBSTATUS"
    "Node: $(node --version 2>&1)"
    "npm: $(npm --version 2>&1)"
) | Set-Content $summaryFile

if (($env:OS -eq "Windows_NT") -and (Test-Path $startFile))
{
    $startUtc = [DateTime]::Parse((Get-Content $startFile -Raw)).ToUniversalTime()
    Get-WinEvent -FilterHashtable @{ LogName = "Application"; StartTime = $startUtc } -ErrorAction SilentlyContinue |
        Where-Object {
            $_.ProviderName -in @("Application Error", "Windows Error Reporting") -or
            $_.Message -match "(?i)\bnode\.exe\b|\brollup\b|\bnpm\b"
        } |
        Select-Object TimeCreated, ProviderName, Id, LevelDisplayName, Message |
        Format-List |
        Out-File $eventLogFile -Width 4096
}

if (Test-Path $fallbackNpmLogPath)
{
    New-Item -ItemType Directory -Force $collectedNpmLogPath | Out-Null
    Copy-Item (Join-Path $fallbackNpmLogPath "*") $collectedNpmLogPath -Recurse -Force
}

$jobName = if ($env:SYSTEM_PHASENAME) { $env:SYSTEM_PHASENAME } else { $env:AGENT_OS }
$jobAttempt = if ($env:SYSTEM_JOBATTEMPT) { $env:SYSTEM_JOBATTEMPT } else { "1" }
$artifactName = "${jobName}_NodeDiagnostics_Attempt_${jobAttempt}"

Get-ChildItem $logOutputPath -File -Recurse | ForEach-Object {
    Write-Host "##vso[artifact.upload containerfolder=$artifactName;artifactname=$artifactName]$($_.FullName)"
}
