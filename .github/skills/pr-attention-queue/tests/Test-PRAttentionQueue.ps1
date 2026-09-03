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
Assert-True ($defaultResult.census.matched -eq 11) "The Blazor preset should match eleven fixture PRs."
Assert-True ($defaultResult.census.pathOnly -eq 1) "One unlabeled PR should match only by Components path."
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
Assert-True ($markdownText.Contains("**Overflow:**")) "Markdown must report digest overflow."
Assert-True (-not $markdownText.Contains("@community-user")) "Markdown must not mention contributors."

Write-Output "All PR attention queue tests passed."
