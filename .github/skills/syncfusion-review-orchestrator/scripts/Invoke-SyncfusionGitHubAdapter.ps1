#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("EnsureReviewTrigger", "ObserveHostedReview", "PublishReview", "PublishClean")]
    [string]$Operation,
    [Parameter(Mandatory)]
    [string]$RequestPath,
    [string]$LedgerPath,
    [string]$FixturePath,
    [switch]$Execute,
    [switch]$AllowLiveMutation
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
    if ($Key.StartsWith("POST ", [StringComparison]::Ordinal) -or
        $Key.StartsWith("DELETE ", [StringComparison]::Ordinal)) {
        Assert-Adapter ($index -lt $responses.Count) "GitHub fixture exhausted mutation responses for '$Key'."
    }
    $effectiveIndex = [Math]::Min($index, $responses.Count - 1)
    $script:FixtureCallCounts[$Key] = $index + 1
    $script:GitHubCallCount++
    Write-Output -NoEnumerate $responses[$effectiveIndex].value
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

    $projection = @($Files | Sort-Object { [string](Get-ObjectValue $_ "filename") } -CaseSensitive | ForEach-Object {
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
        [Parameter(Mandatory)][Collections.IEnumerable]$ReviewComments,
        [Collections.IDictionary]$Exclusions = [ordered]@{
            issueCommentIds = @()
            reviewIds = @()
            reviewCommentIds = @()
        }
    )

    $issueCommentIds = @($Exclusions.issueCommentIds | ForEach-Object { [int64]$_ })
    $reviewIds = @($Exclusions.reviewIds | ForEach-Object { [int64]$_ })
    $reviewCommentIds = @($Exclusions.reviewCommentIds | ForEach-Object { [int64]$_ })
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
        issueComments = @($IssueComments | Where-Object { [int64](Get-ObjectValue $_ "id") -notin $issueCommentIds } | Sort-Object { [int64](Get-ObjectValue $_ "id") } | ForEach-Object { & $projectComment $_ })
        reviews = @($Reviews | Where-Object { [int64](Get-ObjectValue $_ "id") -notin $reviewIds } | Sort-Object { [int64](Get-ObjectValue $_ "id") } | ForEach-Object {
            [ordered]@{
                id = Get-ObjectValue $_ "id"
                submittedAt = Get-ObjectValue $_ "submitted_at"
                author = Get-ObjectValue (Get-ObjectValue $_ "user") "login"
                state = Get-ObjectValue $_ "state"
                commitId = Get-ObjectValue $_ "commit_id"
                body = Get-ObjectValue $_ "body"
            }
        })
        reviewComments = @($ReviewComments | Where-Object { [int64](Get-ObjectValue $_ "id") -notin $reviewCommentIds } | Sort-Object { [int64](Get-ObjectValue $_ "id") } | ForEach-Object {
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

function Assert-UniqueValues {
    param(
        [Parameter(Mandatory)][Collections.IEnumerable]$Items,
        [Parameter(Mandatory)][scriptblock]$Selector,
        [Parameter(Mandatory)][string]$Label
    )

    $values = @($Items | ForEach-Object { & $Selector $_ })
    Assert-Adapter (@($values | Sort-Object -Unique -CaseSensitive).Count -eq $values.Count) "$Label contains duplicate identities."
}

function Get-LiveIdentitySnapshot {
    param(
        [Parameter(Mandatory)][string]$Owner,
        [Parameter(Mandatory)][string]$Repo,
        [Parameter(Mandatory)][int]$Pr,
        [Parameter(Mandatory)][string]$BaseRef,
        [Collections.IDictionary]$FeedbackExclusions = [ordered]@{
            issueCommentIds = @()
            reviewIds = @()
            reviewCommentIds = @()
        }
    )

    $pullEndpoint = "repos/$Owner/$Repo/pulls/$Pr"
    $pull = Invoke-GhJson -Method GET -Endpoint $pullEndpoint -Body $null | ConvertFrom-Json -AsHashtable -Depth 100
    $base = Invoke-GhJson -Method GET -Endpoint "repos/$Owner/$Repo/commits/$BaseRef" -Body $null | ConvertFrom-Json -AsHashtable -Depth 100
    $files = @(Invoke-GhJsonPages "$pullEndpoint/files?per_page=100")
    $issueComments = @(Invoke-GhJsonPages "repos/$Owner/$Repo/issues/$Pr/comments?per_page=100")
    $reviews = @(Invoke-GhJsonPages "$pullEndpoint/reviews?per_page=100")
    $reviewComments = @(Invoke-GhJsonPages "$pullEndpoint/comments?per_page=100")

    Assert-Adapter ($pull.state -ceq "open" -and -not [bool]$pull.draft -and $null -eq $pull.merged_at) "The PR is not open, ready, and unmerged."
    Assert-Adapter ($pull.base.ref -ceq $BaseRef) "The PR base ref moved."
    Assert-Adapter ([int]$pull.changed_files -eq $files.Count) "The complete changed-file count does not match GitHub."
    Assert-UniqueValues -Items $files -Selector { param($item) $item.filename } -Label "Changed files"
    Assert-UniqueValues -Items $issueComments -Selector { param($item) [int64]$item.id } -Label "Issue comments"
    Assert-UniqueValues -Items $reviews -Selector { param($item) [int64]$item.id } -Label "Reviews"
    Assert-UniqueValues -Items $reviewComments -Selector { param($item) [int64]$item.id } -Label "Review comments"
    foreach ($id in @($FeedbackExclusions.issueCommentIds)) {
        Assert-Adapter (@($issueComments | Where-Object { [int64]$_.id -eq [int64]$id }).Count -eq 1) "Excluded issue comment $id is absent."
    }
    foreach ($id in @($FeedbackExclusions.reviewIds)) {
        Assert-Adapter (@($reviews | Where-Object { [int64]$_.id -eq [int64]$id }).Count -eq 1) "Excluded review $id is absent."
    }
    foreach ($id in @($FeedbackExclusions.reviewCommentIds)) {
        Assert-Adapter (@($reviewComments | Where-Object { [int64]$_.id -eq [int64]$id }).Count -eq 1) "Excluded review comment $id is absent."
    }

    $files = @($files | Sort-Object { [string](Get-ObjectValue $_ "filename") } -CaseSensitive)
    $issueComments = @($issueComments | Sort-Object { [int64](Get-ObjectValue $_ "id") })
    $reviews = @($reviews | Sort-Object { [int64](Get-ObjectValue $_ "id") })
    $reviewComments = @($reviewComments | Sort-Object { [int64](Get-ObjectValue $_ "id") })

    $rawFeedbackSha256 = Get-FeedbackSha256 `
        -IssueComments $issueComments `
        -Reviews $reviews `
        -ReviewComments $reviewComments
    $effectiveFeedbackSha256 = Get-FeedbackSha256 `
        -IssueComments $issueComments `
        -Reviews $reviews `
        -ReviewComments $reviewComments `
        -Exclusions $FeedbackExclusions
    $collections = [ordered]@{
        files = $files
        issueComments = $issueComments
        reviews = $reviews
        reviewComments = $reviewComments
    }

    return [ordered]@{
        state = $pull.state
        draft = [bool]$pull.draft
        merged = $null -ne $pull.merged_at
        baseRef = $pull.base.ref
        head = $pull.head.sha
        baseTip = $base.sha
        changedFileCount = $files.Count
        diffSha256 = Get-DiffSha256 $files
        rawFeedbackSha256 = $rawFeedbackSha256
        effectiveFeedbackSha256 = $effectiveFeedbackSha256
        feedbackExclusions = ConvertFrom-SfroJson (ConvertTo-SfroCanonicalJson $FeedbackExclusions)
        files = $files
        issueComments = $issueComments
        reviews = $reviews
        reviewComments = $reviewComments
        collectionsSha256 = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $collections)
    }
}

function Get-StableLiveIdentity {
    param(
        [Parameter(Mandatory)][string]$Owner,
        [Parameter(Mandatory)][string]$Repo,
        [Parameter(Mandatory)][int]$Pr,
        [Parameter(Mandatory)][string]$BaseRef,
        [Collections.IDictionary]$FeedbackExclusions = [ordered]@{
            issueCommentIds = @()
            reviewIds = @()
            reviewCommentIds = @()
        }
    )

    $first = Get-LiveIdentitySnapshot -Owner $Owner -Repo $Repo -Pr $Pr -BaseRef $BaseRef -FeedbackExclusions $FeedbackExclusions
    $second = Get-LiveIdentitySnapshot -Owner $Owner -Repo $Repo -Pr $Pr -BaseRef $BaseRef -FeedbackExclusions $FeedbackExclusions
    $firstJson = ConvertTo-SfroCanonicalJson $first
    $secondJson = ConvertTo-SfroCanonicalJson $second
    Assert-Adapter ($firstJson -ceq $secondJson) "GitHub identity changed during collection: first=$(Get-SfroSha256 $firstJson), second=$(Get-SfroSha256 $secondJson)."
    return $second
}

function Assert-ExpectedIdentity {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Identity,
        [Parameter(Mandatory)][Collections.IDictionary]$Request
    )

    Assert-Adapter ($Identity.head -ceq $Request.head) "The PR head moved."
    Assert-Adapter ($Identity.baseRef -ceq $Request.baseRef) "The PR base ref moved."
    Assert-Adapter ($Identity.baseTip -ceq $Request.baseTip) "The target base tip moved."
    Assert-Adapter ($Identity.diffSha256 -ceq $Request.diffSha256) "The live PR diff no longer matches the frozen diff."
    Assert-Adapter ($Identity.effectiveFeedbackSha256 -ceq $Request.feedbackSha256) "The live effective feedback no longer matches the frozen feedback."
}

function Assert-ExpectedStructuralIdentity {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Identity,
        [Parameter(Mandatory)][Collections.IDictionary]$Request
    )

    Assert-Adapter ($Identity.head -ceq $Request.head) "The PR head moved."
    Assert-Adapter ($Identity.baseRef -ceq $Request.baseRef) "The PR base ref moved."
    Assert-Adapter ($Identity.baseTip -ceq $Request.baseTip) "The target base tip moved."
    Assert-Adapter ($Identity.diffSha256 -ceq $Request.diffSha256) "The live PR diff no longer matches the frozen diff."
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
    $unknown = @($state.intents.Values | Where-Object { $_.status -eq "unknown" })
    Assert-Adapter ($unknown.Count -eq 0) "Unknown intents are reconciliation-only and cannot execute effects."
    $pending = @($state.intents.Values | Where-Object { $_.status -eq "reserved" })
    Assert-Adapter ($pending.Count -eq 1) "The ledger must have exactly one unresolved intent."
    Assert-Adapter ($pending[0].intentId -ceq $Request.intentId) "Request intentId does not match the reserved ledger intent."
    Assert-Adapter ($pending[0].action.kind -ceq $ExpectedKind) "Reserved ledger intent kind mismatch."
    Assert-Adapter ([int]$pending[0].action.pr -eq [int]$Request.pr) "Reserved ledger PR mismatch."
    Assert-Adapter ($pending[0].action.payload.head -ceq $Request.head) "Reserved ledger head mismatch."
    Assert-Adapter ($pending[0].action.payload.baseRef -ceq $Request.baseRef) "Reserved ledger base ref mismatch."
    Assert-Adapter ($pending[0].action.payload.baseTip -ceq $Request.baseTip) "Reserved ledger base tip mismatch."
    Assert-Adapter ($pending[0].action.payload.diffSha256 -ceq $Request.diffSha256) "Reserved ledger diff mismatch."
    Assert-Adapter ($pending[0].action.payload.feedbackSha256 -ceq $Request.feedbackSha256) "Reserved ledger feedback mismatch."
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
        Assert-Adapter ($pending[0].action.payload.visibleComment -ceq $Request.visibleComment) "Reserved clean comment mismatch."
    }
    if ($ExpectedKind -in @("publish-review", "publish-clean")) {
        $entry = $state.entries[[string][int]$Request.pr]
        $expectedExclusions = [ordered]@{
            issueCommentIds = @([int64]$entry.hostedReview.commentId)
            reviewIds = @()
            reviewCommentIds = @()
        }
        Assert-Adapter ((ConvertTo-SfroCanonicalJson $Request.feedbackExclusions) -ceq (ConvertTo-SfroCanonicalJson $expectedExclusions)) "Request feedback exclusions are not the exact controller trigger."
    }
    elseif ($ExpectedKind -eq "observe-hosted-review") {
        Assert-Adapter ((ConvertTo-SfroCanonicalJson $Request.feedbackExclusions) -ceq (ConvertTo-SfroCanonicalJson $pending[0].action.payload.feedbackExclusions)) "Hosted observation exclusions mismatch."
        Assert-Adapter ([int64]$Request.triggerCommentId -eq [int64]$pending[0].action.payload.triggerCommentId) "Hosted observation trigger mismatch."
    }
}

$request = ConvertFrom-SfroJson (Get-Content -LiteralPath $RequestPath -Raw)
Assert-Adapter ($request.repository -cmatch "^[^/]+/[^/]+$") "repository must be owner/name."
Assert-Adapter ([int]$request.pr -gt 0) "pr must be positive."
Assert-Adapter ($request.head -cmatch "^[0-9a-f]{40}$") "head must be a 40-character SHA."
Assert-Adapter ($request.intentId -is [string] -and $request.intentId.Length -gt 0) "intentId is required."
Assert-Adapter ($request.baseRef -is [string] -and $request.baseRef.Length -gt 0) "baseRef is required."
Assert-Adapter ($request.baseTip -cmatch "^[0-9a-f]{40}$") "baseTip must be a 40-character SHA."
Assert-Adapter ($request.diffSha256 -cmatch "^[0-9a-f]{64}$") "diffSha256 is required."
Assert-Adapter ($request.feedbackSha256 -cmatch "^[0-9a-f]{64}$") "feedbackSha256 is required."

if ($FixturePath) {
    $script:GitHubFixture = ConvertFrom-SfroJson (Get-Content -LiteralPath $FixturePath -Raw)
}
if ($Execute -and $null -eq $script:GitHubFixture -and -not $AllowLiveMutation) {
    throw "Live GitHub mutation requires explicit -AllowLiveMutation."
}

$expectedIntentKind = switch ($Operation) {
    "EnsureReviewTrigger" { "ensure-review-trigger" }
    "ObserveHostedReview" { "observe-hosted-review" }
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
    "ObserveHostedReview" {
        Assert-Adapter ([bool]$request.hostedTerminal) "Hosted review must be terminal before observation."
        $mutation = [ordered]@{
            readOnly = $true
            expectedTriggerCommentId = [int64]$request.triggerCommentId
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

$persistentExclusions = if ($Operation -eq "EnsureReviewTrigger") {
    [ordered]@{
        issueCommentIds = if ($mutation.decision -eq "reused") {
            @([int64]$decision.trigger.commentId)
        }
        else {
            @()
        }
        reviewIds = @()
        reviewCommentIds = @()
    }
}
else {
    $request.feedbackExclusions
}
$preIdentity = Get-StableLiveIdentity `
    -Owner $owner `
    -Repo $repo `
    -Pr ([int]$request.pr) `
    -BaseRef $request.baseRef `
    -FeedbackExclusions $persistentExclusions
$detectedRecoveryExclusions = $null
if ($Operation -eq "EnsureReviewTrigger" -and $preIdentity.effectiveFeedbackSha256 -cne $request.feedbackSha256) {
    Assert-Adapter (Test-AdapterRfc3339 $request.intentReservedAtUtc) "Trigger reconciliation requires intentReservedAtUtc."
    $triggerCandidates = @($preIdentity.issueComments | Where-Object {
        $_.body -ceq "/review" -and
        $_.user.login -ceq $login -and
        [datetimeoffset]$_.created_at -ge [datetimeoffset]$request.intentReservedAtUtc
    })
    Assert-Adapter ($triggerCandidates.Count -eq 1) "Feedback changed before trigger and no unique controller trigger explains it."
    $persistentExclusions.issueCommentIds = @([int64]$triggerCandidates[0].id)
    $preIdentity = Get-StableLiveIdentity `
        -Owner $owner `
        -Repo $repo `
        -Pr ([int]$request.pr) `
        -BaseRef $request.baseRef `
        -FeedbackExclusions $persistentExclusions
}
elseif ($Operation -eq "PublishReview" -and $preIdentity.effectiveFeedbackSha256 -cne $request.feedbackSha256) {
    $recoveryReviews = @($preIdentity.reviews | Where-Object {
        $_.commit_id -ceq $request.head -and
        $_.user.login -ceq $login -and
        $_.body -ceq $request.body -and
        $_.body.Contains($request.marker)
    })
    Assert-Adapter ($recoveryReviews.Count -eq 1) "Feedback changed before review and no unique controller review explains it."
    $recoveryReviewId = [int64]$recoveryReviews[0].id
    $recoveryComments = @($preIdentity.reviewComments | Where-Object {
        [int64]$_.pull_request_review_id -eq $recoveryReviewId
    })
    Assert-Adapter ($recoveryComments.Count -eq @($request.comments).Count) "The recovered review comment set is incomplete."
    foreach ($expected in $request.comments) {
        $matchingRecovered = @($recoveryComments | Where-Object {
            $_.user.login -ceq $login -and
            $_.commit_id -ceq $request.head -and
            $null -eq (Get-ObjectValue $_ "in_reply_to_id") -and
            $_.path -ceq $expected.path -and
            [int]$_.line -eq [int]$expected.line -and
            $_.side -ceq "RIGHT" -and
            $_.body -ceq $expected.body
        })
        Assert-Adapter ($matchingRecovered.Count -eq 1) "A recovered inline comment does not match the expected controller effect."
    }
    $detectedRecoveryExclusions = [ordered]@{
        issueCommentIds = @($persistentExclusions.issueCommentIds)
        reviewIds = @($recoveryReviewId)
        reviewCommentIds = @($recoveryComments | ForEach-Object { [int64]$_.id })
    }
    $preIdentity = Get-StableLiveIdentity `
        -Owner $owner `
        -Repo $repo `
        -Pr ([int]$request.pr) `
        -BaseRef $request.baseRef `
        -FeedbackExclusions $detectedRecoveryExclusions
}
elseif ($Operation -eq "PublishClean" -and $preIdentity.effectiveFeedbackSha256 -cne $request.feedbackSha256) {
    $expectedMarkerBody = "$($request.visibleComment)`n`n$($request.marker)"
    $recoveryMarkers = @($preIdentity.issueComments | Where-Object {
        $_.body -ceq $expectedMarkerBody -and
        $_.user.login -ceq $login
    })
    Assert-Adapter ($recoveryMarkers.Count -eq 1) "Feedback changed before clean publication and no unique controller marker explains it."
    $detectedRecoveryExclusions = [ordered]@{
        issueCommentIds = @($persistentExclusions.issueCommentIds) + @([int64]$recoveryMarkers[0].id)
        reviewIds = @()
        reviewCommentIds = @()
    }
    $preIdentity = Get-StableLiveIdentity `
        -Owner $owner `
        -Repo $repo `
        -Pr ([int]$request.pr) `
        -BaseRef $request.baseRef `
        -FeedbackExclusions $detectedRecoveryExclusions
}
if ($Operation -in @("ObserveHostedReview", "PublishReview", "PublishClean")) {
    Assert-Adapter (@($persistentExclusions.issueCommentIds).Count -eq 1) "The operation requires exactly one controller trigger exclusion."
    $triggerId = [int64]$persistentExclusions.issueCommentIds[0]
    $liveTrigger = @($preIdentity.issueComments | Where-Object {
        [int64]$_.id -eq $triggerId -and
        $_.body -ceq "/review" -and
        $_.user.login -ceq $login -and
        $_.issue_url.EndsWith("/issues/$($request.pr)", [StringComparison]::Ordinal)
    })
    Assert-Adapter ($liveTrigger.Count -eq 1) "The bound controller trigger is absent or changed."
}
if ($Operation -in @("EnsureReviewTrigger", "ObserveHostedReview")) {
    Assert-ExpectedStructuralIdentity -Identity $preIdentity -Request $request
}
else {
    Assert-ExpectedIdentity -Identity $preIdentity -Request $request
}

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
            $candidates = @($preIdentity.issueComments | Where-Object {
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
                $beforeTrigger = Get-StableLiveIdentity `
                    -Owner $owner `
                    -Repo $repo `
                    -Pr ([int]$request.pr) `
                    -BaseRef $request.baseRef `
                    -FeedbackExclusions $persistentExclusions
                Assert-ExpectedIdentity -Identity $beforeTrigger -Request $request
                $created = Invoke-GhJson -Method POST -Endpoint $mutation.request.endpoint -Body $mutation.request.body | ConvertFrom-Json -AsHashtable -Depth 100
                $comment = Invoke-GhJson -Method GET -Endpoint "repos/$owner/$repo/issues/comments/$($created.id)" -Body $null | ConvertFrom-Json -AsHashtable -Depth 100
                Assert-Adapter ($comment.body -ceq "/review" -and [int64]$comment.id -eq [int64]$created.id) "Posted /review trigger failed live readback."
                $decision = "posted"
            }
        }
        Assert-Adapter ($comment.issue_url.EndsWith("/issues/$($request.pr)", [StringComparison]::Ordinal)) "/review readback belongs to another PR."
        Assert-Adapter ($comment.user.login -ceq $login) "/review readback author mismatch."
        $postExclusions = [ordered]@{
            issueCommentIds = @([int64]$comment.id)
            reviewIds = @()
            reviewCommentIds = @()
        }
        $postIdentity = Get-StableLiveIdentity `
            -Owner $owner `
            -Repo $repo `
            -Pr ([int]$request.pr) `
            -BaseRef $request.baseRef `
            -FeedbackExclusions $postExclusions
        Assert-ExpectedStructuralIdentity -Identity $postIdentity -Request $request

        [ordered]@{
            head = $request.head
            baseRef = $request.baseRef
            baseTip = $request.baseTip
            diffSha256 = $request.diffSha256
            command = "/review"
            decision = $decision
            commentId = [int64]$comment.id
            commentUrl = $comment.html_url
            readbackVerified = $true
            triggerCountForHead = 1
            rawFeedbackSha256 = $postIdentity.rawFeedbackSha256
            effectiveFeedbackSha256 = $postIdentity.effectiveFeedbackSha256
            excludedIssueCommentIds = @([int64]$comment.id)
            preIdentity = $preIdentity
            postIdentity = $postIdentity
        }
    }
    "ObserveHostedReview" {
        Assert-Adapter (@($persistentExclusions.issueCommentIds).Count -eq 1 -and [int64]$persistentExclusions.issueCommentIds[0] -eq [int64]$request.triggerCommentId) "Hosted observation trigger exclusion mismatch."
        Assert-Adapter ($request.hostedActorLogins -is [array] -and @($request.hostedActorLogins).Count -gt 0) "Hosted observation requires actor identities."
        Assert-Adapter ([int64]$request.hostedRunId -gt 0) "Hosted observation requires a run ID."
        $run = Invoke-GhJson -Method GET -Endpoint "repos/$owner/$repo/actions/runs/$($request.hostedRunId)" -Body $null | ConvertFrom-Json -AsHashtable -Depth 100
        $trigger = @($preIdentity.issueComments | Where-Object { [int64]$_.id -eq [int64]$request.triggerCommentId })
        Assert-Adapter ($trigger.Count -eq 1) "The hosted trigger is absent."
        $triggerCreatedAt = [datetimeoffset]$trigger[0].created_at
        Assert-Adapter ($run.status -ceq "completed") "The hosted run is not terminal."
        Assert-Adapter ($run.conclusion -in @("success", "failure", "cancelled", "timed_out")) "The hosted run conclusion is not a terminal review outcome."
        Assert-Adapter ($run.path -ceq ".github/workflows/pull-request-review.lock.yml" -and $run.event -ceq "issue_comment") "The hosted run is not the pull-request review workflow."
        Assert-Adapter ([datetimeoffset]$run.created_at -ge $triggerCreatedAt) "The hosted run predates the trigger."
        Assert-Adapter ($run.triggering_actor.login -ceq $login) "The hosted run triggering actor mismatch."
        $runs = Invoke-GhJson -Method GET -Endpoint "repos/$owner/$repo/actions/workflows/pull-request-review.lock.yml/runs?event=issue_comment&per_page=100" -Body $null | ConvertFrom-Json -AsHashtable -Depth 100
        $candidates = @($runs.workflow_runs | Where-Object {
            $_.status -ceq "completed" -and
            $_.path -ceq ".github/workflows/pull-request-review.lock.yml" -and
            $_.event -ceq "issue_comment" -and
            $_.display_title -ceq "/review" -and
            $_.triggering_actor.login -ceq $login -and
            [datetimeoffset]$_.created_at -ge $triggerCreatedAt
        })
        Assert-Adapter ($candidates.Count -eq 1 -and [int64]$candidates[0].id -eq [int64]$request.hostedRunId) "The hosted run is not the unique candidate for this trigger."
        Assert-Adapter ($request.hostedCorrelationArtifactPath -is [string] -and (Test-Path -LiteralPath $request.hostedCorrelationArtifactPath -PathType Leaf)) "The hosted run correlation artifact is unavailable."
        Assert-Adapter ((Get-SfroFileSha256 $request.hostedCorrelationArtifactPath) -ceq $request.hostedCorrelationArtifactSha256) "The hosted run correlation artifact hash mismatch."
        $correlation = ConvertFrom-SfroJson (Get-Content -LiteralPath $request.hostedCorrelationArtifactPath -Raw)
        Assert-Adapter ($correlation.source -ceq "workflow-run-event" -and
            [int64]$correlation.runId -eq [int64]$request.hostedRunId -and
            $correlation.repository -ceq $request.repository -and
            [int]$correlation.pr -eq [int]$request.pr -and
            [int64]$correlation.commentId -eq [int64]$request.triggerCommentId -and
            $correlation.workflowPath -ceq ".github/workflows/pull-request-review.lock.yml" -and
            $correlation.event -ceq "issue_comment") "The hosted run correlation does not bind this PR and trigger."
        foreach ($id in @($request.hostedIssueCommentIds)) {
            $item = @($preIdentity.issueComments | Where-Object {
                [int64]$_.id -eq [int64]$id -and
                $_.user.login -in @($request.hostedActorLogins) -and
                [datetimeoffset]$_.created_at -ge $triggerCreatedAt
            })
            Assert-Adapter ($item.Count -eq 1) "Hosted issue comment $id is absent or invalid."
        }
        foreach ($id in @($request.hostedReviewIds)) {
            $item = @($preIdentity.reviews | Where-Object {
                [int64]$_.id -eq [int64]$id -and
                $_.user.login -in @($request.hostedActorLogins) -and
                $_.commit_id -ceq $request.head -and
                [datetimeoffset]$_.submitted_at -ge $triggerCreatedAt
            })
            Assert-Adapter ($item.Count -eq 1) "Hosted review $id is absent or invalid."
        }
        foreach ($id in @($request.hostedReviewCommentIds)) {
            $item = @($preIdentity.reviewComments | Where-Object {
                [int64]$_.id -eq [int64]$id -and
                $_.user.login -in @($request.hostedActorLogins) -and
                $_.commit_id -ceq $request.head -and
                [int64]$_.pull_request_review_id -in @($request.hostedReviewIds | ForEach-Object { [int64]$_ })
            })
            Assert-Adapter ($item.Count -eq 1) "Hosted review comment $id is absent or invalid."
        }
        $preTriggerExclusions = [ordered]@{
            issueCommentIds = @([int64]$request.triggerCommentId) + @($request.hostedIssueCommentIds | ForEach-Object { [int64]$_ })
            reviewIds = @($request.hostedReviewIds | ForEach-Object { [int64]$_ })
            reviewCommentIds = @($request.hostedReviewCommentIds | ForEach-Object { [int64]$_ })
        }
        $preTriggerFeedbackSha256 = Get-FeedbackSha256 `
            -IssueComments $preIdentity.issueComments `
            -Reviews $preIdentity.reviews `
            -ReviewComments $preIdentity.reviewComments `
            -Exclusions $preTriggerExclusions
        Assert-Adapter ($preTriggerFeedbackSha256 -ceq $request.feedbackSha256) "Feedback changed by something other than the exact hosted output."
        $hostedOutputCount = @($request.hostedIssueCommentIds).Count + @($request.hostedReviewIds).Count + @($request.hostedReviewCommentIds).Count
        if ($hostedOutputCount -eq 0) {
            Assert-Adapter ([bool]$request.hostedNoFeedback) "An empty hosted output requires an explicit no-feedback result."
        }
        [ordered]@{
            terminal = $true
            head = $request.head
            baseRef = $request.baseRef
            baseTip = $request.baseTip
            diffSha256 = $request.diffSha256
            rawFeedbackSha256 = $preIdentity.rawFeedbackSha256
            effectiveFeedbackSha256 = $preIdentity.effectiveFeedbackSha256
            feedbackExclusions = $persistentExclusions
            hostedIssueCommentIds = @($request.hostedIssueCommentIds)
            hostedReviewIds = @($request.hostedReviewIds)
            hostedReviewCommentIds = @($request.hostedReviewCommentIds)
            hostedRunId = [int64]$request.hostedRunId
            hostedWorkflowPath = $run.path
            hostedRunEvent = $run.event
            hostedRunCreatedAt = $run.created_at
            hostedCorrelationArtifactSha256 = $request.hostedCorrelationArtifactSha256
            hostedConclusion = $run.conclusion
            hostedNoFeedback = [bool]$request.hostedNoFeedback
            preTriggerFeedbackSha256 = $preTriggerFeedbackSha256
            resultSha256 = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $preIdentity)
            preIdentity = $preIdentity
            postIdentity = $preIdentity
        }
    }
    "PublishReview" {
        $reviews = @($preIdentity.reviews)
        Assert-Adapter (@($request.findings).Count -eq @($request.comments).Count) "Published comments must map one-to-one to finalized findings."
        foreach ($finding in $request.findings) {
            $matchingComment = @($request.comments | Where-Object {
                $_.path -ceq $finding.path -and
                [int]$_.line -eq [int]$finding.line -and
                $_.side -ceq $finding.side -and
                $_.body.Contains($finding.fingerprint)
            })
            Assert-Adapter ($matchingComment.Count -eq 1) "A finalized finding does not have exactly one matching inline comment."
        }
        Assert-ChangedRightAnchors -Files $preIdentity.files -Comments $request.comments
        $dedupeIssueComments = @($preIdentity.issueComments)
        $dedupeReviews = @($preIdentity.reviews)
        $dedupeReviewComments = @($preIdentity.reviewComments)
        if ($null -ne $detectedRecoveryExclusions) {
            $dedupeReviews = @($dedupeReviews | Where-Object { [int64]$_.id -notin @($detectedRecoveryExclusions.reviewIds) })
            $dedupeReviewComments = @($dedupeReviewComments | Where-Object { [int64]$_.id -notin @($detectedRecoveryExclusions.reviewCommentIds) })
        }
        foreach ($finding in $request.findings) {
            Assert-Adapter ($finding.fingerprint -is [string] -and $finding.fingerprint.Length -gt 0) "Every finalized finding requires a fingerprint."
            $duplicate = @($dedupeIssueComments + $dedupeReviews + $dedupeReviewComments | Where-Object {
                $_.body -is [string] -and $_.body.Contains($finding.fingerprint)
            })
            Assert-Adapter ($duplicate.Count -eq 0) "A finalized finding is already covered by live feedback."
        }
        $matching = @($reviews | Where-Object {
            $_.commit_id -ceq $request.head -and
            $_.user.login -ceq $login -and
            $_.body -is [string] -and
            $_.body.Contains($request.marker)
        })
        Assert-Adapter ($matching.Count -le 1) "Multiple matching reviews already exist."
        if ($matching.Count -eq 1) {
            $reviewId = [int64]$matching[0].id
        }
        else {
            $beforeMutation = Get-StableLiveIdentity `
                -Owner $owner `
                -Repo $repo `
                -Pr ([int]$request.pr) `
                -BaseRef $request.baseRef `
                -FeedbackExclusions $persistentExclusions
            Assert-ExpectedIdentity -Identity $beforeMutation -Request $request
            $created = Invoke-GhJson -Method POST -Endpoint $mutation.endpoint -Body $mutation.body | ConvertFrom-Json -AsHashtable -Depth 100
            $reviewId = [int64]$created.id
        }

        $review = Invoke-GhJson -Method GET -Endpoint "$pullEndpoint/reviews/$reviewId" -Body $null | ConvertFrom-Json -AsHashtable -Depth 100
        $comments = @(Invoke-GhJsonPages "$pullEndpoint/reviews/$reviewId/comments?per_page=100")
        Assert-Adapter ($review.commit_id -ceq $request.head -and $review.state -ceq "COMMENTED") "Review readback identity mismatch."
        Assert-Adapter ($review.user.login -ceq $login) "Review readback author mismatch."
        Assert-Adapter ($review.body -ceq $request.body) "Review readback body mismatch."
        Assert-Adapter ($comments.Count -eq @($request.comments).Count) "Review readback inline-comment count mismatch."
        foreach ($expected in $request.comments) {
            $found = @($comments | Where-Object {
                $_.user.login -ceq $login -and
                $_.commit_id -ceq $request.head -and
                $null -eq (Get-ObjectValue $_ "in_reply_to_id") -and
                $_.path -ceq $expected.path -and
                [int]$_.line -eq [int]$expected.line -and
                $_.side -ceq "RIGHT" -and
                $_.body -ceq $expected.body
            })
            Assert-Adapter ($found.Count -eq 1) "Review readback did not match an expected inline comment."
        }
        Assert-UniqueValues -Items $comments -Selector { param($item) [int64]$item.id } -Label "Published review comments"
        $postExclusions = [ordered]@{
            issueCommentIds = @($persistentExclusions.issueCommentIds)
            reviewIds = @([int64]$reviewId)
            reviewCommentIds = @($comments | ForEach-Object { [int64]$_.id })
        }
        if ($null -ne $detectedRecoveryExclusions) {
            Assert-Adapter ((ConvertTo-SfroCanonicalJson $detectedRecoveryExclusions) -ceq (ConvertTo-SfroCanonicalJson $postExclusions)) "Recovered review exclusions differ from readback."
        }
        $postIdentity = Get-StableLiveIdentity `
            -Owner $owner `
            -Repo $repo `
            -Pr ([int]$request.pr) `
            -BaseRef $request.baseRef `
            -FeedbackExclusions $postExclusions
        Assert-ExpectedIdentity -Identity $postIdentity -Request $request

        [ordered]@{
            liveHead = $request.head
            baseRef = $request.baseRef
            baseTip = $request.baseTip
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
            feedbackExclusions = $persistentExclusions
            recoveryExclusions = $postExclusions
            preIdentity = $preIdentity
            postIdentity = $postIdentity
        }
    }
    "PublishClean" {
        $labelsBefore = @(Invoke-GhJsonPages "repos/$owner/$repo/issues/$($request.pr)/labels?per_page=100")
        $commentsBefore = @($preIdentity.issueComments)
        $labelWasAbsent = -not ($labelsBefore | Where-Object { $_.name -ceq "agent-reviewed" })
        $allMarkersBefore = @($commentsBefore | Where-Object {
            $_.body -is [string] -and $_.body.Contains($request.marker)
        })
        $matchingBefore = @($allMarkersBefore | Where-Object {
            $_.body -ceq $mutation.comment.body.body -and
            $_.user.login -ceq $login -and
            $_.issue_url.EndsWith("/issues/$($request.pr)", [StringComparison]::Ordinal)
        })
        Assert-Adapter ($allMarkersBefore.Count -le 1 -and $matchingBefore.Count -eq $allMarkersBefore.Count) "A conflicting clean marker comment exists."
        $markerWasAbsent = $matchingBefore.Count -eq 0
        Assert-Adapter (-not ($markerWasAbsent -and -not $labelWasAbsent)) "The clean label exists without its controller marker."

        if ($markerWasAbsent) {
            $beforeMarker = Get-StableLiveIdentity `
                -Owner $owner `
                -Repo $repo `
                -Pr ([int]$request.pr) `
                -BaseRef $request.baseRef `
                -FeedbackExclusions $persistentExclusions
            Assert-ExpectedIdentity -Identity $beforeMarker -Request $request
            $createdMarker = Invoke-GhJson -Method POST -Endpoint $mutation.comment.endpoint -Body $mutation.comment.body | ConvertFrom-Json -AsHashtable -Depth 100
            $markerId = [int64]$createdMarker.id
        }
        else {
            $markerId = [int64]$matchingBefore[0].id
        }
        $commentsAfter = @(Invoke-GhJsonPages "repos/$owner/$repo/issues/$($request.pr)/comments?per_page=100")
        $allMarkersAfter = @($commentsAfter | Where-Object {
            $_.body -is [string] -and $_.body.Contains($request.marker)
        })
        $matchingComments = @($commentsAfter | Where-Object {
            [int64]$_.id -eq $markerId -and
            $_.body -ceq $mutation.comment.body.body -and
            $_.user.login -ceq $login -and
            $_.issue_url.EndsWith("/issues/$($request.pr)", [StringComparison]::Ordinal)
        })
        Assert-Adapter ($allMarkersAfter.Count -eq 1 -and $matchingComments.Count -eq 1) "Clean marker readback failed."
        $postMarkerExclusions = [ordered]@{
            issueCommentIds = @($persistentExclusions.issueCommentIds) + @($markerId)
            reviewIds = @()
            reviewCommentIds = @()
        }
        $postMarkerIdentity = Get-StableLiveIdentity `
            -Owner $owner `
            -Repo $repo `
            -Pr ([int]$request.pr) `
            -BaseRef $request.baseRef `
            -FeedbackExclusions $postMarkerExclusions
        Assert-ExpectedIdentity -Identity $postMarkerIdentity -Request $request

        if ($labelWasAbsent) {
            $beforeLabel = Get-StableLiveIdentity `
                -Owner $owner `
                -Repo $repo `
                -Pr ([int]$request.pr) `
                -BaseRef $request.baseRef `
                -FeedbackExclusions $postMarkerExclusions
            Assert-ExpectedIdentity -Identity $beforeLabel -Request $request
            Invoke-GhJson -Method POST -Endpoint $mutation.label.endpoint -Body $mutation.label.body | Out-Null
        }
        $labelsAfter = @(Invoke-GhJsonPages "repos/$owner/$repo/issues/$($request.pr)/labels?per_page=100")
        $matchingLabels = @($labelsAfter | Where-Object { $_.name -ceq "agent-reviewed" })
        Assert-Adapter ($matchingLabels.Count -eq 1) "Clean label readback failed."
        $postIdentity = Get-StableLiveIdentity `
            -Owner $owner `
            -Repo $repo `
            -Pr ([int]$request.pr) `
            -BaseRef $request.baseRef `
            -FeedbackExclusions $postMarkerExclusions
        Assert-ExpectedIdentity -Identity $postIdentity -Request $request

        [ordered]@{
            liveHead = $request.head
            baseRef = $request.baseRef
            baseTip = $request.baseTip
            diffSha256 = $request.diffSha256
            feedbackSha256 = $request.feedbackSha256
            feedbackExclusions = $persistentExclusions
            recoveryExclusions = $postMarkerExclusions
            panelComplete = $true
            allOriginalsNoFindings = $true
            assessmentSurvivorCount = 0
            labelWasAbsent = $labelWasAbsent
            markerWasAbsent = $markerWasAbsent
            labelCountAfter = $matchingLabels.Count
            markerCountAfter = $matchingComments.Count
            readbackVerified = $true
            preIdentity = $preIdentity
            postIdentity = $postIdentity
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
