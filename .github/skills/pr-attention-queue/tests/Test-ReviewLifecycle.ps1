# Invoked by the existing harness. Candidate grouping deliberately does not interpret comment text.
Import-Module -Scope Local -Force $modulePath
$candidateModule = Get-Module PRAttentionQueue
$candidateSettings = (Get-Content (Join-Path $skillRoot "presets.json") -Raw | ConvertFrom-Json).settings
$candidateRaw = Get-Content (Join-Path $PSScriptRoot "fixtures/review-lifecycle.json") -Raw
$candidateDisplay = & $candidateModule { Get-DisplayMetadata }
$candidateChecks = 0

function Sync-CandidateRecord($Pr) {
    $d = $Pr.lifecycleDetails
    $Pr.latestReviews = @($d.reviews.nodes)
    $Pr.comments = @($d.comments.nodes)
    $Pr.lifecycleDiscussion = $d | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100
}

function New-CandidateRecord {
    $d = $candidateRaw | ConvertFrom-Json -Depth 100
    $pr = [pscustomobject]@{
        number = 800; title = $d.title; url = $d.url; author = $d.author
        createdAt = "2026-08-30T10:00:00Z"; updatedAt = "2026-09-01T12:00:00Z"
        headRefOid = $d.headRefOid; isDraft = $false
        labels = @([pscustomobject]@{ name = "area-blazor" }); files = @(); changedFiles = 0
        mergeable = $d.mergeable; mergeStateStatus = $d.mergeStateStatus; reviewDecision = $d.reviewDecision
        latestReviews = @(); comments = @()
        reviewRequests = @([pscustomobject]@{ login = "reviewer"; requestedAt = "2026-09-01T12:30:00Z" })
        statusCheckRollup = @([pscustomobject]@{ state = "SUCCESS" })
        lifecycleDetails = $d; lifecycleDiscussion = $null
    }
    Sync-CandidateRecord $pr
    return $pr
}

function Clear-CandidateFeedback($Pr) {
    foreach ($name in @("reviews", "comments", "reviewThreads")) {
        $Pr.lifecycleDetails.$name.nodes = @()
        $Pr.lifecycleDetails.$name.totalCount = 0
    }
    Sync-CandidateRecord $Pr
}

function Add-CandidateComment($Pr, $Login, $Time, $Body) {
    $Pr.lifecycleDetails.comments.nodes += [pscustomobject]@{
        id = "I$($Pr.lifecycleDetails.comments.nodes.Count)"
        author = [pscustomobject]@{ __typename = "User"; login = $Login }
        createdAt = $Time; body = $Body; url = "$($Pr.url)#issuecomment-$($Pr.lifecycleDetails.comments.nodes.Count)"
    }
    $Pr.lifecycleDetails.comments.totalCount++
    Sync-CandidateRecord $Pr
}

function Assert-Candidate($Pr, $Status, $Group, $Message) {
    $actual = & $candidateModule {
        param($pr, $settings, $now)
        $classification = Get-Classification $pr @(Get-LabelNames $pr) (Get-AuthorInfo $pr) $settings $now
        $item = [pscustomobject]@{
            bucket = $classification.Bucket; nextActor = $classification.NextActor
            reasonCodes = $classification.ReasonCodes; digestExclusionReasons = @(); checkState = $classification.CheckState
        }
        Get-ReviewLifecycleAssessment $pr $item $settings
    } $Pr $candidateSettings $snapshot
    Assert-True ($actual.status -eq $Status -and $actual.group -eq $Group) "$Message`: $($actual | ConvertTo-Json -Depth 20 -Compress)"
    Assert-True ($null -eq $actual.PSObject.Properties["nextActor"]) "Candidate data must not infer a next actor."
    Assert-True ($null -eq $actual.evidence.PSObject.Properties["responses"]) "Semantic hand-back outcomes must be removed."
    Assert-True ($null -ne $candidateDisplay.reviewLifecycle.statuses.PSObject.Properties[$Status]) "Candidate status metadata must exist."
    foreach ($reason in @($actual.reasons) + @($actual.caveats)) {
        Assert-True ($null -ne $candidateDisplay.reviewLifecycle.reasons.PSObject.Properties[$reason] -or
            $null -ne $candidateDisplay.reasonCodes.PSObject.Properties[$reason]) "Candidate reason $reason must have display metadata."
    }
    if ($Status -eq "verification-needed") {
        Assert-True ($actual.primaryUncertaintyReason -and $actual.reasons -contains $actual.primaryUncertaintyReason) "Uncertainty requires a retained cause."
    }
    $script:candidateChecks++
    return $actual
}

foreach ($body in @("Done.", "Thanks!", "I'll add that tomorrow.", "> I'll add that tomorrow.`n`nThanks!",
    'The reviewer said "I will fix this".', "Perhaps the existing test is sufficient?", "Ready for another look.",
    "Resolved conflicts.", '```text Please close this PR.```', "", $null)) {
    $p = New-CandidateRecord
    $p.lifecycleDetails.reviewThreads.nodes[0].comments.nodes[1].body = $body
    Sync-CandidateRecord $p
    $a = Assert-Candidate $p "grouped" "review-follow-up-candidates" "Reply wording must not change observable candidate grouping"
    Assert-True ($a.reasons -contains "author-activity-after-feedback") "The reason must describe activity, not fulfillment."
    Assert-True (($a.evidence.events | Where-Object id -eq "C2").body -ceq $body) "Original quotes and body text must remain intact."
}

$p = New-CandidateRecord
Clear-CandidateFeedback $p
Add-CandidateComment $p "contributor" "2026-09-01T12:00:00Z" "I'll add the tests tomorrow."
$null = Assert-Candidate $p "grouped" "initial-review-candidates" "Author wording alone is not human reviewer feedback or an inferred obligation"
Add-CandidateComment $p "contributor" "2026-09-01T13:00:00Z" "Thanks!"
$null = Assert-Candidate $p "grouped" "initial-review-candidates" "Acknowledgment must not erase or fulfill an inferred commitment because no commitment is inferred"

foreach ($body in @("Thanks!", "> Please fix this.", "Unrelated administrative note: milestone updated.", "@contributor, please provide a normal-path reproducer.")) {
    $p = New-CandidateRecord
    Clear-CandidateFeedback $p
    Add-CandidateComment $p "reviewer" "2026-09-01T13:00:00Z" $body
    $a = Assert-Candidate $p "not-grouped" $null "All non-author human top-level discussion counts without semantic filtering"
    Assert-True ($a.evidence.humanFeedbackCount -eq 1) "Zero submitted human reviews cannot erase top-level feedback."
    Add-CandidateComment $p "contributor" "2026-09-01T14:00:00Z" "Thanks!"
    $null = Assert-Candidate $p "grouped" "review-follow-up-candidates" "A later author comment is candidate activity, not a claim that feedback was addressed"
}

$p = New-CandidateRecord
Clear-CandidateFeedback $p
Add-CandidateComment $p "reviewer" "2026-09-01T13:00:00Z" "Thanks!"
foreach ($i in 1..15) {
    Add-CandidateComment $p "policy" "2026-09-01T14:00:00Z" "Notification"
    $p.lifecycleDetails.comments.nodes[-1].author.__typename = "Bot"
}
Sync-CandidateRecord $p
$a = Assert-Candidate $p "not-grouped" $null "Human feedback beyond the ten-comment preview must not be lost"
Assert-True ($a.evidence.events.Count -eq 16 -and $a.evidence.humanFeedbackCount -eq 1) "Assessment uses all normalized events, not preview excerpts."

foreach ($variant in @("inline-only", "newer-inline", "edited-feedback", "partial-reply", "resolved", "outdated",
    "wrong-reply-target", "wrong-reply-author", "pending-reply", "missing-publication", "unknown-inline-actor",
    "truncated-comments", "truncated-threads", "truncated-reviews", "missing-count", "missing-source",
    "head-mismatch", "draft-mismatch", "discussion-head", "discussion-review-count", "source-error",
    "bot-only", "self-only", "deleted-reviewer", "service-author", "outside-zero", "outside-thread-headers",
    "changed-sha-no-date", "author-without-head-date", "pending-private-review", "unknown-checks", "null-nodes", "commit-head-mismatch")) {
    $p = New-CandidateRecord
    $d = $p.lifecycleDetails
    $status = "grouped"; $group = "review-follow-up-candidates"
    switch ($variant) {
        "inline-only" { $d.reviews.nodes = @(); $d.reviews.totalCount = 0 }
        "newer-inline" {
            $c = $d.reviewThreads.nodes[0].comments.nodes[0] | ConvertTo-Json -Depth 20 | ConvertFrom-Json
            $c.id = "C3"; $c.createdAt = "2026-09-01T13:00:00Z"
            $d.reviewThreads.nodes[0].comments.nodes += $c; $d.reviewThreads.nodes[0].comments.totalCount++
            $status = "not-grouped"; $group = $null
        }
        "edited-feedback" {
            $d.reviewThreads.nodes[0].comments.nodes[0] | Add-Member updatedAt "2026-09-01T13:00:00Z"
            $status = "not-grouped"; $group = $null
        }
        "partial-reply" {
            $t = $d.reviewThreads.nodes[0] | ConvertTo-Json -Depth 20 | ConvertFrom-Json
            $t.id = "T2"; $t.comments.nodes = @($t.comments.nodes[0]); $t.comments.totalCount = 1
            $d.reviewThreads.nodes += $t; $d.reviewThreads.totalCount++
        }
        "resolved" { $d.reviewThreads.nodes[0].isResolved = $true }
        "outdated" { $d.reviewThreads.nodes[0].isOutdated = $true }
        "wrong-reply-target" { $d.reviewThreads.nodes[0].comments.nodes[1].replyTo.id = "another-comment" }
        "wrong-reply-author" { $d.reviewThreads.nodes[0].comments.nodes[1].author.login = "another-reviewer"; $status = "not-grouped"; $group = $null }
        "pending-reply" {
            $d.reviewThreads.nodes[0].comments.nodes[1].state = "PENDING"
            $d.reviewThreads.nodes[0].comments.nodes[1].pullRequestReview.state = "PENDING"
            $d.commits.nodes[0].commit.committedDate = "2026-09-01T09:00:00Z"; $status = "not-grouped"; $group = $null
        }
        "missing-publication" { $d.reviewThreads.nodes[0].comments.nodes[1].state = $null; $status = "verification-needed"; $group = $null }
        "unknown-inline-actor" { $d.reviewThreads.nodes[0].comments.nodes[0].author = $null; $status = "verification-needed"; $group = $null }
        "truncated-comments" { $d.reviewThreads.nodes[0].comments.pageInfo.hasPreviousPage = $true; $status = "verification-needed"; $group = $null }
        "truncated-threads" { $d.reviewThreads.pageInfo.hasPreviousPage = $true; $status = "verification-needed"; $group = $null }
        "truncated-reviews" { $d.reviews.pageInfo.hasPreviousPage = $true; $status = "verification-needed"; $group = $null }
        "missing-count" { $d.comments.PSObject.Properties.Remove("totalCount"); $status = "verification-needed"; $group = $null }
        "missing-source" { $d.reviewThreads = $null; $status = "verification-needed"; $group = $null }
        "head-mismatch" { $d.headRefOid = "other-head"; $status = "verification-needed"; $group = $null }
        "draft-mismatch" { $d.isDraft = $true; $status = "verification-needed"; $group = $null }
        "discussion-head" { $status = "verification-needed"; $group = $null }
        "discussion-review-count" { $status = "verification-needed"; $group = $null }
        "source-error" { $p | Add-Member lifecycleDiscussionError "Deliberate source error"; $status = "verification-needed"; $group = $null }
        "bot-only" {
            $d.reviews.nodes[0].author.__typename = "Bot"; $d.reviews.nodes[0].author.login = "Copilot"
            $d.reviewThreads.nodes[0].comments.nodes[0].author.__typename = "Bot"; $d.reviewThreads.nodes[0].comments.nodes[0].author.login = "Copilot"
            $group = "initial-review-candidates"
        }
        "self-only" {
            $d.reviews.nodes[0].author.login = "contributor"
            $d.reviewThreads.nodes[0].comments.nodes[0].author.login = "contributor"
            $group = "initial-review-candidates"
        }
        "deleted-reviewer" { $d.reviews.nodes[0].author = $null; $status = "verification-needed"; $group = $null }
        "service-author" { Clear-CandidateFeedback $p; $d.author.login = "dotnet-bot"; $status = "verification-needed"; $group = $null }
        "outside-zero" { Clear-CandidateFeedback $p; $group = "initial-review-candidates" }
        "outside-thread-headers" { $d.reviewThreads.nodes[0].PSObject.Properties.Remove("comments"); $status = "verification-needed"; $group = $null }
        "changed-sha-no-date" {
            $d.reviewThreads.nodes[0].comments.nodes = @($d.reviewThreads.nodes[0].comments.nodes[0]); $d.reviewThreads.nodes[0].comments.totalCount = 1
            $d.commits.nodes[0].commit.committedDate = $null; $status = "verification-needed"; $group = $null
        }
        "author-without-head-date" { $d.commits.nodes[0].commit.committedDate = $null }
        "pending-private-review" {
            Clear-CandidateFeedback $p
            $r = ($candidateRaw | ConvertFrom-Json -Depth 100).reviews.nodes[0]; $r.state = "PENDING"; $r.submittedAt = $null; $r.author = $null
            $d.reviews.nodes = @($r); $d.reviews.totalCount = 1; $group = "initial-review-candidates"
        }
        "unknown-checks" { $p.statusCheckRollup = @(); $status = "verification-needed"; $group = $null }
        "null-nodes" { Clear-CandidateFeedback $p; $d.reviewThreads.nodes = $null; $status = "verification-needed"; $group = $null }
        "commit-head-mismatch" { $d.commits.nodes[0].commit.oid = "other-head"; $status = "verification-needed"; $group = $null }
    }
    Sync-CandidateRecord $p
    if ($variant -eq "discussion-head") { $p.lifecycleDiscussion.headRefOid = "other-head" }
    if ($variant -eq "discussion-review-count") { $p.lifecycleDiscussion.reviews.totalCount++ }
    if ($variant -in @("outside-zero", "outside-thread-headers")) { $p.lifecycleDiscussion = $null }
    $null = Assert-Candidate $p $status $group "Observable source case $variant"
}

foreach ($state in @("COMMENTED", "APPROVED", "CHANGES_REQUESTED", "DISMISSED")) {
    $p = New-CandidateRecord
    $p.lifecycleDetails.reviews.nodes[0].state = $state
    Sync-CandidateRecord $p
    $null = Assert-Candidate $p "grouped" "review-follow-up-candidates" "Published $state review is historical feedback, not first review"
}

foreach ($variant in @("human-request", "author-request", "team-request", "before-feedback", "unknown-requester", "bot-requester", "missing-timeline")) {
    $p = New-CandidateRecord
    Clear-CandidateFeedback $p
    $r = ($candidateRaw | ConvertFrom-Json -Depth 100).reviews.nodes[0]; $r.state = "APPROVED"; $r.commit.oid = $p.headRefOid
    $p.lifecycleDetails.reviews.nodes = @($r); $p.lifecycleDetails.reviews.totalCount = 1
    $p.lifecycleDetails.commits.nodes[0].commit.committedDate = "2026-09-01T09:00:00Z"
    $e = [pscustomobject]@{
        __typename = "ReviewRequestedEvent"; id = "E1"; createdAt = "2026-09-01T13:00:00Z"
        actor = [pscustomobject]@{ __typename = "User"; login = "reviewer" }
        requestedReviewer = [pscustomobject]@{ __typename = "User"; login = "second-reviewer" }
    }
    $status = "grouped"; $group = "review-follow-up-candidates"
    switch ($variant) {
        "author-request" { $e.actor.login = "contributor" }
        "team-request" { $e.requestedReviewer = [pscustomobject]@{ __typename = "Team"; name = "review-team" } }
        "before-feedback" { $e.createdAt = "2026-09-01T09:00:00Z"; $status = "not-grouped"; $group = $null }
        "unknown-requester" { $e.actor = $null; $status = "verification-needed"; $group = $null }
        "bot-requester" { $e.actor.__typename = "Bot"; $e.actor.login = "automation"; $status = "not-grouped"; $group = $null }
        "missing-timeline" { $status = "verification-needed"; $group = $null }
    }
    $p.lifecycleDetails.timelineItems.nodes = @($e)
    $p.lifecycleDetails.timelineItems.totalCount = 99
    $p.lifecycleDetails.timelineItems.filteredCount = 1
    if ($variant -eq "missing-timeline") { $p.lifecycleDetails.timelineItems = $null }
    Sync-CandidateRecord $p
    $a = Assert-Candidate $p $status $group "Additional review request case $variant"
    if ($group) { Assert-True ($a.reasons -contains "review-request-after-feedback") "An additional request is a follow-up candidate, not merge readiness." }
}

foreach ($gate in @("CLEAN", "UNKNOWN", "BLOCKED", "BEHIND", "draft", "conflict", "failed", "pending", "unknown-checks", "missing-rollup", "design", "blocked-label", "author", "rescue", "pending-review")) {
    $p = New-CandidateRecord
    Clear-CandidateFeedback $p
    $p.reviewDecision = "APPROVED"; $p.lifecycleDetails.reviewDecision = "APPROVED"
    $status = "blocked"; $group = $null
    switch ($gate) {
        { $_ -in @("CLEAN", "UNKNOWN", "BLOCKED", "BEHIND") } {
            $p.mergeStateStatus = $gate; $p.lifecycleDetails.mergeStateStatus = $gate
            if ($gate -in @("CLEAN", "UNKNOWN")) { $status = "grouped"; $group = "merge-candidates" }
        }
        "draft" { $p.isDraft = $true; $p.lifecycleDetails.isDraft = $true }
        "conflict" { $p.mergeable = "CONFLICTING"; $p.lifecycleDetails.mergeable = "CONFLICTING" }
        "failed" { $p.statusCheckRollup[0].state = "FAILURE" }
        "pending" { $p.statusCheckRollup[0].state = "PENDING" }
        "unknown-checks" { $p.statusCheckRollup = @(); $status = "verification-needed" }
        "missing-rollup" { $p.lifecycleDetails.commits.nodes[0].commit.statusCheckRollup = $null; $status = "verification-needed" }
        "design" { $p.labels += [pscustomobject]@{ name = "needs-design" } }
        "blocked-label" { $p.labels += [pscustomobject]@{ name = "do-not-merge" } }
        "author" { $p = New-CandidateRecord; $p.lifecycleDetails.reviews.nodes[0].commit.oid = $p.headRefOid }
        "rescue" { $p.reviewDecision = "REVIEW_REQUIRED"; $p.lifecycleDetails.reviewDecision = "REVIEW_REQUIRED"; $p.createdAt = "2026-06-01T10:00:00Z"; $p.reviewRequests = @() }
        "pending-review" {
            $p.reviewDecision = "REVIEW_REQUIRED"; $p.lifecycleDetails.reviewDecision = "REVIEW_REQUIRED"
            $p.statusCheckRollup[0].state = "PENDING"; $status = "grouped"; $group = "initial-review-candidates"
        }
    }
    Sync-CandidateRecord $p
    $a = Assert-Candidate $p $status $group "Existing gate $gate"
    if ($gate -eq "UNKNOWN") { Assert-True ($a.caveats -contains "merge-state-unknown") "UNKNOWN is a visible unresolved merge gate." }
    if ($gate -eq "pending-review") { Assert-True ($a.evidence.checkState -eq "Pending" -and $a.caveats -contains "checks-pending") "Concurrent review does not imply green CI." }
}

# Exercise the production collectors without another service, a live query or semantic fixture labels.
$hydrated = & $candidateModule {
    param($raw, $settings, $now)
    $queries = [System.Collections.Generic.List[string]]::new()
    function Invoke-GhJson {
        param([string[]]$Arguments)
        $query = @($Arguments | Where-Object { $_ -like "query=*" })[0]; $queries.Add($query)
        $aliases = [regex]::Matches($query, 'pr(\d+): pullRequest')
        if ($aliases.Count -gt 5) { throw "At most five PRs per richer batch." }
        if ($query -notmatch 'headRefOid' -or $query -notmatch 'isDraft' -or $query -notmatch '__typename') { throw "Typed identity is required." }
        $repository = @{}
        foreach ($alias in $aliases) {
            $detail = $raw | ConvertFrom-Json -Depth 100; $detail.number = [int]$alias.Groups[1].Value
            if ($query -notmatch 'comments\(last: 20\)') {
                if ($query -notmatch 'filteredCount' -or $query -notmatch 'reviews\(last: 50\)\s*\{\s*totalCount\s*pageInfo') { throw "History completeness is required." }
                foreach ($thread in $detail.reviewThreads.nodes) { $thread.PSObject.Properties.Remove("comments") }
            }
            elseif ($query -notmatch 'replyTo \{ id \}' -or $query -notmatch 'pullRequestReview \{ id state submittedAt url \}') { throw "Publication metadata is required." }
            $repository["pr$($detail.number)"] = $detail
        }
        return [pscustomobject]@{ data = [pscustomobject]@{ repository = [pscustomobject]$repository } }
    }
    $candidates = @(foreach ($number in 901..906) {
        [pscustomobject]@{ PullRequest = [pscustomobject]@{
            number = $number; headRefOid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; isDraft = $false
            author = [pscustomobject]@{ login = "contributor" }; createdAt = "2026-08-30T10:00:00Z"; updatedAt = "2026-09-01T12:00:00Z"
        } }
    })
    Add-PullRequestDetails "dotnet/aspnetcore" $candidates
    Add-DiscussionEvidenceDetails "dotnet/aspnetcore" $candidates
    $p = $candidates[0].PullRequest
    $c = Get-Classification $p @("area-blazor") (Get-AuthorInfo $p) $settings $now
    $item = [pscustomobject]@{ bucket = $c.Bucket; reasonCodes = $c.ReasonCodes; checkState = $c.CheckState; digestExclusionReasons = @() }
    [pscustomobject]@{
        calls = $queries.Count; assessment = Get-ReviewLifecycleAssessment $p $item $settings
        legacy = Get-DiscussionAssessment $p (Get-AuthorInfo $p) @($settings.knownBotPatterns)
    }
} $candidateRaw $candidateSettings $snapshot
Assert-True ($hydrated.calls -eq 4 -and $hydrated.assessment.group -eq "review-follow-up-candidates") "Real bounded collector normalization must produce candidate data."
Assert-True ($hydrated.legacy.State -eq "verification-needed") "Candidate grouping does not change the legacy discussion veto."

$failedDiscussion = & $candidateModule {
    function Invoke-GhJson { throw "Deliberate lifecycle-only discussion failure." }
    $candidate = [pscustomobject]@{ PullRequest = [pscustomobject]@{ number = 999 } }
    Add-DiscussionEvidenceDetails "dotnet/aspnetcore" @($candidate)
    $candidate.PullRequest
}
Assert-True ($failedDiscussion.lifecycleDiscussionError -like "*Deliberate*" -and -not $failedDiscussion.discussionThreadsComplete) "Lifecycle-only failures remain explicit incomplete evidence."
foreach ($number in @(111, 113)) {
    $item = $digestControlResult.items | Where-Object number -eq $number
    Assert-True ($item.reviewLifecycle.status -eq "blocked" -and $null -eq $item.reviewLifecycle.group) "Explicit author/stack exclusion $number remains effective."
}

$population = @(foreach ($number in 801..825) {
    $p = New-CandidateRecord; Clear-CandidateFeedback $p; $p.number = $number
    $p.lifecycleDiscussion = $null
    $p
})
$populationPath = Join-Path ([System.IO.Path]::GetTempPath()) "pr-attention-candidates-$PID.json"
try {
    $population | ConvertTo-Json -Depth 100 | Set-Content $populationPath
    $full = Invoke-PRAttentionQueue -InputPath $populationPath -Now $snapshot -DisablePersonalInbox -OutputFormat Json | ConvertFrom-Json -Depth 100
    $personal = Invoke-PRAttentionQueue -InputPath $populationPath -Now $snapshot -PersonalLogin "another-user" -OutputFormat Json | ConvertFrom-Json -Depth 100
    Assert-True (($full.reviewLifecycle | ConvertTo-Json -Depth 30 -Compress) -ceq ($personal.reviewLifecycle | ConvertTo-Json -Depth 30 -Compress)) "Personal identity cannot alter general candidate groups."
    Assert-True ($full.reviewLifecycle.kind -eq "review-candidates") "The unpublished candidate extension must identify its new semantics."
    Assert-True (($full.reviewLifecycle.groupOrder -join ",") -eq "merge-candidates,review-follow-up-candidates,initial-review-candidates") "Preserve group priority, not one oldest-first list."
    $group = $full.reviewLifecycle.groups | Where-Object id -eq "initial-review-candidates"
    Assert-True ($group.count -eq 25 -and $group.numbers.Count -eq 25) "Candidate inventories have no digest or per-author cap."
    Assert-True ($full.reviewLifecycle.coverage.reviewWork.denominator -eq 25 -and $full.reviewLifecycle.coverage.reviewWork.outsideDiscussionBudget.grouped -eq 5) "Complete absence beyond twenty remains visible in the fixed denominator."
    Assert-True (@($full.items | Where-Object shownInDigest).Count -le 2) "Legacy per-author cap stays unchanged."
    foreach ($i in 0..24) {
        $item = $full.items | Where-Object number -eq $group.numbers[$i]
        Assert-True ($item.reviewLifecycle.rank -eq $i + 1 -and $item.number -eq 801 + $i) "Ranks and tie breakers remain stable and contiguous."
    }
    foreach ($p in $population) { $p.lifecycleDetails = $null; $p.lifecycleDiscussion = $null }
    $population | ConvertTo-Json -Depth 100 | Set-Content $populationPath
    $unknown = Invoke-PRAttentionQueue -InputPath $populationPath -Now $snapshot -DisablePersonalInbox -OutputFormat Json | ConvertFrom-Json -Depth 100
    Assert-True ($unknown.reviewLifecycle.coverage.inventory.verificationNeeded -eq 25 -and $unknown.reviewLifecycle.coverage.inventory.grouped -eq 0) "Unknown is not initial review."
    Assert-True (@($unknown.items | Where-Object shownInDigest).Count -gt 0) "New uncertainty does not rewrite legacy digest semantics."
    $population[0] = New-CandidateRecord; Clear-CandidateFeedback $population[0]; $population[0].number = 801
    $population[1] = New-CandidateRecord; $population[1].number = 802
    $population[2] = New-CandidateRecord; Clear-CandidateFeedback $population[2]; $population[2].number = 803
    Add-CandidateComment $population[2] "reviewer" "2026-09-01T13:00:00Z" "Thanks!"
    $population[3] = New-CandidateRecord; Clear-CandidateFeedback $population[3]; $population[3].number = 804
    $population[3].reviewDecision = "APPROVED"; $population[3].lifecycleDetails.reviewDecision = "APPROVED"
    $population[3].mergeStateStatus = "UNKNOWN"; $population[3].lifecycleDetails.mergeStateStatus = "UNKNOWN"
    Sync-CandidateRecord $population[3]
    $population | ConvertTo-Json -Depth 100 | Set-Content $populationPath
    $mixed = Invoke-PRAttentionQueue -InputPath $populationPath -Now $snapshot -DisablePersonalInbox -OutputFormat Json | ConvertFrom-Json -Depth 100
    $c = $mixed.reviewLifecycle.coverage.inventory
    Assert-True ($c.denominator -eq 25 -and $c.grouped -eq 3 -and $c.notGrouped -eq 1 -and $c.verificationNeeded -eq 21) "Coverage counts describe grouping, not confidence or established actors."
    Assert-True ($null -eq $c.PSObject.Properties["confidentlyRouted"] -and $null -eq $c.PSObject.Properties["establishedOtherNextActor"]) "Remove certification and inferred-actor coverage claims."
    Assert-True ($mixed.reviewLifecycle.coverage.mergeWork.denominator -eq 1 -and $mixed.reviewLifecycle.coverage.mergeWork.grouped -eq 1) "UNKNOWN merge candidates belong in separate merge coverage."
    Assert-True (($mixed.items | Where-Object number -eq 804).bucket -eq "WaitingOnCI") "UNKNOWN never becomes legacy ReadyToMerge."
    Assert-True (($mixed.reviewLifecycle.statusCounts.PSObject.Properties.Value | Measure-Object -Sum).Sum -eq 25) "All statuses reconcile."
    Assert-True (($c.primaryUncertaintyReasons.PSObject.Properties.Value | Measure-Object -Sum).Sum -eq 21) "One primary uncertainty cause per uncertain PR."
    foreach ($source in $c.sources.PSObject.Properties.Value) {
        Assert-True (($source.PSObject.Properties.Value | Measure-Object -Sum).Sum -eq 25) "Each source reconciles every inventory record."
    }
    $references = @($mixed.reviewLifecycle.groups | ForEach-Object numbers)
    Assert-True (($references -join ",") -eq "804,802,801" -and @($references | Select-Object -Unique).Count -eq $references.Count) "Ordered groups retain unique references into items."
    "[]" | Set-Content $populationPath
    $empty = Invoke-PRAttentionQueue -InputPath $populationPath -Now $snapshot -DisablePersonalInbox -OutputFormat Json | ConvertFrom-Json -Depth 100
    Assert-True ($empty.reviewLifecycle.coverage.inventory.denominator -eq 0 -and $null -eq $empty.reviewLifecycle.coverage.inventory.groupedFraction) "Empty cohorts have no grouping percentage."
}
finally { if (Test-Path $populationPath) { Remove-Item $populationPath } }

if ($env:PR_ATTENTION_QUEUE_REPLAY_PATH) {
    $saved = Get-Content $env:PR_ATTENTION_QUEUE_REPLAY_PATH -Raw | ConvertFrom-Json -Depth 100
    Assert-True ($saved.repository -eq "dotnet/aspnetcore") "Saved replay must be scoped to dotnet/aspnetcore."
    $replayedItems = @(
        foreach ($record in $saved.records) {
            $record.item.reviewLifecycle = & $candidateModule {
                param($pr, $item, $settings)
                Get-ReviewLifecycleAssessment $pr $item $settings
            } $record.pullRequest $record.item $candidateSettings
            $record.item
        }
    )
    foreach ($number in @(67970, 66968)) {
        $item = $replayedItems | Where-Object number -eq $number
        Assert-True ($item -and $item.reviewLifecycle.group -eq "review-follow-up-candidates") "Saved approved example #$number must be an observable follow-up candidate."
    }
    $replayedIndex = & $candidateModule {
        param($items)
        Get-ReviewLifecycleIndex -Items $items -ReviewNumbers @($items | Where-Object bucket -eq "ReviewNow" | ForEach-Object number) `
            -DiscussionNumbers @($items | ForEach-Object number) -MergeNumbers @()
    } $replayedItems
    if ($env:PR_ATTENTION_QUEUE_REPLAY_OUTPUT) {
        [pscustomobject]@{
            kind = "saved-evidence-replay"; repository = $saved.repository; sourceSnapshotAt = $saved.snapshotAt
            reviewLifecycle = $replayedIndex; items = $replayedItems
        } | ConvertTo-Json -Depth 100 | Set-Content $env:PR_ATTENTION_QUEUE_REPLAY_OUTPUT
    }
    Write-Output "Saved evidence replay passed: $($replayedItems.Count) records; #67970 and #66968 are follow-up candidates."
}

if ($env:PR_ATTENTION_QUEUE_BASELINE_MODULE) {
    function Get-LegacyProjection($Value) {
        $Value.PSObject.Properties.Remove("reviewLifecycle")
        $Value.display.PSObject.Properties.Remove("reviewLifecycle")
        foreach ($item in $Value.items) { $item.PSObject.Properties.Remove("reviewLifecycle") }
        $Value.PSObject.Properties.Remove("timing")
        $Value.personal.metrics.PSObject.Properties.Remove("elapsedMs")
        $Value.personal.metrics.PSObject.Properties.Remove("personalMs")
        return $Value | ConvertTo-Json -Depth 100 -Compress
    }
    foreach ($fixture in @("pull-requests", "correctness-pull-requests", "discussion-pull-requests", "inbox-pull-requests")) {
        $inputPath = Join-Path $PSScriptRoot "fixtures/$fixture.json"
        $outputs = @()
        foreach ($sourceModule in @($env:PR_ATTENTION_QUEUE_BASELINE_MODULE, $modulePath)) {
            Remove-Module PRAttentionQueue -Force -ErrorAction SilentlyContinue
            Import-Module $sourceModule -Force
            $json = Invoke-PRAttentionQueue -InputPath $inputPath -Now $snapshot -OutputFormat Json | ConvertFrom-Json -Depth 100
            $markdown = Invoke-PRAttentionQueue -InputPath $inputPath -Now $snapshot -OutputFormat Markdown
            $outputs += [pscustomobject]@{ projection = Get-LegacyProjection $json; markdown = $markdown }
        }
        Assert-True ($outputs[0].projection -ceq $outputs[1].projection) "Legacy JSON changed: $fixture (excluding timing and candidate additions)."
        Assert-True ($outputs[0].markdown -ceq $outputs[1].markdown) "Legacy Markdown changed: $fixture."
        Write-Output "Exact legacy JSON and Markdown match: $fixture"
    }
}
Write-Output "Candidate assertions passed ($candidateChecks observable cases plus public collection, inventory and compatibility checks)."
