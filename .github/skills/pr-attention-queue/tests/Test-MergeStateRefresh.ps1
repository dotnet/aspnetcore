#!/usr/bin/env pwsh
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string[]]$Case,
    [string]$EvidenceDirectory,
    [string]$BaselineModulePath
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "MergeEligibilityTestHelpers.ps1")
$moduleOptions = @{}
if ($BaselineModulePath) { $moduleOptions.ModulePath = $BaselineModulePath }
$fixturePath = Join-Path $PSScriptRoot "fixtures\merge-state-refresh.json"
$results = [Collections.Generic.List[object]]::new()
$cases = @(
    @{ name = "clear"; kind = "regression"; eligibility = "eligible"; retries = 1; bucket = "ReadyToMerge"; reason = "approved"; actor = "merger" },
    @{ name = "unresolved-thread"; kind = "regression"; eligibility = "verification-needed"; retries = 1; bucket = "ReadyToMerge"; reason = "approved"; actor = "merger" },
    @{ name = "unknown-state"; kind = "control"; retries = 1; reason = "merge-state-not-clean" },
    @{ name = "missing-state"; kind = "control"; retries = 1; reason = "merge-state-not-clean" },
    @{ name = "null-state"; kind = "control"; retries = 1; reason = "merge-state-not-clean" },
    @{ name = "blocked"; kind = "control"; retries = 1; reason = "merge-state-not-clean" },
    @{ name = "behind"; kind = "regression"; retries = 1; bucket = "WaitingOnAuthor"; reason = "branch-update-required"; actor = "author/maintainer" },
    @{ name = "conflicting"; kind = "control"; retries = 1; bucket = "WaitingOnAuthor"; reason = "merge-conflict"; actor = "author" },
    @{ name = "unknown-mergeable"; kind = "control"; retries = 3; reason = "mergeability-unknown" },
    @{ name = "missing-mergeable"; kind = "control"; retries = 3; reason = "mergeability-unknown" },
    @{ name = "null-mergeable"; kind = "control"; reason = @("mergeability-unknown", "merge-state-not-clean") },
    @{ name = "missing-record"; kind = "control"; retries = 3; reason = "mergeability-unknown" },
    @{ name = "failed-retry"; kind = "control"; retries = 3; reason = "mergeability-unknown" },
    @{ name = "known-clean"; kind = "control"; eligibility = "eligible"; retries = 0; bucket = "ReadyToMerge"; reason = "approved"; actor = "merger" },
    @{ name = "known-unknown-state"; kind = "control"; retries = 0; reason = "merge-state-not-clean" },
    @{ name = "failed-checks"; kind = "control"; retries = 1; reason = "ci-failed"; actor = "author/CI investigation" },
    @{ name = "changes-requested"; kind = "control"; retries = 1; bucket = "WaitingOnAuthor"; reason = "changes-requested"; actor = "author" }
)
if ($Case) {
    foreach ($name in $Case) {
        if ($name -notin $cases.name) { throw "Unknown merge-state case '$name'." }
    }
    $cases = @($cases | Where-Object { $_.name -in $Case })
}

foreach ($scenario in $cases) {
    $fixture = Get-Content -Raw -LiteralPath $fixturePath | ConvertFrom-Json -Depth 30
    switch ($scenario.name) {
        "unresolved-thread" { $fixture.threads = @(@{ isResolved = $false; isOutdated = $false }) }
        "unknown-state" { $fixture.retryResponse.mergeStateStatus = "UNKNOWN" }
        "missing-state" { $fixture.retryResponse.PSObject.Properties.Remove("mergeStateStatus") }
        "null-state" { $fixture.retryResponse.mergeStateStatus = $null }
        "blocked" { $fixture.retryResponse.mergeStateStatus = "BLOCKED" }
        "behind" { $fixture.retryResponse.mergeStateStatus = "BEHIND" }
        "conflicting" { $fixture.retryResponse.mergeable = "CONFLICTING"; $fixture.retryResponse.mergeStateStatus = "DIRTY" }
        "unknown-mergeable" { $fixture.retryResponse.mergeable = "UNKNOWN" }
        "missing-mergeable" { $fixture.retryResponse.PSObject.Properties.Remove("mergeable") }
        "null-mergeable" { $fixture.retryResponse.mergeable = $null }
        "missing-record" { $fixture.retryResponse = $null }
        "failed-retry" { $fixture | Add-Member failRetry $true }
        "known-clean" { $fixture.mergeable = "MERGEABLE"; $fixture.mergeStateStatus = "CLEAN" }
        "known-unknown-state" { $fixture.mergeable = "MERGEABLE" }
        "failed-checks" { $fixture | Add-Member checkState "FAILURE" }
        "changes-requested" { $fixture | Add-Member reviewDecision "CHANGES_REQUESTED"; $fixture.reviews[0].state = "CHANGES_REQUESTED" }
    }
    $row = [ordered]@{ name = $scenario.name; kind = $scenario.kind; passed = $false; error = $null }
    try {
        $run = Invoke-MergeFixtureQueue -PullRequests @($fixture) @moduleOptions
        $queue = $run.output | ConvertFrom-Json -Depth 50
        $item = $queue.items[0]
        $row.bucket = $item.bucket
        $row.nextActor = $item.nextActor
        $row.mergeEligibility = $item.mergeEligibility
        $row.shownInDigest = $item.shownInDigest
        $row.shownInMergeVerification = $item.shownInMergeVerification
        $row.reasonCodes = @($item.reasonCodes)
        $row.unresolvedMergeable = $queue.census.unresolvedMergeable
        $row.detailNumbers = $run.detailNumbers
        $row.retryNumbers = $run.retryNumbers
        $row.discussionNumbers = $run.discussionNumbers
        if ($EvidenceDirectory) {
            $run.output | Set-Content -LiteralPath (Join-Path $EvidenceDirectory "$($scenario.name)-queue.json")
            $run.requests | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $EvidenceDirectory "$($scenario.name)-requests.json")
        }

        # Assert material output before inspecting the retry projection or coverage.
        if ($scenario.eligibility) {
            if ($item.bucket -ne "ReadyToMerge" -or $item.mergeEligibility -ne $scenario.eligibility) {
                throw "Expected ReadyToMerge/$($scenario.eligibility); actual $($item.bucket)/$($item.mergeEligibility) ($($item.reasonCodes -join ','))."
            }
            $eligible = $scenario.eligibility -eq "eligible"
            if ($item.shownInDigest -ne $eligible -or $item.shownInMergeVerification -eq $eligible) {
                throw "Expected only the $(if ($eligible) { 'ready' } else { 'verification' }) merge lane."
            }
            if ($run.discussionNumbers.Count -ne 1 -or -not $item.discussionAssessment.complete) {
                throw "Expected the real bounded discussion producer to assess the candidate completely."
            }
            if (-not $eligible -and $item.discussionAssessment.signals -notcontains "current-inline-discussion-unassessed") {
                throw "The current unresolved thread must supply the verification signal."
            }
        }
        elseif ($item.bucket -eq "ReadyToMerge" -or $item.mergeEligibility -ne "not-candidate" -or $item.shownInMergeVerification) {
            throw "Incomplete or blocked merge facts must not grant merge clearance."
        }

        $expectedBucket = $scenario.bucket ?? "WaitingOnCI"
        $expectedActor = $scenario.actor ?? "CI/automation"
        if ($item.bucket -ne $expectedBucket -or $item.nextActor -ne $expectedActor -or
            @($item.reasonCodes | Where-Object { $_ -in $scenario.reason }).Count -eq 0) {
            throw "Expected $expectedBucket/$expectedActor with $($scenario.reason -join ' or '); actual $($item.bucket)/$($item.nextActor) ($($item.reasonCodes -join ','))."
        }
        if ($run.detailNumbers.Count -ne 1 -or $run.retryNumbers.Count -gt 3 -or
            ($null -ne $scenario.retries -and $run.retryNumbers.Count -ne $scenario.retries)) {
            throw "Expected one detail query and $($scenario.retries) retries; actual $($run.detailNumbers.Count)/$($run.retryNumbers.Count)."
        }
        if (-not $scenario.eligibility -and $run.discussionNumbers.Count -ne 0) {
            throw "Non-candidates must not consume merge discussion collection."
        }
        $row.passed = $true
        Write-Output "PASS $($scenario.kind) $($scenario.name)"
    }
    catch {
        $row.error = $_.Exception.Message
        Write-Output "FAIL $($scenario.kind) $($scenario.name): $($row.error)"
    }
    $results.Add([pscustomobject]$row)
}
if ($EvidenceDirectory) {
    $results | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $EvidenceDirectory "matrix.json")
}
$failed = @($results | Where-Object { -not $_.passed })
Write-Output "Merge state refresh: $($results.Count - $failed.Count)/$($results.Count) passed; $($failed.Count) failed."
if ($failed.Count -gt 0) { throw "$($failed.Count) merge-state refresh cases failed." }
