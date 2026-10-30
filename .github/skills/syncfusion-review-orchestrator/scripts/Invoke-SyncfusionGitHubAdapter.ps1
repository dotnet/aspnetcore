#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("EnsureReviewTrigger", "PublishReview", "PublishClean")]
    [string]$Operation,
    [Parameter(Mandatory)]
    [string]$RequestPath,
    [string]$LedgerPath,
    [string]$FixturePath,
    [switch]$Execute
)

$ErrorActionPreference = "Stop"

Import-Module -Scope Local -Force -DisableNameChecking (Join-Path $PSScriptRoot "SyncfusionReviewOrchestrator.psm1")

$script:GitHubFixture = $null
$script:FixtureCallCounts = @{}
$script:GitHubCallCount = 0

function Assert-Adapter {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Test-AdapterRfc3339 {
    param([object]$Value)

    if ($Value -isnot [string]) {
        return $false
    }
    $parsed = [datetimeoffset]::MinValue
    return [datetimeoffset]::TryParse(
        $Value,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::RoundtripKind,
        [ref]$parsed)
}

function Get-FixtureResponse {
    param([Parameter(Mandatory)][string]$Key)

    Assert-Adapter ($null -ne $script:GitHubFixture) "No GitHub fixture is loaded."
    Assert-Adapter ($script:GitHubFixture.responses.Contains($Key)) "GitHub fixture has no response for '$Key'."
    $responses = @($script:GitHubFixture.responses[$Key])
    $index = if ($script:FixtureCallCounts.ContainsKey($Key)) {
        [int]$script:FixtureCallCounts[$Key]
    }
    else {
        0
    }
    Assert-Adapter ($index -lt $responses.Count) "GitHub fixture exhausted responses for '$Key'."
    $script:FixtureCallCounts[$Key] = $index + 1
    $script:GitHubCallCount++
    Write-Output -NoEnumerate $responses[$index].value
}

function Invoke-GhJson {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Endpoint,
        [object]$Body
    )

    if ($null -ne $script:GitHubFixture) {
        return ConvertTo-SfroCanonicalJson (Get-FixtureResponse "$Method $Endpoint")
    }

    $arguments = @("api", "--method", $Method, $Endpoint)
    $script:GitHubCallCount++
    if ($null -ne $Body) {
        $json = ConvertTo-Json -InputObject $Body -Depth 100 -Compress
        $arguments += @("--input", "-")
        $output = $json | & gh @arguments
    }
    else {
        $output = & gh @arguments
    }

    if ($LASTEXITCODE -ne 0) {
        throw "gh api failed for $Method $Endpoint."
    }

    return $output
}

function Invoke-GhJsonPages {
    param([Parameter(Mandatory)][string]$Endpoint)

    if ($null -ne $script:GitHubFixture) {
        $value = Get-FixtureResponse "GET $Endpoint"
        foreach ($item in @($value)) {
            Write-Output $item
        }
        return
    }

    $script:GitHubCallCount++
    $raw = & gh api --paginate --slurp $Endpoint
    if ($LASTEXITCODE -ne 0) {
        throw "gh api pagination failed for GET $Endpoint."
    }

    $pages = $raw | ConvertFrom-Json -Depth 100
    return @($pages | ForEach-Object { @($_) })
}

function Get-ChangedRightLines {
    param([Parameter(Mandatory)][string]$Patch)

    $lines = [Collections.Generic.HashSet[int]]::new()
    $newLine = 0
    foreach ($patchLine in $Patch -split "`n") {
        if ($patchLine -match "^@@ -\d+(?:,\d+)? \+(?<line>\d+)(?:,\d+)? @@") {
            $newLine = [int]$Matches.line
            continue
        }
        if ($newLine -eq 0 -or $patchLine.StartsWith("\ No newline")) {
            continue
        }
        if ($patchLine.StartsWith("+") -and -not $patchLine.StartsWith("+++")) {
            [void]$lines.Add($newLine)
            $newLine++
            continue
        }
        if ($patchLine.StartsWith("-") -and -not $patchLine.StartsWith("---")) {
            continue
        }
        $newLine++
    }

    Write-Output -NoEnumerate $lines
}

function Assert-ChangedRightAnchors {
    param(
        [Parameter(Mandatory)][Collections.IEnumerable]$Files,
        [Parameter(Mandatory)][Collections.IEnumerable]$Comments
    )

    foreach ($comment in $Comments) {
        $file = @($Files | Where-Object { $_.filename -ceq $comment.path })
        Assert-Adapter ($file.Count -eq 1) "Changed file '$($comment.path)' was not returned exactly once."
        Assert-Adapter ($file[0].patch -is [string] -and $file[0].patch.Length -gt 0) "Changed file '$($comment.path)' has no complete patch."
        $changedLines = Get-ChangedRightLines $file[0].patch
        Assert-Adapter ($changedLines.Contains([int]$comment.line)) "Line $($comment.line) in '$($comment.path)' is not a changed RIGHT-side line."
    }
}

function Get-ObjectValue {
    param(
        [Parameter(Mandatory)][AllowNull()][object]$Object,
        [Parameter(Mandatory)][string]$Name
    )

    if ($null -eq $Object) {
        return $null
    }
    if ($Object -is [Collections.IDictionary]) {
        if ($Object.Contains($Name)) {
            return $Object[$Name]
        }
        return $null
    }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -ne $property) {
        return $property.Value
    }
    return $null
}

function Get-DiffSha256 {
    param([Parameter(Mandatory)][Collections.IEnumerable]$Files)

    $projection = @($Files | Sort-Object filename | ForEach-Object {
        [ordered]@{
            filename = Get-ObjectValue $_ "filename"
            status = Get-ObjectValue $_ "status"
            sha = Get-ObjectValue $_ "sha"
            patch = Get-ObjectValue $_ "patch"
        }
    })
    return Get-SfroSha256 (ConvertTo-SfroCanonicalJson $projection)
}

function Get-FeedbackSha256 {
    param(
        [Parameter(Mandatory)][Collections.IEnumerable]$IssueComments,
        [Parameter(Mandatory)][Collections.IEnumerable]$Reviews,
        [Parameter(Mandatory)][Collections.IEnumerable]$ReviewComments
    )

    $projectComment = {
        param($comment)
        [ordered]@{
            id = Get-ObjectValue $comment "id"
            updatedAt = Get-ObjectValue $comment "updated_at"
            author = Get-ObjectValue (Get-ObjectValue $comment "user") "login"
            body = Get-ObjectValue $comment "body"
        }
    }
    $projection = [ordered]@{
        issueComments = @($IssueComments | Sort-Object id | ForEach-Object { & $projectComment $_ })
        reviews = @($Reviews | Sort-Object id | ForEach-Object {
            [ordered]@{
                id = Get-ObjectValue $_ "id"
                submittedAt = Get-ObjectValue $_ "submitted_at"
                author = Get-ObjectValue (Get-ObjectValue $_ "user") "login"
                state = Get-ObjectValue $_ "state"
                commitId = Get-ObjectValue $_ "commit_id"
                body = Get-ObjectValue $_ "body"
            }
        })
        reviewComments = @($ReviewComments | Sort-Object id | ForEach-Object {
            [ordered]@{
                id = Get-ObjectValue $_ "id"
                updatedAt = Get-ObjectValue $_ "updated_at"
                author = Get-ObjectValue (Get-ObjectValue $_ "user") "login"
                path = Get-ObjectValue $_ "path"
                line = Get-ObjectValue $_ "line"
                side = Get-ObjectValue $_ "side"
                commitId = Get-ObjectValue $_ "commit_id"
                body = Get-ObjectValue $_ "body"
            }
        })
    }
    return Get-SfroSha256 (ConvertTo-SfroCanonicalJson $projection)
}

function Assert-LiveHead {
    param(
        [Parameter(Mandatory)][string]$PullEndpoint,
        [Parameter(Mandatory)][string]$ExpectedHead
    )

    $pull = Invoke-GhJson -Method GET -Endpoint $PullEndpoint -Body $null | ConvertFrom-Json -AsHashtable -Depth 100
    Assert-Adapter ($pull.state -ceq "open" -and -not [bool]$pull.draft -and $null -eq $pull.merged_at) "The PR is not open, ready, and unmerged."
    Assert-Adapter ($pull.head.sha -ceq $ExpectedHead) "The PR head moved; mutation blocked."
    return $pull
}

function Assert-LedgerIntent {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Request,
        [Parameter(Mandatory)][string]$ExpectedKind
    )

    if ($null -eq $script:GitHubFixture) {
        Assert-Adapter ($LedgerPath -is [string] -and $LedgerPath.Length -gt 0) "Live execution requires -LedgerPath."
    }
    if (-not $LedgerPath) {
        return
    }

    $state = Get-SfroState $LedgerPath
    Assert-Adapter ($state.repository -ceq $Request.repository) "Request repository does not match the campaign ledger."
    Assert-Adapter ($state.campaignId -ceq $Request.campaignId) "Request campaign does not match the campaign ledger."
    $pending = @($state.intents.Values | Where-Object { $_.status -in @("reserved", "unknown") })
    Assert-Adapter ($pending.Count -eq 1) "The ledger must have exactly one unresolved intent."
    Assert-Adapter ($pending[0].intentId -ceq $Request.intentId) "Request intentId does not match the reserved ledger intent."
    Assert-Adapter ($pending[0].action.kind -ceq $ExpectedKind) "Reserved ledger intent kind mismatch."
    Assert-Adapter ([int]$pending[0].action.pr -eq [int]$Request.pr) "Reserved ledger PR mismatch."
    Assert-Adapter ($pending[0].action.payload.head -ceq $Request.head) "Reserved ledger head mismatch."
    if ($ExpectedKind -eq "ensure-review-trigger") {
        Assert-Adapter ($pending[0].reservedAtUtc -ceq $Request.intentReservedAtUtc) "Trigger request reservation time mismatch."
    }
    if ($ExpectedKind -eq "publish-review") {
        Assert-Adapter ($pending[0].action.payload.findingsSha256 -ceq $Request.findingsSha256) "Reserved findings identity mismatch."
        Assert-Adapter ($pending[0].action.payload.diffSha256 -ceq $Request.diffSha256) "Reserved diff identity mismatch."
        Assert-Adapter ($pending[0].action.payload.feedbackSha256 -ceq $Request.feedbackSha256) "Reserved feedback identity mismatch."
        Assert-Adapter ($pending[0].action.payload.marker -ceq $Request.marker) "Reserved review marker mismatch."
    }
    elseif ($ExpectedKind -eq "publish-clean") {
        Assert-Adapter ($pending[0].action.payload.marker -ceq $Request.marker) "Reserved clean marker mismatch."
        Assert-Adapter ($pending[0].action.payload.label -ceq $Request.label) "Reserved clean label mismatch."
    }
}

$request = ConvertFrom-SfroJson (Get-Content -LiteralPath $RequestPath -Raw)
Assert-Adapter ($request.repository -cmatch "^[^/]+/[^/]+$") "repository must be owner/name."
Assert-Adapter ([int]$request.pr -gt 0) "pr must be positive."
Assert-Adapter ($request.head -cmatch "^[0-9a-f]{40}$") "head must be a 40-character SHA."
Assert-Adapter ($request.intentId -is [string] -and $request.intentId.Length -gt 0) "intentId is required."

if ($FixturePath) {
    $script:GitHubFixture = ConvertFrom-SfroJson (Get-Content -LiteralPath $FixturePath -Raw)
}

$expectedIntentKind = switch ($Operation) {
    "EnsureReviewTrigger" { "ensure-review-trigger" }
    "PublishReview" { "publish-review" }
    "PublishClean" { "publish-clean" }
}
if ($Execute) {
    Assert-LedgerIntent -Request $request -ExpectedKind $expectedIntentKind
}

$owner, $repo = $request.repository -split "/", 2
$pullEndpoint = "repos/$owner/$repo/pulls/$($request.pr)"
$mutation = $null
switch ($Operation) {
    "EnsureReviewTrigger" {
        Assert-Adapter ($request.command -ceq "/review") "The hosted trigger must be literal /review."
        $decision = Resolve-SfroReviewTriggerDecision `
            -CampaignId $request.campaignId `
            -Pr ([int]$request.pr) `
            -Head $request.head `
            -KnownTriggers @($request.knownTriggers)
        $mutation = [ordered]@{
            decision = $decision.decision
            key = $decision.key
            request = [ordered]@{
                method = "POST"
                endpoint = "repos/$owner/$repo/issues/$($request.pr)/comments"
                body = [ordered]@{ body = "/review" }
            }
        }
    }
    "PublishReview" {
        Assert-Adapter ($request.event -ceq "COMMENT") "Only COMMENT reviews are allowed."
        Assert-Adapter ($request.marker -is [string] -and $request.body.Contains($request.marker)) "Review body must contain its deterministic marker."
        Assert-Adapter ($request.body.Contains("Human-directed and GitHub Copilot-generated.")) "Review body must contain agent attribution."
        Assert-Adapter (@($request.comments).Count -ge 1) "PublishReview requires at least one inline comment."
        foreach ($comment in $request.comments) {
            Assert-Adapter ($comment.path -is [string] -and $comment.path.Length -gt 0) "Every review comment requires a path."
            Assert-Adapter ([int]$comment.line -gt 0) "Every review comment requires a positive line."
            Assert-Adapter ($comment.side -ceq "RIGHT") "Every review comment must use RIGHT side."
        }
        Assert-Adapter ($request.findingsSha256 -ceq (Get-SfroSha256 (ConvertTo-SfroCanonicalJson $request.findings))) "Review findings hash mismatch."

        $mutation = [ordered]@{
            method = "POST"
            endpoint = "$pullEndpoint/reviews"
            body = [ordered]@{
                commit_id = $request.head
                event = "COMMENT"
                body = $request.body
                comments = @($request.comments | ForEach-Object {
                    [ordered]@{
                        path = $_.path
                        line = [int]$_.line
                        side = "RIGHT"
                        body = $_.body
                    }
                })
            }
        }
    }
    "PublishClean" {
        Assert-Adapter ($request.label -ceq "agent-reviewed") "PublishClean may only add agent-reviewed."
        Assert-Adapter ($request.marker -ceq "<!-- agent-reviewed-local-panel:$($request.head) -->") "Clean marker mismatch."
        Assert-Adapter ([bool]$request.panelComplete) "The panel must be complete."
        Assert-Adapter ([bool]$request.allOriginalsNoFindings) "Every original must be NO_FINDINGS."
        Assert-Adapter ([int]$request.assessmentSurvivorCount -eq 0) "Assessment must have zero survivors."
        $mutation = [ordered]@{
            comment = [ordered]@{
                method = "POST"
                endpoint = "repos/$owner/$repo/issues/$($request.pr)/comments"
                body = [ordered]@{ body = "$($request.visibleComment)`n`n$($request.marker)" }
            }
            label = [ordered]@{
                method = "POST"
                endpoint = "repos/$owner/$repo/issues/$($request.pr)/labels"
                body = [ordered]@{ labels = @("agent-reviewed") }
            }
        }
    }
}

$requestHash = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $request)
if (-not $Execute) {
    [ordered]@{
        schemaVersion = 1
        mode = "dry-run"
        operation = $Operation
        requestSha256 = $requestHash
        githubCalls = 0
        mutationPlan = $mutation
        fixtureRead = [bool]$FixturePath
    } | ConvertTo-Json -Depth 100
    exit 0
}

$login = (Invoke-GhJson -Method GET -Endpoint "user" -Body $null | ConvertFrom-Json).login
if ($request.expectedLogin) {
    Assert-Adapter ($login -ceq $request.expectedLogin) "Authenticated GitHub login mismatch."
}

$pull = Assert-LiveHead -PullEndpoint $pullEndpoint -ExpectedHead $request.head

$evidence = switch ($Operation) {
    "EnsureReviewTrigger" {
        if ($mutation.decision -eq "reused") {
            $trigger = @($request.knownTriggers | Where-Object {
                $_.campaignId -ceq $request.campaignId -and
                [int]$_.pr -eq [int]$request.pr -and
                $_.head -ceq $request.head -and
                $_.command -ceq "/review" -and
                [bool]$_.readbackVerified
            })
            Assert-Adapter ($trigger.Count -eq 1) "Expected one retained trigger."
            $comment = Invoke-GhJson -Method GET -Endpoint "repos/$owner/$repo/issues/comments/$($trigger[0].commentId)" -Body $null | ConvertFrom-Json -AsHashtable -Depth 100
            Assert-Adapter ($comment.body -ceq "/review" -and [int64]$comment.id -eq [int64]$trigger[0].commentId) "Retained /review trigger failed live readback."
            Assert-Adapter ($comment.issue_url.EndsWith("/issues/$($request.pr)", [StringComparison]::Ordinal)) "Retained /review trigger belongs to another PR."
            Assert-Adapter ($comment.user.login -ceq $login) "Retained /review trigger author mismatch."
            $decision = "reused"
        }
        else {
            Assert-Adapter (Test-AdapterRfc3339 $request.intentReservedAtUtc) "Trigger reconciliation requires intentReservedAtUtc."
            $issueComments = @(Invoke-GhJsonPages "repos/$owner/$repo/issues/$($request.pr)/comments?per_page=100")
            $candidates = @($issueComments | Where-Object {
                $_.body -ceq "/review" -and
                $_.user.login -ceq $login -and
                [datetimeoffset]$_.created_at -ge [datetimeoffset]$request.intentReservedAtUtc
            })
            Assert-Adapter ($candidates.Count -le 1) "Multiple possible /review effects exist; reconciliation is ambiguous."
            if ($candidates.Count -eq 1) {
                $comment = $candidates[0]
                $decision = "reused"
            }
            else {
                Assert-LiveHead -PullEndpoint $pullEndpoint -ExpectedHead $request.head | Out-Null
                $created = Invoke-GhJson -Method POST -Endpoint $mutation.request.endpoint -Body $mutation.request.body | ConvertFrom-Json -AsHashtable -Depth 100
                $comment = Invoke-GhJson -Method GET -Endpoint "repos/$owner/$repo/issues/comments/$($created.id)" -Body $null | ConvertFrom-Json -AsHashtable -Depth 100
                Assert-Adapter ($comment.body -ceq "/review" -and [int64]$comment.id -eq [int64]$created.id) "Posted /review trigger failed live readback."
                $decision = "posted"
            }
        }
        Assert-Adapter ($comment.issue_url.EndsWith("/issues/$($request.pr)", [StringComparison]::Ordinal)) "/review readback belongs to another PR."
        Assert-Adapter ($comment.user.login -ceq $login) "/review readback author mismatch."
        Assert-LiveHead -PullEndpoint $pullEndpoint -ExpectedHead $request.head | Out-Null

        [ordered]@{
            head = $request.head
            command = "/review"
            decision = $decision
            commentId = [int64]$comment.id
            commentUrl = $comment.html_url
            readbackVerified = $true
            triggerCountForHead = 1
        }
    }
    "PublishReview" {
        $reviews = @(Invoke-GhJsonPages "$pullEndpoint/reviews?per_page=100")
        $matching = @($reviews | Where-Object {
            $_.commit_id -ceq $request.head -and
            $_.body -is [string] -and
            $_.body.Contains($request.marker)
        })
        Assert-Adapter ($matching.Count -le 1) "Multiple matching reviews already exist."
        if ($matching.Count -eq 1) {
            $reviewId = [int64]$matching[0].id
        }
        else {
            Assert-Adapter (@($request.findings).Count -eq @($request.comments).Count) "Published comments must map one-to-one to finalized findings."
            foreach ($finding in $request.findings) {
                $matchingComment = @($request.comments | Where-Object {
                    $_.path -ceq $finding.path -and
                    [int]$_.line -eq [int]$finding.line -and
                    $_.side -ceq $finding.side
                })
                Assert-Adapter ($matchingComment.Count -eq 1) "A finalized finding does not have exactly one matching inline comment."
            }
            $files = @(Invoke-GhJsonPages "$pullEndpoint/files?per_page=100")
            Assert-ChangedRightAnchors -Files $files -Comments $request.comments
            $liveDiffSha256 = Get-DiffSha256 $files
            Assert-Adapter ($liveDiffSha256 -ceq $request.diffSha256) "The live PR diff no longer matches the frozen diff."
            $issueComments = @(Invoke-GhJsonPages "repos/$owner/$repo/issues/$($request.pr)/comments?per_page=100")
            $reviewComments = @(Invoke-GhJsonPages "$pullEndpoint/comments?per_page=100")
            $liveFeedbackSha256 = Get-FeedbackSha256 -IssueComments $issueComments -Reviews $reviews -ReviewComments $reviewComments
            Assert-Adapter ($liveFeedbackSha256 -ceq $request.feedbackSha256) "The live PR feedback no longer matches the frozen feedback."
            foreach ($finding in $request.findings) {
                Assert-Adapter ($finding.fingerprint -is [string] -and $finding.fingerprint.Length -gt 0) "Every finalized finding requires a fingerprint."
                $duplicate = @($issueComments + $reviews + $reviewComments | Where-Object {
                    $_.body -is [string] -and $_.body.Contains($finding.fingerprint)
                })
                Assert-Adapter ($duplicate.Count -eq 0) "A finalized finding is already covered by live feedback."
            }
            Assert-LiveHead -PullEndpoint $pullEndpoint -ExpectedHead $request.head | Out-Null
            $created = Invoke-GhJson -Method POST -Endpoint $mutation.endpoint -Body $mutation.body | ConvertFrom-Json -AsHashtable -Depth 100
            $reviewId = [int64]$created.id
        }

        $review = Invoke-GhJson -Method GET -Endpoint "$pullEndpoint/reviews/$reviewId" -Body $null | ConvertFrom-Json -AsHashtable -Depth 100
        $comments = @(Invoke-GhJsonPages "$pullEndpoint/reviews/$reviewId/comments?per_page=100")
        Assert-Adapter ($review.commit_id -ceq $request.head -and $review.state -ceq "COMMENTED") "Review readback identity mismatch."
        Assert-Adapter ($review.body -ceq $request.body) "Review readback body mismatch."
        Assert-Adapter ($comments.Count -eq @($request.comments).Count) "Review readback inline-comment count mismatch."
        foreach ($expected in $request.comments) {
            $found = @($comments | Where-Object {
                $_.path -ceq $expected.path -and
                [int]$_.line -eq [int]$expected.line -and
                $_.side -ceq "RIGHT" -and
                $_.body -ceq $expected.body
            })
            Assert-Adapter ($found.Count -eq 1) "Review readback did not match an expected inline comment."
        }
        Assert-LiveHead -PullEndpoint $pullEndpoint -ExpectedHead $request.head | Out-Null

        [ordered]@{
            liveHead = $request.head
            event = "COMMENT"
            atomic = $true
            feedbackComplete = [bool]$request.feedbackComplete
            deduplicated = $true
            changedLinesValid = $true
            agentAttributed = $true
            reviewId = $reviewId
            reviewUrl = $review.html_url
            readbackVerified = $true
            publishedFindingsSha256 = $request.findingsSha256
            diffSha256 = $request.diffSha256
            feedbackSha256 = $request.feedbackSha256
        }
    }
    "PublishClean" {
        $labelsBefore = @(Invoke-GhJsonPages "repos/$owner/$repo/issues/$($request.pr)/labels?per_page=100")
        $commentsBefore = @(Invoke-GhJsonPages "repos/$owner/$repo/issues/$($request.pr)/comments?per_page=100")
        $labelWasAbsent = -not ($labelsBefore | Where-Object { $_.name -ceq "agent-reviewed" })
        $markerWasAbsent = -not ($commentsBefore | Where-Object { $_.body -is [string] -and $_.body.Contains($request.marker) })

        if ($markerWasAbsent) {
            Assert-LiveHead -PullEndpoint $pullEndpoint -ExpectedHead $request.head | Out-Null
            Invoke-GhJson -Method POST -Endpoint $mutation.comment.endpoint -Body $mutation.comment.body | Out-Null
        }
        $commentsAfter = @(Invoke-GhJsonPages "repos/$owner/$repo/issues/$($request.pr)/comments?per_page=100")
        $matchingComments = @($commentsAfter | Where-Object { $_.body -is [string] -and $_.body.Contains($request.marker) })
        Assert-Adapter ($matchingComments.Count -eq 1) "Clean marker readback failed."
        Assert-LiveHead -PullEndpoint $pullEndpoint -ExpectedHead $request.head | Out-Null

        $labelAdded = $false
        if ($labelWasAbsent) {
            Assert-LiveHead -PullEndpoint $pullEndpoint -ExpectedHead $request.head | Out-Null
            Invoke-GhJson -Method POST -Endpoint $mutation.label.endpoint -Body $mutation.label.body | Out-Null
            $labelAdded = $true
        }
        $labelsAfter = @(Invoke-GhJsonPages "repos/$owner/$repo/issues/$($request.pr)/labels?per_page=100")
        $matchingLabels = @($labelsAfter | Where-Object { $_.name -ceq "agent-reviewed" })
        Assert-Adapter ($matchingLabels.Count -eq 1) "Clean label readback failed."
        try {
            Assert-LiveHead -PullEndpoint $pullEndpoint -ExpectedHead $request.head | Out-Null
        }
        catch {
            if ($labelAdded) {
                Invoke-GhJson -Method DELETE -Endpoint "repos/$owner/$repo/issues/$($request.pr)/labels/agent-reviewed" -Body $null | Out-Null
            }
            throw
        }

        [ordered]@{
            liveHead = $request.head
            panelComplete = $true
            allOriginalsNoFindings = $true
            assessmentSurvivorCount = 0
            labelWasAbsent = $labelWasAbsent
            markerWasAbsent = $markerWasAbsent
            labelCountAfter = $matchingLabels.Count
            markerCountAfter = $matchingComments.Count
            readbackVerified = $true
        }
    }
}

[ordered]@{
    schemaVersion = 1
    mode = if ($null -ne $script:GitHubFixture) { "fixture-executed" } else { "executed" }
    operation = $Operation
    requestSha256 = $requestHash
    authenticatedLogin = $login
    githubCalls = $script:GitHubCallCount
    evidence = $evidence
} | ConvertTo-Json -Depth 100
