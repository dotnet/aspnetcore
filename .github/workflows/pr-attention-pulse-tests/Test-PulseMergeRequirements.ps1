#!/usr/bin/env pwsh
#Requires -Version 7.0

param([string[]]$ProducerFixturePath, [string]$BaselineSupportRoot)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Test-PRAttentionPulse.ps1") -FunctionsOnly
$testRoot = $PSScriptRoot
$workflowRoot = Split-Path -Parent $testRoot
$supportRoot = Join-Path $workflowRoot "pr-attention-pulse"
if ($BaselineSupportRoot)
{
    $supportRoot = $BaselineSupportRoot
}
$fixtureRoot = Join-Path $testRoot "fixtures"
$sanitizerPath = Join-Path $supportRoot "Sanitize-PRAttentionPulse.ps1"
$combinerPath = Join-Path $supportRoot "Combine-PRAttentionPulse.ps1"
$rendererPath = Join-Path $supportRoot "Render-PRAttentionPulse.ps1"
$validatorPath = Join-Path $supportRoot "Validate-PRAttentionPulseOutput.ps1"
$lockPath = Join-Path $workflowRoot "pr-attention-pulse.lock.yml"
$attemptTimestamp = [datetime]"2026-09-10T20:04:56Z"
$tempRoot = Join-Path $testRoot ".merge-test-$([guid]::NewGuid().ToString('N'))"
$collectorJsRoot = Join-Path (Get-GhAwExtensionRoot) "actions\setup\js"
$collectorSanitizerPath = Join-Path $collectorJsRoot "sanitize_content.cjs"
$failures = [Collections.Generic.List[string]]::new()
$caseCount = 0

function Invoke-MergeCase
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

function New-MergeRaw
{
    param([string]$Scope = "blazor", [string]$State = "clear", [int]$CandidateLimit = 20, [int]$VerificationLimit = 3)
    $raw = Get-Content -LiteralPath (Join-Path $fixtureRoot "normal-legacy.json") -Raw | ConvertFrom-Json -Depth 100
    if ($Scope -ceq "repository-wide")
    {
        $raw.filter = [pscustomobject]@{
            name = "adhoc"; description = "Ad hoc pull-request scope"; coverage = "all-repo"
            selection = "(all open pull requests)"; allRepositoryPullRequests = $true
        }
        foreach ($item in $raw.items)
        {
            $item.scopeMatch = "all-repo"
        }
    }
    $raw.discussion.candidateLimit = $CandidateLimit
    $raw.caps.readyToMerge = $VerificationLimit
    $raw | Add-Member mergeDiscussion ([pscustomobject]@{
        candidateLimit = $CandidateLimit; assessedCandidateCount = 1; verificationNeededCount = 0
        unassessedCandidateCount = 0; eligibleCount = 1; excludedCandidateCount = 0; verificationLimit = $VerificationLimit
    })
    foreach ($item in $raw.items)
    {
        $item | Add-Member mergeEligibility "not-candidate"
        $item | Add-Member shownInMergeVerification $false
        $item | Add-Member mergeVerificationRank $null
    }
    $ready = $raw.items[4]
    $ready.nextActor = "merger"
    $ready.mergeEligibility = "eligible"
    $ready.discussionAssessment = $raw.items[1].discussionAssessment | ConvertTo-Json -Depth 20 | ConvertFrom-Json -Depth 20
    if ($State -cne "clear")
    {
        $ready.mergeEligibility = $State
        $ready.discussionAssessment.state = $State
        $ready.discussionAssessment.complete = $false
        $ready.discussionAssessment.threads.complete = $false
        $ready.discussionAssessment.signals = @($(if ($State -ceq "not-assessed") { "discussion-not-assessed" } else { "discussion-incomplete" }))
        $ready.shownInDigest = $false
        $ready.digestRank = $null
        $ready.shownInMergeVerification = $VerificationLimit -gt 0
        $ready.mergeVerificationRank = if ($VerificationLimit -gt 0) { 1 } else { $null }
        $raw.overflow.readyToMerge = 1
        $raw.mergeDiscussion.eligibleCount = 0
        if ($State -ceq "not-assessed")
        {
            $raw.mergeDiscussion.assessedCandidateCount = 0
            $raw.mergeDiscussion.unassessedCandidateCount = 1
        }
        else
        {
            $raw.mergeDiscussion.verificationNeededCount = 1
        }
    }
    return $raw
}

function Convert-MergeRaw
{
    param([object]$Raw, [string]$Scope = "blazor")
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

New-Item -ItemType Directory $tempRoot | Out-Null
try
{
    foreach ($scope in @("blazor", "repository-wide"))
    {
        foreach ($state in @("clear", "verification-needed", "not-assessed"))
        {
            Invoke-MergeCase "Consumer/$scope/$state" {
                $pulse = Convert-MergeRaw -Raw (New-MergeRaw -Scope $scope -State $state) -Scope $scope
                Assert-True ($pulse.status -ceq "complete") "The complete merge extension must be accepted."
                Assert-True ($pulse.source.mergeDiscussion.candidateLimit -eq 20 -and $pulse.source.mergeDiscussion.verificationLimit -eq 3) "Merge budgets must survive sanitization."
                $view = if ($state -ceq "clear") { "readyToMerge" } else { "verifyDiscussionBeforeMerge" }
                Assert-True (@($pulse.views.$view).Count -eq 1) "The expected merge view must contain its selected candidate."
                Assert-True ($pulse.views.$view[0].discussionAssessment.state -ceq $state) "Sanitization must preserve merge assessment evidence."
                Assert-True ($pulse.views.$view[0].nextActor -ceq "merger") "Verification must not manufacture an author blocker."
                $otherScope = if ($scope -ceq "blazor") { "repository-wide" } else { "blazor" }
                $other = Convert-MergeRaw -Raw (New-MergeRaw -Scope $otherScope -State $state) -Scope $otherScope
                $combined = if ($scope -ceq "blazor") { New-CombinedPulse $pulse $other } else { New-CombinedPulse $other $pulse }
                $body = Invoke-Renderer -Pulse $combined
                Assert-True ([regex]::Matches($body, "(?m)^## Verify discussion before merge$").Count -eq 2) "Both scopes require explicit merge verification."
                Assert-True ([regex]::Matches($body, "(?m)^<details>$").Count -eq 2 -and -not $body.Contains("<details open>")) "Both scopes remain collapsed."
                Assert-True ($body.IndexOf("<strong>Blazor</strong>") -lt $body.IndexOf("<strong>Repository-wide</strong>")) "Scope order must remain exact."
                Assert-True (-not ($body -match "\b[0-9]+d idle\b")) "Age remains days-open only."
                $canonical = Invoke-PinnedOutputSanitizer -Content $body
                Invoke-PublicationValidator -Pulse $combined -AgentOutput (Invoke-PinnedCollector -Body $canonical) -ExpectedBody $canonical
                $changed = $canonical.Replace("## Verify discussion before merge", "## No merge verification needed")
                Assert-Throws { Invoke-PublicationValidator -Pulse $combined -AgentOutput (New-ValidAgentOutput $changed) -ExpectedBody $canonical } "Tampered merge verification must fail closed." "The emitted body does not exactly match"
            }
        }
        foreach ($budget in @(
            @{ Candidate = 3; Verification = 1 },
            @{ Candidate = 21; Verification = 5 },
            @{ Candidate = 3; Verification = 0 }))
        {
            Invoke-MergeCase "InheritedBudgets/$scope/$($budget.Candidate)/$($budget.Verification)" {
                $raw = New-MergeRaw -Scope $scope -State "verification-needed" -CandidateLimit $budget.Candidate -VerificationLimit $budget.Verification
                $pulse = Convert-MergeRaw -Raw $raw -Scope $scope
                Assert-True ($pulse.status -ceq "complete") "Nondefault existing budgets must not require new Pulse settings."
                Assert-True ($pulse.source.mergeDiscussion.candidateLimit -eq $budget.Candidate -and
                    $pulse.source.mergeDiscussion.verificationLimit -eq $budget.Verification) "Merge budgets must inherit the existing producer limits exactly."
                $expectedShown = if ($budget.Verification -eq 0) { 0 } else { 1 }
                Assert-True (@($pulse.views.verifyDiscussionBeforeMerge).Count -eq $expectedShown) "A zero caller cap must retain inventory without displaying candidates."
                Assert-True ($pulse.source.discussion.assessedCandidateCount -eq 3 -and $pulse.source.mergeDiscussion.assessedCandidateCount -eq 1) "Review and merge assessment budgets must be independent."
                $body = Invoke-Renderer $pulse
                Invoke-PublicationValidator -Pulse $pulse -AgentOutput (New-ValidAgentOutput $body) -ExpectedBody $body
            }
        }
    }
    Invoke-MergeCase "InheritedBudgets/legacy-raw" {
        $raw = Get-Content -LiteralPath (Join-Path $fixtureRoot "normal-legacy.json") -Raw | ConvertFrom-Json -Depth 100
        $raw.discussion.candidateLimit = 3
        $raw.caps.readyToMerge = 5
        $pulse = Convert-MergeRaw $raw
        Assert-True ($pulse.status -ceq "complete" -and $pulse.source.mergeDiscussion.candidateLimit -eq 3 -and
            $pulse.source.mergeDiscussion.verificationLimit -eq 5) "Legacy raw migration must retain the existing caller budgets."
    }
    Invoke-MergeCase "InheritedBudgets/legacy-sanitized" {
        $pulse = Get-Content -LiteralPath (Join-Path $fixtureRoot "presentation\published-34643961191.pulse.json") -Raw | ConvertFrom-Json -Depth 100
        $pulse | Add-Member scope "repository-wide"
        $pulse.source.discussion.candidateLimit = 21
        $pulse.source.caps.readyToMerge = 5
        foreach ($number in @(1, 2))
        {
            $item = $pulse.views.readyToMerge[0] | ConvertTo-Json -Depth 30 | ConvertFrom-Json -Depth 30
            $item.number = $number
            $item.rank = 3 + $number
            $pulse.views.readyToMerge += $item
        }
        $pulse.source.overflow.readyToMerge = 0
        $body = Invoke-Renderer $pulse
        Assert-True ($body.Contains("Merge discussion coverage: 0 of limit 21 assessed;") -and $body.Contains("Verification display cap: 5.")) "Legacy sanitized migration must inherit budgets rather than hardcode defaults."
        $section = Get-PresentationSection -Body $body -Name "Verify discussion before merge"
        Assert-True ([regex]::Matches($section, '(?m)^\| [0-9]+:').Count -eq 5) "Legacy migration must preserve all five selected candidates when the inherited display cap is five."
        Invoke-PublicationValidator -Pulse $pulse -AgentOutput (New-ValidAgentOutput $body) -ExpectedBody $body
    }
    Invoke-MergeCase "Legacy/raw-is-unassessed" {
        $pulse = Invoke-Sanitizer -FixtureName "normal-legacy.json"
        Assert-True ($pulse.status -ceq "complete") "Legacy input remains supported."
        Assert-True (@($pulse.views.readyToMerge).Count -eq 0) "Legacy records cannot imply merge eligibility."
        Assert-True ($pulse.source.mergeDiscussion.unassessedCandidateCount -eq $pulse.source.census.byBucket.ReadyToMerge) "The entire legacy ReadyToMerge inventory is unassessed."
        Assert-True (@($pulse.views.verifyDiscussionBeforeMerge).Count -eq 1) "Legacy selected candidates require merge verification."
    }
    Invoke-MergeCase "Legacy/sanitized-is-unassessed" {
        $pulse = Get-Content -LiteralPath (Join-Path $fixtureRoot "presentation\published-34643961191.pulse.json") -Raw | ConvertFrom-Json -Depth 100
        $pulse | Add-Member scope "repository-wide"
        $before = $pulse | ConvertTo-Json -Depth 100 -Compress
        $body = Invoke-Renderer -Pulse $pulse
        $readySection = Get-PresentationSection -Body $body -Name "Ready to merge"
        Assert-True (-not ($readySection -match "(?m)^\| [0-9]+:")) "Legacy sanitized candidates must not render as eligible."
        Assert-True ($body.Contains("## Verify discussion before merge")) "Legacy sanitized candidates need an explicit verification section."
        Assert-True (($pulse | ConvertTo-Json -Depth 100 -Compress) -ceq $before) "Legacy normalization must not mutate the source."
        Invoke-PublicationValidator -Pulse $pulse -AgentOutput (New-ValidAgentOutput $body) -ExpectedBody $body
    }
    $mutations = [ordered]@{
        "partial-root" = { param($r) $r.PSObject.Properties.Remove("mergeDiscussion") }
        "partial-item" = { param($r) $r.items[0].PSObject.Properties.Remove("mergeEligibility") }
        "partial-shown" = { param($r) $r.items[0].PSObject.Properties.Remove("shownInMergeVerification") }
        "partial-rank" = { param($r) $r.items[0].PSObject.Properties.Remove("mergeVerificationRank") }
        "count-mismatch" = { param($r) $r.mergeDiscussion.eligibleCount = 2 }
        "assessment-limit" = { param($r) $r.mergeDiscussion.candidateLimit = 21 }
        "verification-limit" = { param($r) $r.mergeDiscussion.verificationLimit = 4 }
        "forged-clear-incomplete" = { param($r) $r.items[4].discussionAssessment.complete = $false }
        "forged-clear-truncated" = { param($r) $r.items[4].discussionAssessment.commentEvidenceTruncated = $true }
        "forged-clear-signals" = { param($r) $r.items[4].discussionAssessment.signals = @("discussion-incomplete") }
        "forged-clear-missing" = { param($r) $r.items[4].discussionAssessment = $null }
        "forged-clear-unresolved" = { param($r) $r.items[4].discussionAssessment.threads.unresolvedCount = 1 }
        "forged-clear-thread-coverage" = { param($r) $r.items[4].discussionAssessment.threads.totalCount = 1 }
        "forged-clear-state-whitespace" = { param($r) $r.items[4].discussionAssessment.state = " clear " }
        "forged-clear-state-case" = { param($r) $r.items[4].discussionAssessment.state = "Clear" }
        "string-counter" = { param($r) $r.mergeDiscussion.eligibleCount = "1" }
        "overflow-mismatch" = { param($r) $r.overflow.readyToMerge = 1 }
        "census-bucket-mismatch" = { param($r) $r.census.byBucket.ReadyToMerge = 2; $r.census.byBucket.NeedsRescue = 0 }
        "noncandidate-eligibility" = { param($r) $r.items[0].mergeEligibility = "eligible" }
        "noncandidate-verification" = { param($r) $r.items[0].shownInMergeVerification = $true; $r.items[0].mergeVerificationRank = 1 }
        "duplicate-view" = { param($r) $r.items[4].shownInMergeVerification = $true; $r.items[4].mergeVerificationRank = 1 }
    }
    foreach ($name in $mutations.Keys)
    {
        Invoke-MergeCase "Reject/$name" {
            $raw = New-MergeRaw
            & $mutations[$name] $raw
            $pulse = Convert-MergeRaw $raw
            Assert-True ($pulse.status -ceq "unavailable" -and $pulse.errorCategory -ceq "invalid-contract") "Malformed or forged merge extension must fail closed."
        }
    }
    foreach ($exclusion in @("excluded-author", "stacked-on-unhealthy-pr"))
    {
        Invoke-MergeCase "Excluded/$exclusion" {
            $raw = New-MergeRaw
            $raw.items[4] | Add-Member digestExclusionReasons @($exclusion)
            $raw.items[4].mergeEligibility = "not-assessed"
            $raw.items[4].discussionAssessment = $null
            $raw.items[4].shownInDigest = $false
            $raw.items[4].digestRank = $null
            $raw.mergeDiscussion.assessedCandidateCount = 0
            $raw.mergeDiscussion.eligibleCount = 0
            $raw.mergeDiscussion.excludedCandidateCount = 1
            $raw.overflow.readyToMerge = 1
            $pulse = Convert-MergeRaw $raw
            Assert-True ($pulse.status -ceq "complete") "Preexisting excluded ready inventory must be accepted without fabricated assessments."
            Assert-True ($pulse.source.mergeDiscussion.excludedCandidateCount -eq 1 -and $pulse.source.mergeDiscussion.unassessedCandidateCount -eq 0) "Excluded and unassessed counts must remain separate."
            Assert-True (@($pulse.views.readyToMerge).Count -eq 0 -and @($pulse.views.verifyDiscussionBeforeMerge).Count -eq 0) "Excluded candidates must consume neither display budget."
            $raw.items[4].shownInMergeVerification = $true
            $raw.items[4].mergeVerificationRank = 1
            Assert-True ((Convert-MergeRaw $raw).status -ceq "unavailable") "Excluded candidates cannot enter verification."
        }
    }
    Invoke-MergeCase "Bounded/verification-cap-and-inventory" {
        $raw = New-MergeRaw -State "verification-needed"
        foreach ($number in 106..109)
        {
            $item = $raw.items[4] | ConvertTo-Json -Depth 30 | ConvertFrom-Json -Depth 30
            $item.number = $number
            $item.mergeVerificationRank = if ($number -le 107) { $number - 104 } else { $null }
            $item.shownInMergeVerification = $number -le 107
            $raw.items += $item
        }
        $raw.query.openPullRequestCount = 9
        $raw.query.returnedPullRequestCount = 9
        $raw.census.openPullRequests = 9
        $raw.census.matched = 9
        $raw.census.byBucket.ReadyToMerge = 5
        $raw.mergeDiscussion.assessedCandidateCount = 5
        $raw.mergeDiscussion.verificationNeededCount = 5
        $raw.overflow.readyToMerge = 5
        $pulse = Convert-MergeRaw $raw
        Assert-True ($pulse.status -ceq "complete" -and @($pulse.views.verifyDiscussionBeforeMerge).Count -eq 3) "Verification display is bounded independently of five assessed candidates."
        Assert-True ($pulse.source.mergeDiscussion.verificationNeededCount -eq 5 -and $pulse.source.overflow.readyToMerge -eq 5) "Undisplayed verification inventory must remain visible."
        $body = Invoke-Renderer -Pulse $pulse
        Invoke-PublicationValidator -Pulse $pulse -AgentOutput (New-ValidAgentOutput $body) -ExpectedBody $body
        $raw.items[7].shownInMergeVerification = $true
        $raw.items[7].mergeVerificationRank = 4
        Assert-True ((Convert-MergeRaw $raw).status -ceq "unavailable") "Four selected verification rows must fail closed."
    }
    $sanitizedMutations = [ordered]@{
        "missing-view" = { param($p) $p.views.PSObject.Properties.Remove("verifyDiscussionBeforeMerge") }
        "missing-counts" = { param($p) $p.source.PSObject.Properties.Remove("mergeDiscussion") }
        "counter-mismatch" = { param($p) $p.source.mergeDiscussion.eligibleCount++ }
        "candidate-budget-mismatch" = { param($p) $p.source.mergeDiscussion.candidateLimit++ }
        "display-budget-mismatch" = { param($p) $p.source.mergeDiscussion.verificationLimit++ }
        "missing-eligibility" = { param($p) $p.views.readyToMerge[0].PSObject.Properties.Remove("mergeEligibility") }
        "incomplete-clear" = { param($p) $p.views.readyToMerge[0].discussionAssessment.complete = $false }
        "unclear-eligible" = { param($p) $p.views.readyToMerge[0].discussionAssessment.signals = @("discussion-incomplete") }
        "forged-eligible" = { param($p) $p.views.readyToMerge[0].mergeEligibility = "not-assessed" }
        "wrong-view-bucket" = { param($p) $p.views.readyToMerge[0].bucket = "ReviewNow" }
    }
    foreach ($name in $sanitizedMutations.Keys)
    {
        Invoke-MergeCase "PrivateValidator/$name" {
            $pulse = Convert-MergeRaw (New-MergeRaw)
            Assert-True ($pulse.status -ceq "complete") "The positive source must reach the private validator."
            $body = Invoke-Renderer $pulse
            & $sanitizedMutations[$name] $pulse
            Assert-Throws { Invoke-PublicationValidator -Pulse $pulse -AgentOutput (New-ValidAgentOutput $body) -ExpectedBody $body } "Tampered sanitized merge input must fail closed."
        }
    }
    Invoke-MergeCase "NegativeControl/wrong-repository" {
        Assert-True ((Invoke-Sanitizer -FixtureName "wrong-repository.json").errorCategory -ceq "unexpected-repository") "Existing repository validation must remain."
    }
    Invoke-MergeCase "NegativeControl/review-discussion-unchanged" {
        $pulse = Invoke-Sanitizer -FixtureName "normal-legacy.json"
        Assert-True (@($pulse.views.reviewNow).Count -eq 2 -and @($pulse.views.verifyDiscussionBeforeReview).Count -eq 1) "Review discussion membership must remain unchanged."
    }
    $producerAreas = @{}
    $queueTestRoot = Join-Path (Split-Path -Parent $workflowRoot) "skills\pr-attention-queue\tests"
    . (Join-Path $queueTestRoot "MergeEligibilityTestHelpers.ps1")
    foreach ($scope in @("blazor", "repository-wide"))
    {
        foreach ($maximum in @(0, 1, 5))
        {
            Invoke-MergeCase "Producer/caller-merge-cap/$scope/$maximum" {
                $fixtures = @(
                    foreach ($number in 1..5)
                    {
                        $item = @(Get-Content -LiteralPath (Join-Path $queueTestRoot "fixtures\merge-eligibility.json") -Raw | ConvertFrom-Json -Depth 100)[2]
                        $item.number = $number
                        $item.threads = @(@{ isResolved = $false; isOutdated = $false })
                        $item
                    }
                )
                $run = Invoke-MergeFixtureQueue -PullRequests $fixtures -Scope $scope -Maximum $maximum
                $raw = $run.output | ConvertFrom-Json -Depth 100
                $pulse = Convert-MergeRaw -Raw $raw -Scope $scope
                Assert-True ($pulse.status -ceq "complete") "The consumer must accept the actual caller merge cap."
                Assert-True ($pulse.source.mergeDiscussion.verificationLimit -eq $maximum -and
                    @($pulse.views.verifyDiscussionBeforeMerge).Count -eq $maximum) "Merge verification must share MaxReadyToMerge, including zero and values above three."
                Assert-True ($run.discussionNumbers.Count -eq 5 -and $pulse.source.mergeDiscussion.assessedCandidateCount -eq 5) "A caller display cap must not reduce the independent assessment budget."
                $body = Invoke-Renderer $pulse
                Invoke-PublicationValidator -Pulse $pulse -AgentOutput (New-ValidAgentOutput $body) -ExpectedBody $body
            }
        }
        Invoke-MergeCase "Producer/real-snapshots/$scope" {
            $fixtures = @(Get-Content -LiteralPath (Join-Path $queueTestRoot "fixtures\merge-eligibility.json") -Raw | ConvertFrom-Json -Depth 100)
            $run = Invoke-MergeFixtureQueue -PullRequests $fixtures -Scope $scope
            $raw = $run.output | ConvertFrom-Json -Depth 100
            $pulse = Convert-MergeRaw -Raw $raw -Scope $scope
            Assert-True ($pulse.status -ceq "complete") "Actual bounded producer evidence must survive sanitization."
            Assert-True ((@($pulse.views.readyToMerge).number -join ",") -ceq "67695") "The genuinely clear approved snapshot must remain selected."
            $expectedVerification = if ($scope -ceq "repository-wide") { "65785" } else { "" }
            Assert-True ((@($pulse.views.verifyDiscussionBeforeMerge).number -join ",") -ceq $expectedVerification) "The inline-policy snapshot must remain prospective verification only in its actual scope."
            $expectedCollected = if ($scope -ceq "repository-wide") { "65785,67695" } else { "67695" }
            Assert-True ((@($run.discussionNumbers | Sort-Object) -join ",") -ceq $expectedCollected) "The owning producer must actually collect the displayed merge evidence."
            $producerAreas[$scope] = $pulse
        }
        foreach ($scenario in @("outdated-only", "complete-excerpts-truncated", "bounded-unassessed-and-excluded"))
        {
            Invoke-MergeCase "Producer/$scenario/$scope" {
                $clear = @(Get-Content -LiteralPath (Join-Path $queueTestRoot "fixtures\merge-eligibility.json") -Raw | ConvertFrom-Json -Depth 100)[2]
                $clear.comments = @()
                $clear.requests = @()
                $excludedAuthors = @()
                $fixtures = @($clear)
                if ($scenario -ceq "outdated-only")
                {
                    $clear.threads = @(@{ isResolved = $false; isOutdated = $true })
                }
                elseif ($scenario -ceq "complete-excerpts-truncated")
                {
                    $clear.comments = @(
                        foreach ($index in 1..11)
                        {
                            @{ author = $clear.author; createdAt = "2026-09-15T00:00:00Z"; bodyText = "Thanks!"; authorAssociation = "NONE" }
                        }
                    )
                }
                else
                {
                    $fixtures = @(
                        foreach ($number in 1..25)
                        {
                            $item = $clear | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100
                            $item.number = $number
                            $item
                        }
                    )
                    $excluded = $clear | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100
                    $excluded.number = 99
                    $excluded.author.login = "excluded-user"
                    $fixtures += $excluded
                    $excludedAuthors = @("excluded-user")
                }
                $run = Invoke-MergeFixtureQueue -PullRequests $fixtures -Scope $scope -ExcludedAuthors $excludedAuthors
                $raw = $run.output | ConvertFrom-Json -Depth 100
                $pulse = Convert-MergeRaw -Raw $raw -Scope $scope
                Assert-True ($pulse.status -ceq "complete") "The consumer must preserve the module's actual assessment semantics."
                if ($scenario -ceq "bounded-unassessed-and-excluded")
                {
                    Assert-True ($run.discussionNumbers.Count -eq 20 -and $run.discussionNumbers -notcontains 99) "The producer must enforce the actual assessment budget and exclusion before collection."
                    Assert-True ($pulse.source.mergeDiscussion.assessedCandidateCount -eq 20 -and
                        $pulse.source.mergeDiscussion.eligibleCount -eq 20 -and
                        $pulse.source.mergeDiscussion.unassessedCandidateCount -eq 5 -and
                        $pulse.source.mergeDiscussion.excludedCandidateCount -eq 1) "The entire real inventory must be accounted for without double counting."
                    Assert-True ((@($pulse.views.readyToMerge).number -join ",") -ceq "1,2,3") "Ready display must preserve the producer cap and ranking."
                    Assert-True ((@($pulse.views.verifyDiscussionBeforeMerge).number -join ",") -ceq "21,22,23") "Unassessed candidates need the separate bounded verification view."
                    Assert-True ($pulse.source.overflow.readyToMerge -eq 23) "Ready overflow remains full inventory minus ordinary display."
                }
                else
                {
                    Assert-True ((@($pulse.views.readyToMerge).number -join ",") -ceq "67695") "Pulse must not independently reclassify a complete clear module assessment."
                    $assessment = $pulse.views.readyToMerge[0].discussionAssessment
                    if ($scenario -ceq "outdated-only")
                    {
                        Assert-True ($assessment.threads.unresolvedCount -eq 1 -and $assessment.threads.outdatedUnresolvedCount -eq 1) "Outdated-only evidence must actually exercise the producer path."
                    }
                    else
                    {
                        Assert-True ($assessment.complete -and $assessment.commentEvidenceTruncated -and $assessment.commentTotalCount -eq 11) "Excerpt truncation must not be confused with incomplete collection."
                    }
                }
                $body = Invoke-Renderer $pulse
                Invoke-PublicationValidator -Pulse $pulse -AgentOutput (New-ValidAgentOutput $body) -ExpectedBody $body
            }
        }
    }
    foreach ($fixturePath in $ProducerFixturePath)
    {
        $fixtureName = Split-Path -Leaf $fixturePath
        Invoke-MergeCase "Producer/$fixtureName" {
            $raw = Get-Content -LiteralPath $fixturePath -Raw | ConvertFrom-Json -Depth 100
            $scope = if ($raw.filter.allRepositoryPullRequests) { "repository-wide" } else { "blazor" }
            Assert-True ($null -ne $raw.mergeDiscussion) "Producer integration requires the full extension, not a legacy fallback."
            $pulse = Convert-MergeRaw -Raw $raw -Scope $scope
            Assert-True ($pulse.status -ceq "complete") "The actual producer output must survive sanitization."
            foreach ($viewName in @("readyToMerge", "verifyDiscussionBeforeMerge"))
            {
                $shownProperty, $rankProperty = if ($viewName -ceq "readyToMerge") { "shownInDigest", "digestRank" } else { "shownInMergeVerification", "mergeVerificationRank" }
                $expected = @($raw.items | Where-Object { $_.bucket -ceq "ReadyToMerge" -and $_.$shownProperty } | Sort-Object $rankProperty)
                $actual = @($pulse.views.$viewName)
                Assert-True (($actual.number -join ",") -ceq ($expected.number -join ",")) "The consumer must preserve actual producer membership and ranking in $viewName."
                for ($index = 0; $index -lt $actual.Count; $index++)
                {
                    Assert-True ($actual[$index].mergeEligibility -ceq $expected[$index].mergeEligibility -and
                        ($actual[$index].discussionAssessment.signals -join ",") -ceq ($expected[$index].discussionAssessment.signals -join ",")) "Producer eligibility and stable discussion signals must remain unchanged."
                }
            }
            $producerAreas[$scope] = $pulse
        }
    }
    Invoke-MergeCase "Producer/BothScopesPublication" {
        Assert-True ($producerAreas.ContainsKey("blazor") -and $producerAreas.ContainsKey("repository-wide")) "Producer integration must include both independent scopes."
        $combined = New-CombinedPulse -Blazor $producerAreas.blazor -RepositoryWide $producerAreas["repository-wide"]
        $body = Invoke-PinnedOutputSanitizer (Invoke-Renderer $combined)
        $collected = Invoke-PinnedCollector $body
        Invoke-PublicationValidator -Pulse $combined -AgentOutput $collected -ExpectedBody $body
        $bodyPath = Join-Path $tempRoot "producer-publication.txt"
        [IO.File]::WriteAllText($bodyPath, $body, [Text.UTF8Encoding]::new($false))
        & node (Join-Path $testRoot "Test-PulseIssueUpdate.cjs") $collectorJsRoot $lockPath $bodyPath
        Assert-True ($LASTEXITCODE -eq 0) "The pinned publication handler must retain the combined producer body exactly."
    }
}
finally
{
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Output "Pulse merge requirements: $($caseCount - $failures.Count)/$caseCount passed; $($failures.Count) failed."
if ($failures.Count -gt 0)
{
    throw "$($failures.Count) merge requirement cases failed."
}
