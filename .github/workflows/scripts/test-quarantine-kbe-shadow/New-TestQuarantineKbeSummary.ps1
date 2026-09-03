#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Renders a short, human-readable Markdown summary of one test-quarantine-kbe-shadow run.

.DESCRIPTION
    Reads the collector's dossier (and, when present, the evaluator's receipt) and writes a
    Markdown summary intended for the workflow's step summary and as a small uploaded artifact.
    Purely a read-only presentation layer: it does not re-derive or override any collector or
    evaluator decision.

.PARAMETER DossierFile
    Path to the dossier JSON produced by Collect-TestQuarantineKbeEvidence.ps1.

.PARAMETER ReceiptFile
    Optional path to the receipt JSON produced by Evaluate-TestQuarantineKbeCandidate.ps1. Only
    present when the dossier's outcome was 'candidate'.

.PARAMETER OutputFile
    Path to write the rendered Markdown summary to.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DossierFile,

    [string]$ReceiptFile,

    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-PropertyOrDefault
{
    param($Object, [Parameter(Mandatory = $true)][string]$Name, $Default = $null)

    if ($null -eq $Object)
    {
        return $Default
    }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property)
    {
        return $Default
    }
    return $property.Value
}

function ConvertTo-DisplayTimestamp
{
    # ConvertFrom-Json auto-parses ISO-8601 strings into [datetime]; render explicitly as
    # round-trip UTC text instead of relying on the current culture's default ToString().
    param($Value)

    if ($null -eq $Value)
    {
        return ""
    }
    if ($Value -is [datetime])
    {
        return $Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
    }
    return [string]$Value
}

$dossier = Get-Content -LiteralPath $DossierFile -Raw | ConvertFrom-Json -Depth 32

$lines = [System.Collections.Generic.List[string]]::new()
$null = $lines.Add("# Test quarantine KBE shadow report")
$null = $lines.Add("")
$null = $lines.Add("This is a **read-only, non-authoritative shadow evaluation**. It never labels, comments on, or otherwise mutates any issue, pull request, branch, or repository file.")
$null = $lines.Add("")
$null = $lines.Add("| Field | Value |")
$null = $lines.Add("|---|---|")
$null = $lines.Add("| Issue | [#$($dossier.issue.number)]($($dossier.issue.url)) |")
$null = $lines.Add("| Collector outcome | ``$($dossier.outcome)`` |")
$null = $lines.Add("| Fixture mode | $($dossier.collector.fixture_mode) |")
$null = $lines.Add("| Manual signature supplied | $($dossier.collector.manual_signature_provided) |")
$null = $lines.Add("| Generated (UTC) | $(ConvertTo-DisplayTimestamp -Value $dossier.collector.generated_utc) |")
$null = $lines.Add("")

if ($dossier.outcome -eq "incomplete")
{
    $null = $lines.Add("## Incomplete")
    $null = $lines.Add("")
    $null = $lines.Add($dossier.incomplete.message)
    $null = $lines.Add("")
    $null = $lines.Add("| Reason code | ")
    $null = $lines.Add("|---|")
    foreach ($code in @($dossier.incomplete.reason_codes))
    {
        $null = $lines.Add("| ``$code`` |")
    }
    $null = $lines.Add("")
    if (@($dossier.incomplete.missing_evidence).Count -gt 0)
    {
        $null = $lines.Add("<details><summary>Missing evidence detail</summary>")
        $null = $lines.Add("")
        foreach ($item in @($dossier.incomplete.missing_evidence))
        {
            $null = $lines.Add("- **$($item.kind)**: $($item.detail)")
        }
        $null = $lines.Add("")
        $null = $lines.Add("</details>")
        $null = $lines.Add("")
    }
}
else
{
    $candidate = $dossier.candidate
    $null = $lines.Add("## Candidate")
    $null = $lines.Add("")
    $null = $lines.Add("- **Test**: ``$($candidate.test.fully_qualified_name)``")
    $null = $lines.Add("- **Proposed classification**: ``$($candidate.proposed_classification)``")
    $null = $lines.Add("- **Duplicate check status**: ``$($candidate.duplicate_check.status)``")
    if (@($candidate.duplicate_check.references).Count -gt 0)
    {
        $formattedReferences = (@($candidate.duplicate_check.references) | ForEach-Object { '`' + $_ + '`' }) -join ', '
        $null = $lines.Add("- **Duplicate references**: $formattedReferences")
    }
    $failureCount = @($candidate.evidence.raw_logs | Where-Object { $_.role -eq "failure" }).Count
    $negativeCount = @($candidate.evidence.raw_logs | Where-Object { $_.role -eq "negative" }).Count
    $null = $lines.Add("- **Failure evidence logs**: $failureCount")
    $null = $lines.Add("- **Negative evidence logs**: $negativeCount")
    $null = $lines.Add("")

    if (-not [string]::IsNullOrEmpty($ReceiptFile) -and (Test-Path -LiteralPath $ReceiptFile))
    {
        $receipt = Get-Content -LiteralPath $ReceiptFile -Raw | ConvertFrom-Json -Depth 32
        $null = $lines.Add("## Evaluator receipt")
        $null = $lines.Add("")
        $null = $lines.Add("| Field | Value |")
        $null = $lines.Add("|---|---|")
        $null = $lines.Add("| Deterministic status | ``$($receipt.deterministic_status)`` |")
        $null = $lines.Add("| Shadow recommendation | ``$($receipt.shadow_recommendation)`` |")
        $null = $lines.Add("| Eligible for KBE enrichment | $($receipt.eligible_for_kbe_enrichment) |")
        $null = $lines.Add("| Evidence provenance verified | $($receipt.evidence_provenance_verified) |")
        $null = $lines.Add("| Human review required | $($receipt.human_review_required) |")
        $null = $lines.Add("")
        $null = $lines.Add("**Reasons:**")
        foreach ($reason in @($receipt.reasons))
        {
            $null = $lines.Add("- $reason")
        }
        $null = $lines.Add("")
    }
}

if (@($dossier.provenance.build_insights_snapshots).Count -gt 0)
{
    $null = $lines.Add("## Build Insights snapshots (corroborating only, never authoritative)")
    $null = $lines.Add("")
    $null = $lines.Add("| Commit | Found | Conclusion | Exact test referenced | Known issue referenced |")
    $null = $lines.Add("|---|---|---|---|---|")
    foreach ($snapshot in @($dossier.provenance.build_insights_snapshots))
    {
        $shortSha = $snapshot.source_version.Substring(0, 7)
        $conclusion = Get-PropertyOrDefault -Object $snapshot -Name "conclusion" -Default "(none)"
        $null = $lines.Add("| ``$shortSha`` | $($snapshot.found) | $conclusion | $($snapshot.exact_test_referenced) | $($snapshot.known_issue_referenced) |")
    }
    $null = $lines.Add("")
}

$outputDirectory = Split-Path -Parent $OutputFile
if ($outputDirectory)
{
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}
[System.IO.File]::WriteAllText($OutputFile, ($lines -join [System.Environment]::NewLine) + [System.Environment]::NewLine)
Write-Host "Wrote summary to $OutputFile"
