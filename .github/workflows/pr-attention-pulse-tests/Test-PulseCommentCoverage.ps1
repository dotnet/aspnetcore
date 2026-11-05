#!/usr/bin/env pwsh
#Requires -Version 7.0

param(
    [string]$SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [string]$BaselineSupportRoot
)

$ErrorActionPreference = "Stop"
$testRoot = Join-Path $SourceRoot ".github\workflows\pr-attention-pulse-tests"
. (Join-Path $testRoot "Test-PRAttentionPulse.ps1") -FunctionsOnly
$queueTestRoot = Join-Path $SourceRoot ".github\skills\pr-attention-queue\tests"
. (Join-Path $queueTestRoot "MergeEligibilityTestHelpers.ps1")
$supportRoot = Join-Path $SourceRoot ".github\workflows\pr-attention-pulse"
if ($BaselineSupportRoot)
{
    $supportRoot = $BaselineSupportRoot
}
$sanitizerPath = Join-Path $supportRoot "Sanitize-PRAttentionPulse.ps1"
$combinerPath = Join-Path $supportRoot "Combine-PRAttentionPulse.ps1"
$rendererPath = Join-Path $supportRoot "Render-PRAttentionPulse.ps1"
$validatorPath = Join-Path $supportRoot "Validate-PRAttentionPulseOutput.ps1"
$modulePath = Join-Path $SourceRoot ".github\skills\pr-attention-queue\scripts\PRAttentionQueue.psm1"
$collectorJsRoot = Join-Path (Get-GhAwExtensionRoot) "actions\setup\js"
$collectorSanitizerPath = Join-Path $collectorJsRoot "sanitize_content.cjs"
$attemptTimestamp = [datetime]"2026-09-16T15:00:00Z"
$tempRoot = Join-Path $PSScriptRoot ".comment-coverage-$([guid]::NewGuid().ToString('N'))"
$failures = [Collections.Generic.List[string]]::new()
$caseCount = 0

function Invoke-CoverageCase
{
    param([string]$CaseName, [scriptblock]$Action)

    $script:caseCount++
    try
    {
        & $Action
        Write-Output "PASS $CaseName"
    }
    catch
    {
        $failures.Add("$CaseName`: $($_.Exception.Message)")
        Write-Output "FAIL $CaseName`: $($_.Exception.Message)"
    }
}

function Copy-CoverageObject
{
    param([object]$Value)

    return $Value | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100
}

function Convert-CoverageRaw
{
    param([object]$Raw, [string]$Scope)

    $path = Join-Path $tempRoot "raw-$([guid]::NewGuid().ToString('N')).json"
    Write-JsonFile -Value $Raw -Path $path
    try
    {
        return Invoke-Sanitizer -SourcePath $path -Scope $Scope
    }
    finally
    {
        Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
    }
}

function New-CoverageProducerResult
{
    param([string]$Scope, [string]$Scenario)

    $fixture = @(Get-Content -LiteralPath (Join-Path $queueTestRoot "fixtures\merge-eligibility.json") -Raw | ConvertFrom-Json -Depth 100)[2]
    $fixture.requests = @()
    $fixture.threads = @()
    $count = switch ($Scenario)
    {
        "ten-comments" { 10 }
        "fifty-comments" { 50 }
        "hundred-comments-latest-fifty" { 50 }
        default { 11 }
    }
    $fixture.comments = @(
        foreach ($index in 1..$count)
        {
            [pscustomobject]@{
                author = [pscustomobject]@{ login = $fixture.author.login }
                createdAt = ([datetime]"2026-09-12T00:00:00Z").AddMinutes($index).ToString("o")
                bodyText = "Thanks!"
                authorAssociation = "NONE"
            }
        }
    )
    switch ($Scenario)
    {
        "missing-author" { $fixture.comments[0].author = $null }
        "missing-timestamp" { $fixture.comments[0].PSObject.Properties.Remove("createdAt") }
        "oldest-comment-needs-verification"
        {
            $fixture.comments[0].author.login = "different-reviewer"
            $fixture.comments[0].bodyText = "Please update coverage."
        }
        "verification-with-missing-author"
        {
            $fixture.comments[0].author.login = "different-reviewer"
            $fixture.comments[0].bodyText = "Please update coverage."
            $fixture.comments[-1].author = $null
        }
        "hundred-comments-latest-fifty" { $fixture | Add-Member truncateComments $true }
        "missing-comment-nodes" { $fixture | Add-Member missingCommentNodes $true }
    }
    $run = Invoke-MergeFixtureQueue -PullRequests @($fixture) -Scope $Scope -ModulePath $modulePath
    Assert-True (($run.discussionNumbers -join ",") -ceq "67695") "The actual producer must request this candidate's discussion evidence."
    $queries = @($run.requests | Where-Object { $_.Contains("reviewThreads(last: 50)") })
    Assert-True ($queries.Count -eq 1 -and $queries[0].Contains("comments(last: 50)")) "Exercise the real fifty-comment query, not an injected assessment."

    return $run.output | ConvertFrom-Json -Depth 100
}

function Assert-CoveragePublication
{
    param([object]$Pulse)

    $body = Invoke-PinnedOutputSanitizer -Content (Invoke-Renderer -Pulse $Pulse)
    Invoke-PublicationValidator -Pulse $Pulse -AgentOutput (New-ValidAgentOutput -Body $body) -ExpectedBody $body
}

New-Item -ItemType Directory -Path $tempRoot | Out-Null
try
{
    $combinedAreas = @{}
    foreach ($scope in @("blazor", "repository-wide"))
    {
        $results = @{}
        foreach ($scenario in @(
            "ten-comments", "eleven-comments", "fifty-comments", "missing-author", "missing-timestamp",
            "oldest-comment-needs-verification", "verification-with-missing-author",
            "hundred-comments-latest-fifty", "missing-comment-nodes"))
        {
            Invoke-CoverageCase "CommentCoverage/$scope/$scenario" {
                $raw = New-CoverageProducerResult -Scope $scope -Scenario $scenario
                $assessment = $raw.items[0].discussionAssessment
                $incomplete = $scenario -cin @("hundred-comments-latest-fifty", "missing-comment-nodes")
                $verification = $incomplete -or $scenario -cin @("oldest-comment-needs-verification", "verification-with-missing-author")
                $expectedTotal = switch ($scenario)
                {
                    "ten-comments" { 10 }
                    "fifty-comments" { 50 }
                    "hundred-comments-latest-fifty" { 100 }
                    default { 11 }
                }
                $expectedRetained = if ($scenario -ceq "missing-comment-nodes") { 0 } else { 10 }
                $expectedTruncated = $scenario -cnotin @("ten-comments", "missing-author", "missing-timestamp", "verification-with-missing-author")
                Assert-True ($assessment.commentTotalCount -eq $expectedTotal -and
                    @($assessment.comments).Count -eq $expectedRetained) "Connection total and retained excerpts must have distinct producer meanings."
                Assert-True ($assessment.commentEvidenceTruncated -eq $expectedTruncated -and
                    $assessment.complete -eq (-not $incomplete)) "Excerpt retention is independent of collection completeness."
                $expectedEligibility = if ($verification) { "verification-needed" } else { "eligible" }
                Assert-True ($raw.items[0].mergeEligibility -ceq $expectedEligibility) "Actual producer assessment must determine eligibility."
                if ($scenario -ceq "oldest-comment-needs-verification")
                {
                    Assert-True (@($assessment.comments | Where-Object author -CEQ "different-reviewer").Count -eq 0 -and
                        @($assessment.signals | Where-Object { $_.StartsWith("non-author-discussion-") }).Count -gt 0) "The producer must assess the eleventh comment even though its excerpt is not retained."
                }
                if ($incomplete)
                {
                    Assert-True ($assessment.signals -ccontains "discussion-incomplete") "Missing collection must have the actual producer's incompleteness signal."
                }
                $pulse = Convert-CoverageRaw -Raw $raw -Scope $scope
                Assert-True ($pulse.status -ceq "complete") "A valid producer result must reach the real sanitizer."
                $view = if ($verification) { "verifyDiscussionBeforeMerge" } else { "readyToMerge" }
                $otherView = if ($verification) { "readyToMerge" } else { "verifyDiscussionBeforeMerge" }
                Assert-True (@($pulse.views.$view).Count -eq 1) "The sanitizer must preserve the producer's clear versus verification view."
                Assert-True (@($pulse.views.$otherView).Count -eq 0) "A candidate must not leak into the opposite merge presentation."
                Assert-CoveragePublication -Pulse $pulse
                $results[$scenario] = [pscustomobject]@{ raw = $raw; pulse = $pulse }
                Write-Output "EVIDENCE $scope/$scenario total=$expectedTotal retained=$expectedRetained truncated=$expectedTruncated complete=$(-not $incomplete) eligibility=$expectedEligibility"
            }
        }

        foreach ($flag in @($false, $true))
        {
            Invoke-CoverageCase "CommentCoverage/$scope/incomplete-relabelled-eligible/flag-$flag" {
                $raw = Copy-CoverageObject $results["hundred-comments-latest-fifty"].raw
                $item = $raw.items[0]
                $item.mergeEligibility = "eligible"
                $item.discussionAssessment.state = "clear"
                $item.discussionAssessment.commentEvidenceTruncated = $flag
                $item.shownInDigest = $true
                $item.digestRank = 1
                $item.shownInMergeVerification = $false
                $item.mergeVerificationRank = $null
                $raw.mergeDiscussion.eligibleCount = 1
                $raw.mergeDiscussion.verificationNeededCount = 0
                $raw.overflow.readyToMerge = 0
                $pulse = Convert-CoverageRaw -Raw $raw -Scope $scope
                Assert-True ($pulse.status -ceq "unavailable" -and $pulse.errorCategory -ceq "invalid-contract") "Neither flag value can turn real incomplete assessment evidence into an eligible result."

                $pulse = Copy-CoverageObject $results["hundred-comments-latest-fifty"].pulse
                $body = Invoke-PinnedOutputSanitizer -Content (Invoke-Renderer -Pulse $pulse)
                $item = $pulse.views.verifyDiscussionBeforeMerge[0]
                $item.mergeEligibility = "eligible"
                $item.discussionAssessment.state = "clear"
                $item.discussionAssessment.commentEvidenceTruncated = $flag
                $pulse.views.readyToMerge = @($item)
                $pulse.views.verifyDiscussionBeforeMerge = @()
                $pulse.source.mergeDiscussion.eligibleCount = 1
                $pulse.source.mergeDiscussion.verificationNeededCount = 0
                Assert-Throws {
                    Invoke-PublicationValidator -Pulse $pulse -AgentOutput (New-ValidAgentOutput -Body $body) -ExpectedBody $body
                } "The private merge gate must also reject relabelled incomplete evidence with either excerpt flag." "Eligible merge candidates require complete clear discussion evidence."
            }
        }

        $mutations = [ordered]@{
            "false-to-true-at-ten" = @{ Source = "ten-comments"; Change = { param($a) $a.commentEvidenceTruncated = $true }; Accepted = $false }
            "true-to-false-at-eleven" = @{ Source = "eleven-comments"; Change = { param($a) $a.commentEvidenceTruncated = $false }; Accepted = $true }
            "false-to-true-with-filtered-comment" = @{ Source = "missing-author"; Change = { param($a) $a.commentEvidenceTruncated = $true }; Accepted = $true }
            "count-below-retention-with-true-flag" = @{ Source = "eleven-comments"; Change = { param($a) $a.commentTotalCount = 10 }; Accepted = $false }
            "nonboolean-flag" = @{ Source = "eleven-comments"; Change = { param($a) $a.commentEvidenceTruncated = "false" }; Accepted = $false }
            "noninteger-count" = @{ Source = "eleven-comments"; Change = { param($a) $a.commentTotalCount = "11" }; Accepted = $false }
            "negative-count" = @{ Source = "eleven-comments"; Change = { param($a) $a.commentTotalCount = -1 }; Accepted = $false }
        }
        foreach ($mutationName in $mutations.Keys)
        {
            Invoke-CoverageCase "CommentCoverage/$scope/raw/$mutationName" {
                $mutation = $mutations[$mutationName]
                $raw = Copy-CoverageObject $results[$mutation.Source].raw
                & $mutation.Change $raw.items[0].discussionAssessment
                $pulse = Convert-CoverageRaw -Raw $raw -Scope $scope
                $expectedStatus = if ($mutation.Accepted) { "complete" } else { "unavailable" }
                Assert-True ($pulse.status -ceq $expectedStatus) "Validate only constraints derivable from the sanitized producer contract, not total-count equality with filtered excerpt count."
                if ($mutation.Accepted)
                {
                    Assert-True ($pulse.views.readyToMerge[0].discussionAssessment.complete -and
                        $pulse.views.readyToMerge[0].discussionAssessment.signals.Count -eq 0) "Accepted ambiguous excerpt flags must not weaken the completeness and signal checks."
                }
            }
        }

        foreach ($mutationName in @("true-to-false", "false-to-true", "count-decreased", "count-increased"))
        {
            Invoke-CoverageCase "CommentCoverage/$scope/private/$mutationName" {
                $source = if ($mutationName -ceq "false-to-true") { "verification-with-missing-author" } else { "oldest-comment-needs-verification" }
                $pulse = Copy-CoverageObject $results[$source].pulse
                $body = Invoke-PinnedOutputSanitizer -Content (Invoke-Renderer -Pulse $pulse)
                Assert-CoveragePublication -Pulse $pulse
                $assessment = $pulse.views.verifyDiscussionBeforeMerge[0].discussionAssessment
                switch ($mutationName)
                {
                    "true-to-false" { $assessment.commentEvidenceTruncated = $false }
                    "false-to-true" { $assessment.commentEvidenceTruncated = $true }
                    "count-decreased" { $assessment.commentTotalCount-- }
                    "count-increased" { $assessment.commentTotalCount++ }
                }
                Assert-Throws {
                    Invoke-PublicationValidator -Pulse $pulse -AgentOutput (New-ValidAgentOutput -Body $body) -ExpectedBody $body
                } "The private validator must reject changed published discussion evidence and clean publishable output." "The trusted Pulse body does not match the sanitized input."
            }
        }
        $combinedAreas[$scope] = $results["missing-author"].pulse
    }
    Invoke-CoverageCase "CommentCoverage/combined-legitimate-false-flag" {
        $combined = New-CombinedPulse -Blazor $combinedAreas.blazor -RepositoryWide $combinedAreas["repository-wide"]
        Assert-CoveragePublication -Pulse $combined
    }
}
finally
{
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Output "Pulse comment coverage: $($caseCount - $failures.Count)/$caseCount passed; $($failures.Count) failed."
if ($failures.Count -gt 0)
{
    throw "$($failures.Count) comment coverage cases failed."
}
