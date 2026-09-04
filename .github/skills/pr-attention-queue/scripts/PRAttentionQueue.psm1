#Requires -Version 7.0

$ErrorActionPreference = "Stop"

function Get-PropertyValue {
    param(
        [object]$Object,
        [string]$Name,
        [object]$DefaultValue = $null
    )

    if ($null -eq $Object) {
        return $DefaultValue
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $DefaultValue
    }

    return $property.Value
}

function ConvertTo-Array {
    param([object]$Value)

    if ($null -eq $Value) {
        return @()
    }

    return @($Value)
}

function Invoke-GhJson {
    param(
        [string[]]$Arguments,
        [ValidateRange(1, 5)]
        [int]$MaxAttempts = 3
    )

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = "gh"
        $startInfo.UseShellExecute = $false
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true

        foreach ($argument in $Arguments) {
            $startInfo.ArgumentList.Add($argument)
        }

        $process = [System.Diagnostics.Process]::new()
        $process.StartInfo = $startInfo

        if (-not $process.Start()) {
            throw "Failed to start the GitHub CLI."
        }

        $stdout = $process.StandardOutput.ReadToEnd()
        $stderr = $process.StandardError.ReadToEnd()
        $process.WaitForExit()

        if ($process.ExitCode -eq 0) {
            if ([string]::IsNullOrWhiteSpace($stdout)) {
                return $null
            }

            return $stdout | ConvertFrom-Json -Depth 100
        }

        $message = if ([string]::IsNullOrWhiteSpace($stderr)) { $stdout } else { $stderr }
        $isTransient = $message -match "HTTP 50[234]|Bad Gateway|timeout|timed out|stream error|dial tcp|connection abort|connection reset"

        if (-not $isTransient -or $attempt -eq $MaxAttempts) {
            throw "GitHub CLI failed: $($message.Trim())"
        }

        Start-Sleep -Seconds ([Math]::Pow(2, $attempt))
    }

    throw "GitHub CLI failed after $MaxAttempts attempts."
}

function Test-AnyWildcardMatch {
    param(
        [string[]]$Values,
        [string[]]$Patterns
    )

    foreach ($pattern in $Patterns) {
        foreach ($value in $Values) {
            if ($value -like $pattern) {
                return $true
            }
        }
    }

    return $false
}

function Test-AllWildcardMatches {
    param(
        [string[]]$Values,
        [string[]]$Patterns
    )

    foreach ($pattern in $Patterns) {
        if (-not (Test-AnyWildcardMatch -Values $Values -Patterns @($pattern))) {
            return $false
        }
    }

    return $true
}

function Test-IsBotLogin {
    param(
        [string]$Login,
        [bool]$IsBot,
        [string[]]$KnownBotPatterns
    )

    if ($IsBot -or [string]::IsNullOrWhiteSpace($Login)) {
        return $IsBot
    }

    if ($Login.StartsWith("app/", [System.StringComparison]::OrdinalIgnoreCase) -or
        $Login.EndsWith("[bot]", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    return Test-AnyWildcardMatch -Values @($Login) -Patterns $KnownBotPatterns
}

function Get-LabelNames {
    param([object]$PullRequest)

    return @(
        foreach ($labelValue in ConvertTo-Array (Get-PropertyValue -Object $PullRequest -Name "labels")) {
            $name = Get-PropertyValue -Object $labelValue -Name "name"
            if (-not [string]::IsNullOrWhiteSpace($name)) {
                $name
            }
        }
    )
}

function Get-FilePaths {
    param([object]$PullRequest)

    return @(
        foreach ($file in ConvertTo-Array (Get-PropertyValue -Object $PullRequest -Name "files")) {
            $pathValue = Get-PropertyValue -Object $file -Name "path"
            if (-not [string]::IsNullOrWhiteSpace($pathValue)) {
                $pathValue
            }
        }
    )
}

function Get-AuthorInfo {
    param([object]$PullRequest)

    $authorValue = Get-PropertyValue -Object $PullRequest -Name "author"
    return [pscustomobject]@{
        Login = [string](Get-PropertyValue -Object $authorValue -Name "login" -DefaultValue "unknown")
        IsBot = [bool](Get-PropertyValue -Object $authorValue -Name "is_bot" -DefaultValue $false)
    }
}

function Get-CheckState {
    param([object]$PullRequest)

    $checks = ConvertTo-Array (Get-PropertyValue -Object $PullRequest -Name "statusCheckRollup")
    if ($checks.Count -eq 0) {
        return "Unknown"
    }

    $hasPending = $false

    foreach ($check in $checks) {
        $conclusion = [string](Get-PropertyValue -Object $check -Name "conclusion" -DefaultValue "")
        $status = [string](Get-PropertyValue -Object $check -Name "status" -DefaultValue "")
        $state = [string](Get-PropertyValue -Object $check -Name "state" -DefaultValue "")

        if ($conclusion -in @("FAILURE", "CANCELLED", "TIMED_OUT", "ACTION_REQUIRED", "STARTUP_FAILURE") -or
            $state -in @("ERROR", "FAILURE")) {
            return "Failed"
        }

        if (($status -and $status -ne "COMPLETED") -or $state -in @("PENDING", "EXPECTED")) {
            $hasPending = $true
        }
    }

    if ($hasPending) {
        return "Pending"
    }

    return "Passing"
}

function Get-HumanReviews {
    param(
        [object]$PullRequest,
        [string[]]$KnownBotPatterns
    )

    return @(
        foreach ($review in ConvertTo-Array (Get-PropertyValue -Object $PullRequest -Name "latestReviews")) {
            $reviewAuthor = Get-PropertyValue -Object $review -Name "author"
            $login = [string](Get-PropertyValue -Object $reviewAuthor -Name "login" -DefaultValue "")
            $isBot = [bool](Get-PropertyValue -Object $reviewAuthor -Name "is_bot" -DefaultValue $false)
            $state = [string](Get-PropertyValue -Object $review -Name "state" -DefaultValue "")
            $submittedAt = Get-PropertyValue -Object $review -Name "submittedAt"

            if ($state -ne "DISMISSED" -and
                $submittedAt -and
                -not (Test-IsBotLogin -Login $login -IsBot $isBot -KnownBotPatterns $KnownBotPatterns)) {
                $commit = Get-PropertyValue -Object $review -Name "commit"
                [pscustomobject]@{
                    Login = $login
                    State = $state
                    SubmittedAt = [datetime]$submittedAt
                    CommitOid = [string](Get-PropertyValue -Object $commit -Name "oid" -DefaultValue "")
                }
            }
        }
    ) | Sort-Object -Property SubmittedAt -Descending
}

function Get-LatestAuthorCommentAt {
    param(
        [object]$PullRequest,
        [string]$AuthorLogin
    )

    $dates = @(
        foreach ($comment in ConvertTo-Array (Get-PropertyValue -Object $PullRequest -Name "comments")) {
            $commentAuthor = Get-PropertyValue -Object $comment -Name "author"
            $login = [string](Get-PropertyValue -Object $commentAuthor -Name "login" -DefaultValue "")
            $createdAt = Get-PropertyValue -Object $comment -Name "createdAt"

            if ($login -eq $AuthorLogin -and $createdAt) {
                [datetime]$createdAt
            }
        }

    )

    if ($dates.Count -eq 0) {
        return $null
    }

    return $dates | Sort-Object -Descending | Select-Object -First 1
}

function Get-HumanReviewRequests {
    param(
        [object]$PullRequest,
        [string[]]$KnownBotPatterns
    )

    foreach ($request in ConvertTo-Array (Get-PropertyValue -Object $PullRequest -Name "reviewRequests")) {
        $login = [string](Get-PropertyValue -Object $request -Name "login" -DefaultValue "")
        $isBot = [bool](Get-PropertyValue -Object $request -Name "is_bot" -DefaultValue $false)
        $name = [string](Get-PropertyValue -Object $request -Name "name" -DefaultValue "")
        $requestedAt = Get-PropertyValue -Object $request -Name "requestedAt"

        if ($login) {
            if (-not (Test-IsBotLogin -Login $login -IsBot $isBot -KnownBotPatterns $KnownBotPatterns)) {
                [pscustomobject]@{
                    Login = $login
                    Name = ""
                    RequestedAt = if ($requestedAt) { [datetime]$requestedAt } else { $null }
                }
            }
        }
        elseif ($name) {
            [pscustomobject]@{
                Login = ""
                Name = $name
                RequestedAt = if ($requestedAt) { [datetime]$requestedAt } else { $null }
            }
        }
    }
}

function Get-DaysSince {
    param(
        [datetime]$From,
        [datetime]$To
    )

    return [Math]::Max(0, [Math]::Floor(($To.ToUniversalTime() - $From.ToUniversalTime()).TotalDays))
}

function Get-FullFilePaths {
    param(
        [string]$RepositoryName,
        [int]$Number
    )

    $pages = Invoke-GhJson -Arguments @(
        "api",
        "--paginate",
        "--slurp",
        "repos/$RepositoryName/pulls/$Number/files?per_page=100"
    )

    return @(
        foreach ($page in ConvertTo-Array $pages) {
            foreach ($file in ConvertTo-Array $page) {
                $filename = Get-PropertyValue -Object $file -Name "filename"
                if ($filename) {
                    [string]$filename
                }
            }
        }
    )
}

function Add-PullRequestDetails {
    param(
        [string]$RepositoryName,
        [object[]]$Candidates
    )

    if ($Candidates.Count -eq 0) {
        return
    }

    $repositoryParts = $RepositoryName.Split("/")
    if ($repositoryParts.Count -ne 2) {
        throw "Repository must use the owner/name format."
    }

    for ($offset = 0; $offset -lt $Candidates.Count; $offset += 20) {
        $chunk = @($Candidates | Select-Object -Skip $offset -First 20)
        $aliases = @(
            foreach ($candidate in $chunk) {
                $number = [int]$candidate.PullRequest.number
                @"
pr$number`: pullRequest(number: $number) {
  number
  mergeable
  reviewDecision
  reviews(last: 50) {
    nodes {
      author { login }
      state
      submittedAt
      commit { oid }
    }
  }
  reviewRequests(first: 20) {
    nodes {
      requestedReviewer {
        ... on User { login }
        ... on Team { name }
      }
    }
  }
  comments(last: 50) {
    nodes {
      author { login }
      createdAt
    }
  }
  timelineItems(itemTypes: [REVIEW_REQUESTED_EVENT], last: 50) {
    nodes {
      ... on ReviewRequestedEvent {
        createdAt
        requestedReviewer {
          ... on User { login }
          ... on Team { name }
        }
      }
    }
  }
  commits(last: 1) {
    nodes {
      commit {
        statusCheckRollup { state }
      }
    }
  }
}
"@
            }
        )

        $query = 'query($owner:String!,$name:String!){repository(owner:$owner,name:$name){' +
            ($aliases -join [Environment]::NewLine) +
            '}}'
        $result = Invoke-GhJson -Arguments @(
            "api",
            "graphql",
            "-f",
            "query=$query",
            "-F",
            "owner=$($repositoryParts[0])",
            "-F",
            "name=$($repositoryParts[1])"
        )

        foreach ($candidate in $chunk) {
            $pullRequest = $candidate.PullRequest
            $number = [int]$pullRequest.number
            $detail = Get-PropertyValue -Object $result.data.repository -Name "pr$number"
            if ($null -eq $detail) {
                throw "GitHub did not return details for pull request #$number."
            }

            $reviews = @(
                ConvertTo-Array (Get-PropertyValue `
                    -Object (Get-PropertyValue -Object $detail -Name "reviews") `
                    -Name "nodes")
            )
            $comments = @(
                ConvertTo-Array (Get-PropertyValue `
                    -Object (Get-PropertyValue -Object $detail -Name "comments") `
                    -Name "nodes")
            )
            $reviewRequestEvents = @(
                ConvertTo-Array (Get-PropertyValue `
                    -Object (Get-PropertyValue -Object $detail -Name "timelineItems") `
                    -Name "nodes")
            )
            $reviewRequests = @(
                foreach ($requestNode in ConvertTo-Array (Get-PropertyValue `
                    -Object (Get-PropertyValue -Object $detail -Name "reviewRequests") `
                    -Name "nodes")) {
                    $requestedReviewer = Get-PropertyValue -Object $requestNode -Name "requestedReviewer"
                    if ($requestedReviewer) {
                        $login = [string](Get-PropertyValue -Object $requestedReviewer -Name "login" -DefaultValue "")
                        $name = [string](Get-PropertyValue -Object $requestedReviewer -Name "name" -DefaultValue "")
                        $matchingEvent = @(
                            $reviewRequestEvents |
                                Where-Object {
                                    $eventReviewer = Get-PropertyValue -Object $_ -Name "requestedReviewer"
                                    $eventLogin = [string](Get-PropertyValue -Object $eventReviewer -Name "login" -DefaultValue "")
                                    $eventName = [string](Get-PropertyValue -Object $eventReviewer -Name "name" -DefaultValue "")
                                    ($login -and $eventLogin -eq $login) -or ($name -and $eventName -eq $name)
                                } |
                                Sort-Object { [datetime]$_.createdAt } -Descending |
                                Select-Object -First 1
                        )

                        [pscustomobject]@{
                            login = $login
                            name = $name
                            requestedAt = if ($matchingEvent.Count -gt 0) {
                                Get-PropertyValue -Object $matchingEvent[0] -Name "createdAt"
                            }
                            else {
                                $null
                            }
                        }
                    }
                }
            )
            $commitNodes = @(
                ConvertTo-Array (Get-PropertyValue `
                    -Object (Get-PropertyValue -Object $detail -Name "commits") `
                    -Name "nodes")
            )
            $latestCommit = if ($commitNodes.Count -gt 0) {
                Get-PropertyValue -Object $commitNodes[0] -Name "commit"
            }
            else {
                $null
            }
            $statusRollup = Get-PropertyValue -Object $latestCommit -Name "statusCheckRollup"
            $statusState = Get-PropertyValue -Object $statusRollup -Name "state"
            $statusCheckRollup = if ($statusState) {
                @([pscustomobject]@{ state = [string]$statusState })
            }
            else {
                @()
            }

            $pullRequest | Add-Member `
                -NotePropertyName "mergeable" `
                -NotePropertyValue (Get-PropertyValue -Object $detail -Name "mergeable" -DefaultValue "UNKNOWN") `
                -Force
            $pullRequest | Add-Member `
                -NotePropertyName "reviewDecision" `
                -NotePropertyValue (Get-PropertyValue -Object $detail -Name "reviewDecision" -DefaultValue "") `
                -Force
            $pullRequest | Add-Member -NotePropertyName "latestReviews" -NotePropertyValue $reviews -Force
            $pullRequest | Add-Member -NotePropertyName "reviewRequests" -NotePropertyValue $reviewRequests -Force
            $pullRequest | Add-Member -NotePropertyName "comments" -NotePropertyValue $comments -Force
            $pullRequest | Add-Member -NotePropertyName "statusCheckRollup" -NotePropertyValue $statusCheckRollup -Force

        }
    }
}

function Resolve-UnknownMergeable {
    <#
    .SYNOPSIS
    Resolves pull requests whose mergeable state GitHub has not computed yet.

    .DESCRIPTION
    GitHub computes mergeability lazily. The first query for a pull request
    returns UNKNOWN and only schedules the background calculation, so a single
    pass classifies conflicting pull requests as though they were mergeable and
    a later run returns a different queue for unchanged data. This re-queries
    the unresolved pull requests until GitHub reports a value or the attempts
    run out, and reports how many remain unresolved.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$Candidates,

        [Parameter(Mandatory)]
        [string]$RepositoryName,

        [int]$MaxAttempts = 3,

        [int]$DelayMilliseconds = 2000
    )

    if ($Candidates.Count -eq 0) {
        return 0
    }

    $repositoryParts = $RepositoryName.Split("/")
    if ($repositoryParts.Count -ne 2) {
        throw "Repository must use the owner/name format."
    }

    $unresolved = {
        @(
            $Candidates | Where-Object {
                [string](Get-PropertyValue -Object $_.PullRequest -Name "mergeable" -DefaultValue "UNKNOWN") -eq "UNKNOWN"
            }
        )
    }

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        $pending = & $unresolved
        if ($pending.Count -eq 0) {
            return 0
        }

        Start-Sleep -Milliseconds $DelayMilliseconds

        # Add-PullRequestDetails chunks at 20 because its query is heavy. This query
        # is small, but an UNKNOWN mergeable is the expensive case server-side, so
        # this stays well below that to avoid provoking a GraphQL timeout.
        for ($offset = 0; $offset -lt $pending.Count; $offset += 25) {
            $chunk = @($pending | Select-Object -Skip $offset -First 25)
            $aliases = @(
                foreach ($candidate in $chunk) {
                    $number = [int]$candidate.PullRequest.number
                    "pr$number`: pullRequest(number: $number) { number mergeable }"
                }
            )
            $query = 'query($owner:String!,$name:String!){repository(owner:$owner,name:$name){' +
                ($aliases -join [Environment]::NewLine) +
                '}}'

            $result = $null
            try {
                $result = Invoke-GhJson -Arguments @(
                    "api",
                    "graphql",
                    "-f",
                    "query=$query",
                    "-F",
                    "owner=$($repositoryParts[0])",
                    "-F",
                    "name=$($repositoryParts[1])"
                )
            }
            catch {
                # This pass only refines an already-complete queue, so a failed chunk
                # must not discard the chunks that already resolved.
                $result = $null
            }

            if ($null -eq $result) {
                continue
            }

            foreach ($candidate in $chunk) {
                $number = [int]$candidate.PullRequest.number
                $detail = Get-PropertyValue -Object $result.data.repository -Name "pr$number"
                if ($null -eq $detail) {
                    continue
                }

                $candidate.PullRequest | Add-Member `
                    -NotePropertyName "mergeable" `
                    -NotePropertyValue (Get-PropertyValue -Object $detail -Name "mergeable" -DefaultValue "UNKNOWN") `
                    -Force
            }
        }
    }

    return (& $unresolved).Count
}

function Resolve-QueueScope {
    param(
        [object]$Configuration,
        [string]$PresetName,
        [string[]]$AdHocLabels,
        [string[]]$AdHocPaths,
        [string[]]$RequiredLabels,
        [string[]]$ExcludedLabels,
        [string[]]$Authors,
        [bool]$UseAllRepo
    )

    $hasAdHocScope = $UseAllRepo -or $AdHocLabels.Count -gt 0 -or $AdHocPaths.Count -gt 0
    if ($PresetName -and $hasAdHocScope) {
        throw "Do not combine -Preset with -Label, -Path, or -AllRepo."
    }

    if (-not $PresetName -and -not $hasAdHocScope) {
        $PresetName = [string]$Configuration.defaultPreset
    }

    $labelsAny = @()
    $pathsAny = @()
    $presetExcludedLabels = @()
    $allRepositoryPullRequests = $UseAllRepo
    $scopeName = "adhoc"
    $description = "Ad hoc pull-request scope"

    if ($PresetName) {
        $presetProperty = $Configuration.presets.PSObject.Properties[$PresetName]
        if ($null -eq $presetProperty) {
            $available = @($Configuration.presets.PSObject.Properties.Name | Sort-Object) -join ", "
            throw "Unknown preset '$PresetName'. Available presets: $available."
        }

        $presetValue = $presetProperty.Value
        $scopeName = $PresetName
        $description = [string](Get-PropertyValue -Object $presetValue -Name "description" -DefaultValue $PresetName)
        $labelsAny = @((ConvertTo-Array (Get-PropertyValue -Object $presetValue -Name "labelsAny")) | ForEach-Object { [string]$_ })
        $pathsAny = @((ConvertTo-Array (Get-PropertyValue -Object $presetValue -Name "pathsAny")) | ForEach-Object { [string]$_ })
        $presetExcludedLabels = @((ConvertTo-Array (Get-PropertyValue -Object $presetValue -Name "excludeLabels")) | ForEach-Object { [string]$_ })
        $allRepositoryPullRequests = [bool](Get-PropertyValue -Object $presetValue -Name "allRepositoryPullRequests" -DefaultValue $false)
    }
    elseif (-not $UseAllRepo) {
        $labelsAny = $AdHocLabels
        $pathsAny = $AdHocPaths
    }

    if (-not $allRepositoryPullRequests -and $labelsAny.Count -eq 0 -and $pathsAny.Count -eq 0) {
        throw "The resolved scope has no labels or paths. Use -AllRepo for an unrestricted queue."
    }

    return [pscustomobject]@{
        Name = $scopeName
        Description = $description
        AllRepositoryPullRequests = $allRepositoryPullRequests
        LabelsAny = @($labelsAny)
        PathsAny = @($pathsAny)
        RequireLabels = @($RequiredLabels)
        ExcludeLabels = @($presetExcludedLabels + $ExcludedLabels | Select-Object -Unique)
        Authors = @($Authors)
        Coverage = if ($allRepositoryPullRequests) {
            "all-repo"
        }
        elseif ($labelsAny.Count -gt 0 -and $pathsAny.Count -gt 0) {
            "labels-and-paths"
        }
        elseif ($pathsAny.Count -gt 0) {
            "paths-only"
        }
        else {
            "labels-only"
        }
    }
}

function Get-Classification {
    param(
        [object]$PullRequest,
        [string[]]$Labels,
        [object]$AuthorInfo,
        [object]$Settings,
        [datetime]$SnapshotTime
    )

    $knownBotPatterns = @($Settings.knownBotPatterns)
    $humanReviews = @(Get-HumanReviews -PullRequest $PullRequest -KnownBotPatterns $knownBotPatterns)
    $latestHumanReview = if ($humanReviews.Count -gt 0) { $humanReviews[0] } else { $null }
    $latestAuthorCommentAt = Get-LatestAuthorCommentAt -PullRequest $PullRequest -AuthorLogin $AuthorInfo.Login
    $humanReviewRequests = @(Get-HumanReviewRequests -PullRequest $PullRequest -KnownBotPatterns $knownBotPatterns)
    $humanReviewRequestCount = $humanReviewRequests.Count
    $latestReviewRequestAt = @(
        $humanReviewRequests |
            Where-Object { $_.RequestedAt } |
            ForEach-Object { $_.RequestedAt } |
            Sort-Object -Descending |
            Select-Object -First 1
    )
    $latestReviewRequestAt = if ($latestReviewRequestAt.Count -gt 0) { $latestReviewRequestAt[0] } else { $null }
    $createdAt = [datetime](Get-PropertyValue -Object $PullRequest -Name "createdAt")
    $updatedAt = [datetime](Get-PropertyValue -Object $PullRequest -Name "updatedAt" -DefaultValue $createdAt)
    $headSha = [string](Get-PropertyValue -Object $PullRequest -Name "headRefOid" -DefaultValue "")
    $reviewDecision = [string](Get-PropertyValue -Object $PullRequest -Name "reviewDecision" -DefaultValue "")
    $mergeable = [string](Get-PropertyValue -Object $PullRequest -Name "mergeable" -DefaultValue "UNKNOWN")
    $isDraft = [bool](Get-PropertyValue -Object $PullRequest -Name "isDraft" -DefaultValue $false)
    $checkState = Get-CheckState -PullRequest $PullRequest
    $ageDays = Get-DaysSince -From $createdAt -To $SnapshotTime
    $isCommunity = Test-AnyWildcardMatch -Values $Labels -Patterns @($Settings.communityLabels)
    $isBot = Test-IsBotLogin -Login $AuthorInfo.Login -IsBot $AuthorInfo.IsBot -KnownBotPatterns $knownBotPatterns
    $blocked = Test-AnyWildcardMatch -Values $Labels -Patterns @($Settings.blockedLabels)
    $designGate = Test-AnyWildcardMatch -Values $Labels -Patterns @($Settings.designGateLabels)
    $headChangedAfterReview = $latestHumanReview -and
        $latestHumanReview.CommitOid -and
        $headSha -and
        $latestHumanReview.CommitOid -ne $headSha
    $authorCommentedAfterReview = $latestHumanReview -and
        $latestAuthorCommentAt -and
        $latestAuthorCommentAt -gt $latestHumanReview.SubmittedAt
    $authorRespondedAfterReview = $headChangedAfterReview -or $authorCommentedAfterReview
    $reviewerRescueAfterDays = [int]$Settings.reviewerRescueAfterDays

    $bucket = "ReviewNow"
    $nextActor = "human reviewer"
    $reasonCodes = [System.Collections.Generic.List[string]]::new()
    $blockers = [System.Collections.Generic.List[string]]::new()
    $waitingSince = $createdAt

    if ($isBot) {
        $bucket = "Excluded"
        $nextActor = "none"
        $reasonCodes.Add("bot-authored")
    }
    elseif ($isDraft) {
        $bucket = "Draft"
        $nextActor = "author"
        $reasonCodes.Add("draft")
    }
    elseif ($designGate) {
        $bucket = "DesignDecision"
        $nextActor = "API/design owner"
        $reasonCodes.Add("design-gate")
        $blockers.Add("An API or design decision is still required.")
    }
    elseif ($mergeable -eq "CONFLICTING") {
        $bucket = "WaitingOnAuthor"
        $nextActor = "author"
        $reasonCodes.Add("merge-conflict")
        $blockers.Add("The pull request conflicts with its base branch.")
    }
    elseif ($blocked) {
        $bucket = "NeedsRescue"
        $nextActor = "maintainer/triager"
        $reasonCodes.Add("blocked-label")
        $blockers.Add("A blocking label requires an explicit triage decision.")
    }
    elseif ($reviewDecision -eq "CHANGES_REQUESTED" -or
        ($latestHumanReview -and $latestHumanReview.State -eq "CHANGES_REQUESTED")) {
        if ($authorRespondedAfterReview) {
            $reasonCodes.Add("author-responded")
            $reasonCodes.Add("roundtrip-waiting")
            if ($headChangedAfterReview) {
                $reasonCodes.Add("head-changed-after-review")
            }
            $waitingSince = if ($authorCommentedAfterReview) { $latestAuthorCommentAt } else { $updatedAt }
            if ($checkState -eq "Failed") {
                $bucket = "WaitingOnCI"
                $nextActor = "author/CI investigation"
                $reasonCodes.Add("ci-failed")
                $blockers.Add("The failure is not classified as unrelated or flaky.")
            }
            elseif ((Get-DaysSince -From $waitingSince -To $SnapshotTime) -ge $reviewerRescueAfterDays) {
                $bucket = "NeedsRescue"
                $nextActor = "maintainer/triager"
                $reasonCodes.Add("reviewer-idle-30d")
                $reasonCodes.Add("review-abandoned")
            }
            else {
                $bucket = "ReviewNow"
                $nextActor = "human reviewer"
            }
        }
        else {
            $bucket = "WaitingOnAuthor"
            $nextActor = "author"
            $reasonCodes.Add("changes-requested")
            if ($latestHumanReview) {
                $waitingSince = $latestHumanReview.SubmittedAt
            }
        }
    }
    elseif ($reviewDecision -eq "APPROVED") {
        if ($checkState -eq "Failed") {
            $bucket = "WaitingOnCI"
            $nextActor = "author/CI investigation"
            $reasonCodes.Add("ci-failed")
            $blockers.Add("Required checks are failing or incomplete.")
        }
        elseif ($checkState -eq "Pending" -or $mergeable -eq "UNKNOWN") {
            $bucket = "WaitingOnCI"
            $nextActor = "CI/automation"
            $reasonCodes.Add($(if ($checkState -eq "Pending") { "ci-pending" } else { "mergeability-unknown" }))
        }
        else {
            $bucket = "ReadyToMerge"
            $nextActor = "merger"
            $reasonCodes.Add("approved")
            $reasonCodes.Add("mergeable")
            if ($checkState -eq "Passing") {
                $reasonCodes.Add("ci-green")
            }
        }
    }
    elseif ($checkState -eq "Failed") {
        $bucket = "WaitingOnCI"
        $nextActor = "author/CI investigation"
        $reasonCodes.Add("ci-failed")
        $blockers.Add("The failure is not classified as unrelated or flaky.")
    }
    elseif ($authorRespondedAfterReview) {
        $reasonCodes.Add("author-responded")
        $reasonCodes.Add("roundtrip-waiting")
        if ($latestHumanReview.State -eq "COMMENTED") {
            $reasonCodes.Add("reviewer-commented")
        }
        if ($headChangedAfterReview) {
            $reasonCodes.Add("head-changed-after-review")
        }
        $waitingSince = if ($authorCommentedAfterReview) { $latestAuthorCommentAt } else { $updatedAt }
        $responseIdleDays = Get-DaysSince -From $waitingSince -To $SnapshotTime
        if ($responseIdleDays -ge $reviewerRescueAfterDays) {
            $bucket = "NeedsRescue"
            $nextActor = "maintainer/triager"
            $reasonCodes.Add("reviewer-idle-30d")
            $reasonCodes.Add("review-abandoned")
        }
        else {
            $bucket = "ReviewNow"
            $nextActor = "human reviewer"
        }
    }
    elseif ($humanReviewRequestCount -gt 0) {
        $reasonCodes.Add("review-requested")
        if ($latestReviewRequestAt) {
            $waitingSince = $latestReviewRequestAt
        }
        else {
            $waitingSince = $SnapshotTime
            $reasonCodes.Add("review-request-age-unknown")
        }
        $reviewRequestIdleDays = Get-DaysSince -From $waitingSince -To $SnapshotTime
        if ($latestReviewRequestAt -and $reviewRequestIdleDays -ge $reviewerRescueAfterDays) {
            $bucket = "NeedsRescue"
            $nextActor = "maintainer/triager"
            $reasonCodes.Add("reviewer-idle-30d")
        }
        else {
            $bucket = "ReviewNow"
            $nextActor = "human reviewer"
        }
    }
    elseif ($humanReviews.Count -eq 0) {
        if ($ageDays -ge [int]$Settings.rescueAfterDays) {
            $bucket = "NeedsRescue"
            $nextActor = "maintainer/triager"
            $reasonCodes.Add("never-reviewed")
            $reasonCodes.Add("orphan-unassigned")
        }
        else {
            $bucket = "ReviewNow"
            $nextActor = "human reviewer"
            $reasonCodes.Add("needs-first-review")
        }
    }
    elseif ($latestHumanReview.State -eq "COMMENTED") {
        $bucket = "WaitingOnAuthor"
        $nextActor = "author"
        $reasonCodes.Add("reviewer-commented")
        $waitingSince = $latestHumanReview.SubmittedAt
    }
    else {
        $bucket = "ReviewNow"
        $nextActor = "human reviewer"
        $reasonCodes.Add("review-required")
        $waitingSince = $latestHumanReview.SubmittedAt
    }

    if ($bucket -eq "ReviewNow" -and $checkState -eq "Pending") {
        $reasonCodes.Add("ci-pending")
    }

    if ($isCommunity) {
        $reasonCodes.Add("community-contribution")
    }

    $idleDays = Get-DaysSince -From $waitingSince -To $SnapshotTime

    return [pscustomobject]@{
        Bucket = $bucket
        NextActor = $nextActor
        ReasonCodes = @($reasonCodes)
        Blockers = @($blockers)
        CheckState = $checkState
        AgeDays = $ageDays
        IdleDays = $idleDays
        IsCommunity = $isCommunity
        HumanReviewCount = $humanReviews.Count
        HumanReviewRequestCount = $humanReviewRequestCount
    }
}

function Get-DisplayMetadata {
    return [pscustomobject]@{
        buckets = [pscustomobject][ordered]@{
            ReviewNow = [pscustomobject]@{
                label = "Review now"
                description = "A human reviewer can productively act on this pull request now."
            }
            NeedsRescue = [pscustomobject]@{
                label = "Needs rescue"
                description = "A maintainer must restore ownership or decide how this pull request should proceed."
            }
            ReadyToMerge = [pscustomobject]@{
                label = "Ready to merge"
                description = "The pull request is approved, mergeable, and no required check is blocking it."
            }
            WaitingOnAuthor = [pscustomobject]@{
                label = "Waiting on author"
                description = "The author must respond to feedback, resolve conflicts, or otherwise update the pull request."
            }
            WaitingOnCI = [pscustomobject]@{
                label = "Waiting on CI"
                description = "Checks, mergeability computation, or CI investigation must complete before review can progress."
            }
            DesignDecision = [pscustomobject]@{
                label = "Design decision"
                description = "An API or design owner must resolve a decision before normal review."
            }
            Draft = [pscustomobject]@{
                label = "Draft"
                description = "The author has not marked the pull request ready for review."
            }
            Excluded = [pscustomobject]@{
                label = "Excluded"
                description = "The pull request is automated or explicitly outside this attention queue."
            }
        }
        reasonCodes = [pscustomobject][ordered]@{
            "approved" = [pscustomobject]@{
                label = "Approved"
                description = "A human review approved the current pull request."
            }
            "author-responded" = [pscustomobject]@{
                label = "Author responded"
                description = "The author commented or pushed after the latest human review."
            }
            "blocked-label" = [pscustomobject]@{
                label = "Blocking label"
                description = "A blocking label requires an explicit maintainer decision."
            }
            "bot-authored" = [pscustomobject]@{
                label = "Bot authored"
                description = "The pull request was opened by a known automation account."
            }
            "changes-requested" = [pscustomobject]@{
                label = "Changes requested"
                description = "The latest actionable human review requires an author response."
            }
            "ci-failed" = [pscustomobject]@{
                label = "CI failed"
                description = "A required check is failing or otherwise unsuccessful."
            }
            "ci-green" = [pscustomobject]@{
                label = "CI green"
                description = "Required checks completed successfully."
            }
            "ci-pending" = [pscustomobject]@{
                label = "CI pending"
                description = "One or more required checks have not completed."
            }
            "community-contribution" = [pscustomobject]@{
                label = "Community contribution"
                description = "The repository labels this pull request as a community contribution."
            }
            "design-gate" = [pscustomobject]@{
                label = "Design gate"
                description = "A configured API or design label blocks normal review."
            }
            "draft" = [pscustomobject]@{
                label = "Draft"
                description = "The pull request is still marked as a draft."
            }
            "head-changed-after-review" = [pscustomobject]@{
                label = "Head changed after review"
                description = "The author pushed a new head commit after the latest human review."
            }
            "merge-conflict" = [pscustomobject]@{
                label = "Merge conflict"
                description = "The pull request conflicts with its base branch."
            }
            "mergeability-unknown" = [pscustomobject]@{
                label = "Mergeability unknown"
                description = "GitHub has not finished computing whether the pull request is mergeable."
            }
            "mergeable" = [pscustomobject]@{
                label = "Mergeable"
                description = "GitHub reports that the pull request can merge cleanly."
            }
            "needs-first-review" = [pscustomobject]@{
                label = "Needs first review"
                description = "No meaningful human review has been submitted yet."
            }
            "never-reviewed" = [pscustomobject]@{
                label = "Never reviewed"
                description = "The pull request passed the rescue age without receiving meaningful human review."
            }
            "orphan-unassigned" = [pscustomobject]@{
                label = "Orphaned"
                description = "The pull request has no active human review or review request."
            }
            "review-abandoned" = [pscustomobject]@{
                label = "Review abandoned"
                description = "Human review began, but overdue reviewer follow-up now requires maintainer rescue."
            }
            "review-request-age-unknown" = [pscustomobject]@{
                label = "Review request age unknown"
                description = "A human review is requested, but GitHub did not provide when the request began."
            }
            "review-requested" = [pscustomobject]@{
                label = "Review requested"
                description = "At least one human reviewer is currently requested."
            }
            "review-required" = [pscustomobject]@{
                label = "Review required"
                description = "The pull request still requires a human review."
            }
            "reviewer-commented" = [pscustomobject]@{
                label = "Reviewer commented"
                description = "The latest meaningful human review was submitted as comments rather than approval."
            }
            "reviewer-idle-30d" = [pscustomobject]@{
                label = "Reviewer idle"
                description = "Reviewer follow-up has exceeded the configured rescue threshold."
            }
            "roundtrip-waiting" = [pscustomobject]@{
                label = "Roundtrip waiting"
                description = "The author responded to review and the pull request is waiting for reviewer follow-up."
            }
        }
    }
}

function Escape-MarkdownCell {
    param([string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    return $Value.Replace("|", "\|").Replace("`r", " ").Replace("`n", " ")
}

function Render-MarkdownTable {
    param(
        [object[]]$Items,
        [string]$EmptyText
    )

    if ($Items.Count -eq 0) {
        return $EmptyText
    }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("| PR | Title | Author | Waiting | Next actor | Why |")
    $lines.Add("|---|---|---|---:|---|---|")

    foreach ($item in $Items) {
        $title = Escape-MarkdownCell -Value $item.title
        $whyParts = [System.Collections.Generic.List[string]]::new()
        $whyParts.Add(($item.reasonCodes -join ", "))
        if ($item.blockers.Count -gt 0) {
            $whyParts.Add("Blocker: $($item.blockers -join ' ')")
        }
        $why = Escape-MarkdownCell -Value ($whyParts -join ". ")
        $lines.Add("| [#$($item.number)]($($item.url)) | $title | ``$($item.author)`` | $($item.idleDays)d | $($item.nextActor) | $why |")
    }

    return $lines -join [Environment]::NewLine
}

function Render-Markdown {
    param([object]$Result)

    $reviewNow = @($Result.items | Where-Object { $_.shownInDigest -and $_.bucket -eq "ReviewNow" })
    $needsRescue = @($Result.items | Where-Object { $_.shownInDigest -and $_.bucket -eq "NeedsRescue" })
    $readyToMerge = @($Result.items | Where-Object { $_.shownInDigest -and $_.bucket -eq "ReadyToMerge" })

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("<!-- PR_ATTENTION_QUEUE_BEGIN -->")
    $lines.Add("# PR attention queue")
    $lines.Add("")
    $lines.Add("**Scope:** $($Result.filter.name) - $($Result.filter.description)")
    $lines.Add("")
    $lines.Add("**Selection:** $($Result.filter.selection)")
    $lines.Add("")
    $lines.Add("**Snapshot:** $($Result.generatedAt) · **Repository:** $($Result.repository)")
    $lines.Add("")
    $lines.Add("## Review now ($($reviewNow.Count))")
    $lines.Add("")
    $lines.Add((Render-MarkdownTable -Items $reviewNow -EmptyText "No pull requests currently have a human reviewer as the next actor."))
    $lines.Add("")
    $lines.Add("## Needs rescue ($($needsRescue.Count))")
    $lines.Add("")
    $lines.Add((Render-MarkdownTable -Items $needsRescue -EmptyText "No pull requests currently require rescue or triage."))
    $lines.Add("")
    $lines.Add("## Ready to merge ($($readyToMerge.Count))")
    $lines.Add("")
    $lines.Add((Render-MarkdownTable -Items $readyToMerge -EmptyText "No pull requests are currently ready to merge."))
    $lines.Add("")
    $lines.Add("## Queue census")
    $lines.Add("")
    $lines.Add("| Bucket | Count |")
    $lines.Add("|---|---:|")

    foreach ($entry in $Result.census.byBucket.PSObject.Properties) {
        $lines.Add("| $($entry.Name) | $($entry.Value) |")
    }

    $lines.Add("")
    $lines.Add("Matched $($Result.census.matched) of $($Result.census.openPullRequests) open pull requests. " +
        "$($Result.census.pathOnly) matched only by changed path.")
    $incidental = [int](Get-PropertyValue -Object $Result.census -Name "incidentalPathExcluded" -DefaultValue 0)
    if ($incidental -gt 0) {
        $lines.Add("")
        $lines.Add("Excluded $incidental pull request(s) that touched in-scope paths only incidentally.")
    }
    $lines.Add("")
    $lines.Add("**Overflow:** Review now $($Result.overflow.reviewNow), Needs rescue $($Result.overflow.needsRescue), " +
        "Ready to merge $($Result.overflow.readyToMerge).")

    if ($Result.warnings.Count -gt 0) {
        $lines.Add("")
        $lines.Add("## Warnings")
        $lines.Add("")
        foreach ($warning in $Result.warnings) {
            $lines.Add("- $warning")
        }
    }

    return $lines -join [Environment]::NewLine
}

function Select-ReviewNowDigestItems {
    param(
        [object[]]$Items,
        [int]$MaximumItems,
        [int]$MaximumPerAuthor
    )

    $selected = [System.Collections.Generic.List[object]]::new()
    $authorCounts = @{}

    foreach ($item in $Items) {
        if ($selected.Count -ge $MaximumItems) {
            break
        }

        $authorCount = [int]($authorCounts[$item.author] ?? 0)
        if ($MaximumPerAuthor -gt 0 -and $authorCount -ge $MaximumPerAuthor) {
            continue
        }

        $selected.Add($item)
        $authorCounts[$item.author] = $authorCount + 1
    }

    return @($selected)
}

function Invoke-PRAttentionQueue {
    [CmdletBinding()]
    param(
        [string]$Repository = "dotnet/aspnetcore",
        [string]$Preset,
        [string[]]$Label = @(),
        [string[]]$Path = @(),
        [string[]]$RequireLabel = @(),
        [string[]]$ExcludeLabel = @(),
        [string[]]$Author = @(),
        [switch]$AllRepo,
        [ValidateSet("Markdown", "Json")]
        [string]$OutputFormat = "Markdown",
        [ValidateRange(0, 100)]
        [int]$MaxReviewNow = 5,
        [ValidateRange(0, 100)]
        [int]$MaxNeedsRescue = 3,
        [ValidateRange(0, 100)]
        [int]$MaxReadyToMerge = 3,
        [ValidateRange(0, 100)]
        [int]$MaxReviewNowPerAuthor = 2,
        [string]$InputPath,
        [datetime]$Now = [datetime]::UtcNow
    )

    $presetPath = Join-Path (Split-Path -Parent $PSScriptRoot) "presets.json"
    $configuration = Get-Content -Raw -Path $presetPath | ConvertFrom-Json -Depth 20
    $scope = Resolve-QueueScope `
        -Configuration $configuration `
        -PresetName $Preset `
        -AdHocLabels $Label `
        -AdHocPaths $Path `
        -RequiredLabels $RequireLabel `
        -ExcludedLabels $ExcludeLabel `
        -Authors $Author `
        -UseAllRepo $AllRepo.IsPresent

    $warnings = [System.Collections.Generic.List[string]]::new()
    if ($scope.Coverage -eq "labels-only") {
        $warnings.Add("This is a labels-only scope. Pull requests with missing or incorrect labels may be absent.")
    }

    $pullRequests = @()
    $openPullRequestCount = 0

    if ($InputPath) {
        $inputValue = Get-Content -Raw -Path $InputPath | ConvertFrom-Json -Depth 100
        $pullRequests = @(ConvertTo-Array $inputValue)
        $openPullRequestCount = $pullRequests.Count
    }
    else {
        Get-Command gh -ErrorAction Stop | Out-Null

        $repositoryParts = $Repository.Split("/")
        if ($repositoryParts.Count -ne 2) {
            throw "Repository must use the owner/name format."
        }

        $countQuery = 'query($owner:String!,$name:String!){repository(owner:$owner,name:$name){pullRequests(states:OPEN){totalCount}}}'
        $countResult = Invoke-GhJson -Arguments @(
            "api",
            "graphql",
            "-f",
            "query=$countQuery",
            "-F",
            "owner=$($repositoryParts[0])",
            "-F",
            "name=$($repositoryParts[1])"
        )
        $openPullRequestCount = [int]$countResult.data.repository.pullRequests.totalCount

        if ($openPullRequestCount -gt 1000) {
            throw "The repository has $openPullRequestCount open pull requests, exceeding the script's safe 1000-PR limit."
        }

        $fields = @(
            "number",
            "title",
            "url",
            "author",
            "isDraft",
            "labels",
            "createdAt",
            "updatedAt",
            "headRefOid",
            "baseRefName",
            "files",
            "additions",
            "deletions",
            "changedFiles",
            "milestone",
            "assignees"
        ) -join ","

        $pullRequests = @(
            ConvertTo-Array (Invoke-GhJson -Arguments @(
                "pr",
                "list",
                "--repo",
                $Repository,
                "--state",
                "open",
                "--limit",
                [string]$openPullRequestCount,
                "--json",
                $fields
            ))
        )

        if ($pullRequests.Count -ne $openPullRequestCount) {
            throw "GitHub reported $openPullRequestCount open pull requests, but the query returned $($pullRequests.Count). Refusing to rank a partial universe."
        }
    }

    $matchedItems = [System.Collections.Generic.List[object]]::new()
    $matchedCandidates = [System.Collections.Generic.List[object]]::new()
    $pathOnlyCount = 0
    $labelOnlyCount = 0
    $bothCount = 0
    $unresolvedPathCoverage = 0
    $incidentalPathCount = 0
    $unresolvedMergeable = 0
    $pathMatchMinimumShare = [double](Get-PropertyValue `
        -Object $configuration.settings -Name "pathMatchMinimumShare" -DefaultValue 0)

    foreach ($pullRequest in $pullRequests) {
        $labels = @(Get-LabelNames -PullRequest $pullRequest)
        $files = @(Get-FilePaths -PullRequest $pullRequest)
        $changedFiles = [int](Get-PropertyValue -Object $pullRequest -Name "changedFiles" -DefaultValue $files.Count)

        if (-not $InputPath -and $changedFiles -gt $files.Count) {
            $files = @(Get-FullFilePaths -RepositoryName $Repository -Number ([int]$pullRequest.number))
        }
        elseif ($InputPath -and $changedFiles -gt $files.Count) {
            $unresolvedPathCoverage++
        }

        $labelMatch = $scope.LabelsAny.Count -gt 0 -and
            (Test-AnyWildcardMatch -Values $labels -Patterns $scope.LabelsAny)
        $matchedPaths = if ($scope.PathsAny.Count -gt 0) {
            @($files | Where-Object { Test-AnyWildcardMatch -Values @($_) -Patterns $scope.PathsAny })
        }
        else {
            @()
        }
        $pathMatch = $matchedPaths.Count -gt 0

        # A repository-wide sweep incidentally touches a few in-scope files. Without
        # a share test it lands in a narrow queue and wastes the reviewer's time, so
        # a path-only match has to be a meaningful portion of the pull request.
        if ($pathMatch -and -not $labelMatch -and $pathMatchMinimumShare -gt 0 -and $files.Count -gt 0) {
            $matchedShare = $matchedPaths.Count / $files.Count
            if ($matchedShare -lt $pathMatchMinimumShare) {
                $pathMatch = $false
                $incidentalPathCount++
            }
        }

        $scopeMatch = $scope.AllRepositoryPullRequests -or $labelMatch -or $pathMatch

        if (-not $scopeMatch) {
            continue
        }

        if (-not (Test-AllWildcardMatches -Values $labels -Patterns $scope.RequireLabels)) {
            continue
        }

        if ($scope.ExcludeLabels.Count -gt 0 -and
            (Test-AnyWildcardMatch -Values $labels -Patterns $scope.ExcludeLabels)) {
            continue
        }

        $authorInfo = Get-AuthorInfo -PullRequest $pullRequest
        if ($scope.Authors.Count -gt 0 -and $authorInfo.Login -notin $scope.Authors) {
            continue
        }

        if ($labelMatch -and $pathMatch) {
            $bothCount++
        }
        elseif ($labelMatch) {
            $labelOnlyCount++
        }
        elseif ($pathMatch) {
            $pathOnlyCount++
        }

        $matchedCandidates.Add([pscustomobject]@{
            PullRequest = $pullRequest
            Labels = $labels
            Files = $files
            ChangedFiles = $changedFiles
            LabelMatch = $labelMatch
            PathMatch = $pathMatch
            AuthorInfo = $authorInfo
        })
    }

    if (-not $InputPath) {
        Add-PullRequestDetails -RepositoryName $Repository -Candidates @($matchedCandidates)
        $mergeableAttempts = [int](Get-PropertyValue `
            -Object $configuration.settings -Name "mergeableResolveAttempts" -DefaultValue 0)
        if ($mergeableAttempts -gt 0) {
            try {
                $unresolvedMergeable = Resolve-UnknownMergeable `
                    -Candidates @($matchedCandidates) `
                    -RepositoryName $Repository `
                    -MaxAttempts $mergeableAttempts
            }
            catch {
                # The queue is already complete at this point. Report the gap instead
                # of failing the run and returning nothing.
                $unresolvedMergeable = @(
                    $matchedCandidates | Where-Object {
                        [string](Get-PropertyValue -Object $_.PullRequest -Name "mergeable" -DefaultValue "UNKNOWN") -eq "UNKNOWN"
                    }
                ).Count
            }
        }
    }

    foreach ($candidate in $matchedCandidates) {
        $pullRequest = $candidate.PullRequest
        $labels = $candidate.Labels
        $files = $candidate.Files
        $changedFiles = $candidate.ChangedFiles
        $labelMatch = $candidate.LabelMatch
        $pathMatch = $candidate.PathMatch
        $authorInfo = $candidate.AuthorInfo

        $classification = Get-Classification `
            -PullRequest $pullRequest `
            -Labels $labels `
            -AuthorInfo $authorInfo `
            -Settings $configuration.settings `
            -SnapshotTime $Now

        $milestoneValue = Get-PropertyValue -Object $pullRequest -Name "milestone"
        $milestoneTitle = [string](Get-PropertyValue -Object $milestoneValue -Name "title" -DefaultValue "")

        $matchedItems.Add([pscustomobject]@{
            number = [int]$pullRequest.number
            title = [string]$pullRequest.title
            url = [string]$pullRequest.url
            headSha = [string](Get-PropertyValue -Object $pullRequest -Name "headRefOid" -DefaultValue "")
            author = $authorInfo.Login
            authorClass = if ($classification.IsCommunity) { "Community" } else { "InternalOrUnknown" }
            labels = $labels
            files = $files
            baseBranch = [string](Get-PropertyValue -Object $pullRequest -Name "baseRefName" -DefaultValue "")
            milestone = $milestoneTitle
            additions = [int](Get-PropertyValue -Object $pullRequest -Name "additions" -DefaultValue 0)
            deletions = [int](Get-PropertyValue -Object $pullRequest -Name "deletions" -DefaultValue 0)
            changedFiles = $changedFiles
            bucket = $classification.Bucket
            nextActor = $classification.NextActor
            reasonCodes = $classification.ReasonCodes
            blockers = $classification.Blockers
            checkState = $classification.CheckState
            ageDays = $classification.AgeDays
            idleDays = $classification.IdleDays
            humanReviewCount = $classification.HumanReviewCount
            humanReviewRequestCount = $classification.HumanReviewRequestCount
            scopeMatch = if ($scope.AllRepositoryPullRequests) {
                "all-repo"
            }
            elseif ($labelMatch -and $pathMatch) {
                "label-and-path"
            }
            elseif ($labelMatch) {
                "label-only"
            }
            else {
                "path-only"
            }
            shownInDigest = $false
        })
    }

    if ($unresolvedPathCoverage -gt 0) {
        $warnings.Add("$unresolvedPathCoverage fixture pull request(s) had incomplete changed-file data.")
    }

    if ($unresolvedMergeable -gt 0) {
        $warnings.Add("GitHub had not finished computing mergeability for $unresolvedMergeable pull request(s). " +
            "Conflicting pull requests among them can appear in a review bucket until the next run.")
    }

    $reviewNow = @(
        $matchedItems |
            Where-Object { $_.bucket -eq "ReviewNow" } |
            Sort-Object `
                @{ Expression = { $_.idleDays }; Descending = $true },
                @{ Expression = { if ($_.authorClass -eq "Community") { 1 } else { 0 } }; Descending = $true },
                @{ Expression = { $_.ageDays }; Descending = $true },
                @{ Expression = { $_.changedFiles }; Descending = $false },
                @{ Expression = { $_.number }; Descending = $false }
    )

    $needsRescue = @(
        $matchedItems |
            Where-Object { $_.bucket -eq "NeedsRescue" } |
            Sort-Object `
                @{ Expression = { $_.idleDays }; Descending = $true },
                @{ Expression = { if ($_.authorClass -eq "Community") { 1 } else { 0 } }; Descending = $true },
                @{ Expression = { if ($_.reasonCodes -contains "never-reviewed") { 1 } else { 0 } }; Descending = $true },
                @{ Expression = { $_.ageDays }; Descending = $true },
                @{ Expression = { $_.number }; Descending = $false }
    )

    $readyToMerge = @(
        $matchedItems |
            Where-Object { $_.bucket -eq "ReadyToMerge" } |
            Sort-Object `
                @{ Expression = { $_.idleDays }; Descending = $true },
                @{ Expression = { $_.ageDays }; Descending = $true },
                @{ Expression = { $_.number }; Descending = $false }
    )

    foreach ($item in Select-ReviewNowDigestItems `
        -Items $reviewNow `
        -MaximumItems $MaxReviewNow `
        -MaximumPerAuthor $MaxReviewNowPerAuthor) {
        $item.shownInDigest = $true
    }

    foreach ($item in $needsRescue | Select-Object -First $MaxNeedsRescue) {
        $item.shownInDigest = $true
    }

    foreach ($item in $readyToMerge | Select-Object -First $MaxReadyToMerge) {
        $item.shownInDigest = $true
    }

    $orderedItems = @(
        $matchedItems | Sort-Object `
            @{ Expression = {
                switch ($_.bucket) {
                    "ReviewNow" { 0 }
                    "NeedsRescue" { 1 }
                    "ReadyToMerge" { 2 }
                    "WaitingOnAuthor" { 3 }
                    "WaitingOnCI" { 4 }
                    "DesignDecision" { 5 }
                    "Draft" { 6 }
                    default { 7 }
                }
            }; Descending = $false },
            @{ Expression = { $_.idleDays }; Descending = $true },
            @{ Expression = { $_.number }; Descending = $false }
    )

    $bucketCounts = [ordered]@{
        ReviewNow = @($orderedItems | Where-Object { $_.bucket -eq "ReviewNow" }).Count
        NeedsRescue = @($orderedItems | Where-Object { $_.bucket -eq "NeedsRescue" }).Count
        ReadyToMerge = @($orderedItems | Where-Object { $_.bucket -eq "ReadyToMerge" }).Count
        WaitingOnAuthor = @($orderedItems | Where-Object { $_.bucket -eq "WaitingOnAuthor" }).Count
        WaitingOnCI = @($orderedItems | Where-Object { $_.bucket -eq "WaitingOnCI" }).Count
        DesignDecision = @($orderedItems | Where-Object { $_.bucket -eq "DesignDecision" }).Count
        Draft = @($orderedItems | Where-Object { $_.bucket -eq "Draft" }).Count
        Excluded = @($orderedItems | Where-Object { $_.bucket -eq "Excluded" }).Count
    }

    $includeParts = [System.Collections.Generic.List[string]]::new()
    $constraintParts = [System.Collections.Generic.List[string]]::new()
    if ($scope.AllRepositoryPullRequests) {
        $includeParts.Add("all open pull requests")
    }
    else {
        if ($scope.LabelsAny.Count -gt 0) {
            $includeParts.Add("labels any of [$($scope.LabelsAny -join ', ')]")
        }
        if ($scope.PathsAny.Count -gt 0) {
            $includeParts.Add("paths any of [$($scope.PathsAny -join ', ')]")
        }
    }
    if ($scope.RequireLabels.Count -gt 0) {
        $constraintParts.Add("requires all labels [$($scope.RequireLabels -join ', ')]")
    }
    if ($scope.ExcludeLabels.Count -gt 0) {
        $constraintParts.Add("excludes labels [$($scope.ExcludeLabels -join ', ')]")
    }
    if ($scope.Authors.Count -gt 0) {
        $constraintParts.Add("authors [$($scope.Authors -join ', ')]")
    }

    $selection = "($($includeParts -join ' OR '))"
    if ($constraintParts.Count -gt 0) {
        $selection += " AND $($constraintParts -join ' AND ')"
    }

    $result = [pscustomobject]@{
        schemaVersion = "1.0.0"
        display = Get-DisplayMetadata
        generatedAt = $Now.ToUniversalTime().ToString("o")
        repository = $Repository
        filter = [pscustomobject]@{
            name = $scope.Name
            description = $scope.Description
            coverage = $scope.Coverage
            labelsAny = $scope.LabelsAny
            pathsAny = $scope.PathsAny
            requireLabels = $scope.RequireLabels
            excludeLabels = $scope.ExcludeLabels
            authors = $scope.Authors
            allRepositoryPullRequests = $scope.AllRepositoryPullRequests
            selection = $selection
        }
        query = [pscustomobject]@{
            openPullRequestCount = $openPullRequestCount
            returnedPullRequestCount = $pullRequests.Count
            complete = $pullRequests.Count -eq $openPullRequestCount
        }
        census = [pscustomobject]@{
            openPullRequests = $openPullRequestCount
            matched = $orderedItems.Count
            labelOnly = $labelOnlyCount
            pathOnly = $pathOnlyCount
            labelAndPath = $bothCount
            incidentalPathExcluded = $incidentalPathCount
            unresolvedMergeable = $unresolvedMergeable
            byBucket = [pscustomobject]$bucketCounts
        }
        overflow = [pscustomobject]@{
            reviewNow = [Math]::Max(0, $bucketCounts.ReviewNow - @($orderedItems | Where-Object { $_.shownInDigest -and $_.bucket -eq "ReviewNow" }).Count)
            needsRescue = [Math]::Max(0, $bucketCounts.NeedsRescue - @($orderedItems | Where-Object { $_.shownInDigest -and $_.bucket -eq "NeedsRescue" }).Count)
            readyToMerge = [Math]::Max(0, $bucketCounts.ReadyToMerge - @($orderedItems | Where-Object { $_.shownInDigest -and $_.bucket -eq "ReadyToMerge" }).Count)
        }
        caps = [pscustomobject]@{
            reviewNow = $MaxReviewNow
            reviewNowPerAuthor = $MaxReviewNowPerAuthor
            needsRescue = $MaxNeedsRescue
            readyToMerge = $MaxReadyToMerge
        }
        warnings = @($warnings)
        items = $orderedItems
    }

    if ($OutputFormat -eq "Json") {
        $result | ConvertTo-Json -Depth 20
    }
    else {
        Render-Markdown -Result $result
    }
}

Export-ModuleMember -Function Invoke-PRAttentionQueue
