param(
    [string]$DumpOutputPath = "artifacts/dumps/",
    [string]$LogOutputPath = "artifacts/log/node-crash/"
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$dumpOutputPath = Join-Path $repoRoot $DumpOutputPath
$logOutputPath = Join-Path $repoRoot $LogOutputPath
$reportOutputPath = Join-Path $logOutputPath "reports"
$npmLogOutputPath = Join-Path $logOutputPath "npm"

New-Item -ItemType Directory -Force $dumpOutputPath, $reportOutputPath, $npmLogOutputPath | Out-Null
(Get-Date).ToUniversalTime().ToString("O") | Set-Content (Join-Path $logOutputPath "start-utc.txt")

if ($env:OS -eq "Windows_NT")
{
    $localDumpsKey = "HKCU:\Software\Microsoft\Windows\Windows Error Reporting\LocalDumps\node.exe"
    New-Item -Path $localDumpsKey -Force | Out-Null
    New-ItemProperty -Path $localDumpsKey -Name DumpFolder -Value $dumpOutputPath -PropertyType ExpandString -Force | Out-Null
    New-ItemProperty -Path $localDumpsKey -Name DumpCount -Value 10 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $localDumpsKey -Name DumpType -Value 2 -PropertyType DWord -Force | Out-Null
}
else
{
    Write-Warning "Windows Error Reporting dump collection is only configured on Windows."
}

$nodeOptions = @(
    $env:NODE_OPTIONS
    "--report-on-fatalerror"
    "--report-uncaught-exception"
    "--report-directory=$reportOutputPath"
) | Where-Object { $_ }

Write-Host "##vso[task.setvariable variable=NODE_OPTIONS]$($nodeOptions -join ' ')"
Write-Host "##vso[task.setvariable variable=NPM_CONFIG_LOGS_DIR]$npmLogOutputPath"
Write-Host "Node crash dumps will be written to '$dumpOutputPath'."
Write-Host "Node reports and npm logs will be written to '$logOutputPath'."
