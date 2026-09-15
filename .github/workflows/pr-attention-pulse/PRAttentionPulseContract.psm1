Set-StrictMode -Version Latest

function Format-PulseTimestamp
{
    param([Parameter(Mandatory)][object]$Value)

    if ($Value -is [datetimeoffset])
    {
        return $Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", [Globalization.CultureInfo]::InvariantCulture)
    }

    if ($Value -is [datetime])
    {
        return ([datetimeoffset]$Value).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", [Globalization.CultureInfo]::InvariantCulture)
    }

    $timestamp = [datetimeoffset]::MinValue
    if ($Value -isnot [string] -or
        -not [datetimeoffset]::TryParse(
        [string]$Value,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AssumeUniversal,
        [ref]$timestamp))
    {
        throw "Pulse timestamp is invalid."
    }

    return $timestamp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", [Globalization.CultureInfo]::InvariantCulture)
}

function Add-PulseSection
{
    param(
        [AllowEmptyCollection()][AllowEmptyString()][Collections.Generic.List[string]]$Lines,
        [Parameter(Mandatory)][string]$Name
    )

    if ($Lines.Count -gt 0)
    {
        $Lines.Add("")
    }

    $Lines.Add("## $Name")
    $Lines.Add("")
}

function Format-PulseCodes
{
    param([Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Codes)

    if ($Codes.Count -eq 0)
    {
        return "None."
    }

    return (($Codes | ForEach-Object { "``$_``" }) -join ", ") + "."
}

function Format-PulsePullRequestLink
{
    param([Parameter(Mandatory)][object]$Number)

    if ($Number -isnot [byte] -and
        $Number -isnot [int16] -and
        $Number -isnot [int32] -and
        $Number -isnot [int64])
    {
        throw "Pulse pull request number must be an integer."
    }

    $validatedNumber = [int64]$Number
    if ($validatedNumber -lt 1 -or $validatedNumber -gt [int32]::MaxValue)
    {
        throw "Pulse pull request number is outside the allowed range."
    }

    return "[dotnet/aspnetcore#$validatedNumber](https://github.com/dotnet/aspnetcore/pull/$validatedNumber)"
}

function Add-PulseCandidateView
{
    param(
        [AllowEmptyCollection()][AllowEmptyString()][Collections.Generic.List[string]]$Lines,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Items,
        [Parameter(Mandatory)][int]$SourceTotal,
        [switch]$IncludeDiscussion,
        [switch]$IncludeScope
    )

    $Lines.Add("")
    if ($Items.Count -eq 0)
    {
        if ($SourceTotal -eq 0)
        {
            $Lines.Add("None in this complete inventory.")
        }
        else
        {
            $Lines.Add("No candidates shown in this view.")
        }

        return
    }

    if ($IncludeDiscussion)
    {
        $Lines.Add("| PR / Author | Title | Next actor / Age | Reasons / Blockers | Discussion / Comments | Threads |")
    }
    else
    {
        $Lines.Add("| PR | Title | Author | Next actor | Idle / Open | Reasons / Blockers |")
    }
    $Lines.Add("| --- | --- | --- | --- | --- | --- |")

    foreach ($item in $Items)
    {
        $identity = "$($item.rank): $(Format-PulsePullRequestLink -Number $item.number)"
        $ages = "$($item.idleDays)d idle / $($item.ageDays)d open"
        $reasons = Format-PulseCodes -Codes @($item.reasonCodes)
        $blockers = if (@($item.blockers).Count -eq 0)
        {
            "None."
        }
        else
        {
            @($item.blockers) -join "; "
        }
        $reasonsAndBlockers = "**Reasons:** $reasons **Blockers:** $blockers"
        if ($IncludeScope)
        {
            $reasonsAndBlockers += " **Scope match:** $($item.scopeMatch)."
        }

        if ($IncludeDiscussion)
        {
            $assessment = $item.discussionAssessment
            $signals = Format-PulseCodes -Codes @($assessment.signals)
            $discussion = "**State:** ``$($assessment.state)``; assessment complete: $($assessment.complete.ToString().ToLowerInvariant()). **Signals:** $signals **Comments:** $($assessment.commentTotalCount) total; evidence truncated: $($assessment.commentEvidenceTruncated.ToString().ToLowerInvariant())."
            $threads = $assessment.threads
            $threadCounts = "$($threads.returnedCount) of $($threads.totalCount) returned; complete: $($threads.complete.ToString().ToLowerInvariant()); $($threads.unresolvedCount) unresolved; $($threads.outdatedUnresolvedCount) outdated unresolved"
            $Lines.Add("| $identity; **Author:** $($item.author) | $($item.title) | **Next:** $($item.nextActor); $ages | $reasonsAndBlockers | $discussion | $threadCounts |")
        }
        else
        {
            $Lines.Add("| $identity | $($item.title) | $($item.author) | $($item.nextActor) | $ages | $reasonsAndBlockers |")
        }
    }
}

function ConvertTo-PRAttentionPulseBody
{
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Pulse)

    $lines = [Collections.Generic.List[string]]::new()
    $unavailable = [string]::Equals([string]$Pulse.status, "unavailable", [StringComparison]::Ordinal)

    if ($unavailable)
    {
        $lines.Add("> [!WARNING]")
        $lines.Add("> Attention data unavailable")
        $lines.Add("")
    }
    elseif (-not [string]::Equals([string]$Pulse.status, "complete", [StringComparison]::Ordinal))
    {
        throw "Unsupported Pulse status '$($Pulse.status)'."
    }

    $lines.Add("> [!IMPORTANT]")
    $lines.Add("> These views identify pull requests worth inspecting. They do not certify readiness, prove that feedback was addressed, authorize merge or review, or reliably establish completion.")
    $lines.Add("")
    $lines.Add("> Auto-generated by PR Attention Pulse. Manual edits are replaced on the next manual run.")

    if ($unavailable)
    {
        $lines.Add("> Attempted: ``$(Format-PulseTimestamp -Value $Pulse.attemptedAt)``.")
        $lines.Add("> Source repository: ``$($Pulse.source.repository)``. Error category: ``$($Pulse.errorCategory)``.")
        Add-PulseSection -Lines $lines -Name "Summary counts"
        $lines.Add("Candidate counts unavailable.")
        foreach ($name in @(
            "Review now",
            "Verify discussion before review",
            "Needs rescue",
            "Ready to merge"))
        {
            Add-PulseSection -Lines $lines -Name $name
            $lines.Add("Unavailable because collection did not produce a complete compatible inventory.")
        }
        Add-PulseSection -Lines $lines -Name "Coverage and data quality"
        $lines.Add("No complete source coverage was available.")

        return $lines -join "`n"
    }

    $source = $Pulse.source
    $displayed = @($Pulse.views.reviewNow) + @($Pulse.views.verifyDiscussionBeforeReview) + @($Pulse.views.needsRescue) + @($Pulse.views.readyToMerge)
    $commonScope = $null
    if ($displayed.Count -gt 0)
    {
        $commonScope = $displayed[0].scopeMatch
        foreach ($item in $displayed)
        {
            if (-not [string]::Equals($commonScope, $item.scopeMatch, [StringComparison]::Ordinal))
            {
                $commonScope = $null
                break
            }
        }
    }
    $includeScope = $null -eq $commonScope

    $lines.Add("> Source generated: ``$(Format-PulseTimestamp -Value $source.generatedAt)``; attempted: ``$(Format-PulseTimestamp -Value $Pulse.attemptedAt)``.")
    $lines.Add("> Source: ``$($source.repository)``. $($source.filter.description); coverage ``$($source.filter.coverage)``; selection $($source.filter.selection).")
    if ($null -ne $commonScope)
    {
        $lines.Add("> Scope match for all displayed candidates: $commonScope.")
    }
    if (@($source.warnings).Count -gt 0)
    {
        $lines.Add("")
        $lines.Add("> [!WARNING]")
        foreach ($warning in @($source.warnings))
        {
            $lines.Add("> $warning")
        }
    }

    Add-PulseSection -Lines $lines -Name "Summary counts"
    $lines.Add("Open: $($source.census.openPullRequests). Matched: $($source.census.matched). Returned: $($source.query.returnedPullRequestCount) of $($source.query.openPullRequestCount); query complete: $($source.query.complete.ToString().ToLowerInvariant()).")
    $lines.Add("")
    $lines.Add("| View | Displayed | Source total |")
    $lines.Add("| --- | ---: | --- |")
    $lines.Add("| Review now | $(@($Pulse.views.reviewNow).Count) | $($source.census.byBucket.ReviewNow) in the ReviewNow inventory bucket, not $($source.census.byBucket.ReviewNow) cleared for review |")
    $lines.Add("| Verify discussion before review | $(@($Pulse.views.verifyDiscussionBeforeReview).Count) | $($source.discussion.verificationNeededCount) assessed candidates need verification |")
    $lines.Add("| Needs rescue | $(@($Pulse.views.needsRescue).Count) | $($source.census.byBucket.NeedsRescue) in the NeedsRescue inventory bucket |")
    $lines.Add("| Ready to merge | $(@($Pulse.views.readyToMerge).Count) | $($source.census.byBucket.ReadyToMerge) in the ReadyToMerge inventory bucket |")

    Add-PulseSection -Lines $lines -Name "Review now"
    $lines.Add("Displaying $(@($Pulse.views.reviewNow).Count) candidates from a ReviewNow inventory of $($source.census.byBucket.ReviewNow). Legacy overflow: $($source.overflow.reviewNow).")
    $lines.Add("The inventory includes discussion-verification and unassessed candidates; see coverage below.")
    Add-PulseCandidateView -Lines $lines -Items @($Pulse.views.reviewNow) -SourceTotal $source.census.byBucket.ReviewNow -IncludeScope:$includeScope
    Add-PulseSection -Lines $lines -Name "Verify discussion before review"
    $verificationShown = @($Pulse.views.verifyDiscussionBeforeReview).Count
    $lines.Add("Displaying $verificationShown of $($source.discussion.verificationNeededCount) assessed candidates needing verification; $($source.discussion.verificationNeededCount - $verificationShown) are not displayed.")
    $lines.Add("Assessment budget: $($source.discussion.candidateLimit) candidates, not a claim that $($source.discussion.candidateLimit) verification rows can be shown.")
    Add-PulseCandidateView -Lines $lines -Items @($Pulse.views.verifyDiscussionBeforeReview) -SourceTotal $source.discussion.verificationNeededCount -IncludeDiscussion -IncludeScope:$includeScope
    Add-PulseSection -Lines $lines -Name "Needs rescue"
    $lines.Add("Displaying $(@($Pulse.views.needsRescue).Count) of $($source.census.byBucket.NeedsRescue) inventory candidates. Legacy overflow: $($source.overflow.needsRescue).")
    Add-PulseCandidateView -Lines $lines -Items @($Pulse.views.needsRescue) -SourceTotal $source.census.byBucket.NeedsRescue -IncludeScope:$includeScope
    Add-PulseSection -Lines $lines -Name "Ready to merge"
    $lines.Add("Displaying $(@($Pulse.views.readyToMerge).Count) of $($source.census.byBucket.ReadyToMerge) inventory candidates. Legacy overflow: $($source.overflow.readyToMerge).")
    Add-PulseCandidateView -Lines $lines -Items @($Pulse.views.readyToMerge) -SourceTotal $source.census.byBucket.ReadyToMerge -IncludeScope:$includeScope

    Add-PulseSection -Lines $lines -Name "Coverage and data quality"
    $lines.Add("- Query coverage: $($source.query.returnedPullRequestCount) of $($source.query.openPullRequestCount) open pull requests returned; complete $($source.query.complete.ToString().ToLowerInvariant()).")
    $lines.Add("- Discussion coverage: $($source.discussion.assessedCandidateCount) of limit $($source.discussion.candidateLimit) assessed; $($source.discussion.verificationNeededCount) need verification; $($source.discussion.unassessedReviewNowCount) Review now candidates unassessed.")
    $lines.Add("- Queue census: Review now $($source.census.byBucket.ReviewNow); Needs rescue $($source.census.byBucket.NeedsRescue); Ready to merge $($source.census.byBucket.ReadyToMerge); Waiting on author $($source.census.byBucket.WaitingOnAuthor); Waiting on CI $($source.census.byBucket.WaitingOnCI); Design decision $($source.census.byBucket.DesignDecision); Draft $($source.census.byBucket.Draft); Excluded $($source.census.byBucket.Excluded).")
    $lines.Add("- Scope census: $($source.census.labelOnly) label-only; $($source.census.pathOnly) path-only; $($source.census.labelAndPath) label-and-path; $($source.census.incidentalPathExcluded) incidental paths excluded; $($source.census.unresolvedMergeable) unresolved mergeability.")
    $lines.Add("- Overflow: Review now $($source.overflow.reviewNow); Needs rescue $($source.overflow.needsRescue); Ready to merge $($source.overflow.readyToMerge).")
    $lines.Add("- Caps: Review now $($source.caps.reviewNow); Review now per author $($source.caps.reviewNowPerAuthor); Needs rescue $($source.caps.needsRescue); Ready to merge $($source.caps.readyToMerge).")
    if (@($source.warnings).Count -eq 0)
    {
        $lines.Add("- Warnings: None.")
    }
    else
    {
        foreach ($warning in @($source.warnings))
        {
            $lines.Add("- Warning: $warning")
        }
    }

    return $lines -join "`n"
}

function Assert-PRAttentionPulseOutput
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$AgentOutput,
        [Parameter(Mandatory)][object]$Pulse,
        [Parameter(Mandatory)][string]$ExpectedBody,
        [int]$ExpectedIssueNumber = 58
    )

    $topProperties = @($AgentOutput.PSObject.Properties.Name)
    $expectedTopProperties = @("errors", "items")
    if ($topProperties.Count -ne 2 -or
        -not [string]::Equals(
            (($topProperties | Sort-Object) -join ","),
            ($expectedTopProperties -join ","),
            [StringComparison]::Ordinal))
    {
        throw "Agent output must contain only the items and errors arrays."
    }

    if ($AgentOutput.errors -isnot [array] -or @($AgentOutput.errors).Count -ne 0)
    {
        throw "Agent output ingestion must contain an empty errors array."
    }

    if ($AgentOutput.items -isnot [array] -or @($AgentOutput.items).Count -ne 1)
    {
        throw "Agent output must contain exactly one item."
    }

    $item = @($AgentOutput.items)[0]
    $itemProperties = @($item.PSObject.Properties.Name | Sort-Object)
    $expectedProperties = @("body", "issue_number", "operation", "type")
    if (-not [string]::Equals(
        ($itemProperties -join ","),
        ($expectedProperties -join ","),
        [StringComparison]::Ordinal))
    {
        throw "The update payload must contain only type, issue_number, operation, and body; actual properties: $($itemProperties -join ',')."
    }

    if (-not [string]::Equals([string]$item.type, "update_issue", [StringComparison]::Ordinal))
    {
        throw "The only permitted payload type is update_issue."
    }

    if ($item.issue_number -isnot [byte] -and
        $item.issue_number -isnot [int16] -and
        $item.issue_number -isnot [int32] -and
        $item.issue_number -isnot [int64])
    {
        throw "The issue number must be numeric."
    }

    if ([int64]$item.issue_number -ne $ExpectedIssueNumber)
    {
        throw "The update payload targeted the wrong issue."
    }

    if (-not [string]::Equals([string]$item.operation, "replace", [StringComparison]::Ordinal))
    {
        throw "The update payload must replace the issue body."
    }

    if ($item.body -isnot [string] -or
        -not [string]::Equals($item.body, $ExpectedBody, [StringComparison]::Ordinal))
    {
        throw "The emitted body does not exactly match the deterministic Pulse rendering."
    }

    $body = $item.body
    if ($body.Length -lt 200 -or $body.Length -gt 65000)
    {
        throw "The issue body is outside the allowed size range."
    }

    $expectedNumbers = if ([string]::Equals([string]$Pulse.status, "complete", [StringComparison]::Ordinal))
    {
        @(
            foreach ($viewName in @(
                "reviewNow",
                "verifyDiscussionBeforeReview",
                "needsRescue",
                "readyToMerge"))
            {
                foreach ($candidate in @($Pulse.views.$viewName))
                {
                    [string]$candidate.number
                }
            }
        )
    }
    elseif ([string]::Equals([string]$Pulse.status, "unavailable", [StringComparison]::Ordinal))
    {
        @()
    }
    else
    {
        throw "Unsupported Pulse status '$($Pulse.status)'."
    }

    if (@($expectedNumbers | Select-Object -Unique).Count -ne @($expectedNumbers).Count)
    {
        throw "The sanitized Pulse views contain duplicate displayed pull request numbers."
    }

    $linkPattern = [regex]::new(
        "\[dotnet/aspnetcore#(?<label>[1-9][0-9]*)\]\(https://github\.com/dotnet/aspnetcore/pull/(?<target>[1-9][0-9]*)\)",
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    $actualLinks = @($linkPattern.Matches($body))
    $actualNumbers = @(
        foreach ($match in $actualLinks)
        {
            if (-not [string]::Equals(
                $match.Groups["label"].Value,
                $match.Groups["target"].Value,
                [StringComparison]::Ordinal))
            {
                throw "A pull request link label does not match its target."
            }

            $match.Groups["label"].Value
        }
    )
    if (-not [string]::Equals(
        ($actualNumbers -join ","),
        ($expectedNumbers -join ","),
        [StringComparison]::Ordinal))
    {
        throw "The body did not preserve one exact upstream pull request link per displayed candidate in legacy order. Expected '$($expectedNumbers -join ',')'; actual '$($actualNumbers -join ',')'."
    }

    $withoutAllowedLinks = $linkPattern.Replace($body, "")
    foreach ($pattern in @(
        "(?i)\b(?:https?|ftp)://",
        "(?i)\bwww\.",
        "(?i)(?<![\w.-])(?:[A-Z0-9-]+\.)+[A-Z]{2,}(?::[0-9]+)?(?:[/#?]\S*)?",
        "\]\(",
        "(?i)<\s*a\b",
        "(?<![\w])@[A-Za-z0-9]",
        "(?i)\bGH-[0-9]+\b",
        "(?i)(?<![0-9A-Z])(?=[0-9A-F]{7,40}(?![0-9A-Z]))(?=[0-9A-F]{0,39}[A-F])(?=[0-9A-F]{0,39}[0-9])[0-9A-F]{7,40}(?![0-9A-Z])"))
    {
        if ($withoutAllowedLinks -match $pattern)
        {
            throw "The issue body contains prohibited content matching '$pattern'."
        }
    }

    if ($withoutAllowedLinks -match "(?<!#)#[0-9]+" -or
        $withoutAllowedLinks -match "\b[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+#[0-9]+")
    {
        throw "The body contains an unexpected GitHub reference."
    }
}

Export-ModuleMember -Function ConvertTo-PRAttentionPulseBody, Assert-PRAttentionPulseOutput
