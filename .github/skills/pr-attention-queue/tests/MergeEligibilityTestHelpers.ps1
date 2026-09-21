function Invoke-MergeFixtureQueue {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$PullRequests,
        [string]$OutputFormat = "Json",
        [string]$Scope = "repository-wide",
        [int]$Maximum = 3,
        [string[]]$ExcludedAuthors = @(),
        [string]$ModulePath = (Join-Path $PSScriptRoot "..\scripts\PRAttentionQueue.psm1")
    )

    Import-Module $ModulePath -Force
    $module = Get-Module PRAttentionQueue
    return & $module {
        param($fixtures, $format, $scope, $maximum, $excludedAuthors)
        $requests = [Collections.Generic.List[string]]::new()
        $discussionNumbers = [Collections.Generic.List[int]]::new()
        function Invoke-GhJson {
            param([string[]]$Arguments)
            $query = [string]($Arguments | Where-Object { $_ -like "query=*" } | Select-Object -First 1)
            $requests.Add(($Arguments -join " "))
            if ($Arguments[0] -eq "pr") {
                return @($fixtures | ForEach-Object {
                    [pscustomobject]@{
                        number = $_.number
                        title = $_.title
                        url = "https://github.com/dotnet/aspnetcore/pull/$($_.number)"
                        author = $_.author
                        isDraft = [bool]$_.isDraft
                        createdAt = $_.createdAt
                        updatedAt = $_.updatedAt
                        headRefOid = $_.headRefOid
                        headRefName = "fixture-$($_.number)"
                        baseRefName = "main"
                        isCrossRepository = $false
                        labels = $_.labels
                        files = $_.files
                        changedFiles = @($_.files).Count
                    }
                })
            }
            if ($query -like "*pullRequests(states:OPEN)*") {
                return [pscustomobject]@{ data = @{ repository = @{ pullRequests = @{ totalCount = $fixtures.Count } } } }
            }
            $repository = @{}
            foreach ($match in [regex]::Matches($query, 'pr(\d+): pullRequest')) {
                $number = [int]$match.Groups[1].Value
                $fixture = $fixtures | Where-Object number -eq $number
                if ($query -like "*reviewThreads(last: 50)*") {
                    $discussionNumbers.Add($number)
                    if ($fixture.failDiscussion) { throw "Fixture discussion transport failure for #$number." }
                    $detail = @{
                        comments = @{
                            totalCount = @($fixture.comments).Count + $(if ($fixture.truncateComments) { 50 } else { 0 })
                            pageInfo = @{ hasPreviousPage = [bool]$fixture.truncateComments }
                            nodes = @($fixture.comments)
                        }
                        reviewThreads = @{
                            totalCount = @($fixture.threads).Count + $(if ($fixture.truncateThreads) { 50 } else { 0 })
                            pageInfo = @{ hasPreviousPage = [bool]$fixture.truncateThreads }
                            nodes = @($fixture.threads | Select-Object isResolved, isOutdated)
                        }
                    }
                    if ($fixture.missingDiscussion) { $detail = @{} }
                    if ($fixture.missingCommentNodes) { $detail.comments.Remove("nodes") }
                }
                else {
                    $reviews = @($fixture.reviews | ConvertTo-Json -Depth 20 | ConvertFrom-Json -Depth 20)
                    if ($query -notmatch 'reviews\(last: 50\)[\s\S]*?bodyText') {
                        foreach ($review in $reviews) { $review.PSObject.Properties.Remove("bodyText") }
                    }
                    $detail = @{
                        number = $number
                        mergeable = "MERGEABLE"
                        mergeStateStatus = if ($fixture.mergeStateStatus) { $fixture.mergeStateStatus } else { "CLEAN" }
                        reviewDecision = if ($fixture.reviewDecision) { $fixture.reviewDecision } else { "APPROVED" }
                        reviews = @{
                            totalCount = $reviews.Count
                            nodes = $reviews
                            pageInfo = @{ hasPreviousPage = [bool]$fixture.truncateReviews }
                        }
                        comments = @{ nodes = @($fixture.comments | Select-Object author, createdAt) }
                        reviewRequests = @{
                            nodes = @($fixture.requests | ForEach-Object { @{ requestedReviewer = $_ } })
                            totalCount = @($fixture.requests).Count
                            pageInfo = @{ hasNextPage = $false }
                        }
                        timelineItems = @{
                            nodes = @($fixture.requests | ForEach-Object {
                                @{ createdAt = $_.requestedAt; requestedReviewer = $_ }
                            })
                            pageInfo = @{ hasPreviousPage = $false }
                        }
                        commits = @{ nodes = @(@{ commit = @{ statusCheckRollup = @{
                            state = if ($fixture.checkState) { $fixture.checkState } else { "SUCCESS" }
                        } } }) }
                    }
                }
                $repository["pr$number"] = $detail
            }
            return @{ data = @{ repository = $repository } } | ConvertTo-Json -Depth 30 | ConvertFrom-Json -Depth 30
        }
        $options = @{ AllRepo = $true }
        if ($scope -eq "blazor") { $options = @{ Preset = "blazor" } }
        $output = Invoke-PRAttentionQueue @options -DisablePersonalInbox -MaxReadyToMerge $maximum `
            -ExcludeDigestAuthor $excludedAuthors -Now ([datetime]"2026-09-16T15:00:00Z") -OutputFormat $format
        [pscustomobject]@{
            output = $output
            requests = @($requests)
            discussionNumbers = @($discussionNumbers)
        }
    } $PullRequests $OutputFormat $Scope $Maximum $ExcludedAuthors
}
