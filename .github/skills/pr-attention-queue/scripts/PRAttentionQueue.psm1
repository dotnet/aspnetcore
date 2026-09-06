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

function Invoke-GhText {
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
            return $stdout.Trim()
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

function Test-AnyExactMatch {
    param(
        [string[]]$Values,
        [string[]]$ExpectedValues
    )

    foreach ($expectedValue in $ExpectedValues) {
        foreach ($value in $Values) {
            if ([string]::Equals($value, $expectedValue, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }
        }
    }

    return $false
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
        [string[]]$KnownBotPatterns,
        [string]$AuthorLogin
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
                -not [string]::Equals($login, $AuthorLogin, [System.StringComparison]::OrdinalIgnoreCase) -and
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

function Get-DiscussionCommentKind {
    param(
        [string]$Body,
        [bool]$IsAuthor
    )

    $normalizedBody = ($Body -replace "\s+", " ").Trim().ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($normalizedBody)) {
        return "unknown"
    }

    if ($IsAuthor -and $normalizedBody -match "\b(no longer reproduce|unable to reproduce|happy to close|may no longer be required|no longer needed|obsolete)\b") {
        return "disposition"
    }

    if ($normalizedBody -match "\b(please|should|need to|needs to|avoid|regress|could you|would you|why|fix|change|update|remove|add|consider|incorrect|wrong|issue|problem|fail|failure|break|blocking|block|must)\b|\?") {
        return "actionable"
    }

    if (-not $IsAuthor -and
        $normalizedBody -match "^(fyi|for context|thanks|thank you|nit:)([,.!:\s]|$)") {
        return "informational"
    }

    if ($IsAuthor) {
        return "author-response"
    }

    return "unknown"
}

function Get-DiscussionAssessment {
    param(
        [object]$PullRequest,
        [object]$AuthorInfo,
        [string[]]$KnownBotPatterns
    )

    $humanReviews = @(
        Get-HumanReviews `
            -PullRequest $PullRequest `
            -KnownBotPatterns $KnownBotPatterns `
            -AuthorLogin $AuthorInfo.Login
    )
    $latestHumanReview = if ($humanReviews.Count -gt 0) { $humanReviews[0] } else { $null }
    $discussionComments = @(
        ConvertTo-Array (Get-PropertyValue -Object $PullRequest -Name "discussionComments")
    )
    $hasDiscussionCommentData = $null -ne $PullRequest.PSObject.Properties["discussionComments"]
    if (-not $hasDiscussionCommentData) {
        $discussionComments = @(
            ConvertTo-Array (Get-PropertyValue -Object $PullRequest -Name "comments")
        )
    }

    $hasDiscussionThreadData = $null -ne $PullRequest.PSObject.Properties["discussionThreads"]
    $discussionThreads = @(
        ConvertTo-Array (Get-PropertyValue -Object $PullRequest -Name "discussionThreads")
    )
    $commentsComplete = [bool](Get-PropertyValue `
        -Object $PullRequest `
        -Name "discussionCommentsComplete" `
        -DefaultValue ($discussionComments.Count -eq 0 -or ($discussionComments | Where-Object {
            $null -eq $_.PSObject.Properties["bodyText"] -and $null -eq $_.PSObject.Properties["body"]
        }).Count -eq 0))
    $threadsComplete = [bool](Get-PropertyValue `
        -Object $PullRequest `
        -Name "discussionThreadsComplete" `
        -DefaultValue ($hasDiscussionThreadData -or $discussionThreads.Count -eq 0))
    $commentTotalCount = [int](Get-PropertyValue `
        -Object $PullRequest `
        -Name "discussionCommentTotalCount" `
        -DefaultValue $discussionComments.Count)
    $threadTotalCount = [int](Get-PropertyValue `
        -Object $PullRequest `
        -Name "discussionThreadTotalCount" `
        -DefaultValue $discussionThreads.Count)

    $comments = @(
        foreach ($comment in $discussionComments) {
            $commentAuthor = Get-PropertyValue -Object $comment -Name "author"
            $login = [string](Get-PropertyValue -Object $commentAuthor -Name "login" -DefaultValue "")
            $createdAtValue = Get-PropertyValue -Object $comment -Name "createdAt"
            if ([string]::IsNullOrWhiteSpace($login) -or -not $createdAtValue) {
                continue
            }

            $body = [string](Get-PropertyValue `
                -Object $comment `
                -Name "bodyText" `
                -DefaultValue (Get-PropertyValue -Object $comment -Name "body" -DefaultValue ""))
            $isAuthor = [string]::Equals($login, $AuthorInfo.Login, [System.StringComparison]::OrdinalIgnoreCase)
            $isBot = Test-IsBotLogin -Login $login -IsBot $false -KnownBotPatterns $KnownBotPatterns
            $association = [string](Get-PropertyValue -Object $comment -Name "authorAssociation" -DefaultValue "NONE")
            $actor = if ($isAuthor) {
                "author"
            }
            elseif ($isBot) {
                "automation"
            }
            elseif ($association -in @("OWNER", "MEMBER", "COLLABORATOR")) {
                "repository-member"
            }
            else {
                "non-author"
            }
            [pscustomobject]@{
                Author = $login
                Actor = $actor
                Association = $association
                CreatedAt = [datetime]$createdAtValue
                Kind = Get-DiscussionCommentKind -Body $body -IsAuthor $isAuthor
                Excerpt = if ($body.Length -gt 280) { "$($body.Substring(0, 277))..." } else { $body }
            }
        }
    ) | Sort-Object -Property CreatedAt -Descending

    $latestAuthorActivityAt = @(
        $comments |
            Where-Object {
                $_.Actor -eq "author"
            } |
            Select-Object -First 1
    )
    $latestAuthorActivityAt = if ($latestAuthorActivityAt.Count -gt 0) {
        $latestAuthorActivityAt[0].CreatedAt
    }
    else {
        $null
    }

    $signals = [System.Collections.Generic.List[string]]::new()
    if (-not $commentsComplete -or -not $threadsComplete) {
        $signals.Add("discussion-incomplete")
    }

    foreach ($comment in $comments) {
        if ($comment.Actor -eq "author" -and
            $comment.Kind -eq "disposition") {
            $signals.Add("author-disposition-mentioned")
        }
        elseif ($comment.Actor -notin @("author", "automation") -and
            $comment.Kind -ne "informational") {
            if ($latestAuthorActivityAt -and $comment.CreatedAt -gt $latestAuthorActivityAt) {
                $signals.Add("non-author-discussion-after-author-response")
            }
            else {
                $signals.Add("non-author-discussion-requires-verification")
            }
        }
    }

    $unresolvedThreads = @($discussionThreads | Where-Object { -not [bool](Get-PropertyValue -Object $_ -Name "isResolved" -DefaultValue $false) })
    $outdatedUnresolvedThreads = @(
        $unresolvedThreads | Where-Object { [bool](Get-PropertyValue -Object $_ -Name "isOutdated" -DefaultValue $false) }
    )
    $currentUnresolvedThreads = @(
        $unresolvedThreads | Where-Object { -not [bool](Get-PropertyValue -Object $_ -Name "isOutdated" -DefaultValue $false) }
    )
    if ($currentUnresolvedThreads.Count -gt 0) {
        $signals.Add("current-inline-discussion-unassessed")
    }
    $uniqueSignals = @($signals | Select-Object -Unique)
    $state = if ($uniqueSignals.Count -gt 0) { "verification-needed" } else { "clear" }

    return [pscustomobject]@{
        State = $state
        Complete = $commentsComplete -and $threadsComplete
        Signals = $uniqueSignals
        Comments = @($comments | Select-Object -First 10)
        CommentTotalCount = $commentTotalCount
        CommentEvidenceTruncated = $comments.Count -gt 10 -or -not $commentsComplete
        Threads = [pscustomobject]@{
            TotalCount = $threadTotalCount
            ReturnedCount = $discussionThreads.Count
            Complete = $threadsComplete
            UnresolvedCount = $unresolvedThreads.Count
            OutdatedUnresolvedCount = $outdatedUnresolvedThreads.Count
        }
    }
}

function ConvertTo-UtcDateTime {
    param([object]$Value)

    if ($null -eq $Value) {
        return $null
    }

    if ($Value -is [datetime]) {
        $dateTime = [datetime]$Value

        switch ($dateTime.Kind) {
            ([System.DateTimeKind]::Utc) { return $dateTime }
            ([System.DateTimeKind]::Local) { return $dateTime.ToUniversalTime() }
            default { return [System.DateTime]::SpecifyKind($dateTime, [System.DateTimeKind]::Utc) }
        }
    }

    $stringValue = [string]$Value
    if ([string]::IsNullOrWhiteSpace($stringValue)) {
        return $null
    }

    try {
        return [System.DateTimeOffset]::Parse($stringValue).UtcDateTime
    }
    catch {
        try {
            return [datetime]::Parse(
                $stringValue,
                [System.Globalization.CultureInfo]::InvariantCulture,
                [System.Globalization.DateTimeStyles]::AssumeUniversal -bor [System.Globalization.DateTimeStyles]::AdjustToUniversal)
        }
        catch {
            return $null
        }
    }
}

function Get-DaysSince {
    param(
        [datetime]$From,
        [datetime]$To
    )

    $fromUtc = ConvertTo-UtcDateTime -Value $From
    $toUtc = ConvertTo-UtcDateTime -Value $To
    if ($null -eq $fromUtc -or $null -eq $toUtc) {
        return 0
    }

    return [Math]::Max(0, [Math]::Floor(($toUtc - $fromUtc).TotalDays))
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

function Get-PersonalCoverage {
    param(
        [object]$PullRequest,
        [string]$SourceName
    )

    $coverage = Get-PropertyValue -Object (Get-PropertyValue -Object $PullRequest -Name "personalCoverage") -Name $SourceName
    if ($coverage) {
        return [pscustomobject]@{
            state = [string](Get-PropertyValue -Object $coverage -Name "state" -DefaultValue "assessed")
            detail = [string](Get-PropertyValue -Object $coverage -Name "detail" -DefaultValue "")
        }
    }

    if ($null -ne $PullRequest.PSObject.Properties["personalCoverage"]) {
        return [pscustomobject]@{
            state = "assessed"
            detail = ""
        }
    }

    return [pscustomobject]@{
        state = "unavailable"
        detail = "Personal evidence was not collected."
    }
}

function Get-PersonalInboxItem {
    param(
        [object]$Item,
        [string]$PersonalLogin
    )

    $reviewRequests = @(
        ConvertTo-Array (Get-PropertyValue -Object $Item -Name "personalReviewRequests")
    )
    $directRequests = @(
        $reviewRequests |
            Where-Object {
                [string]::Equals(
                    [string](Get-PropertyValue -Object $_ -Name "login" -DefaultValue ""),
                    $PersonalLogin,
                    [System.StringComparison]::OrdinalIgnoreCase)
            }
    )
    $reviews = @(
        ConvertTo-Array (Get-PropertyValue -Object $Item -Name "personalReviews")
    ) | Sort-Object {
        ConvertTo-UtcDateTime -Value (Get-PropertyValue -Object $_ -Name "submittedAt")
    } -Descending
    $latestReview = if ($reviews.Count -gt 0) { $reviews[0] } else { $null }
    $latestReviewCommit = [string](Get-PropertyValue -Object $latestReview -Name "commitOid" -DefaultValue "")
    $headSha = [string](Get-PropertyValue -Object $Item -Name "headRefOid" -DefaultValue (Get-PropertyValue -Object $Item -Name "headSha" -DefaultValue ""))
    $changedSinceReviewStatus = if ($null -eq $latestReview) {
        "unassessed"
    }
    elseif ([string]::IsNullOrWhiteSpace($latestReviewCommit) -or [string]::IsNullOrWhiteSpace($headSha)) {
        "unknown"
    }
    elseif ([string]::Equals($latestReviewCommit, $headSha, [System.StringComparison]::OrdinalIgnoreCase)) {
        "no"
    }
    else {
        "yes"
    }

    $notifications = @(
        ConvertTo-Array (Get-PropertyValue -Object $Item -Name "personalNotifications")
    ) | Where-Object {
        [bool](Get-PropertyValue -Object $_ -Name "unread" -DefaultValue $false)
    }
    $latestNotification = $notifications |
        Sort-Object { ConvertTo-UtcDateTime -Value (Get-PropertyValue -Object $_ -Name "updatedAt") } -Descending |
        Select-Object -First 1

    $threadReplies = [System.Collections.Generic.List[object]]::new()
    foreach ($thread in ConvertTo-Array (Get-PropertyValue -Object $Item -Name "personalReviewThreads")) {
        $participation = @(
            ConvertTo-Array (Get-PropertyValue -Object $thread -Name "participation")
        ) | Sort-Object {
            ConvertTo-UtcDateTime -Value (Get-PropertyValue -Object $_ -Name "createdAt")
        } -Descending
        $latestOwnParticipation = $participation |
            Where-Object {
                [string]::Equals(
                    [string](Get-PropertyValue -Object $_ -Name "authorLogin" -DefaultValue ""),
                    $PersonalLogin,
                    [System.StringComparison]::OrdinalIgnoreCase)
            } |
            Select-Object -First 1
        if ($null -eq $latestOwnParticipation) {
            continue
        }

        $reply = $participation |
            Where-Object {
                -not [string]::IsNullOrWhiteSpace(
                    [string](Get-PropertyValue -Object $_ -Name "authorLogin" -DefaultValue "")
                ) -and
                $null -ne (ConvertTo-UtcDateTime -Value (Get-PropertyValue -Object $_ -Name "createdAt")) -and
                -not [string]::Equals(
                    [string](Get-PropertyValue -Object $_ -Name "authorLogin" -DefaultValue ""),
                    $PersonalLogin,
                    [System.StringComparison]::OrdinalIgnoreCase) -and
                (ConvertTo-UtcDateTime -Value (Get-PropertyValue -Object $_ -Name "createdAt")) -gt
                (ConvertTo-UtcDateTime -Value (Get-PropertyValue -Object $latestOwnParticipation -Name "createdAt"))
            } |
            Select-Object -First 1
        if ($reply) {
            $threadReplies.Add([pscustomobject]@{
                threadId = [string](Get-PropertyValue -Object $thread -Name "threadId" -DefaultValue "")
                authorLogin = [string](Get-PropertyValue -Object $reply -Name "authorLogin" -DefaultValue "")
                createdAt = Get-PropertyValue -Object $reply -Name "createdAt"
                url = [string](Get-PropertyValue -Object $reply -Name "url" -DefaultValue $Item.url)
                isResolved = [bool](Get-PropertyValue -Object $thread -Name "isResolved" -DefaultValue $false)
            })
        }
    }

    $signals = [System.Collections.Generic.List[object]]::new()
    if ($directRequests.Count -gt 0) {
        $request = $directRequests |
            Sort-Object { ConvertTo-UtcDateTime -Value (Get-PropertyValue -Object $_ -Name "requestedAt") } -Descending |
            Select-Object -First 1
        $signals.Add([pscustomobject]@{
            kind = "direct-request"
            eventAt = Get-PropertyValue -Object $request -Name "requestedAt"
            evidenceUrl = [string](Get-PropertyValue -Object $request -Name "url" -DefaultValue $Item.url)
            detail = "GitHub currently requests your review."
        })
    }
    if ($latestNotification) {
        $signals.Add([pscustomobject]@{
            kind = "follow-up-notification"
            eventAt = Get-PropertyValue -Object $latestNotification -Name "updatedAt"
            evidenceUrl = [string](Get-PropertyValue -Object $latestNotification -Name "url" -DefaultValue $Item.url)
            detail = "GitHub has unread activity associated with your participation or mention."
            unread = $true
            reason = [string](Get-PropertyValue -Object $latestNotification -Name "reason" -DefaultValue "")
        })
    }
    if ($changedSinceReviewStatus -eq "yes") {
        $signals.Add([pscustomobject]@{
            kind = "changed-since-own-review"
            eventAt = $null
            evidenceUrl = [string](Get-PropertyValue -Object $latestReview -Name "url" -DefaultValue $Item.url)
            detail = "The current head differs from the commit attached to your latest submitted review."
            baselineCommit = $latestReviewCommit
            currentHead = $headSha
        })
    }
    foreach ($reply in $threadReplies) {
        $signals.Add([pscustomobject]@{
            kind = "review-thread-reply"
            eventAt = $reply.createdAt
            evidenceUrl = $reply.url
            detail = "A participant replied after your latest participation in a review thread."
            responder = $reply.authorLogin
            threadId = $reply.threadId
            resolved = $reply.isResolved
        })
    }

    $discoveryKinds = @(
        ConvertTo-Array (Get-PropertyValue -Object $Item -Name "personalDiscoveryKinds")
    )
    $participated = $reviews.Count -gt 0 -or
        @($Item.personalComments).Count -gt 0 -or
        @($Item.personalMentions).Count -gt 0 -or
        "reviewed-by" -in $discoveryKinds
    $signalOrder = @{
        "direct-request" = 0
        "follow-up-notification" = 1
        "changed-since-own-review" = 2
        "review-thread-reply" = 3
    }
    $orderedSignals = @(
        $signals |
            Sort-Object `
                @{ Expression = { $signalOrder[$_.kind] }; Ascending = $true },
                @{ Expression = { ConvertTo-UtcDateTime -Value $_.eventAt }; Descending = $true },
                @{ Expression = { [int]$Item.number }; Ascending = $true }
    )

    $coverage = [ordered]@{
        discovery = Get-PersonalCoverage -PullRequest $Item -SourceName "discovery"
        notifications = Get-PersonalCoverage -PullRequest $Item -SourceName "notifications"
        ownReview = Get-PersonalCoverage -PullRequest $Item -SourceName "ownReview"
        reviewThreads = Get-PersonalCoverage -PullRequest $Item -SourceName "reviewThreads"
    }

    return [pscustomobject]@{
        number = [int]$Item.number
        title = [string]$Item.title
        url = [string]$Item.url
        author = [string]$Item.author
        bucket = [string](Get-PropertyValue -Object $Item -Name "bucket" -DefaultValue "Unknown")
        nextActor = [string](Get-PropertyValue -Object $Item -Name "nextActor" -DefaultValue "unknown")
        blockers = @($Item.blockers)
        headSha = $headSha
        latestOwnReview = if ($latestReview) {
            [pscustomobject]@{
                state = [string](Get-PropertyValue -Object $latestReview -Name "state" -DefaultValue "")
                submittedAt = Get-PropertyValue -Object $latestReview -Name "submittedAt"
                commitOid = $latestReviewCommit
                url = [string](Get-PropertyValue -Object $latestReview -Name "url" -DefaultValue $Item.url)
            }
        }
        else { $null }
        changedSinceOwnReview = [pscustomobject]@{
            status = $changedSinceReviewStatus
            baselineCommit = $latestReviewCommit
            currentHead = $headSha
        }
        teamReviewRequests = @(
            ConvertTo-Array (Get-PropertyValue -Object $Item -Name "personalTeamReviewRequests")
        )
        discoveryKinds = @(
            ConvertTo-Array (Get-PropertyValue -Object $Item -Name "personalDiscoveryKinds")
        )
        participatedOrMentioned = $participated
        directRequest = $directRequests.Count -gt 0
        signals = $orderedSignals
        coverage = [pscustomobject]$coverage
        digestVisible = [bool](Get-PropertyValue -Object $Item -Name "shownInDigest" -DefaultValue $false)
        digestRank = Get-PropertyValue -Object $Item -Name "digestRank"
        generalScope = [string](Get-PropertyValue -Object $Item -Name "scopeMatch" -DefaultValue "personal-only")
        generalDigestExclusionReasons = @($Item.digestExclusionReasons)
    }
}

function Get-PersonalSearchCandidates {
    param(
        [string]$RepositoryName,
        [string]$PersonalLogin
    )

    $queries = @(
        "repo:$RepositoryName is:pr is:open reviewed-by:$PersonalLogin",
        "repo:$RepositoryName is:pr is:open review-requested:$PersonalLogin",
        "repo:$RepositoryName is:pr is:open commenter:$PersonalLogin",
        "repo:$RepositoryName is:pr is:open mentions:$PersonalLogin"
    )
    $queryKinds = @(
        "reviewed-by",
        "review-requested",
        "commenter",
        "mentions"
    )
    $results = @{}
    $incomplete = $false
    for ($queryIndex = 0; $queryIndex -lt $queries.Count; $queryIndex++) {
        $query = $queries[$queryIndex]
        $queryKind = $queryKinds[$queryIndex]
        for ($page = 1; $page -le 10; $page++) {
            $encodedQuery = [Uri]::EscapeDataString($query)
            $response = Invoke-GhJson -Arguments @(
                "api",
                "search/issues?q=$encodedQuery&per_page=100&page=$page"
            )
            $incomplete = $incomplete -or [bool](Get-PropertyValue -Object $response -Name "incomplete_results" -DefaultValue $false)
            $pageItems = @(ConvertTo-Array (Get-PropertyValue -Object $response -Name "items"))
            foreach ($item in $pageItems) {
                $number = [int](Get-PropertyValue -Object $item -Name "number")
                $existing = if ($results.ContainsKey($number)) { $results[$number] } else { $null }
                if ($existing) {
                    $kinds = @(
                        ConvertTo-Array (Get-PropertyValue -Object $existing -Name "personalDiscoveryKinds")
                    )
                    if ($queryKind -notin $kinds) {
                        $existing.personalDiscoveryKinds = @($kinds + $queryKind)
                    }
                    continue
                }

                $item | Add-Member `
                    -NotePropertyName "personalDiscoveryKinds" `
                    -NotePropertyValue @($queryKind) `
                    -Force
                $results[$number] = $item
            }

            $totalCount = [int](Get-PropertyValue -Object $response -Name "total_count" -DefaultValue $pageItems.Count)
            if ($pageItems.Count -eq 0 -or $page * 100 -ge $totalCount) {
                break
            }
        }
    }

    return [pscustomobject]@{
        items = @($results.Values | Sort-Object number)
        coverage = if ($incomplete) { "partial" } else { "assessed" }
    }
}

function Get-RepositoryNotifications {
    param([string]$RepositoryName)

    $allNotifications = [System.Collections.Generic.List[object]]::new()
    $coverageState = "assessed"
    $coverageDetail = "Repository notification access succeeded for the bounded feed."
    try {
        for ($page = 1; $page -le 10; $page++) {
            $notifications = Invoke-GhJson -Arguments @(
                "api",
                "repos/$RepositoryName/notifications?all=true&per_page=100&page=$page"
            )
            foreach ($notification in ConvertTo-Array $notifications) {
                $allNotifications.Add($notification)
            }

            if (@($notifications).Count -lt 100) {
                break
            }

            if ($page -eq 10) {
                $coverageState = "partial"
                $coverageDetail = "Repository notification access was capped at ten pages of 100 entries."
            }
        }
    }
    catch {
        $coverageState = "unavailable"
        $coverageDetail = "Repository notification access failed: $($_.Exception.Message)"
    }

    return [pscustomobject]@{
        items = @($allNotifications)
        coverage = [pscustomobject]@{
            state = $coverageState
            detail = $coverageDetail
        }
    }
}

function Add-PersonalReviewThreadDetails {
    param(
        [string]$RepositoryName,
        [object[]]$Candidates
    )

    if ($Candidates.Count -eq 0) {
        return
    }

    $repositoryParts = $RepositoryName.Split("/")
    for ($offset = 0; $offset -lt $Candidates.Count; $offset += 20) {
        $chunk = @($Candidates | Select-Object -Skip $offset -First 20)
        $aliases = @(
            foreach ($candidate in $chunk) {
                $number = [int]$candidate.number
                @"
pr$number`: pullRequest(number: $number) {
  reviewThreads(last: 50) {
    pageInfo { hasPreviousPage }
    nodes {
      id
      isResolved
      isOutdated
      comments(last: 20) {
        pageInfo { hasPreviousPage }
        nodes {
          author { login }
          createdAt
          url
        }
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
            $detail = Get-PropertyValue -Object $result.data.repository -Name "pr$([int]$candidate.number)"
            $threads = @(
                ConvertTo-Array (Get-PropertyValue `
                    -Object (Get-PropertyValue -Object $detail -Name "reviewThreads") `
                    -Name "nodes")
            )
            $threadConnection = Get-PropertyValue -Object $detail -Name "reviewThreads"
            $candidate | Add-Member -NotePropertyName "personalReviewThreads" -NotePropertyValue @(
                foreach ($thread in $threads) {
                    $commentConnection = Get-PropertyValue -Object $thread -Name "comments"
                    [pscustomobject]@{
                        threadId = [string](Get-PropertyValue -Object $thread -Name "id" -DefaultValue "")
                        isResolved = [bool](Get-PropertyValue -Object $thread -Name "isResolved" -DefaultValue $false)
                        isOutdated = [bool](Get-PropertyValue -Object $thread -Name "isOutdated" -DefaultValue $false)
                        commentsPartial = [bool](Get-PropertyValue -Object (Get-PropertyValue -Object $commentConnection -Name "pageInfo") -Name "hasPreviousPage" -DefaultValue $false)
                        participation = @(
                            foreach ($comment in ConvertTo-Array (Get-PropertyValue `
                                -Object (Get-PropertyValue -Object $thread -Name "comments") `
                                -Name "nodes")) {
                                $commentAuthor = Get-PropertyValue -Object $comment -Name "author"
                                [pscustomobject]@{
                                    authorLogin = [string](Get-PropertyValue -Object $commentAuthor -Name "login" -DefaultValue "")
                                    createdAt = Get-PropertyValue -Object $comment -Name "createdAt"
                                    url = [string](Get-PropertyValue -Object $comment -Name "url" -DefaultValue $candidate.url)
                                }
                            }
                        )
                    }
                }
            ) -Force
            $candidate | Add-Member -NotePropertyName "personalReviewThreadsPartial" -NotePropertyValue (
                [bool](Get-PropertyValue -Object (Get-PropertyValue -Object $threadConnection -Name "pageInfo") -Name "hasPreviousPage" -DefaultValue $false) -or
                @($candidate.personalReviewThreads | Where-Object { $_.commentsPartial }).Count -gt 0
            ) -Force
        }
    }
}

function Add-PersonalEvidenceDetails {
    param(
        [string]$RepositoryName,
        [object[]]$Candidates,
        [string]$PersonalLogin,
        [object[]]$Notifications,
        [object]$NotificationCoverage
    )

    $repositoryParts = $RepositoryName.Split("/")
    if ($repositoryParts.Count -ne 2) {
        throw "Repository must use the owner/name format."
    }

    for ($offset = 0; $offset -lt $Candidates.Count; $offset += 20) {
        $chunk = @($Candidates | Select-Object -Skip $offset -First 20)
        $aliases = @(
            foreach ($candidate in $chunk) {
                $number = [int](Get-PropertyValue -Object $candidate -Name "number")
                @"
pr$number`: pullRequest(number: $number) {
  number
  title
  url
  author { login }
  state
  isDraft
  updatedAt
  headRefOid
  reviewRequests(first: 50) {
    pageInfo { hasNextPage }
    nodes {
      requestedReviewer {
        ... on User { login }
        ... on Team { slug name }
      }
    }
  }
  reviews(last: 1, author: `$login, states: [COMMENTED, APPROVED, CHANGES_REQUESTED, DISMISSED]) {
    nodes {
      author { login }
      state
      submittedAt
      url
      commit { oid }
    }
  }
}
"@
            }
        )
        $query = 'query($owner:String!,$name:String!,$login:String!){repository(owner:$owner,name:$name){' +
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
            "name=$($repositoryParts[1])",
            "-F",
            "login=$PersonalLogin"
        )

        foreach ($candidate in $chunk) {
            $number = [int](Get-PropertyValue -Object $candidate -Name "number")
            $details = Get-PropertyValue -Object $result.data.repository -Name "pr$number"
            if ($null -eq $details) {
                throw "GitHub did not return personal details for pull request #$number."
            }

            foreach ($propertyName in @(
                "number", "title", "url", "author", "state", "isDraft", "updatedAt", "headRefOid"
            )) {
                $propertyValue = Get-PropertyValue -Object $details -Name $propertyName
                if ($null -ne $propertyValue) {
                    $candidate | Add-Member -NotePropertyName $propertyName -NotePropertyValue $propertyValue -Force
                }
            }

            $reviewRequestConnection = Get-PropertyValue -Object $details -Name "reviewRequests"
            $reviewRequests = @(
                ConvertTo-Array (Get-PropertyValue -Object $reviewRequestConnection -Name "nodes")
            )
            $candidate | Add-Member -NotePropertyName "personalReviewRequests" -NotePropertyValue @(
                foreach ($request in $reviewRequests) {
                    $reviewer = Get-PropertyValue -Object $request -Name "requestedReviewer"
                    $login = [string](Get-PropertyValue -Object $reviewer -Name "login" -DefaultValue "")
                    if ([string]::Equals($login, $PersonalLogin, [System.StringComparison]::OrdinalIgnoreCase)) {
                        [pscustomobject]@{
                            login = $login
                            requestedAt = $null
                            url = [string](Get-PropertyValue -Object $candidate -Name "url" -DefaultValue $candidate.html_url)
                        }
                    }
                }
            ) -Force
            $candidate | Add-Member -NotePropertyName "personalTeamReviewRequests" -NotePropertyValue @(
                foreach ($request in $reviewRequests) {
                    $reviewer = Get-PropertyValue -Object $request -Name "requestedReviewer"
                    $teamSlug = [string](Get-PropertyValue -Object $reviewer -Name "slug" -DefaultValue "")
                    if ($teamSlug) {
                        [pscustomobject]@{
                            slug = $teamSlug
                            name = [string](Get-PropertyValue -Object $reviewer -Name "name" -DefaultValue "")
                            url = [string](Get-PropertyValue -Object $candidate -Name "url" -DefaultValue $candidate.html_url)
                        }
                    }
                }
            ) -Force
            $candidate | Add-Member -NotePropertyName "personalReviews" -NotePropertyValue @(
                ConvertTo-Array (Get-PropertyValue -Object (Get-PropertyValue -Object $details -Name "reviews") -Name "nodes") |
                    ForEach-Object {
                        $commit = Get-PropertyValue -Object $_ -Name "commit"
                        [pscustomobject]@{
                            state = [string](Get-PropertyValue -Object $_ -Name "state")
                            submittedAt = Get-PropertyValue -Object $_ -Name "submittedAt"
                            commitOid = [string](Get-PropertyValue -Object $commit -Name "oid" -DefaultValue "")
                            url = [string](Get-PropertyValue -Object $_ -Name "url" -DefaultValue $candidate.html_url)
                        }
                    }
            ) -Force
            $requestCoverage = if ([bool](Get-PropertyValue -Object (Get-PropertyValue -Object $reviewRequestConnection -Name "pageInfo") -Name "hasNextPage" -DefaultValue $false)) {
                [pscustomobject]@{
                    state = "partial"
                    detail = "GitHub returned the first 50 current review requests."
                }
            }
            else {
                [pscustomobject]@{
                    state = "assessed"
                    detail = "GitHub returned all current review requests."
                }
            }
            $candidate | Add-Member -NotePropertyName "personalReviewRequestCoverage" -NotePropertyValue $requestCoverage -Force
        }
    }

    foreach ($candidate in $Candidates) {
        $candidateNumber = [int](Get-PropertyValue -Object $candidate -Name "number")
        $discoveryKinds = @(
            ConvertTo-Array (Get-PropertyValue -Object $candidate -Name "personalDiscoveryKinds")
        )
        $hasSearchEvidence = @(
            $discoveryKinds | Where-Object {
                $_ -in @("reviewed-by", "review-requested", "commenter", "mentions")
            }
        ).Count -gt 0
        $hasCurrentDirectRequest = @(
            ConvertTo-Array (Get-PropertyValue -Object $candidate -Name "personalReviewRequests")
        ).Count -gt 0
        $candidate | Add-Member -NotePropertyName "personalNotifications" -NotePropertyValue @(
            foreach ($notification in $Notifications) {
                $subject = Get-PropertyValue -Object $notification -Name "subject"
                $subjectUrl = [string](Get-PropertyValue -Object $subject -Name "url" -DefaultValue "")
                $reason = [string](Get-PropertyValue -Object $notification -Name "reason" -DefaultValue "")
                $isPersonallyRelevant = $hasSearchEvidence -or
                    $reason -eq "mention" -or
                    ($reason -eq "review_requested" -and $hasCurrentDirectRequest)
                if ($isPersonallyRelevant -and
                    ($subjectUrl -match "/issues/$candidateNumber$" -or $subjectUrl -match "/pulls/$candidateNumber$")) {
                    [pscustomobject]@{
                        unread = [bool](Get-PropertyValue -Object $notification -Name "unread" -DefaultValue $false)
                        updatedAt = Get-PropertyValue -Object $notification -Name "updated_at"
                        reason = $reason
                        url = [string](Get-PropertyValue -Object $candidate -Name "html_url" -DefaultValue $candidate.url)
                    }
                }
            }
        ) -Force
        $candidate | Add-Member -NotePropertyName "personalComments" -NotePropertyValue @(
            if ("commenter" -in $discoveryKinds) {
                [pscustomobject]@{
                    source = "commenter-search"
                    url = [string](Get-PropertyValue -Object $candidate -Name "url" -DefaultValue $candidate.html_url)
                }
            }
        ) -Force
        $candidate | Add-Member -NotePropertyName "personalMentions" -NotePropertyValue @(
            if ("mentions" -in $discoveryKinds) {
                [pscustomobject]@{
                    source = "mentions-search"
                    url = [string](Get-PropertyValue -Object $candidate -Name "url" -DefaultValue $candidate.html_url)
                }
            }
        ) -Force
        $candidate | Add-Member -NotePropertyName "personalCoverage" -NotePropertyValue ([pscustomobject]@{
            discovery = [pscustomobject]@{ state = "assessed"; detail = "Four bounded repository search qualifiers were queried." }
            notifications = $NotificationCoverage
            ownReview = [pscustomobject]@{ state = "assessed"; detail = "GitHub returned the latest submitted review authored by the authenticated user." }
            reviewRequests = Get-PropertyValue -Object $candidate -Name "personalReviewRequestCoverage" -DefaultValue ([pscustomobject]@{
                state = "assessed"
                detail = "GitHub returned current review requests."
            })
            reviewThreads = [pscustomobject]@{ state = "unassessed"; detail = "Review-thread hydration is deferred in this bounded collector." }
        }) -Force
    }

    try {
        Add-PersonalReviewThreadDetails -RepositoryName $RepositoryName -Candidates $Candidates
        foreach ($candidate in $Candidates) {
            $coverage = Get-PropertyValue -Object $candidate -Name "personalCoverage"
            if ($coverage) {
                $coverage.reviewThreads = [pscustomobject]@{
                    state = if ([bool](Get-PropertyValue -Object $candidate -Name "personalReviewThreadsPartial" -DefaultValue $true)) {
                        "partial"
                    }
                    else {
                        "assessed"
                    }
                    detail = "Up to 50 review threads and 20 comments per thread were hydrated; publication requires an author, timestamp, and canonical URL."
                }
            }
        }
    }
    catch {
        foreach ($candidate in $Candidates) {
            $coverage = Get-PropertyValue -Object $candidate -Name "personalCoverage"
            if ($coverage) {
                $coverage.reviewThreads = [pscustomobject]@{
                    state = "unavailable"
                    detail = "GitHub did not permit bounded review-thread hydration."
                }
            }
            $candidate | Add-Member -NotePropertyName "personalReviewThreads" -NotePropertyValue @() -Force
        }
    }
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
  mergeStateStatus
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
                -NotePropertyName "mergeStateStatus" `
                -NotePropertyValue (Get-PropertyValue -Object $detail -Name "mergeStateStatus" -DefaultValue "UNKNOWN") `
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

function Add-DiscussionEvidenceDetails {
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
  comments(last: 50) {
    totalCount
    pageInfo { hasPreviousPage }
    nodes {
      author { login }
      authorAssociation
      createdAt
      bodyText
    }
  }
  reviewThreads(last: 50) {
    totalCount
    pageInfo { hasPreviousPage }
    nodes {
      isResolved
      isOutdated
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
                throw "GitHub did not return discussion evidence for pull request #$number."
            }

            $commentsConnection = Get-PropertyValue -Object $detail -Name "comments"
            $threadsConnection = Get-PropertyValue -Object $detail -Name "reviewThreads"
            $comments = @(
                ConvertTo-Array (Get-PropertyValue -Object $commentsConnection -Name "nodes")
            )
            $threads = @(
                ConvertTo-Array (Get-PropertyValue -Object $threadsConnection -Name "nodes")
            )
            $commentsPageInfo = Get-PropertyValue -Object $commentsConnection -Name "pageInfo"
            $threadsPageInfo = Get-PropertyValue -Object $threadsConnection -Name "pageInfo"

            $pullRequest | Add-Member -NotePropertyName "discussionComments" -NotePropertyValue $comments -Force
            $pullRequest | Add-Member `
                -NotePropertyName "discussionCommentTotalCount" `
                -NotePropertyValue ([int](Get-PropertyValue -Object $commentsConnection -Name "totalCount" -DefaultValue $comments.Count)) `
                -Force
            $pullRequest | Add-Member `
                -NotePropertyName "discussionCommentsComplete" `
                -NotePropertyValue (-not [bool](Get-PropertyValue -Object $commentsPageInfo -Name "hasPreviousPage" -DefaultValue $true)) `
                -Force
            $pullRequest | Add-Member -NotePropertyName "discussionThreads" -NotePropertyValue $threads -Force
            $pullRequest | Add-Member `
                -NotePropertyName "discussionThreadTotalCount" `
                -NotePropertyValue ([int](Get-PropertyValue -Object $threadsConnection -Name "totalCount" -DefaultValue $threads.Count)) `
                -Force
            $pullRequest | Add-Member `
                -NotePropertyName "discussionThreadsComplete" `
                -NotePropertyValue (-not [bool](Get-PropertyValue -Object $threadsPageInfo -Name "hasPreviousPage" -DefaultValue $true)) `
                -Force
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
    $humanReviews = @(
        Get-HumanReviews `
            -PullRequest $PullRequest `
            -KnownBotPatterns $knownBotPatterns `
            -AuthorLogin $AuthorInfo.Login
    )
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
    $mergeStateStatus = [string](Get-PropertyValue -Object $PullRequest -Name "mergeStateStatus" -DefaultValue "CLEAN")
    $isDraft = [bool](Get-PropertyValue -Object $PullRequest -Name "isDraft" -DefaultValue $false)
    $checkState = Get-CheckState -PullRequest $PullRequest
    $ageDays = Get-DaysSince -From $createdAt -To $SnapshotTime
    $isCommunity = Test-AnyWildcardMatch -Values $Labels -Patterns @($Settings.communityLabels)
    $isBot = Test-IsBotLogin -Login $AuthorInfo.Login -IsBot $AuthorInfo.IsBot -KnownBotPatterns $knownBotPatterns
    $blocked = (Test-AnyWildcardMatch -Values $Labels -Patterns @($Settings.blockedLabels)) -or
        (Test-AnyExactMatch -Values $Labels -ExpectedValues @($Settings.blockedLabelsExact))
    $ciRerunPending = Test-AnyExactMatch -Values $Labels -ExpectedValues @($Settings.pendingCiLabels)
    $designGate = Test-AnyWildcardMatch -Values $Labels -Patterns @($Settings.designGateLabels)
    $headChangedAfterReview = $latestHumanReview -and
        $latestHumanReview.CommitOid -and
        $headSha -and
        $latestHumanReview.CommitOid -ne $headSha
    $authorCommentedAfterReview = $latestHumanReview -and
        $latestAuthorCommentAt -and
        $latestAuthorCommentAt -gt $latestHumanReview.SubmittedAt
    $authorRespondedAfterReview = $headChangedAfterReview -or $authorCommentedAfterReview
    $latestReviewerCommentIsCurrent = $latestHumanReview -and
        $latestHumanReview.State -eq "COMMENTED" -and
        (-not $latestReviewRequestAt -or $latestHumanReview.SubmittedAt -ge $latestReviewRequestAt)
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
    elseif ($blocked) {
        $bucket = "NeedsRescue"
        $nextActor = "maintainer/triager"
        $reasonCodes.Add("blocked-label")
        $blockers.Add("A blocking label requires an explicit triage decision.")
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
    elseif ($ciRerunPending) {
        $bucket = "WaitingOnCI"
        $nextActor = "CI/automation"
        $reasonCodes.Add("ci-rerun-pending")
        $blockers.Add("The pull request is explicitly waiting for CI to be rerun.")
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
        elseif ($mergeStateStatus -eq "BEHIND") {
            $bucket = "WaitingOnAuthor"
            $nextActor = "author/maintainer"
            $reasonCodes.Add("branch-update-required")
            $blockers.Add("The head branch must be updated with its base branch before merge.")
        }
        elseif ($mergeStateStatus -ne "CLEAN") {
            $bucket = "WaitingOnCI"
            $nextActor = "CI/automation"
            $reasonCodes.Add("merge-state-not-clean")
            $blockers.Add("GitHub reports merge state '$mergeStateStatus'; Ready to merge requires CLEAN.")
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
    elseif ($latestReviewerCommentIsCurrent) {
        $bucket = "WaitingOnAuthor"
        $nextActor = "author"
        $reasonCodes.Add("reviewer-commented")
        $waitingSince = $latestHumanReview.SubmittedAt
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
            "branch-update-required" = [pscustomobject]@{
                label = "Branch update required"
                description = "The approved pull request is behind its base branch and must be updated before merge."
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
            "ci-rerun-pending" = [pscustomobject]@{
                label = "CI rerun pending"
                description = "The repository explicitly marks the pull request as waiting for a CI rerun."
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
            "merge-state-not-clean" = [pscustomobject]@{
                label = "Merge state not clean"
                description = "GitHub does not report the pull request merge state as CLEAN."
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
        digestExclusionReasons = [pscustomobject][ordered]@{
            "excluded-author" = [pscustomobject]@{
                label = "Excluded author"
                description = "The pull request remains in the census but does not consume a digest slot because its author was explicitly excluded."
            }
            "stacked-on-unhealthy-pr" = [pscustomobject]@{
                label = "Unhealthy stack ancestor"
                description = "The pull request remains reviewable but does not consume an unattended digest slot while an ancestor pull request is unhealthy."
            }
            "discussion-verification-needed" = [pscustomobject]@{
                label = "Discussion verification needed"
                description = "Recent discussion requires a human to verify whether ordinary code review is the right next action."
            }
            "discussion-not-assessed" = [pscustomobject]@{
                label = "Discussion not assessed"
                description = "Bounded discussion evidence was not collected for this lower-ranked candidate, so it cannot enter the unattended digest."
            }
        }
        discussion = [pscustomobject][ordered]@{
            states = [pscustomobject][ordered]@{
                "clear" = [pscustomobject]@{
                    label = "Discussion checked"
                    description = "The bounded recent discussion evidence did not identify a disposition, non-author concern, or unread current inline thread."
                }
                "verification-needed" = [pscustomobject]@{
                    label = "Verify discussion"
                    description = "Recent discussion or incomplete evidence needs human interpretation before normal review begins."
                }
                "not-assessed" = [pscustomobject]@{
                    label = "Discussion not assessed"
                    description = "This lower-ranked candidate was outside the bounded discussion-evidence pass."
                }
            }
            signals = [pscustomobject][ordered]@{
                "author-disposition-mentioned" = [pscustomobject]@{
                    label = "Author requested disposition"
                    description = "The author raised whether the pull request should continue or close."
                }
                "current-inline-discussion-unassessed" = [pscustomobject]@{
                    label = "Current inline discussion"
                    description = "A current unresolved review thread was found, but its comment evidence was not collected."
                }
                "discussion-incomplete" = [pscustomobject]@{
                    label = "Discussion incomplete"
                    description = "The bounded comments or review threads were truncated, so the assessment cannot be complete."
                }
                "discussion-not-assessed" = [pscustomobject]@{
                    label = "Discussion not assessed"
                    description = "The candidate was outside the bounded discussion-evidence pass."
                }
                "non-author-discussion-after-author-response" = [pscustomobject]@{
                    label = "Later non-author discussion"
                    description = "A non-author comment after the latest author response needs human interpretation."
                }
                "non-author-discussion-requires-verification" = [pscustomobject]@{
                    label = "Non-author discussion"
                    description = "A non-author actionable or unknown comment needs human interpretation."
                }
            }
            commentKinds = [pscustomobject][ordered]@{
                "actionable" = [pscustomobject]@{
                    label = "Actionable"
                    description = "The deterministic text heuristic found an explicit request, question, or concern."
                }
                "author-response" = [pscustomobject]@{
                    label = "Author response"
                    description = "The author replied after review without an explicit disposition phrase."
                }
                "disposition" = [pscustomobject]@{
                    label = "Disposition"
                    description = "The author explicitly raised whether the pull request should continue or close."
                }
                "informational" = [pscustomobject]@{
                    label = "Informational"
                    description = "The comment has an explicit informational marker and no detected request, question, or concern."
                }
                "unknown" = [pscustomobject]@{
                    label = "Unknown"
                    description = "The bounded text heuristic could not safely classify the comment."
                }
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

function Get-ResponseEvidence {
    param(
        [object]$DiscussionAssessment,
        [string]$ItemUrl
    )

    if ($null -eq $DiscussionAssessment) {
        return [pscustomobject]@{
            status = "unknown"
            recordedNonAuthorHumanResponse = $false
            complete = $false
            commentTotalCount = 0
            evidenceCoverage = "not-collected"
            canonicalEvidenceUrl = $ItemUrl
        }
    }

    $comments = @($DiscussionAssessment.comments)
    $recordedResponse = @(
        $comments |
            Where-Object {
                $_.Actor -in @("repository-member", "non-author") -and
                $_.Kind -ne "informational"
            }
    ).Count -gt 0
    $complete = [bool](Get-PropertyValue -Object $DiscussionAssessment -Name "complete" -DefaultValue $false)
    $commentTotalCount = [int](Get-PropertyValue -Object $DiscussionAssessment -Name "commentTotalCount" -DefaultValue $comments.Count)

    $signals = @(
        if ($null -ne $DiscussionAssessment -and $null -ne $DiscussionAssessment.PSObject.Properties["signals"]) {
            @($DiscussionAssessment.signals)
        }
        else {
            @()
        }
    )
    $state = [string](Get-PropertyValue -Object $DiscussionAssessment -Name "state" -DefaultValue "unknown")
    $commentEvidenceTruncated = [bool](Get-PropertyValue -Object $DiscussionAssessment -Name "commentEvidenceTruncated" -DefaultValue $false)
    $hasIncompleteOrAmbiguousEvidence = $state -eq "verification-needed" -or
        $commentEvidenceTruncated -or
        -not $complete -or
        $signals.Count -gt 0

    $status = if ($recordedResponse) {
        "recorded-response"
    }
    elseif ($complete -and $commentTotalCount -eq 0 -and -not $hasIncompleteOrAmbiguousEvidence) {
        "no-response"
    }
    else {
        "unknown"
    }

    return [pscustomobject]@{
        status = $status
        recordedNonAuthorHumanResponse = $recordedResponse
        complete = $complete
        commentTotalCount = $commentTotalCount
        evidenceCoverage = if ($complete) {
            "complete"
        }
        elseif ($DiscussionAssessment.state -eq "verification-needed") {
            "bounded-and-ambiguous"
        }
        else {
            "bounded"
        }
        canonicalEvidenceUrl = $ItemUrl
    }
}

function Get-InboxItemSummary {
    param(
        [object]$Item,
        [object]$Settings
    )

    $discussionAssessment = Get-PropertyValue -Object $Item -Name "discussionAssessment"
    $responseEvidence = Get-ResponseEvidence -DiscussionAssessment $discussionAssessment -ItemUrl $Item.url
    $itemLabels = @($Item.labels)
    $inboxProvenance = if (Test-AnyWildcardMatch -Values $itemLabels -Patterns @($Settings.communityLabels)) {
        "community"
    }
    else {
        "unclassified"
    }

    $createdAtValue = ConvertTo-UtcDateTime -Value $Item.createdAt
    return [pscustomobject]@{
        number = [int]$Item.number
        title = [string]$Item.title
        url = [string]$Item.url
        createdAt = if ($null -ne $createdAtValue) { $createdAtValue.ToUniversalTime().ToString("o") } else { $null }
        bucket = [string]$Item.bucket
        nextActor = [string]$Item.nextActor
        reasonCodes = @($Item.reasonCodes)
        provenance = $inboxProvenance
        responseEvidence = $responseEvidence
    }
}

function Get-InboxData {
    param(
        [object[]]$Items,
        [object]$Settings,
        [datetime]$SnapshotTime
    )

    $communityLabelPatterns = @($Settings.communityLabels)
    $recentWindowDays = 7
    $snapshotUtc = ConvertTo-UtcDateTime -Value $SnapshotTime
    $recentWindowStart = if ($null -ne $snapshotUtc) { $snapshotUtc.AddDays(-$recentWindowDays) } else { $SnapshotTime.AddDays(-$recentWindowDays) }

    $communityItems = @(
        $Items |
            Where-Object {
                Test-AnyWildcardMatch -Values $_.labels -Patterns $communityLabelPatterns
            } |
            Sort-Object `
                @{ Expression = {
                    $createdAt = ConvertTo-UtcDateTime -Value $_.createdAt
                    if ($null -eq $createdAt) { [datetime]::MinValue } else { $createdAt }
                }; Descending = $true },
                @{ Expression = { $_.number }; Descending = $false }
    )

    $unclassifiedItems = @(
        $Items |
            Where-Object {
                -not (Test-AnyWildcardMatch -Values $_.labels -Patterns $communityLabelPatterns)
            } |
            Sort-Object `
                @{ Expression = {
                    switch ($_.bucket) {
                        "NeedsRescue" { 0 }
                        "ReviewNow" { 1 }
                        "WaitingOnAuthor" { 2 }
                        "WaitingOnCI" { 3 }
                        "DesignDecision" { 4 }
                        "ReadyToMerge" { 5 }
                        "Draft" { 6 }
                        default { 7 }
                    }
                }; Ascending = $true },
                @{ Expression = { $_.idleDays }; Descending = $true },
                @{ Expression = { $_.number }; Descending = $false }
    )

    $recentCommunity = @(
        $communityItems |
            Where-Object {
                $createdAt = ConvertTo-UtcDateTime -Value $_.createdAt
                $createdAt -and $createdAt -ge $recentWindowStart -and $createdAt -le $snapshotUtc
            }
    )

    $communityPreview = @($communityItems | Select-Object -First 5)
    $unclassifiedPreview = @($unclassifiedItems | Select-Object -First 5)

    return [pscustomobject]@{
        recentCommunityWindowDays = $recentWindowDays
        recentCommunityWindowStart = $recentWindowStart.ToUniversalTime().ToString("o")
        recentCommunityWindowEnd = $snapshotUtc.ToUniversalTime().ToString("o")
        recentCommunity = [pscustomobject]@{
            count = $recentCommunity.Count
            newest = if ($recentCommunity.Count -gt 0) { [int]$recentCommunity[0].number } else { $null }
            preview = @(
                $recentCommunity |
                    Select-Object -First 5 |
                    ForEach-Object { Get-InboxItemSummary -Item $_ -Settings $Settings }
            )
            inventory = @(
                $recentCommunity |
                    ForEach-Object { Get-InboxItemSummary -Item $_ -Settings $Settings }
            )
        }
        community = [pscustomobject]@{
            count = $communityItems.Count
            preview = @(
                $communityPreview |
                    ForEach-Object { Get-InboxItemSummary -Item $_ -Settings $Settings }
            )
            inventory = @(
                $communityItems |
                    ForEach-Object { Get-InboxItemSummary -Item $_ -Settings $Settings }
            )
        }
        unclassified = [pscustomobject]@{
            count = $unclassifiedItems.Count
            preview = @(
                $unclassifiedPreview |
                    ForEach-Object { Get-InboxItemSummary -Item $_ -Settings $Settings }
            )
            inventory = @(
                $unclassifiedItems |
                    ForEach-Object { Get-InboxItemSummary -Item $_ -Settings $Settings }
            )
        }
        evidence = [pscustomobject]@{
            collection = "bounded top-level comments and review threads"
            coverage = "bounded and explicit; no claim of 'no response' when evidence is incomplete"
            recordedResponseCount = @(
                $Items |
                    Where-Object {
                        (Get-ResponseEvidence -DiscussionAssessment $_.discussionAssessment -ItemUrl $_.url).status -eq "recorded-response"
                    }
            ).Count
            unknownResponseCount = @(
                $Items |
                    Where-Object {
                        (Get-ResponseEvidence -DiscussionAssessment $_.discussionAssessment -ItemUrl $_.url).status -eq "unknown"
                    }
            ).Count
            noResponseCount = @(
                $Items |
                    Where-Object {
                        (Get-ResponseEvidence -DiscussionAssessment $_.discussionAssessment -ItemUrl $_.url).status -eq "no-response"
                    }
            ).Count
        }
    }
}

function Get-PersonalData {
    param(
        [object[]]$GeneralItems,
        [object[]]$PersonalCandidates,
        [string]$PersonalLogin,
        [string]$CollectionState = "assessed"
    )

    if ([string]::IsNullOrWhiteSpace($PersonalLogin)) {
        return [pscustomobject]@{
            enabled = $false
            login = $null
            scope = "all-repo"
            preview = @()
            inventory = @()
            coverage = [pscustomobject]@{
                state = "unavailable"
                discovery = "No authenticated GitHub identity was available."
                notifications = "unavailable"
                ownReview = "unavailable"
                reviewThreads = "unavailable"
            }
        }
    }

    $generalByNumber = @{}
    foreach ($item in $GeneralItems) {
        $generalByNumber[[int]$item.number] = $item
    }

    $candidateByNumber = @{}
    foreach ($candidate in $PersonalCandidates) {
        $state = [string](Get-PropertyValue -Object $candidate -Name "state" -DefaultValue "OPEN")
        if ($state -and $state -ne "OPEN") {
            continue
        }

        $candidateByNumber[[int](Get-PropertyValue -Object $candidate -Name "number")] = $candidate
    }

    $allCandidates = [System.Collections.Generic.List[object]]::new()
    foreach ($number in $candidateByNumber.Keys) {
        $candidate = $candidateByNumber[$number]
        if ($generalByNumber.ContainsKey([int]$number)) {
            $generalItem = $generalByNumber[[int]$number]
            foreach ($propertyName in @(
                "personalReviews", "personalReviewRequests", "personalNotifications",
                "personalReviewThreads", "personalComments", "personalMentions", "personalCoverage",
                "personalTeamReviewRequests", "personalDiscoveryKinds"
            )) {
                $property = $candidate.PSObject.Properties[$propertyName]
                if ($null -ne $property) {
                    $generalItem | Add-Member -NotePropertyName $propertyName -NotePropertyValue $property.Value -Force
                }
            }
            $allCandidates.Add($generalItem)
            continue
        }

        $headSha = [string](Get-PropertyValue -Object $candidate -Name "headRefOid" -DefaultValue "")
        $mergeState = [string](Get-PropertyValue -Object $candidate -Name "mergeStateStatus" -DefaultValue "")
        $isDraft = [bool](Get-PropertyValue -Object $candidate -Name "isDraft" -DefaultValue $false)
        $bucket = if ($isDraft) { "Draft" } elseif ($mergeState -eq "CONFLICTING") { "WaitingOnAuthor" } else { "OutOfScope" }
        $nextActor = if ($isDraft -or $mergeState -eq "CONFLICTING") { "author" } else { "unknown" }
        $allCandidates.Add([pscustomobject]@{
            number = [int]$candidate.number
            title = [string](Get-PropertyValue -Object $candidate -Name "title" -DefaultValue "")
            url = [string](Get-PropertyValue -Object $candidate -Name "url" -DefaultValue (Get-PropertyValue -Object $candidate -Name "html_url" -DefaultValue ""))
            author = [string](Get-PropertyValue -Object (Get-PropertyValue -Object $candidate -Name "author") -Name "login" -DefaultValue "")
            state = [string](Get-PropertyValue -Object $candidate -Name "state" -DefaultValue "OPEN")
            bucket = $bucket
            nextActor = $nextActor
            blockers = if ($mergeState -eq "CONFLICTING") { @("The pull request conflicts with its base branch.") } else { @() }
            headRefOid = $headSha
            headSha = $headSha
            personalReviews = @($candidate.personalReviews)
            personalReviewRequests = @($candidate.personalReviewRequests)
            personalNotifications = @($candidate.personalNotifications)
            personalReviewThreads = @($candidate.personalReviewThreads)
            personalComments = @($candidate.personalComments)
            personalMentions = @($candidate.personalMentions)
            personalTeamReviewRequests = @($candidate.personalTeamReviewRequests)
            personalDiscoveryKinds = @($candidate.personalDiscoveryKinds)
            personalCoverage = $candidate.personalCoverage
            digestExclusionReasons = @()
            shownInDigest = $false
            digestRank = $null
            scopeMatch = "personal-only"
        })
    }

    $cards = @(
        $allCandidates |
            ForEach-Object { Get-PersonalInboxItem -Item $_ -PersonalLogin $PersonalLogin } |
            Where-Object { $_.signals.Count -gt 0 -or $_.participatedOrMentioned } |
            Sort-Object `
                @{ Expression = {
                    if ($_.signals.Count -eq 0) { 4 } else {
                        switch ($_.signals[0].kind) {
                            "direct-request" { 0 }
                            "follow-up-notification" { 1 }
                            "changed-since-own-review" { 2 }
                            "review-thread-reply" { 3 }
                            default { 4 }
                        }
                    }
                }; Ascending = $true },
                @{ Expression = {
                    $eventAt = if ($_.signals.Count -gt 0) { $_.signals[0].eventAt } else { $null }
                    ConvertTo-UtcDateTime -Value $eventAt
                }; Descending = $true },
                @{ Expression = { $_.number }; Ascending = $true }
    )

    $activeCards = @($cards | Where-Object { $_.signals.Count -gt 0 })
    return [pscustomobject]@{
        enabled = $true
        login = $PersonalLogin
        scope = "all-repo"
        activeCount = $activeCards.Count
        preview = @($activeCards | Select-Object -First 5)
        inventory = @($cards)
        coverage = [pscustomobject]@{
            state = if ($CollectionState -eq "unavailable") {
                "unavailable"
            }
            elseif ($CollectionState -eq "partial" -or @(
                $cards |
                    Where-Object {
                        $_.coverage.discovery.state -eq "unavailable" -or
                        $_.coverage.notifications.state -eq "unavailable" -or
                        $_.coverage.ownReview.state -eq "unavailable" -or
                        $_.coverage.reviewThreads.state -eq "unavailable"
                    }
            ).Count -gt 0) { "partial" } else { "assessed" }
            discovery = if ($CollectionState -eq "unavailable") {
                "Personal GitHub search or hydration was unavailable; no empty result is claimed."
            }
            elseif ($CollectionState -eq "partial") {
                "Four repository search qualifiers were unioned; GitHub reported incomplete search coverage."
            }
            else {
                "Four repository search qualifiers were unioned and deduplicated."
            }
            notifications = if (@($cards | Where-Object { $_.coverage.notifications.state -eq "unavailable" }).Count -gt 0) { "partial" } else { "assessed" }
            ownReview = "bounded"
            reviewThreads = "partial; only hydrated thread evidence is asserted"
        }
    }
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
        $discussionAssessment = Get-PropertyValue -Object $item -Name "discussionAssessment"
        if ($discussionAssessment -and $discussionAssessment.state -eq "verification-needed") {
            $whyParts.Add("Discussion: $($discussionAssessment.signals -join ', ')")
        }
        $why = Escape-MarkdownCell -Value ($whyParts -join ". ")
        $lines.Add("| [#$($item.number)]($($item.url)) | $title | ``$($item.author)`` | $($item.idleDays)d | $($item.nextActor) | $why |")
    }

    return $lines -join [Environment]::NewLine
}

function Render-Markdown {
    param([object]$Result)

    $reviewNow = @(
        $Result.items |
            Where-Object { $_.shownInDigest -and $_.bucket -eq "ReviewNow" } |
            Sort-Object digestRank
    )
    $needsRescue = @(
        $Result.items |
            Where-Object { $_.shownInDigest -and $_.bucket -eq "NeedsRescue" } |
            Sort-Object digestRank
    )
    $readyToMerge = @(
        $Result.items |
            Where-Object { $_.shownInDigest -and $_.bucket -eq "ReadyToMerge" } |
            Sort-Object digestRank
    )
    $discussionVerification = @(
        $Result.items |
            Where-Object { $_.shownInDiscussionVerification } |
            Sort-Object discussionVerificationRank
    )
    $inbox = Get-PropertyValue -Object $Result -Name "inbox" -DefaultValue ([pscustomobject]@{})
    $recentCommunity = @(Get-PropertyValue -Object $inbox -Name "recentCommunity" -DefaultValue ([pscustomobject]@{ inventory = @() }).inventory)
    $communityInventory = @(Get-PropertyValue -Object $inbox -Name "community" -DefaultValue ([pscustomobject]@{ inventory = @() }).inventory)
    $unclassifiedInventory = @(Get-PropertyValue -Object $inbox -Name "unclassified" -DefaultValue ([pscustomobject]@{ inventory = @() }).inventory)

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("<!-- PR_ATTENTION_QUEUE_BEGIN -->")
    $lines.Add("# PR attention queue")
    $lines.Add("")
    $lines.Add("**Scope:** $($Result.filter.name) - $($Result.filter.description)")
    $lines.Add("")
    $lines.Add("**Selection:** $($Result.filter.selection)")
    if ($Result.filter.excludeDigestAuthors.Count -gt 0) {
        $lines.Add("")
        $lines.Add("**Digest author exclusions:** $($Result.filter.excludeDigestAuthors -join ', ')")
    }
    $lines.Add("")
    $lines.Add("**Snapshot:** $($Result.generatedAt) · **Repository:** $($Result.repository)")
    $lines.Add("")
    $lines.Add("## Review now ($($reviewNow.Count))")
    $lines.Add("")
    $lines.Add((Render-MarkdownTable -Items $reviewNow -EmptyText "No pull requests currently have a human reviewer as the next actor."))
    $lines.Add("")
    $lines.Add("## Verify discussion before review ($($discussionVerification.Count))")
    $lines.Add("")
    $lines.Add((Render-MarkdownTable `
        -Items $discussionVerification `
        -EmptyText "No selected review candidates require discussion verification."))
    $lines.Add("")
    $lines.Add("## Needs rescue ($($needsRescue.Count))")
    $lines.Add("")
    $lines.Add((Render-MarkdownTable -Items $needsRescue -EmptyText "No pull requests currently require rescue or triage."))
    $lines.Add("")
    $lines.Add("## Ready to merge ($($readyToMerge.Count))")
    $lines.Add("")
    $lines.Add((Render-MarkdownTable -Items $readyToMerge -EmptyText "No pull requests are currently ready to merge."))
    $lines.Add("")
    $lines.Add("## Recent community contributions (7-day window)")
    $lines.Add("")
    $inboxRecentCommunity = Get-PropertyValue -Object $inbox -Name "recentCommunity" -DefaultValue ([pscustomobject]@{ count = 0; preview = @(); inventory = @(); windowStart = $null; windowEnd = $null })
    $inboxRecentWindowStart = Get-PropertyValue -Object $inbox -Name "recentCommunityWindowStart" -DefaultValue $null
    $inboxRecentWindowEnd = Get-PropertyValue -Object $inbox -Name "recentCommunityWindowEnd" -DefaultValue $null
    $lines.Add("**Window:** $($inboxRecentWindowStart) to $($inboxRecentWindowEnd) · **Count:** $($inboxRecentCommunity.count)")
    $lines.Add("")
    $lines.Add((Render-MarkdownTable -Items @($inboxRecentCommunity.preview | ForEach-Object { [pscustomobject]@{
            number = $_.number
            title = $_.title
            url = $_.url
            author = $_.provenance
            idleDays = 0
            nextActor = $_.nextActor
            reasonCodes = $_.reasonCodes
            blockers = @()
            discussionAssessment = $null
        } }) -EmptyText "No community contributions opened in the last seven days."))
    $lines.Add("")
    $lines.Add("## Community attention ($($communityInventory.Count))")
    $lines.Add("")
    $lines.Add((Render-MarkdownTable -Items @($communityInventory | ForEach-Object { [pscustomobject]@{
            number = $_.number
            title = $_.title
            url = $_.url
            author = $_.provenance
            idleDays = 0
            nextActor = $_.nextActor
            reasonCodes = $_.reasonCodes
            blockers = @()
            discussionAssessment = $null
        } }) -EmptyText "No community contributions are currently in the scoped inventory."))
    $lines.Add("")
    $lines.Add("## Unclassified contributions ($($unclassifiedInventory.Count))")
    $lines.Add("")
    $lines.Add((Render-MarkdownTable -Items @($unclassifiedInventory | ForEach-Object { [pscustomobject]@{
            number = $_.number
            title = $_.title
            url = $_.url
            author = $_.provenance
            idleDays = 0
            nextActor = $_.nextActor
            reasonCodes = $_.reasonCodes
            blockers = @()
            discussionAssessment = $null
        } }) -EmptyText "No in-scope pull requests are currently unclassified."))
    $lines.Add("")
    $lines.Add("## Inbox evidence coverage")
    $lines.Add("")
    $inboxEvidence = Get-PropertyValue -Object $inbox -Name "evidence" -DefaultValue ([pscustomobject]@{ collection = "not-collected"; coverage = "not-collected"; recordedResponseCount = 0; unknownResponseCount = 0; noResponseCount = 0 })
    $lines.Add("**Collection:** $($inboxEvidence.collection)")
    $lines.Add("**Coverage:** $($inboxEvidence.coverage)")
    $lines.Add("**Recorded responses:** $($inboxEvidence.recordedResponseCount) · **Unknown:** $($inboxEvidence.unknownResponseCount) · **No response:** $($inboxEvidence.noResponseCount)")
    $lines.Add("")
    $personal = Get-PropertyValue -Object $Result -Name "personal" -DefaultValue ([pscustomobject]@{
        enabled = $false
        login = $null
        activeCount = 0
        preview = @()
        coverage = [pscustomobject]@{ state = "unavailable" }
    })
    $lines.Add("## My PR inbox (All dotnet/aspnetcore)")
    $lines.Add("")
    if (-not $personal.enabled) {
        $lines.Add("Personal GitHub follow-up evidence is unavailable for this run.")
    }
    else {
        $lines.Add("**Identity:** ``$($personal.login)`` · **Active signals:** $($personal.activeCount) · **Coverage:** $($personal.coverage.state)")
        $lines.Add("")
        $personalRows = @(
            $personal.preview | ForEach-Object {
                [pscustomobject]@{
                    number = $_.number
                    title = $_.title
                    url = $_.url
                    author = $_.author
                    idleDays = 0
                    nextActor = if ($_.signals.Count -gt 0) { ($_.signals.kind -join ", ") } else { "participated/mentioned" }
                    reasonCodes = @($_.signals.detail)
                    blockers = $_.blockers
                    discussionAssessment = $null
                }
            }
        )
        $lines.Add((Render-MarkdownTable -Items $personalRows -EmptyText "No active personal follow-ups were found."))
        $lines.Add("")
        $lines.Add("The full personal inventory is retained in JSON. Direct requests, unread notifications, changed heads, and evidenced review-thread replies are separate signals.")
    }
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

function Add-DigestExclusions {
    param(
        [object[]]$Items,
        [string[]]$ExcludedAuthors
    )

    $itemsByHeadBranch = @{}
    foreach ($item in $Items) {
        if ($item.isCrossRepository -or [string]::IsNullOrWhiteSpace($item.headBranch)) {
            continue
        }

        if (-not $itemsByHeadBranch.ContainsKey($item.headBranch)) {
            $itemsByHeadBranch[$item.headBranch] = [System.Collections.Generic.List[object]]::new()
        }

        $itemsByHeadBranch[$item.headBranch].Add($item)
    }

    $unhealthyBuckets = @(
        "NeedsRescue",
        "WaitingOnAuthor",
        "WaitingOnCI",
        "DesignDecision",
        "Draft",
        "Excluded"
    )

    foreach ($item in $Items) {
        if (Test-AnyExactMatch -Values @($item.author) -ExpectedValues $ExcludedAuthors) {
            $item.digestExclusionReasons = @($item.digestExclusionReasons + "excluded-author")
        }

        if ($item.bucket -ne "ReviewNow" -or [string]::IsNullOrWhiteSpace($item.baseBranch)) {
            continue
        }

        $visitedBranches = [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::OrdinalIgnoreCase)
        $baseBranch = $item.baseBranch
        $blockedAncestors = [System.Collections.Generic.List[int]]::new()
        $stackDepth = 0

        while ($stackDepth -lt 20 -and $visitedBranches.Add($baseBranch)) {
            if (-not $itemsByHeadBranch.ContainsKey($baseBranch)) {
                break
            }

            $ancestors = @($itemsByHeadBranch[$baseBranch])
            if ($ancestors.Count -ne 1) {
                break
            }

            $ancestor = $ancestors[0]
            $stackDepth++
            if ($ancestor.bucket -in $unhealthyBuckets) {
                $blockedAncestors.Add([int]$ancestor.number)
            }

            if ([string]::IsNullOrWhiteSpace($ancestor.baseBranch)) {
                break
            }

            $baseBranch = $ancestor.baseBranch
        }

        $item.stackDepth = $stackDepth
        $item.stackBlockedBy = @($blockedAncestors)
        if ($blockedAncestors.Count -gt 0) {
            $item.digestExclusionReasons = @($item.digestExclusionReasons + "stacked-on-unhealthy-pr")
        }
    }
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
        [string[]]$ExcludeDigestAuthor = @(),
        [string]$PersonalLogin,
        [switch]$DisablePersonalInbox,
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
            "headRefName",
            "baseRefName",
            "isCrossRepository",
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

    $resolvedPersonalLogin = $PersonalLogin
    $personalCandidates = @()
    $personalNotifications = @()
    $personalCollectionState = "unavailable"
    $personalSearchCoverage = "unavailable"
    if (-not $DisablePersonalInbox.IsPresent) {
        if ($InputPath) {
            $personalCandidates = @(
                $pullRequests | Where-Object {
                    $null -ne $_.PSObject.Properties["personalReviews"] -or
                    $null -ne $_.PSObject.Properties["personalReviewRequests"] -or
                    $null -ne $_.PSObject.Properties["personalNotifications"] -or
                    $null -ne $_.PSObject.Properties["personalReviewThreads"] -or
                    $null -ne $_.PSObject.Properties["personalComments"] -or
                    $null -ne $_.PSObject.Properties["personalMentions"]
                }
            )
            if ([string]::IsNullOrWhiteSpace($resolvedPersonalLogin)) {
                $resolvedPersonalLogin = [string](Get-PropertyValue `
                    -Object ($personalCandidates | Select-Object -First 1) `
                    -Name "personalLogin" `
                    -DefaultValue "")
            }
            $personalCollectionState = if ($resolvedPersonalLogin) { "assessed" } else { "unavailable" }
            $personalSearchCoverage = $personalCollectionState
        }
        else {
            try {
                $resolvedPersonalLogin = if ($resolvedPersonalLogin) {
                    $resolvedPersonalLogin
                }
                else {
                    Invoke-GhText -Arguments @("api", "user", "--jq", ".login")
                }
                $searchResult = Get-PersonalSearchCandidates -RepositoryName $Repository -PersonalLogin $resolvedPersonalLogin
                $personalCandidates = @($searchResult.items)
                $personalSearchCoverage = [string]$searchResult.coverage
                $notificationResult = Get-RepositoryNotifications -RepositoryName $Repository
                $personalNotifications = @($notificationResult.items)
                foreach ($notification in $personalNotifications) {
                    $subject = Get-PropertyValue -Object $notification -Name "subject"
                    $subjectUrl = [string](Get-PropertyValue -Object $subject -Name "url" -DefaultValue "")
                    $reason = [string](Get-PropertyValue -Object $notification -Name "reason" -DefaultValue "")
                    if ($reason -in @("mention", "review_requested") -and
                        $subjectUrl -match "/(?:issues|pulls)/(\d+)$") {
                        $number = [int]$Matches[1]
                        $isOpenPullRequest = @(
                            $pullRequests |
                                Where-Object { [int](Get-PropertyValue -Object $_ -Name "number" -DefaultValue 0) -eq $number }
                        ).Count -gt 0
                        if ($isOpenPullRequest -and -not @($personalCandidates | Where-Object { [int]$_.number -eq $number })) {
                            $personalCandidates += [pscustomobject]@{
                                number = $number
                                personalDiscoveryKinds = @("notification-only")
                            }
                        }
                    }
                }
                Add-PersonalEvidenceDetails `
                    -RepositoryName $Repository `
                    -Candidates $personalCandidates `
                    -PersonalLogin $resolvedPersonalLogin `
                    -Notifications $personalNotifications `
                    -NotificationCoverage $notificationResult.coverage
                $personalCollectionState = $personalSearchCoverage
            }
            catch {
                $warnings.Add("Personal inbox collection unavailable: $($_.Exception.Message)")
                $personalCandidates = @()
                $personalNotifications = @()
                $personalCollectionState = "unavailable"
            }
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
    $totalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $queryStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

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

    $queryStopwatch.Stop()
    $classificationStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

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
        $personalCandidate = @(
            $personalCandidates |
                Where-Object { [int](Get-PropertyValue -Object $_ -Name "number" -DefaultValue 0) -eq [int]$pullRequest.number } |
                Select-Object -First 1
        )
        $personalCandidate = if ($personalCandidate.Count -gt 0) { $personalCandidate[0] } else { $null }

        $matchedItems.Add([pscustomobject]@{
            number = [int]$pullRequest.number
            title = [string]$pullRequest.title
            url = [string]$pullRequest.url
            createdAt = ConvertTo-UtcDateTime -Value (Get-PropertyValue -Object $pullRequest -Name "createdAt")
            headSha = [string](Get-PropertyValue -Object $pullRequest -Name "headRefOid" -DefaultValue "")
            headRefOid = [string](Get-PropertyValue -Object $pullRequest -Name "headRefOid" -DefaultValue "")
            headBranch = [string](Get-PropertyValue -Object $pullRequest -Name "headRefName" -DefaultValue "")
            isCrossRepository = [bool](Get-PropertyValue `
                -Object $pullRequest `
                -Name "isCrossRepository" `
                -DefaultValue $false)
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
            mergeStateStatus = [string](Get-PropertyValue -Object $pullRequest -Name "mergeStateStatus" -DefaultValue "CLEAN")
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
            digestRank = $null
            digestExclusionReasons = @()
            stackDepth = 0
            stackBlockedBy = @()
            deterministicReviewRank = $null
            discussionAssessment = $null
            shownInDiscussionVerification = $false
            discussionVerificationRank = $null
            personalReviews = if ($personalCandidate) { @($personalCandidate.personalReviews) } else { @() }
            personalReviewRequests = if ($personalCandidate) { @($personalCandidate.personalReviewRequests) } else { @() }
            personalNotifications = if ($personalCandidate) { @($personalCandidate.personalNotifications) } else { @() }
            personalReviewThreads = if ($personalCandidate) { @($personalCandidate.personalReviewThreads) } else { @() }
            personalComments = if ($personalCandidate) { @($personalCandidate.personalComments) } else { @() }
            personalMentions = if ($personalCandidate) { @($personalCandidate.personalMentions) } else { @() }
            personalCoverage = if ($personalCandidate) { $personalCandidate.personalCoverage } else { $null }
        })
    }

    $classificationStopwatch.Stop()

    if ($unresolvedPathCoverage -gt 0) {
        $warnings.Add("$unresolvedPathCoverage fixture pull request(s) had incomplete changed-file data.")
    }

    if ($unresolvedMergeable -gt 0) {
        $warnings.Add("GitHub had not finished computing mergeability for $unresolvedMergeable pull request(s). " +
            "Conflicting pull requests among them can appear in a review bucket until the next run.")
    }

    Add-DigestExclusions -Items @($matchedItems) -ExcludedAuthors $ExcludeDigestAuthor

    $reviewNow = @(
        $matchedItems |
            Where-Object { $_.bucket -eq "ReviewNow" -and $_.digestExclusionReasons.Count -eq 0 } |
            Sort-Object `
                @{ Expression = { $_.idleDays }; Descending = $true },
                @{ Expression = { if ($_.authorClass -eq "Community") { 1 } else { 0 } }; Descending = $true },
                @{ Expression = { $_.ageDays }; Descending = $true },
                @{ Expression = { $_.changedFiles }; Descending = $false },
                @{ Expression = { $_.number }; Descending = $false }
    )

    for ($index = 0; $index -lt $reviewNow.Count; $index++) {
        $reviewNow[$index].deterministicReviewRank = $index + 1
    }

    $discussionCandidateLimit = [int](Get-PropertyValue `
        -Object $configuration.settings `
        -Name "discussionCandidateLimit" `
        -DefaultValue 20)
    $maxDiscussionVerification = [int](Get-PropertyValue `
        -Object $configuration.settings `
        -Name "maxDiscussionVerification" `
        -DefaultValue 3)
    $discussionStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $discussionItems = @($reviewNow | Select-Object -First $discussionCandidateLimit)
    $candidatesByNumber = @{}
    foreach ($candidate in $matchedCandidates) {
        $candidatesByNumber[[int]$candidate.PullRequest.number] = $candidate
    }

    if (-not $InputPath) {
        $discussionCandidates = @(
            foreach ($item in $discussionItems) {
                $candidatesByNumber[[int]$item.number]
            }
        )
        Add-DiscussionEvidenceDetails -RepositoryName $Repository -Candidates $discussionCandidates
    }

    foreach ($item in $discussionItems) {
        $candidate = $candidatesByNumber[[int]$item.number]
        $assessment = Get-DiscussionAssessment `
            -PullRequest $candidate.PullRequest `
            -AuthorInfo $candidate.AuthorInfo `
            -KnownBotPatterns @($configuration.settings.knownBotPatterns)
        $item.discussionAssessment = [pscustomobject]@{
            state = $assessment.State
            complete = $assessment.Complete
            signals = $assessment.Signals
            commentTotalCount = $assessment.CommentTotalCount
            commentEvidenceTruncated = $assessment.CommentEvidenceTruncated
            comments = @(
                foreach ($comment in $assessment.Comments) {
                    [pscustomobject]@{
                        author = $comment.Author
                        actor = $comment.Actor
                        association = $comment.Association
                        createdAt = $comment.CreatedAt.ToUniversalTime().ToString("o")
                        kind = $comment.Kind
                        excerpt = $comment.Excerpt
                    }
                }
            )
            threads = [pscustomobject]@{
                totalCount = $assessment.Threads.TotalCount
                returnedCount = $assessment.Threads.ReturnedCount
                complete = $assessment.Threads.Complete
                unresolvedCount = $assessment.Threads.UnresolvedCount
                outdatedUnresolvedCount = $assessment.Threads.OutdatedUnresolvedCount
            }
        }
        if ($assessment.State -eq "verification-needed") {
            $item.digestExclusionReasons = @($item.digestExclusionReasons + "discussion-verification-needed")
        }
    }

    foreach ($item in $reviewNow | Select-Object -Skip $discussionCandidateLimit) {
        $item.discussionAssessment = [pscustomobject]@{
            state = "not-assessed"
            complete = $false
            signals = @("discussion-not-assessed")
            commentTotalCount = 0
            commentEvidenceTruncated = $false
            comments = @()
            threads = [pscustomobject]@{
                totalCount = 0
                returnedCount = 0
                complete = $false
                unresolvedCount = 0
                outdatedUnresolvedCount = 0
            }
        }
        $item.digestExclusionReasons = @($item.digestExclusionReasons + "discussion-not-assessed")
    }

    $discussionVerificationItems = @(
        $reviewNow |
            Where-Object { $_.discussionAssessment.state -eq "verification-needed" }
    )
    for ($index = 0; $index -lt [Math]::Min($discussionVerificationItems.Count, $maxDiscussionVerification); $index++) {
        $item = $discussionVerificationItems[$index]
        $item.shownInDiscussionVerification = $true
        $item.discussionVerificationRank = $index + 1
    }

    $unassessedDiscussionItems = @(
        $reviewNow | Where-Object { $_.discussionAssessment.state -eq "not-assessed" }
    )
    if ($unassessedDiscussionItems.Count -gt 0) {
        $warnings.Add("Discussion evidence was assessed for the first $discussionCandidateLimit Review now candidate(s). " +
            "$($unassessedDiscussionItems.Count) lower-ranked candidate(s) cannot enter the unattended digest.")
    }

    $discussionStopwatch.Stop()

    $needsRescue = @(
        $matchedItems |
            Where-Object { $_.bucket -eq "NeedsRescue" -and $_.digestExclusionReasons.Count -eq 0 } |
            Sort-Object `
                @{ Expression = { $_.idleDays }; Descending = $true },
                @{ Expression = { if ($_.authorClass -eq "Community") { 1 } else { 0 } }; Descending = $true },
                @{ Expression = { if ($_.reasonCodes -contains "never-reviewed") { 1 } else { 0 } }; Descending = $true },
                @{ Expression = { $_.ageDays }; Descending = $true },
                @{ Expression = { $_.number }; Descending = $false }
    )

    $readyToMerge = @(
        $matchedItems |
            Where-Object { $_.bucket -eq "ReadyToMerge" -and $_.digestExclusionReasons.Count -eq 0 } |
            Sort-Object `
                @{ Expression = { $_.idleDays }; Descending = $true },
                @{ Expression = { $_.ageDays }; Descending = $true },
                @{ Expression = { $_.number }; Descending = $false }
    )

    $selectedReviewNow = @(
        Select-ReviewNowDigestItems `
        -Items @($reviewNow | Where-Object { $_.digestExclusionReasons.Count -eq 0 }) `
        -MaximumItems $MaxReviewNow `
        -MaximumPerAuthor $MaxReviewNowPerAuthor
    )
    for ($index = 0; $index -lt $selectedReviewNow.Count; $index++) {
        $item = $selectedReviewNow[$index]
        $item.shownInDigest = $true
        $item.digestRank = $index + 1
    }

    $selectedNeedsRescue = @($needsRescue | Select-Object -First $MaxNeedsRescue)
    for ($index = 0; $index -lt $selectedNeedsRescue.Count; $index++) {
        $item = $selectedNeedsRescue[$index]
        $item.shownInDigest = $true
        $item.digestRank = $index + 1
    }

    $selectedReadyToMerge = @($readyToMerge | Select-Object -First $MaxReadyToMerge)
    for ($index = 0; $index -lt $selectedReadyToMerge.Count; $index++) {
        $item = $selectedReadyToMerge[$index]
        $item.shownInDigest = $true
        $item.digestRank = $index + 1
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

    # Timing is kept local to the queue result: query cost covers repository fetch and
    # scope matching; classification, discussion, and inbox costs are reported separately.
    $inboxStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $inbox = Get-InboxData -Items $orderedItems -Settings $configuration.settings -SnapshotTime $Now
    $inboxStopwatch.Stop()
    $personalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $personal = Get-PersonalData `
        -GeneralItems $orderedItems `
        -PersonalCandidates $personalCandidates `
        -PersonalLogin $resolvedPersonalLogin `
        -CollectionState $personalCollectionState
    $personalStopwatch.Stop()
    $totalStopwatch.Stop()

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
            excludeDigestAuthors = @($ExcludeDigestAuthor)
            allRepositoryPullRequests = $scope.AllRepositoryPullRequests
            selection = $selection
        }
        query = [pscustomobject]@{
            openPullRequestCount = $openPullRequestCount
            returnedPullRequestCount = $pullRequests.Count
            complete = $pullRequests.Count -eq $openPullRequestCount
        }
        discussion = [pscustomobject]@{
            candidateLimit = $discussionCandidateLimit
            assessedCandidateCount = $discussionItems.Count
            verificationNeededCount = $discussionVerificationItems.Count
            unassessedReviewNowCount = $unassessedDiscussionItems.Count
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
        inbox = $inbox
        personal = $personal
        timing = [pscustomobject]@{
            collectionMs = [int]($queryStopwatch.ElapsedMilliseconds)
            queryMs = [int]($queryStopwatch.ElapsedMilliseconds)
            classificationMs = [int]($classificationStopwatch.ElapsedMilliseconds)
            discussionMs = [int]($discussionStopwatch.ElapsedMilliseconds)
            inboxMs = [int]($inboxStopwatch.ElapsedMilliseconds)
            personalMs = [int]($personalStopwatch.ElapsedMilliseconds)
            totalMs = [int]($totalStopwatch.ElapsedMilliseconds)
        }
    }

    if ($OutputFormat -eq "Json") {
        $result | ConvertTo-Json -Depth 20
    }
    else {
        Render-Markdown -Result $result
    }
}

Export-ModuleMember -Function Invoke-PRAttentionQueue
