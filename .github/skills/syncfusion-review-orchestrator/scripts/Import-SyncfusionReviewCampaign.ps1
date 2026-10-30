#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ControlJsonPath,
    [string]$MarchingOrdersPath,
    [string]$SqliteExportPath,
    [Parameter(Mandatory)]
    [string]$OutputPath,
    [Parameter(Mandatory)]
    [string]$RootSessionId
)

$ErrorActionPreference = "Stop"

Import-Module -Scope Local -Force -DisableNameChecking (Join-Path $PSScriptRoot "SyncfusionReviewOrchestrator.psm1")

$sourceHashesBefore = [ordered]@{
    controlJson = Get-SfroFileSha256 $ControlJsonPath
}
if ($MarchingOrdersPath) {
    $sourceHashesBefore.marchingOrders = Get-SfroFileSha256 $MarchingOrdersPath
}
if ($SqliteExportPath) {
    $sourceHashesBefore.sqliteExport = Get-SfroFileSha256 $SqliteExportPath
}

$control = ConvertFrom-SfroJson (Get-Content -LiteralPath $ControlJsonPath -Raw)
$continuation = $control.syncfusionContinuation
if ($null -eq $continuation) {
    throw "The control JSON has no syncfusionContinuation object."
}

$queue = @($continuation.queueOrder)
if ($queue.Count -ne 33 -or @($queue | Sort-Object -Unique).Count -ne 33) {
    throw "The imported queue must contain exactly 33 unique PR numbers."
}
$completedCases = if ($continuation.completedCases) { $continuation.completedCases } else { [ordered]@{} }
foreach ($completedPr in @($completedCases.Keys)) {
    if ([int]$completedPr -notin $queue) {
        throw "Completed PR #$completedPr is not in the imported queue."
    }
}
$activeCase = if ($continuation.activeCase) { $continuation.activeCase } else { $null }
if ($null -ne $activeCase) {
    $activePrKey = [string][int]$activeCase.pr
    if ($activePrKey -in @($completedCases.Keys)) {
        throw "The imported active PR cannot also be completed."
    }
}

if ($MarchingOrdersPath) {
    $marchingOrders = Get-Content -LiteralPath $MarchingOrdersPath -Raw
    $mentionedPrs = @([regex]::Matches($marchingOrders, "#(?<pr>\d+)") | ForEach-Object { [int]$_.Groups["pr"].Value } | Sort-Object -Unique)
    foreach ($pr in $queue) {
        if ([int]$pr -notin $mentionedPrs) {
            throw "Marching orders do not mention queued PR #$pr."
        }
    }
}

$sqliteMetadata = $null
if ($SqliteExportPath) {
    $sqliteExport = ConvertFrom-SfroJson (Get-Content -LiteralPath $SqliteExportPath -Raw)
    if ($sqliteExport.schemaVersion -ne 1 -or
        $sqliteExport.sourceHashes -isnot [Collections.IDictionary] -or
        $sqliteExport.tables -isnot [Collections.IDictionary]) {
        throw "The SQLite export is malformed."
    }
    $sqliteMetadata = [ordered]@{
        sourceHashes = $sqliteExport.sourceHashes
        tables = $sqliteExport.tables
        exportSha256 = $sourceHashesBefore.sqliteExport
    }
}

$reviewNow = [Collections.Generic.HashSet[int]]::new()
foreach ($pr in @($queue | Select-Object -First 8)) {
    [void]$reviewNow.Add([int]$pr)
}
$needsRescue = [Collections.Generic.HashSet[int]]::new()
foreach ($pr in @($queue | Select-Object -Skip 8 -First 7)) {
    [void]$needsRescue.Add([int]$pr)
}

$entries = for ($index = 0; $index -lt $queue.Count; $index++) {
    $pr = [int]$queue[$index]
    $lane = if ($reviewNow.Contains($pr)) {
        "ReviewNow"
    }
    elseif ($needsRescue.Contains($pr)) {
        "NeedsRescue"
    }
    else {
        "WaitingOnCI"
    }

    [ordered]@{
        ordinal = $index + 1
        pr = $pr
        lane = $lane
    }
}

$sourceHashesAfter = [ordered]@{
    controlJson = Get-SfroFileSha256 $ControlJsonPath
}
if ($MarchingOrdersPath) {
    $sourceHashesAfter.marchingOrders = Get-SfroFileSha256 $MarchingOrdersPath
}
if ($SqliteExportPath) {
    $sourceHashesAfter.sqliteExport = Get-SfroFileSha256 $SqliteExportPath
}

if ((ConvertTo-SfroCanonicalJson $sourceHashesBefore) -cne (ConvertTo-SfroCanonicalJson $sourceHashesAfter)) {
    throw "A migration source changed while it was being read."
}

$configuration = [ordered]@{
    schemaVersion = 1
    campaignId = "syncfusion-review-2026-10"
    repository = "dotnet/aspnetcore"
    rootSessionId = $RootSessionId
    queue = $entries
    import = [ordered]@{
        sourceHashes = $sourceHashesBefore
        queueHash = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $entries)
        activeCase = $activeCase
        completedFacts = $completedCases
        sqlite = $sqliteMetadata
        requiresExplicitAcceptance = $true
    }
}

$json = $configuration | ConvertTo-Json -Depth 100
$resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
[IO.File]::WriteAllText(
    $resolvedOutputPath,
    $json + "`n",
    [Text.UTF8Encoding]::new($false))

[ordered]@{
    status = "candidate-written"
    outputPath = $resolvedOutputPath
    queueCount = $entries.Count
    queueHash = $configuration.import.queueHash
    sourceHashes = $sourceHashesBefore
    sourceFilesUnchanged = $true
    requiresExplicitAcceptance = $true
} | ConvertTo-Json -Depth 100
