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
        $Lines.Add("| PR | Title | Author | Next actor | Open age | Reasons / Blockers |")
    }
    $Lines.Add("| --- | --- | --- | --- | --- | --- |")

    foreach ($item in $Items)
    {
        $identity = "$($item.rank): $(Format-PulsePullRequestLink -Number $item.number)"
        $openAge = "$($item.ageDays)d open"
        $author = [string]$item.author
        if (@($item.reasonCodes) -ccontains "community-contribution")
        {
            $author += " **Community**"
        }
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
            $Lines.Add("| $identity; **Author:** $author | $($item.title) | **Next:** $($item.nextActor); $openAge | $reasonsAndBlockers | $discussion | $threadCounts |")
        }
        else
        {
            $Lines.Add("| $identity | $($item.title) | $author | $($item.nextActor) | $openAge | $reasonsAndBlockers |")
        }
    }
}

function Get-PulseAreas
{
    param([Parameter(Mandatory)][object]$Pulse)

    if ([string]::Equals([string]$Pulse.schemaVersion, "2.0.0", [StringComparison]::Ordinal))
    {
        if ([string]$Pulse.status -notin @("complete", "partial", "unavailable") -or
            @($Pulse.areas).Count -ne 2)
        {
            throw "The combined Pulse envelope is invalid."
        }

        $areas = @($Pulse.areas)
        $expected = @(
            @{ Id = "blazor"; Label = "Blazor"; Open = $false },
            @{ Id = "repository-wide"; Label = "Repository-wide"; Open = $false }
        )
        for ($index = 0; $index -lt $expected.Count; $index++)
        {
            if (-not [string]::Equals([string]$areas[$index].id, $expected[$index].Id, [StringComparison]::Ordinal) -or
                -not [string]::Equals([string]$areas[$index].label, $expected[$index].Label, [StringComparison]::Ordinal) -or
                $areas[$index].openByDefault -isnot [bool] -or
                $areas[$index].openByDefault -ne $expected[$index].Open)
            {
                throw "The combined Pulse area order or metadata is invalid."
            }
        }

        $completeCount = @($areas | Where-Object { $_.status -ceq "complete" }).Count
        $expectedStatus = if ($completeCount -eq 2)
        {
            "complete"
        }
        elseif ($completeCount -eq 0)
        {
            "unavailable"
        }
        else
        {
            "partial"
        }
        if (-not [string]::Equals([string]$Pulse.status, $expectedStatus, [StringComparison]::Ordinal))
        {
            throw "The combined Pulse status does not match its area results."
        }

        return $areas
    }

    if (-not [string]::Equals([string]$Pulse.schemaVersion, "1.0.0", [StringComparison]::Ordinal) -or
        [string]$Pulse.scope -notin @("blazor", "repository-wide"))
    {
        throw "The Pulse area envelope is invalid."
    }

    $area = [ordered]@{
        id = [string]$Pulse.scope
        label = if ($Pulse.scope -ceq "blazor") { "Blazor" } else { "Repository-wide" }
        openByDefault = $false
        status = [string]$Pulse.status
        attemptedAt = $Pulse.attemptedAt
        candidateCountsAvailable = $Pulse.candidateCountsAvailable
        source = $Pulse.source
    }
    if ([string]::Equals([string]$Pulse.status, "complete", [StringComparison]::Ordinal))
    {
        $area["views"] = $Pulse.views
    }
    else
    {
        $area["errorCategory"] = $Pulse.errorCategory
    }

    return @([pscustomobject]$area)
}

function Get-PulseAreaSummary
{
    param([Parameter(Mandatory)][object]$Area)

    if ([string]::Equals([string]$Area.status, "complete", [StringComparison]::Ordinal))
    {
        return "<summary><strong>$($Area.label)</strong> - $($Area.source.census.matched) matched; shown: $(@($Area.views.reviewNow).Count) review now, $(@($Area.views.verifyDiscussionBeforeReview).Count) verify discussion, $(@($Area.views.needsRescue).Count) rescue, $(@($Area.views.readyToMerge).Count) ready; generated $(Format-PulseTimestamp -Value $Area.source.generatedAt)</summary>"
    }

    if ([string]::Equals([string]$Area.status, "unavailable", [StringComparison]::Ordinal))
    {
        return "<summary><strong>$($Area.label)</strong> - data unavailable; attempted $(Format-PulseTimestamp -Value $Area.attemptedAt)</summary>"
    }

    throw "Unsupported Pulse area status '$($Area.status)'."
}

function Assert-PulseAreaScope
{
    param([Parameter(Mandatory)][object]$Area)

    if ([string]::Equals([string]$Area.status, "unavailable", [StringComparison]::Ordinal))
    {
        return
    }

    if (-not [string]::Equals([string]$Area.status, "complete", [StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$Area.source.repository, "dotnet/aspnetcore", [StringComparison]::Ordinal))
    {
        throw "The Pulse area source is invalid."
    }

    $filter = $Area.source.filter
    if ([string]::Equals([string]$Area.id, "blazor", [StringComparison]::Ordinal))
    {
        if (-not [string]::Equals([string]$filter.name, "blazor", [StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$filter.description, "Blazor and Components pull requests", [StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$filter.coverage, "labels-and-paths", [StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$filter.selection, "(labels any of [area-blazor, feature-blazor-*, Blazor ♥ *] OR paths any of [src/Components/**])", [StringComparison]::Ordinal) -or
            $filter.allRepositoryPullRequests -isnot [bool] -or
            $filter.allRepositoryPullRequests)
        {
            throw "The Blazor Pulse area does not use the maintained preset."
        }
    }
    elseif ([string]::Equals([string]$Area.id, "repository-wide", [StringComparison]::Ordinal))
    {
        if (-not [string]::Equals([string]$filter.name, "adhoc", [StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$filter.description, "Ad hoc pull-request scope", [StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$filter.coverage, "all-repo", [StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$filter.selection, "(all open pull requests)", [StringComparison]::Ordinal) -or
            $filter.allRepositoryPullRequests -isnot [bool] -or
            -not $filter.allRepositoryPullRequests)
        {
            throw "The repository-wide Pulse area does not use the all-repository baseline."
        }
    }
    else
    {
        throw "Unsupported Pulse area '$($Area.id)'."
    }
}

function Add-PulseArea
{
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][AllowEmptyString()][Collections.Generic.List[string]]$Lines,
        [Parameter(Mandatory)][object]$Area
    )

    Assert-PulseAreaScope -Area $Area
    $Lines.Add("")
    $Lines.Add($(if ($Area.openByDefault) { "<details open>" } else { "<details>" }))
    $Lines.Add((Get-PulseAreaSummary -Area $Area))

    if ([string]::Equals([string]$Area.status, "unavailable", [StringComparison]::Ordinal))
    {
        $Lines.Add("")
        $Lines.Add("> [!WARNING]")
        $Lines.Add("> $($Area.label) attention data unavailable")
        $Lines.Add(">")
        $Lines.Add("> Attempted: ``$(Format-PulseTimestamp -Value $Area.attemptedAt)``. Source repository: ``$($Area.source.repository)``. Error category: ``$($Area.errorCategory)``.")
        Add-PulseSection -Lines $Lines -Name "Summary counts"
        $Lines.Add("Candidate counts unavailable.")
        foreach ($name in @("Review now", "Verify discussion before review", "Needs rescue", "Ready to merge"))
        {
            Add-PulseSection -Lines $Lines -Name $name
            $Lines.Add("Unavailable because this area's collection did not produce a complete compatible inventory.")
        }
        Add-PulseSection -Lines $Lines -Name "Coverage and data quality"
        $Lines.Add("No complete source coverage was available for this area.")
        $Lines.Add("")
        $Lines.Add("</details>")
        return
    }

    $source = $Area.source
    $displayed = @($Area.views.reviewNow) + @($Area.views.verifyDiscussionBeforeReview) + @($Area.views.needsRescue) + @($Area.views.readyToMerge)
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

    $Lines.Add("")
    $Lines.Add("> Source generated: ``$(Format-PulseTimestamp -Value $source.generatedAt)``; attempted: ``$(Format-PulseTimestamp -Value $Area.attemptedAt)``.")
    $Lines.Add("> Source: ``$($source.repository)``. $($source.filter.description); coverage ``$($source.filter.coverage)``; selection $($source.filter.selection).")
    if ($null -ne $commonScope)
    {
        $Lines.Add("> Scope match for all displayed candidates: $commonScope.")
    }
    if (@($source.warnings).Count -gt 0)
    {
        $Lines.Add("")
        $Lines.Add("> [!WARNING]")
        foreach ($warning in @($source.warnings))
        {
            $Lines.Add("> $warning")
        }
    }

    Add-PulseSection -Lines $Lines -Name "Summary counts"
    $Lines.Add("Open: $($source.census.openPullRequests). Matched: $($source.census.matched). Returned: $($source.query.returnedPullRequestCount) of $($source.query.openPullRequestCount); query complete: $($source.query.complete.ToString().ToLowerInvariant()).")
    $Lines.Add("")
    $Lines.Add("| View | Displayed | Source total |")
    $Lines.Add("| --- | ---: | --- |")
    $Lines.Add("| Review now | $(@($Area.views.reviewNow).Count) | $($source.census.byBucket.ReviewNow) in the ReviewNow inventory bucket, not $($source.census.byBucket.ReviewNow) cleared for review |")
    $Lines.Add("| Verify discussion before review | $(@($Area.views.verifyDiscussionBeforeReview).Count) | $($source.discussion.verificationNeededCount) assessed candidates need verification |")
    $Lines.Add("| Needs rescue | $(@($Area.views.needsRescue).Count) | $($source.census.byBucket.NeedsRescue) in the NeedsRescue inventory bucket |")
    $Lines.Add("| Ready to merge | $(@($Area.views.readyToMerge).Count) | $($source.census.byBucket.ReadyToMerge) in the ReadyToMerge inventory bucket |")

    Add-PulseSection -Lines $Lines -Name "Review now"
    $Lines.Add("Displaying $(@($Area.views.reviewNow).Count) candidates from a ReviewNow inventory of $($source.census.byBucket.ReviewNow). Legacy overflow: $($source.overflow.reviewNow).")
    $Lines.Add("The inventory includes discussion-verification and unassessed candidates; see coverage below.")
    Add-PulseCandidateView -Lines $Lines -Items @($Area.views.reviewNow) -SourceTotal $source.census.byBucket.ReviewNow -IncludeScope:$includeScope
    Add-PulseSection -Lines $Lines -Name "Verify discussion before review"
    $verificationShown = @($Area.views.verifyDiscussionBeforeReview).Count
    $Lines.Add("Displaying $verificationShown of $($source.discussion.verificationNeededCount) assessed candidates needing verification; $($source.discussion.verificationNeededCount - $verificationShown) are not displayed.")
    $Lines.Add("Assessment budget: $($source.discussion.candidateLimit) candidates, not a claim that $($source.discussion.candidateLimit) verification rows can be shown.")
    Add-PulseCandidateView -Lines $Lines -Items @($Area.views.verifyDiscussionBeforeReview) -SourceTotal $source.discussion.verificationNeededCount -IncludeDiscussion -IncludeScope:$includeScope
    Add-PulseSection -Lines $Lines -Name "Needs rescue"
    $Lines.Add("Displaying $(@($Area.views.needsRescue).Count) of $($source.census.byBucket.NeedsRescue) inventory candidates. Legacy overflow: $($source.overflow.needsRescue).")
    Add-PulseCandidateView -Lines $Lines -Items @($Area.views.needsRescue) -SourceTotal $source.census.byBucket.NeedsRescue -IncludeScope:$includeScope
    Add-PulseSection -Lines $Lines -Name "Ready to merge"
    $Lines.Add("Displaying $(@($Area.views.readyToMerge).Count) of $($source.census.byBucket.ReadyToMerge) inventory candidates. Legacy overflow: $($source.overflow.readyToMerge).")
    Add-PulseCandidateView -Lines $Lines -Items @($Area.views.readyToMerge) -SourceTotal $source.census.byBucket.ReadyToMerge -IncludeScope:$includeScope

    Add-PulseSection -Lines $Lines -Name "Coverage and data quality"
    $Lines.Add("- Query coverage: $($source.query.returnedPullRequestCount) of $($source.query.openPullRequestCount) open pull requests returned; complete $($source.query.complete.ToString().ToLowerInvariant()).")
    $Lines.Add("- Discussion coverage: $($source.discussion.assessedCandidateCount) of limit $($source.discussion.candidateLimit) assessed; $($source.discussion.verificationNeededCount) need verification; $($source.discussion.unassessedReviewNowCount) Review now candidates unassessed.")
    $Lines.Add("- Queue census: Review now $($source.census.byBucket.ReviewNow); Needs rescue $($source.census.byBucket.NeedsRescue); Ready to merge $($source.census.byBucket.ReadyToMerge); Waiting on author $($source.census.byBucket.WaitingOnAuthor); Waiting on CI $($source.census.byBucket.WaitingOnCI); Design decision $($source.census.byBucket.DesignDecision); Draft $($source.census.byBucket.Draft); Excluded $($source.census.byBucket.Excluded).")
    $Lines.Add("- Scope census: $($source.census.labelOnly) label-only; $($source.census.pathOnly) path-only; $($source.census.labelAndPath) label-and-path; $($source.census.incidentalPathExcluded) incidental paths excluded; $($source.census.unresolvedMergeable) unresolved mergeability.")
    $Lines.Add("- Overflow: Review now $($source.overflow.reviewNow); Needs rescue $($source.overflow.needsRescue); Ready to merge $($source.overflow.readyToMerge).")
    $Lines.Add("- Caps: Review now $($source.caps.reviewNow); Review now per author $($source.caps.reviewNowPerAuthor); Needs rescue $($source.caps.needsRescue); Ready to merge $($source.caps.readyToMerge).")
    if (@($source.warnings).Count -eq 0)
    {
        $Lines.Add("- Warnings: None.")
    }
    else
    {
        foreach ($warning in @($source.warnings))
        {
            $Lines.Add("- Warning: $warning")
        }
    }
    $Lines.Add("")
    $Lines.Add("</details>")
}

function ConvertTo-PRAttentionPulseBody
{
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Pulse)

    $areas = @(Get-PulseAreas -Pulse $Pulse)
    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add("> [!IMPORTANT]")
    $lines.Add("> These views identify pull requests worth inspecting. They do not certify readiness, prove that feedback was addressed, authorize merge or review, or reliably establish completion.")
    $lines.Add("")
    $lines.Add("> Auto-generated by PR Attention Pulse. Manual edits are replaced on the next manual run.")
    $lines.Add("")
    if ($areas.Count -eq 2)
    {
        $lines.Add("This initial area composition includes the maintained **Blazor** view and a **Repository-wide** baseline.")
        $lines.Add("Additional product areas will be added only after maintainers define their exact label/path queries and decide whether this report shape is useful.")
    }
    else
    {
        $lines.Add("This report contains the **$($areas[0].label)** Pulse area.")
    }

    foreach ($area in $areas)
    {
        Add-PulseArea -Lines $lines -Area $area
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
        [Parameter(Mandatory)]
        [ValidateRange(1, [int]::MaxValue)]
        [int]$ExpectedIssueNumber
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

    $linkPattern = [regex]::new(
        "\[dotnet/aspnetcore#(?<label>[1-9][0-9]*)\]\(https://github\.com/dotnet/aspnetcore/pull/(?<target>[1-9][0-9]*)\)",
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    $areas = @(Get-PulseAreas -Pulse $Pulse)
    $areaCursor = 0
    foreach ($area in $areas)
    {
        $opening = if ($area.openByDefault) { "<details open>" } else { "<details>" }
        $areaMarker = "$opening`n$(Get-PulseAreaSummary -Area $area)"
        $areaStart = $body.IndexOf($areaMarker, $areaCursor, [StringComparison]::Ordinal)
        if ($areaStart -lt $areaCursor)
        {
            throw "The '$($area.id)' details block is missing or out of order."
        }
        $areaEnd = $body.IndexOf("</details>", $areaStart, [StringComparison]::Ordinal)
        if ($areaEnd -lt 0)
        {
            throw "The '$($area.id)' details block is not closed."
        }
        $areaBody = $body.Substring($areaStart, ($areaEnd + "</details>".Length) - $areaStart)
        $areaCursor = $areaEnd + "</details>".Length

        $expectedNumbers = if ([string]::Equals([string]$area.status, "complete", [StringComparison]::Ordinal))
        {
            @(
                foreach ($viewName in @("reviewNow", "verifyDiscussionBeforeReview", "needsRescue", "readyToMerge"))
                {
                    foreach ($candidate in @($area.views.$viewName))
                    {
                        [string]$candidate.number
                    }
                }
            )
        }
        else
        {
            @()
        }

        if (@($expectedNumbers | Select-Object -Unique).Count -ne @($expectedNumbers).Count)
        {
            throw "The '$($area.id)' Pulse views contain duplicate displayed pull request numbers."
        }

        $actualLinks = @($linkPattern.Matches($areaBody))
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
            throw "The '$($area.id)' body did not preserve one exact upstream pull request link per displayed candidate in area order. Expected '$($expectedNumbers -join ',')'; actual '$($actualNumbers -join ',')'."
        }
    }

    if ([regex]::Matches($body, "(?m)^<details(?: open)?>$").Count -ne $areas.Count -or
        [regex]::Matches($body, "(?m)^</details>$").Count -ne $areas.Count -or
        [regex]::Matches($body, "(?m)^<summary>").Count -ne $areas.Count)
    {
        throw "The body contains an unexpected details or summary block."
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
