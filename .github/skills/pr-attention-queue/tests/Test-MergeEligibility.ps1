#!/usr/bin/env pwsh
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$EvidenceDirectory,
    [string]$BaselineModulePath
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "MergeEligibilityTestHelpers.ps1")
$PSDefaultParameterValues = @{}
if ($BaselineModulePath) {
    $PSDefaultParameterValues["Invoke-MergeFixtureQueue:ModulePath"] = $BaselineModulePath
}
$fixturePath = Join-Path $PSScriptRoot "fixtures\merge-eligibility.json"
$failures = [Collections.Generic.List[string]]::new()

function Assert-MergeCase {
    param([string]$Name, [scriptblock]$Test)
    try {
        & $Test
        Write-Output "PASS $Name"
    }
    catch {
        $failures.Add($Name)
        Write-Output "FAIL $Name`: $($_.Exception.Message)"
    }
}

function Require {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-Fixtures {
    return @(Get-Content -Raw -LiteralPath $fixturePath | ConvertFrom-Json -Depth 30)
}

function Get-ClearFixture {
    $fixture = (Get-Fixtures)[2]
    $fixture.comments = @()
    $fixture.requests = @()
    return $fixture
}

function Get-Result {
    param([AllowEmptyCollection()][object[]]$Fixtures, [int]$Maximum = 3, [string[]]$ExcludedAuthors = @())
    $run = Invoke-MergeFixtureQueue -PullRequests $Fixtures -Maximum $Maximum -ExcludedAuthors $ExcludedAuthors
    return $run.output | ConvertFrom-Json -Depth 40
}

$realRun = Invoke-MergeFixtureQueue -PullRequests (Get-Fixtures)
$real = $realRun.output | ConvertFrom-Json -Depth 40
if ($EvidenceDirectory) {
    $realRun.output | Set-Content -LiteralPath (Join-Path $EvidenceDirectory "producer-queue.json")
    (Invoke-MergeFixtureQueue -PullRequests (Get-Fixtures) -OutputFormat Markdown).output |
        Set-Content -LiteralPath (Join-Path $EvidenceDirectory "producer-queue.md")
}
Assert-MergeCase "62861 approval then explicit COMMENTED feedback" {
    $item = $real.items | Where-Object number -eq 62861
    Require ($item.bucket -eq "WaitingOnAuthor" -and $item.reasonCodes -contains "reviewer-commented" -and -not $item.shownInDigest) "Expected WaitingOnAuthor/reviewer-commented, not unqualified ReadyToMerge."
}
Assert-MergeCase "65785 inline policy discussion is not an author blocker" {
    $item = $real.items | Where-Object number -eq 65785
    Require ($item.bucket -eq "ReadyToMerge" -and $item.nextActor -eq "merger") "Unresolved inline discussion alone must not invent author ownership."
    Require (-not $item.shownInDigest -and $item.shownInMergeVerification -and $item.mergeEligibility -eq "verification-needed") "Expected visible merge verification instead of unqualified ready."
    Require ($item.discussionAssessment.signals -contains "current-inline-discussion-unassessed") "The real bounded thread producer must supply the uncertainty signal."
}
Assert-MergeCase "67695 later approval addresses same reviewer top-level feedback" {
    $item = $real.items | Where-Object number -eq 67695
    Require ($item.bucket -eq "ReadyToMerge" -and $item.shownInDigest) "Clear real merge candidate must remain visible."
    Require ($item.mergeEligibility -eq "eligible" -and $item.discussionAssessment.state -eq "clear") "Expected an explicit complete clear assessment, not null."
}
Assert-MergeCase "67695 baseline-passing eligibility control" {
    Require (($real.items | Where-Object number -eq 67695).shownInDigest) "A genuine clear candidate stays displayed."
}
Assert-MergeCase "standalone projections preserve merge uncertainty" {
    $summary = $real.inbox.community.inventory | Where-Object number -eq 65785
    Require ($summary.mergeEligibility -eq "verification-needed") "Existing inbox summaries must not discard merge uncertainty."
    $fixture = (Get-Fixtures)[1]
    $fixture.number = 1001
    $fixture.createdAt = "2026-09-10T00:00:00Z"
    $fixture.updatedAt = "2026-09-12T00:00:00Z"
    $fixture.reviews = @(@{ author = @{ login = "reviewer" }; state = "APPROVED"; submittedAt = "2026-09-11T00:00:00Z"; commit = @{ oid = $fixture.headRefOid }; bodyText = "" })
    $fixture.requests = @()
    $fixture.comments = @()
    $markdown = (Invoke-MergeFixtureQueue -PullRequests @($fixture) -OutputFormat Markdown).output -join "`n"
    $rows = @($markdown -split "`n" | Where-Object { $_.StartsWith("|") -and $_.Contains("/pull/1001)") })
    Require ($rows.Count -eq 2) "A recent contribution should appear in merge verification and the existing recent-community preview."
    Require (@($rows | Where-Object { -not $_.Contains("Merge eligibility: verification-needed") }).Count -eq 0) "Every standalone rendering must preserve the eligibility qualifier."
}

foreach ($scenario in @("author-response", "new-head", "renewed-request", "subsequent-approval")) {
    Assert-MergeCase "feedback roundtrip $scenario" {
        $fixture = (Get-Fixtures)[0]
        switch ($scenario) {
            "author-response" { $fixture.comments += @{ author = $fixture.author; createdAt = "2026-09-15T00:00:00Z"; bodyText = "Updated."; authorAssociation = "NONE" } }
            "new-head" { $fixture.headRefOid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; $fixture.updatedAt = "2026-09-15T00:00:00Z" }
            "renewed-request" { $fixture.requests = @(@{ login = "halter73"; requestedAt = "2026-09-15T00:00:00Z" }) }
            "subsequent-approval" {
                $fixture.reviews += @{ author = @{ login = "halter73" }; state = "APPROVED"; submittedAt = "2026-09-15T00:00:00Z"; commit = @{ oid = $fixture.headRefOid }; bodyText = "" }
                $fixture.threads = @()
            }
        }
        $item = (Get-Result @($fixture)).items[0]
        if ($scenario -eq "subsequent-approval") {
            Require ($item.bucket -eq "ReadyToMerge" -and $item.shownInDigest) "Subsequent approval should restore eligibility once discussion is clear."
        }
        else {
            $reason = if ($scenario -eq "renewed-request") { "review-requested" } else { "author-responded" }
            Require ($item.bucket -eq "ReviewNow" -and $item.nextActor -eq "human reviewer" -and $item.reasonCodes -contains $reason) "Expected reviewer ownership and $reason after actual feedback."
        }
    }
}
foreach ($scenario in @("thanks-after-approval", "head-differs-old-approval", "author-self-review", "bot-review", "informational-review", "resolved-threads", "outdated-threads")) {
    Assert-MergeCase "baseline-passing negative control $scenario" {
        $fixture = Get-ClearFixture
        switch ($scenario) {
            "thanks-after-approval" { $fixture.comments = @(@{ author = $fixture.author; createdAt = "2026-09-15T00:00:00Z"; bodyText = "Thanks!"; authorAssociation = "NONE" }) }
            "head-differs-old-approval" { $fixture.headRefOid = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" }
            "author-self-review" { $fixture.reviews += @{ author = $fixture.author; state = "COMMENTED"; submittedAt = "2026-09-15T00:00:00Z"; commit = @{ oid = $fixture.headRefOid }; bodyText = "Please update this." } }
            "bot-review" { $fixture.reviews += @{ author = @{ login = "copilot-pull-request-reviewer" }; state = "COMMENTED"; submittedAt = "2026-09-15T00:00:00Z"; commit = @{ oid = $fixture.headRefOid }; bodyText = "Please update this." } }
            "informational-review" { $fixture.reviews += @{ author = @{ login = "reviewer" }; state = "COMMENTED"; submittedAt = "2026-09-15T00:00:00Z"; commit = @{ oid = $fixture.headRefOid }; bodyText = "FYI: looks good." } }
            "resolved-threads" { $fixture.threads = @(@{ isResolved = $true; isOutdated = $false }) }
            "outdated-threads" { $fixture.threads = @(@{ isResolved = $false; isOutdated = $true }) }
        }
        $item = (Get-Result @($fixture)).items[0]
        Require ($item.bucket -eq "ReadyToMerge" -and $item.shownInDigest) "This negative control must remain merge eligible."
    }
}
foreach ($scenario in @("human-thread", "bot-thread", "unknown-review", "informational-top-level", "other-reviewer-old-concern", "later-concern", "missing", "truncated-comments", "truncated-threads", "truncated-reviews", "missing-comment-nodes")) {
    Assert-MergeCase "merge evidence $scenario" {
        $fixture = Get-ClearFixture
        switch ($scenario) {
            "human-thread" { $fixture.threads = @(@{ isResolved = $false; isOutdated = $false; author = "reviewer" }) }
            "bot-thread" { $fixture.threads = @(@{ isResolved = $false; isOutdated = $false; author = "copilot-pull-request-reviewer" }) }
            "unknown-review" { $fixture.reviews += @{ author = @{ login = "reviewer" }; state = "COMMENTED"; submittedAt = "2026-09-15T00:00:00Z"; commit = @{ oid = $fixture.headRefOid }; bodyText = "Hmm." } }
            "informational-top-level" { $fixture.comments = @(@{ author = @{ login = "reviewer" }; createdAt = "2026-09-15T00:00:00Z"; bodyText = "FYI: context only."; authorAssociation = "MEMBER" }) }
            "other-reviewer-old-concern" { $fixture.comments = @(@{ author = @{ login = "different-reviewer" }; createdAt = "2026-08-20T00:00:00Z"; bodyText = "Please update coverage."; authorAssociation = "MEMBER" }) }
            "later-concern" { $fixture.comments = @(@{ author = @{ login = "PureWeen" }; createdAt = "2026-09-15T00:00:00Z"; bodyText = "Please update coverage."; authorAssociation = "MEMBER" }) }
            "missing" { $fixture | Add-Member missingDiscussion $true }
            "truncated-comments" { $fixture | Add-Member truncateComments $true }
            "truncated-threads" { $fixture | Add-Member truncateThreads $true }
            "truncated-reviews" { $fixture | Add-Member truncateReviews $true }
            "missing-comment-nodes" { $fixture | Add-Member missingCommentNodes $true }
        }
        $item = (Get-Result @($fixture)).items[0]
        if ($scenario -eq "informational-top-level") {
            Require ($item.shownInDigest -and $item.mergeEligibility -eq "eligible") "Explicit informational discussion is a clear control."
        }
        else {
            Require ($item.bucket -eq "ReadyToMerge" -and -not $item.shownInDigest -and $item.shownInMergeVerification) "Uncertainty must be visible as merge verification, not a fabricated author blocker or clear ready."
        }
    }
}
Assert-MergeCase "failed discussion collection fails queue closed" {
    $fixture = Get-ClearFixture
    $fixture | Add-Member failDiscussion $true
    $errorText = ""
    try { $null = Get-Result @($fixture) } catch { $errorText = $_.Exception.Message }
    Require ($errorText -like "*Fixture discussion transport failure*") "The actual producer failure must propagate, not return a complete zero or ready card."
}
Assert-MergeCase "independent budgets bounded refill caps and exclusions" {
    $fixtures = @(
        foreach ($number in 1..25) {
            $fixture = Get-ClearFixture
            $fixture.number = $number
            if ($number -le 3) { $fixture.threads = @(@{ isResolved = $false; isOutdated = $false }) }
            $fixture
        }
        foreach ($number in 101..125) {
            $fixture = Get-ClearFixture
            $fixture.number = $number
            $fixture.reviews = @()
            $fixture | Add-Member reviewDecision "REVIEW_REQUIRED"
            $fixture.requests = @(@{ login = "reviewer"; requestedAt = "2026-09-15T00:00:00Z" })
            $fixture
        }
    )
    $run = Invoke-MergeFixtureQueue -PullRequests $fixtures -Maximum 2
    $result = $run.output | ConvertFrom-Json -Depth 40
    Require ($result.discussion.assessedCandidateCount -eq 20 -and $result.discussion.unassessedReviewNowCount -eq 5) "ReviewNow must retain its independent 20-candidate capacity."
    Require ($result.mergeDiscussion.assessedCandidateCount -eq 20 -and $result.mergeDiscussion.unassessedCandidateCount -eq 5) "Merge candidates must have an independently bounded 20-candidate pass."
    Require ($run.discussionNumbers.Count -eq 40 -and @($run.discussionNumbers | Select-Object -Unique).Count -eq 40) "Only 20 candidates per class may be fetched, once each."
    Require ((@($result.items | Where-Object { $_.bucket -eq "ReadyToMerge" -and $_.shownInDigest } | Sort-Object digestRank).number -join ",") -eq "4,5") "Clear candidates refill ready slots within the assessed prefix."
    Require (@($result.items | Where-Object shownInMergeVerification).Count -eq 2 -and $result.mergeDiscussion.verificationLimit -eq 2) "Verification display must reuse caller MaxReadyToMerge."
    Require (@($result.items | Where-Object { $_.bucket -eq "ReviewNow" -and $_.shownInDigest }).Count -eq 2) "Existing ReviewNow per-author cap must remain intact."
    $excluded = Get-Result -Fixtures $fixtures -ExcludedAuthors @("Yuvan111")
    Require ($excluded.mergeDiscussion.excludedCandidateCount -eq 25 -and $excluded.mergeDiscussion.assessedCandidateCount -eq 0) "Excluded authors remain reconciled without consuming assessment positions."
}
Assert-MergeCase "unassessed visible without budget overrun" {
    $fixtures = @(foreach ($number in 1..21) {
        $fixture = Get-ClearFixture
        $fixture.number = $number
        $fixture
    })
    $result = Get-Result $fixtures
    $item = $result.items | Where-Object number -eq 21
    Require ($item.mergeEligibility -eq "not-assessed" -and $item.shownInMergeVerification -and -not $item.shownInDigest) "The first unassessed candidate must be visible and cannot claim clear readiness."
    Require (@($result.warnings -match "merge").Count -gt 0) "Coverage warning must disclose the unassessed tail."
}
Assert-MergeCase "empty inventories require no detail calls" {
    $run = Invoke-MergeFixtureQueue -PullRequests @()
    $result = $run.output | ConvertFrom-Json -Depth 40
    Require ($result.census.matched -eq 0 -and $result.mergeDiscussion.assessedCandidateCount -eq 0 -and $result.mergeDiscussion.unassessedCandidateCount -eq 0) "Complete zero must remain explicit."
    Require ($run.requests.Count -eq 2 -and $run.discussionNumbers.Count -eq 0) "Empty inventory must not issue detail calls."
}

if ($failures.Count -gt 0) { throw "$($failures.Count) merge eligibility cases failed: $($failures -join ', ')." }
Write-Output "All merge eligibility cases passed."
