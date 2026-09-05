#!/usr/bin/env pwsh
#Requires -Version 7.0

$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$skillRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $skillRoot "scripts/Get-PRAttentionQueue.ps1"
$modulePath = Join-Path $skillRoot "scripts/PRAttentionQueue.psm1"
$fixturePath = Join-Path $PSScriptRoot "fixtures/pull-requests.json"
$correctnessFixturePath = Join-Path $PSScriptRoot "fixtures/correctness-pull-requests.json"
$discussionFixturePath = Join-Path $PSScriptRoot "fixtures/discussion-pull-requests.json"
$snapshot = [datetime]"2026-09-03T18:00:00Z"

Import-Module -Scope Local -Force $modulePath
$exportedCommands = @(Get-Command -Module PRAttentionQueue)

Assert-True ($exportedCommands.Count -eq 1) "The module must export exactly one command."
Assert-True ($exportedCommands[0].Name -eq "Invoke-PRAttentionQueue") "The module must export Invoke-PRAttentionQueue."

$defaultJson = & $scriptPath `
    -InputPath $fixturePath `
    -Now $snapshot `
    -OutputFormat Json
$defaultResult = $defaultJson | ConvertFrom-Json -Depth 100

Assert-True ($defaultResult.filter.name -eq "blazor") "The default scope must be the Blazor preset."
Assert-True ($defaultResult.query.complete) "The fixture universe must be complete."
Assert-True ($defaultResult.census.matched -eq 14) "The Blazor preset should match fourteen fixture PRs."
Assert-True ($defaultResult.census.pathOnly -eq 1) "One unlabeled PR should match only by Components path."
Assert-True ($defaultResult.census.incidentalPathExcluded -eq 1) "A repository-wide sweep must not enter a narrow scope."
Assert-True (-not ($defaultResult.items | Where-Object number -eq 13)) "PR 13 touches Components incidentally and must be excluded."
Assert-True (($defaultResult.items | Where-Object number -eq 14).bucket -eq "WaitingOnAuthor") "PR 14 conflicts and belongs to its author."
Assert-True (($defaultResult.items | Where-Object number -eq 14).reasonCodes -contains "merge-conflict") "PR 14 should explain the merge conflict."
Assert-True (($defaultResult.items | Where-Object number -eq 1).bucket -eq "ReviewNow") "PR 1 should be reviewable now."
Assert-True (($defaultResult.items | Where-Object number -eq 2).bucket -eq "NeedsRescue") "PR 2 should need rescue."
Assert-True (($defaultResult.items | Where-Object number -eq 3).bucket -eq "WaitingOnAuthor") "PR 3 should wait on its author."
Assert-True (($defaultResult.items | Where-Object number -eq 4).bucket -eq "ReadyToMerge") "PR 4 should be ready to merge."
Assert-True (($defaultResult.items | Where-Object number -eq 5).bucket -eq "WaitingOnCI") "PR 5 should wait on CI."
Assert-True (($defaultResult.items | Where-Object number -eq 6).bucket -eq "DesignDecision") "PR 6 should wait on API review."
Assert-True (($defaultResult.items | Where-Object number -eq 7).bucket -eq "Excluded") "PR 7 should be excluded as a bot."
Assert-True (($defaultResult.items | Where-Object number -eq 9).bucket -eq "ReviewNow") "PR 9 should return to review after the author responds."
Assert-True (($defaultResult.items | Where-Object number -eq 9).reasonCodes -contains "author-responded") "PR 9 should explain the author roundtrip."
Assert-True (($defaultResult.items | Where-Object number -eq 10).bucket -eq "ReviewNow") "PR 10 should return to review after a new commit."
Assert-True (($defaultResult.items | Where-Object number -eq 10).reasonCodes -contains "author-responded") "PR 10 should explain the commit roundtrip."
Assert-True (($defaultResult.items | Where-Object number -eq 11).bucket -eq "WaitingOnAuthor") "PR 11 should handle a bot-only change request without crashing."
Assert-True (($defaultResult.items | Where-Object number -eq 11).author -eq "pedro") "A human login ending in 'o' must not be classified as a bot."
Assert-True (($defaultResult.items | Where-Object number -eq 12).bucket -eq "NeedsRescue") "PR 12 should treat a stale review request as rescue work."
Assert-True (($defaultResult.items | Where-Object number -eq 12).reasonCodes -contains "reviewer-idle-30d") "PR 12 should explain reviewer silence."
Assert-True (($defaultResult.items | Where-Object number -eq 15).bucket -eq "NeedsRescue") "PR 15 should treat an abandoned human review as rescue work."
Assert-True (($defaultResult.items | Where-Object number -eq 15).nextActor -eq "maintainer/triager") "PR 15 should require maintainer triage."
Assert-True (($defaultResult.items | Where-Object number -eq 15).reasonCodes -contains "review-abandoned") "PR 15 should identify the abandoned review."
Assert-True (($defaultResult.items | Where-Object number -eq 15).reasonCodes -contains "reviewer-commented") "PR 15 should preserve the specific review state."
Assert-True (($defaultResult.items | Where-Object number -eq 15).reasonCodes -contains "reviewer-idle-30d") "PR 15 should explain reviewer inactivity."
Assert-True (($defaultResult.items | Where-Object number -eq 16).bucket -eq "WaitingOnCI") "PR 16 should remain blocked on CI."
Assert-True (-not (($defaultResult.items | Where-Object number -eq 16).reasonCodes -contains "review-abandoned")) "PR 16 must not be marked abandoned while CI is failing."
Assert-True ($defaultResult.schemaVersion -eq "1.0.0") "The JSON contract must declare its schema version."
Assert-True ($defaultResult.display.buckets.ReviewNow.label -eq "Review now") "Bucket display metadata must be emitted in-band."
Assert-True ($defaultResult.display.buckets.NeedsRescue.description -ne "") "Bucket display metadata must include descriptions."
Assert-True ($defaultResult.display.reasonCodes.'review-abandoned'.label -eq "Review abandoned") "Reason-code display metadata must include review-abandoned."
Assert-True ($defaultResult.display.reasonCodes.'ci-failed'.description -ne "") "Reason-code display metadata must describe existing codes."
foreach ($bucket in @($defaultResult.census.byBucket.PSObject.Properties.Name)) {
    Assert-True ($null -ne $defaultResult.display.buckets.PSObject.Properties[$bucket]) "Bucket '$bucket' must have display metadata."
}
foreach ($reasonCode in @($defaultResult.items.reasonCodes | Select-Object -Unique)) {
    Assert-True ($null -ne $defaultResult.display.reasonCodes.PSObject.Properties[$reasonCode]) "Reason code '$reasonCode' must have display metadata."
}
Assert-True (@($defaultResult.items | Where-Object { $_.bucket -eq "ReviewNow" -and $_.shownInDigest }).Count -le 5) "Review now must respect its cap."
Assert-True (@($defaultResult.items | Where-Object { $_.bucket -eq "NeedsRescue" -and $_.shownInDigest }).Count -le 3) "Needs rescue must respect its cap."
Assert-True ($defaultResult.overflow.reviewNow -ge 0) "Review now overflow must be reported."

Import-Module -Scope Local -Force $modulePath
$identityJson = Invoke-PRAttentionQueue `
    -InputPath $fixturePath `
    -Now $snapshot `
    -Label area-identity `
    -Path "src/Identity/**" `
    -OutputFormat Json
$identityResult = $identityJson | ConvertFrom-Json -Depth 100

Assert-True ($identityResult.filter.name -eq "adhoc") "Explicit filters must disable the Blazor default."
Assert-True ($identityResult.census.matched -eq 1) "The Identity scope should match one fixture PR."
Assert-True ($identityResult.items[0].number -eq 8) "The Identity scope should return PR 8."

$forwardedJson = & $scriptPath `
    -InputPath $fixturePath `
    -Now $snapshot `
    -MaxReviewNow 1 `
    -OutputFormat Json
$forwardedResult = $forwardedJson | ConvertFrom-Json -Depth 100

Assert-True ($forwardedResult.caps.reviewNow -eq 1) "The entry point must forward explicit parameter values."
Assert-True (@($forwardedResult.items | Where-Object { $_.bucket -eq "ReviewNow" -and $_.shownInDigest }).Count -eq 1) "The forwarded Review now cap must be applied."

$markdown = & $scriptPath `
    -InputPath $fixturePath `
    -Now $snapshot `
    -OutputFormat Markdown
$markdownText = $markdown -join [Environment]::NewLine

Assert-True ($markdownText.Contains("## Review now")) "Markdown must contain Review now."
Assert-True ($markdownText.Contains("## Needs rescue")) "Markdown must contain Needs rescue."
Assert-True ($markdownText.Contains("matched only by changed path")) "Markdown must report path-only coverage."
Assert-True ($markdownText.Contains("incidentally")) "Markdown must report incidental path exclusions."
Assert-True ($markdownText.Contains("**Overflow:**")) "Markdown must report digest overflow."
Assert-True (-not $markdownText.Contains("@community-user")) "Markdown must not mention contributors."

$correctnessJson = & $scriptPath `
    -InputPath $correctnessFixturePath `
    -Now $snapshot `
    -OutputFormat Json
$correctnessResult = $correctnessJson | ConvertFrom-Json -Depth 100

Assert-True (($correctnessResult.items | Where-Object number -eq 101).bucket -eq "NeedsRescue") "An explicit no-merge label must win over pending CI."
Assert-True (($correctnessResult.items | Where-Object number -eq 101).reasonCodes -contains "blocked-label") "The no-merge result must identify the blocking label."
Assert-True (($correctnessResult.items | Where-Object number -eq 102).bucket -eq "WaitingOnCI") "A pending CI rerun must not be ready to merge."
Assert-True (($correctnessResult.items | Where-Object number -eq 102).reasonCodes -contains "ci-rerun-pending") "The pending rerun must have a stable reason."
Assert-True (($correctnessResult.items | Where-Object number -eq 103).bucket -eq "WaitingOnCI") "A non-clean merge state must not be ready to merge."
Assert-True (($correctnessResult.items | Where-Object number -eq 103).reasonCodes -contains "merge-state-not-clean") "The non-clean merge state must have a stable reason."
Assert-True (($correctnessResult.items | Where-Object number -eq 104).bucket -eq "ReadyToMerge") "An approved clean pull request should be ready to merge."
Assert-True (($correctnessResult.items | Where-Object number -eq 116).bucket -eq "WaitingOnAuthor") "A branch behind its base must not be assigned to CI."
Assert-True (($correctnessResult.items | Where-Object number -eq 116).reasonCodes -contains "branch-update-required") "A behind branch must explain the required update."
Assert-True (($correctnessResult.items | Where-Object number -eq 105).bucket -eq "WaitingOnAuthor") "Recent reviewer feedback must override a stale team request."
Assert-True (($correctnessResult.items | Where-Object number -eq 106).bucket -eq "ReviewNow") "A later author response should return the pull request to review."
Assert-True (($correctnessResult.items | Where-Object number -eq 107).humanReviewCount -eq 0) "Author-authored reviews must not count as human reviewer activity."
Assert-True (($correctnessResult.items | Where-Object number -eq 107).bucket -eq "NeedsRescue") "A stale request must remain rescue work when the only review is author-authored."

$rankingJson = & $scriptPath `
    -InputPath $correctnessFixturePath `
    -Now $snapshot `
    -Label area-ranking `
    -MaxReviewNow 2 `
    -OutputFormat Json
$rankingResult = $rankingJson | ConvertFrom-Json -Depth 100
$rankedItems = @($rankingResult.items | Where-Object shownInDigest | Sort-Object digestRank)
Assert-True ($rankedItems[0].number -eq 109) "Community neglect risk must determine the first digest rank."
Assert-True ($rankedItems[0].digestRank -eq 1) "The first selected item must expose digest rank one."
Assert-True ($rankedItems[1].number -eq 108) "The second selected item must preserve the deterministic rank."
Assert-True ($rankedItems[1].digestRank -eq 2) "The second selected item must expose digest rank two."

$rankingMarkdown = & $scriptPath `
    -InputPath $correctnessFixturePath `
    -Now $snapshot `
    -Label area-ranking `
    -MaxReviewNow 2 `
    -OutputFormat Markdown
$rankingMarkdownText = $rankingMarkdown -join [Environment]::NewLine
Assert-True ($rankingMarkdownText.IndexOf("[#109]") -lt $rankingMarkdownText.IndexOf("[#108]")) "Markdown must render selected items by digest rank."

$digestControlJson = & $scriptPath `
    -InputPath $correctnessFixturePath `
    -Now $snapshot `
    -Label area-stack `
    -ExcludeDigestAuthor current-user `
    -MaxReviewNow 2 `
    -OutputFormat Json
$digestControlResult = $digestControlJson | ConvertFrom-Json -Depth 100
Assert-True (($digestControlResult.items | Where-Object number -eq 111).bucket -eq "ReviewNow") "Stack health must not rewrite the child bucket."
Assert-True (-not ($digestControlResult.items | Where-Object number -eq 111).shownInDigest) "A child with an unhealthy ancestor must not consume a digest slot."
Assert-True (($digestControlResult.items | Where-Object number -eq 111).digestExclusionReasons -contains "stacked-on-unhealthy-pr") "The child must explain its digest exclusion."
Assert-True (($digestControlResult.items | Where-Object number -eq 113).bucket -eq "ReviewNow") "Caller exclusion must not rewrite the bucket."
Assert-True (-not ($digestControlResult.items | Where-Object number -eq 113).shownInDigest) "A caller-owned pull request must not consume a digest slot."
Assert-True (($digestControlResult.items | Where-Object number -eq 113).digestExclusionReasons -contains "excluded-author") "Caller exclusion must be explicit."
Assert-True (($digestControlResult.items | Where-Object number -eq 112).shownInDigest) "An eligible independent pull request should fill the digest."
Assert-True (($digestControlResult.items | Where-Object number -eq 112).stackDepth -eq 0) "A fork branch named main must not be treated as an upstream stack ancestor."
Assert-True ($digestControlResult.filter.excludeDigestAuthors -contains "current-user") "The resolved filter must echo digest author exclusions."

$digestControlMarkdown = & $scriptPath `
    -InputPath $correctnessFixturePath `
    -Now $snapshot `
    -Label area-stack `
    -ExcludeDigestAuthor current-user `
    -OutputFormat Markdown
$digestControlMarkdownText = $digestControlMarkdown -join [Environment]::NewLine
Assert-True ($digestControlMarkdownText.Contains("**Digest author exclusions:** current-user")) "Markdown must echo digest author exclusions."

$discussionJson = & $scriptPath `
    -InputPath $discussionFixturePath `
    -Now $snapshot `
    -Label area-discussion `
    -MaxReviewNow 5 `
    -OutputFormat Json
$discussionResult = $discussionJson | ConvertFrom-Json -Depth 100

Assert-True ($discussionResult.census.byBucket.ReviewNow -eq 7) "Discussion assessment must not rewrite deterministic classification."
Assert-True (($discussionResult.items | Where-Object number -eq 117).discussionAssessment.state -eq "verification-needed") "An author close-or-continue response must require discussion verification."
Assert-True (($discussionResult.items | Where-Object number -eq 117).discussionAssessment.signals -contains "author-disposition-mentioned") "Author disposition evidence must be explicit."
Assert-True (($discussionResult.items | Where-Object number -eq 117).reasonCodes -contains "needs-first-review") "An author disposition test must not require a formal review."
Assert-True (-not (($discussionResult.items | Where-Object number -eq 117).shownInDigest)) "An author disposition response must not be presented as ordinary review."
Assert-True (($discussionResult.items | Where-Object number -eq 117).shownInDiscussionVerification) "An author disposition response must surface for discussion verification."
Assert-True (($discussionResult.items | Where-Object number -eq 117).discussionAssessment.threads.unresolvedCount -eq 1) "Unresolved thread state must be surfaced as evidence."
Assert-True (($discussionResult.items | Where-Object number -eq 118).discussionAssessment.signals -contains "non-author-discussion-after-author-response") "Later owner feedback must require discussion verification."
Assert-True (-not (($discussionResult.items | Where-Object number -eq 118).shownInDigest)) "Later owner feedback must not be presented as ordinary review."
Assert-True (($discussionResult.items | Where-Object number -eq 118).discussionAssessment.comments[0].actor -eq "repository-member") "Repository-member feedback must be attributed."
Assert-True (($discussionResult.items | Where-Object number -eq 118).discussionAssessment.comments[0].kind -eq "actionable") "A polite prefix must not hide actionable feedback."
Assert-True (($discussionResult.items | Where-Object number -eq 119).discussionAssessment.signals -contains "current-inline-discussion-unassessed") "A current unresolved thread without comment evidence must require verification."
Assert-True (-not (($discussionResult.items | Where-Object number -eq 119).shownInDigest)) "Unread current inline feedback must not enter the unattended digest."
Assert-True (($discussionResult.items | Where-Object number -eq 120).discussionAssessment.state -eq "clear") "Explicit informational follow-up must not block ordinary review."
Assert-True (($discussionResult.items | Where-Object number -eq 120).shownInDigest) "An informational follow-up must not remove the candidate from the digest."
Assert-True (($discussionResult.items | Where-Object number -eq 121).discussionAssessment.signals -contains "discussion-incomplete") "Truncated discussion must be disclosed."
Assert-True (-not (($discussionResult.items | Where-Object number -eq 121).shownInDigest)) "Truncated discussion must not enter the unattended digest."
Assert-True (($discussionResult.items | Where-Object number -eq 122).discussionAssessment.state -eq "clear") "Resolved and outdated threads must remain a positive control."
Assert-True (($discussionResult.items | Where-Object number -eq 122).shownInDigest) "Resolved and outdated threads must not remove a normal roundtrip from the digest."
Assert-True (($discussionResult.items | Where-Object number -eq 123).discussionAssessment.signals -contains "non-author-discussion-requires-verification") "An initial owner concern without a formal review or author response must require verification."
Assert-True (-not (($discussionResult.items | Where-Object number -eq 123).shownInDigest)) "An initial owner concern must not enter the unattended digest."
Assert-True ($discussionResult.discussion.assessedCandidateCount -eq 7) "The bounded assessment count must be emitted."
Assert-True ($discussionResult.discussion.verificationNeededCount -eq 5) "The assessment summary must count verification-needed candidates."

$discussionMarkdown = & $scriptPath `
    -InputPath $discussionFixturePath `
    -Now $snapshot `
    -Label area-discussion `
    -OutputFormat Markdown
$discussionMarkdownText = $discussionMarkdown -join [Environment]::NewLine
Assert-True ($discussionMarkdownText.Contains("## Verify discussion before review")) "Markdown must separate discussion verification from ordinary review."
Assert-True ($discussionMarkdownText.Contains("[#117]")) "Markdown must surface author disposition evidence."

Write-Output "All PR attention queue tests passed."
