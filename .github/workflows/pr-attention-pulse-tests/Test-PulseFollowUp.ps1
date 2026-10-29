#!/usr/bin/env pwsh
#Requires -Version 7.0

$ErrorActionPreference = "Stop"

function Assert-True
{
    param([bool]$Condition, [string]$Message)

    if (-not $Condition)
    {
        throw $Message
    }
}

function Write-Json
{
    param([object]$Value, [string]$Path)

    [IO.File]::WriteAllText(
        $Path,
        ($Value | ConvertTo-Json -Depth 100 -Compress) + "`n",
        [Text.UTF8Encoding]::new($false))
}

function New-IndexItem
{
    param(
        [int]$Number,
        [string]$HeadSha,
        [string]$Bucket,
        [string]$NextActor,
        [string]$CheckState = "Passing",
        [string]$MergeEligibility = "not-candidate"
    )

    return [pscustomobject]@{
        number = $Number
        title = "PR $Number"
        author = "author-$Number"
        bucket = $Bucket
        nextActor = $NextActor
        reasonCodes = @()
        blockers = @()
        headSha = $HeadSha
        checkState = $CheckState
        scopeMatch = "all-repo"
        discussionAssessment = [pscustomobject]@{
            state = "not-applicable"
            complete = $false
            signals = @()
            commentTotalCount = 0
            commentEvidenceTruncated = $false
            threads = [pscustomobject]@{
                totalCount = 0
                returnedCount = 0
                complete = $false
                unresolvedCount = 0
                outdatedUnresolvedCount = 0
            }
        }
        mergeEligibility = $MergeEligibility
    }
}

function New-Area
{
    param([string]$Id, [object[]]$Items, [string]$GeneratedAt)

    return [pscustomobject]@{
        id = $Id
        status = "complete"
        source = [pscustomobject]@{ generatedAt = $GeneratedAt }
        followUpIndex = @($Items)
    }
}

$comparePath = Join-Path (Split-Path -Parent $PSScriptRoot) "pr-attention-pulse/Compare-PRAttentionPulse.ps1"
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) "pulse-follow-up-$PID"
try
{
    New-Item -ItemType Directory -Force $tempRoot | Out-Null
    $previousItem = New-IndexItem `
        -Number 1 `
        -HeadSha ("a" * 40) `
        -Bucket "WaitingOnCI" `
        -NextActor "CI/automation" `
        -CheckState "Failed" `
        -MergeEligibility "not-assessed"
    $currentItem = New-IndexItem `
        -Number 1 `
        -HeadSha ("b" * 40) `
        -Bucket "ReadyToMerge" `
        -NextActor "merger" `
        -MergeEligibility "eligible"
    $previous = [pscustomobject]@{
        schemaVersion = "2.0.0"
        areas = @(
            (New-Area -Id "blazor" -Items @(
                $previousItem,
                (New-IndexItem -Number 4 -HeadSha ("e" * 40) -Bucket "WaitingOnCI" -NextActor "unchanged actor" -CheckState "Failed")
            ) -GeneratedAt "2026-10-28T12:00:00.0000000Z"),
            (New-Area -Id "repository-wide" -Items @(
                $previousItem,
                (New-IndexItem -Number 2 -HeadSha ("c" * 40) -Bucket "ReviewNow" -NextActor "human reviewer"),
                (New-IndexItem -Number 4 -HeadSha ("e" * 40) -Bucket "WaitingOnCI" -NextActor "unchanged actor" -CheckState "Failed")
            ) -GeneratedAt "2026-10-28T12:01:00.0000000Z")
        )
    }
    $current = [pscustomobject]@{
        schemaVersion = "2.0.0"
        areas = @(
            (New-Area -Id "blazor" -Items @(
                $currentItem,
                (New-IndexItem -Number 3 -HeadSha ("d" * 40) -Bucket "ReviewNow" -NextActor "human reviewer"),
                (New-IndexItem -Number 4 -HeadSha ("e" * 40) -Bucket "Draft" -NextActor "unchanged actor" -CheckState "Failed")
            ) -GeneratedAt "2026-10-29T12:00:00.0000000Z"),
            (New-Area -Id "repository-wide" -Items @(
                $currentItem,
                (New-IndexItem -Number 4 -HeadSha ("e" * 40) -Bucket "Draft" -NextActor "unchanged actor" -CheckState "Failed")
            ) -GeneratedAt "2026-10-29T12:01:00.0000000Z")
        )
    }

    $currentPath = Join-Path $tempRoot "current.json"
    $previousPath = Join-Path $tempRoot "previous.json"
    $outputPath = Join-Path $tempRoot "output.json"
    Write-Json -Value $current -Path $currentPath
    Write-Json -Value ([pscustomobject]@{ status = "available"; reason = ""; pulse = $previous }) -Path $previousPath
    & $comparePath -CurrentPath $currentPath -PreviousPath $previousPath -OutputPath $outputPath
    $actual = Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json -Depth 100
    foreach ($area in @($actual.areas))
    {
        Assert-True ($area.changes.status -ceq "complete") "Compatible complete indexes must produce a complete comparison."
        Assert-True (@($area.changes.items).Count -eq 1) "Only PRs present in both complete indexes with supported changes may be reported."
        Assert-True (($area.changes.items[0].signals -join ",") -ceq "head-updated,next-actor-changed,ci-recovered,merge-became-eligible") "All supported transitions must coalesce deterministically."
        Assert-True ($area.changes.items[0].number -eq 1) "New, removed, capped, or out-of-scope PRs must not be reported as completion."
    }

    Write-Json -Value ([pscustomobject]@{
        status = "unavailable"
        reason = "previous-snapshot-unavailable"
        pulse = $null
    }) -Path $previousPath
    & $comparePath -CurrentPath $currentPath -PreviousPath $previousPath -OutputPath $outputPath
    $unavailable = Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json -Depth 100
    foreach ($area in @($unavailable.areas))
    {
        Assert-True ($area.changes.status -ceq "unavailable" -and
            $area.changes.reason -ceq "previous-snapshot-unavailable" -and
            @($area.changes.items).Count -eq 0) "Unavailable prior evidence must remain an explicit limitation, never an empty-success comparison."
    }

    $retrievalScript = Join-Path (Split-Path -Parent $PSScriptRoot) "pr-attention-pulse/Get-PreviousPRAttentionPulse.ps1"
    $embeddedJson = [ordered]@{
        schemaVersion = "2.0.0"
        status = "complete"
        areas = @($previous.areas)
    } | ConvertTo-Json -Depth 100 -Compress -EscapeHandling EscapeNonAscii
    $embeddedHash = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($embeddedJson))
    ).ToLowerInvariant()
    $embeddedBodyPath = Join-Path $tempRoot "embedded-body.md"
    $embeddedOutputPath = Join-Path $tempRoot "embedded-previous.json"
    $embeddedBody = @(
        "Repository: ``dotnet/aspnetcore``; run ID: ``40877005681``; attempt: ``1``.",
        "JSON SHA-256: ``$embeddedHash``.",
        "",
        "<details>",
        "<summary>Snapshot JSON (exact sanitized bytes)</summary>",
        "",
        '```json',
        $embeddedJson,
        '```',
        "",
        "</details>"
    ) -join "`n"
    [IO.File]::WriteAllText($embeddedBodyPath, $embeddedBody, [Text.UTF8Encoding]::new($false))
    & $retrievalScript -IssueBodyPath $embeddedBodyPath -OutputPath $embeddedOutputPath -Token "unused"
    $embeddedResult = Get-Content -LiteralPath $embeddedOutputPath -Raw | ConvertFrom-Json -Depth 100
    Assert-True ($embeddedResult.status -ceq "available" -and
        $embeddedResult.pulse.areas[0].followUpIndex[0].headSha -ceq $previous.areas[0].followUpIndex[0].headSha) "The production retrieval wrapper must verify and retain exact embedded snapshot provenance without an API call."

    $missingBodyPath = Join-Path $tempRoot "missing-snapshot-body.md"
    $missingOutputPath = Join-Path $tempRoot "missing-previous.json"
    [IO.File]::WriteAllText($missingBodyPath, "No published snapshot.", [Text.UTF8Encoding]::new($false))
    & $retrievalScript -IssueBodyPath $missingBodyPath -OutputPath $missingOutputPath -Token "unused" 3>$null
    $missingResult = Get-Content -LiteralPath $missingOutputPath -Raw | ConvertFrom-Json -Depth 100
    Assert-True ($missingResult.status -ceq "unavailable" -and
        $missingResult.reason -ceq "previous-snapshot-unavailable" -and
        $null -eq $missingResult.pulse) "Missing publication identity must remain explicitly unavailable and must not trigger a stale fallback."
}
finally
{
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Output "Pulse follow-up comparison tests passed."
