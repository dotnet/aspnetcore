#!/usr/bin/env pwsh
#Requires -Version 7.0

param(
    [ValidateNotNullOrEmpty()][string]$PulseInputPath,
    [ValidateNotNullOrEmpty()][string]$PublishedBodyPath
)

$ErrorActionPreference = "Stop"
if ($PSBoundParameters.ContainsKey("PulseInputPath") -ne $PSBoundParameters.ContainsKey("PublishedBodyPath"))
{
    throw "Supply both -PulseInputPath and -PublishedBodyPath, or neither."
}
$includeProducerPublication = $PSBoundParameters.ContainsKey("PulseInputPath")
. (Join-Path $PSScriptRoot "Test-PRAttentionPulse.ps1") -FunctionsOnly

$script:retrievalCaseCount = 0
$script:retrievalFixtureCount = 0
$retrievalFailures = [Collections.Generic.List[string]]::new()
$knownParent = (Resolve-Path -LiteralPath $PSScriptRoot).Path
$fixtureRoot = Join-Path $knownParent ".snapshot-retrieval-$([guid]::NewGuid().ToString('N'))"
$utf8 = [Text.UTF8Encoding]::new($false, $true)

function Invoke-RetrievalCase
{
    param([string]$Name, [scriptblock]$Action)

    $script:retrievalCaseCount++
    try
    {
        & $Action | Out-Null
        Write-Output "PASS retrieval: $Name"
    }
    catch
    {
        $retrievalFailures.Add("$Name`: $($_.Exception.Message)")
        Write-Output "FAIL retrieval: $Name`: $($_.Exception.Message)"
    }
}

function Get-RetrievalHash
{
    param([byte[]]$Bytes)

    $hash = [Security.Cryptography.SHA256]::Create()
    try
    {
        return [BitConverter]::ToString($hash.ComputeHash($Bytes)).Replace("-", "").ToLowerInvariant()
    }
    finally
    {
        $hash.Dispose()
    }
}

function New-RetrievalAssessment
{
    param([ValidateSet("clear", "verification-needed", "not-assessed")][string]$State)

    $clear = $State -ceq "clear"
    $unassessed = $State -ceq "not-assessed"
    return [pscustomobject][ordered]@{
        state = $State
        complete = $clear
        signals = @(if ($unassessed)
        {
            "discussion-not-assessed"
        }
        elseif (-not $clear)
        {
            "discussion-incomplete"
            "current-inline-discussion-unassessed"
        })
        commentTotalCount = $clear ? 11 : ($unassessed ? 0 : 61)
        commentEvidenceTruncated = -not $unassessed
        threads = [pscustomobject][ordered]@{
            totalCount = $clear ? 2 : ($unassessed ? 0 : 60)
            returnedCount = $clear ? 2 : ($unassessed ? 0 : 50)
            complete = $clear
            unresolvedCount = $clear ? 1 : ($unassessed ? 0 : 2)
            outdatedUnresolvedCount = $unassessed ? 0 : 1
        }
    }
}

function New-RetrievalItem
{
    param([int]$Number, [string]$Bucket, [int]$Rank, [string]$ScopeMatch)

    return [pscustomobject][ordered]@{
        number = $Number
        title = "Sanitized fixture (at) title number $Number"
        author = "fixture-author"
        bucket = $Bucket
        rank = $Rank
        nextActor = switch ($Bucket)
        {
            "ReviewNow" { "Human reviewer" }
            "NeedsRescue" { "Maintainer/triager" }
            "ReadyToMerge" { "Merger" }
        }
        reasonCodes = switch ($Bucket)
        {
            "ReviewNow" { @("needs-first-review", "ci-green") }
            "NeedsRescue" { @("never-reviewed", "orphan-unassigned") }
            "ReadyToMerge" { @("approved", "mergeable", "ci-green") }
        }
        blockers = @()
        ageDays = 12
        idleDays = 5
        scopeMatch = $ScopeMatch
    }
}

function New-RetrievalPulse
{
    param(
        [ValidateSet("complete", "complete-zero", "blazor-unavailable", "repository-unavailable", "unavailable", "bounded-empty")]
        [string]$Scenario = "complete"
    )

    $areas = [Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt 2; $index++)
    {
        $allRepo = $index -eq 1
        $unavailable = $Scenario -ceq "unavailable" -or
            ($index -eq 0 -and $Scenario -ceq "blazor-unavailable") -or
            ($index -eq 1 -and $Scenario -ceq "repository-unavailable")
        $area = [pscustomobject][ordered]@{
            id = $allRepo ? "repository-wide" : "blazor"
            label = $allRepo ? "Repository-wide" : "Blazor"
            openByDefault = $false
            status = $unavailable ? "unavailable" : "complete"
            attemptedAt = $allRepo ? "23.09.2026 19:25:00" : "09/23/2026 19:20:00"
            candidateCountsAvailable = -not $unavailable
            source = [pscustomobject]@{ repository = "dotnet/aspnetcore" }
        }
        if ($unavailable)
        {
            $area | Add-Member errorCategory "collection-failed"
            $areas.Add($area)
            continue
        }

        $zero = $Scenario -ceq "complete-zero"
        $scopeMatch = $allRepo ? "all-repo" : "label-and-path"
        $area.source = [pscustomobject][ordered]@{
            repository = "dotnet/aspnetcore"
            schemaVersion = "1.0.0"
            generatedAt = $allRepo ? "2026-09-23T19:25:00.0000000Z" : "2026-09-23T19:20:00.0000000Z"
            query = [pscustomobject]@{
                openPullRequestCount = $zero ? 0 : 20
                returnedPullRequestCount = $zero ? 0 : 20
                complete = $true
            }
            filter = [pscustomobject][ordered]@{
                name = $allRepo ? "adhoc" : "blazor"
                description = $allRepo ? "Ad hoc pull-request scope" : "Blazor and Components pull requests"
                coverage = $allRepo ? "all-repo" : "labels-and-paths"
                selection = $allRepo ? "(all open pull requests)" : "(labels any of [area-blazor, feature-blazor-*, Blazor ♥ *] OR paths any of [src/Components/**])"
                allRepositoryPullRequests = $allRepo
            }
            discussion = [pscustomobject][ordered]@{
                candidateLimit = 3
                assessedCandidateCount = $zero ? 0 : 3
                verificationNeededCount = $zero ? 0 : 1
                unassessedReviewNowCount = $zero ? 0 : 1
            }
            mergeDiscussion = [pscustomobject][ordered]@{
                candidateLimit = 3
                assessedCandidateCount = $zero ? 0 : 3
                eligibleCount = $zero ? 0 : 2
                verificationNeededCount = $zero ? 0 : 1
                unassessedCandidateCount = $zero ? 0 : 1
                excludedCandidateCount = 0
                verificationLimit = 3
            }
            census = [pscustomobject][ordered]@{
                openPullRequests = $zero ? 0 : 20
                matched = $zero ? 0 : ($allRepo ? 20 : 15)
                labelOnly = ($zero -or $allRepo) ? 0 : 5
                pathOnly = ($zero -or $allRepo) ? 0 : 4
                labelAndPath = ($zero -or $allRepo) ? 0 : 6
                incidentalPathExcluded = ($zero -or $allRepo) ? 0 : 1
                unresolvedMergeable = 0
                byBucket = [pscustomobject][ordered]@{
                    ReviewNow = $zero ? 0 : 4
                    NeedsRescue = $zero ? 0 : 2
                    ReadyToMerge = $zero ? 0 : 4
                    WaitingOnAuthor = $zero ? 0 : 1
                    WaitingOnCI = $zero ? 0 : 1
                    DesignDecision = $zero ? 0 : 1
                    Draft = $zero ? 0 : 1
                    Excluded = $zero ? 0 : ($allRepo ? 6 : 1)
                }
            }
            overflow = [pscustomobject]@{
                reviewNow = $zero ? 0 : 2
                needsRescue = $zero ? 0 : 1
                readyToMerge = $zero ? 0 : 3
            }
            caps = [pscustomobject]@{ reviewNow = 5; reviewNowPerAuthor = 2; needsRescue = 3; readyToMerge = 3 }
            warnings = @(if (-not $zero)
            {
                "Discussion is bounded; some candidates are unassessed."
            })
        }
        $views = [pscustomobject][ordered]@{
            reviewNow = @()
            verifyDiscussionBeforeReview = @()
            needsRescue = @()
            readyToMerge = @()
            verifyDiscussionBeforeMerge = @()
        }
        if (-not $zero)
        {
            $views.reviewNow = @(
                New-RetrievalItem 61002 "ReviewNow" 1 $scopeMatch
                New-RetrievalItem 61001 "ReviewNow" 2 $scopeMatch
            )
            $review = New-RetrievalItem 61003 "ReviewNow" 1 $scopeMatch
            $review | Add-Member discussionAssessment (New-RetrievalAssessment "verification-needed")
            $views.verifyDiscussionBeforeReview = @($review)
            $rescue = New-RetrievalItem 61004 "NeedsRescue" 1 $scopeMatch
            $rescue.blockers = @("Needs maintainer ownership")
            $views.needsRescue = @($rescue)
            $ready = New-RetrievalItem 61005 "ReadyToMerge" 1 $scopeMatch
            $ready | Add-Member mergeEligibility "eligible"
            $ready | Add-Member discussionAssessment (New-RetrievalAssessment "clear")
            $views.readyToMerge = @($ready)
            $verify = New-RetrievalItem 61006 "ReadyToMerge" 1 $scopeMatch
            $verify | Add-Member mergeEligibility "verification-needed"
            $verify | Add-Member discussionAssessment (New-RetrievalAssessment "verification-needed")
            $unassessed = New-RetrievalItem 61007 "ReadyToMerge" 2 $scopeMatch
            $unassessed | Add-Member mergeEligibility "not-assessed"
            $unassessed | Add-Member discussionAssessment (New-RetrievalAssessment "not-assessed")
            $views.verifyDiscussionBeforeMerge = @($verify, $unassessed)
        }
        if ($Scenario -ceq "bounded-empty")
        {
            foreach ($property in $views.PSObject.Properties)
            {
                $property.Value = @()
            }
            $area.source.caps.reviewNow = 0
            $area.source.caps.needsRescue = 0
            $area.source.caps.readyToMerge = 0
            $area.source.discussion.candidateLimit = 0
            $area.source.discussion.assessedCandidateCount = 0
            $area.source.discussion.verificationNeededCount = 0
            $area.source.discussion.unassessedReviewNowCount = 4
            $area.source.mergeDiscussion.candidateLimit = 0
            $area.source.mergeDiscussion.assessedCandidateCount = 0
            $area.source.mergeDiscussion.eligibleCount = 0
            $area.source.mergeDiscussion.verificationNeededCount = 0
            $area.source.mergeDiscussion.unassessedCandidateCount = 4
            $area.source.mergeDiscussion.verificationLimit = 0
            $area.source.overflow.reviewNow = 4
            $area.source.overflow.needsRescue = 2
            $area.source.overflow.readyToMerge = 4
        }
        $area | Add-Member views $views
        $areas.Add($area)
    }
    $completeCount = @($areas | Where-Object status -CEQ "complete").Count
    return [pscustomobject][ordered]@{
        schemaVersion = "2.0.0"
        status = @("unavailable", "partial", "complete")[$completeCount]
        areas = $areas.ToArray()
    }
}

function New-RetrievalBody
{
    param(
        [byte[]]$JsonBytes,
        [string]$RunId = "34643961191",
        [string]$Attempt = "1",
        [string]$GeneratedAt = "2026-09-23T19:30:00.0000000Z",
        [switch]$Embedded
    )

    # These are consumer fixtures, not output from the producer or renderer.
    $template = @'
# Local Pulse retrieval fixture

<details>
<summary>Blazor</summary>

Captured area A.

</details>

<details>
<summary>Repository-wide</summary>

Captured area B.

</details>

## Snapshot

Snapshot generated: `{0}`. [Producing workflow run](https://github.com/dotnet/aspnetcore/actions/runs/{1}/attempts/{2}).
Artifact: `pulse-publication-evidence`; files: `pulse-input.json`, `pulse-body.md`.
Capped report results, not the full queue. Authenticated ZIP artifact retained for seven days as secondary audit evidence.

<details>
<summary>Snapshot identity</summary>

Repository: `dotnet/aspnetcore`; run ID: `{1}`; attempt: `{2}`.
JSON SHA-256: `{3}`.

</details>
'@
    $header = $template.Replace("`r`n", "`n") -f $GeneratedAt, $RunId, $Attempt, (Get-RetrievalHash $JsonBytes)
    if ($Embedded)
    {
        $json = $utf8.GetString($JsonBytes)
        $longestRun = 0
        foreach ($match in [regex]::Matches($json, '`+'))
        {
            if ($match.Length -gt $longestRun)
            {
                $longestRun = $match.Length
            }
        }
        $fence = "``" * [Math]::Max(3, $longestRun + 1)
        $tail = @(
            ""
            "<details>"
            "<summary>Snapshot JSON (exact sanitized bytes)</summary>"
            ""
            "${fence}json"
            $json
            $fence
            ""
            "</details>"
        ) -join "`n"
        return $header + "`n" + $tail + "`n"
    }
    $fallbackTail = @'

<details>
<summary>Snapshot JSON</summary>

The exact JSON does not fit within the issue body size limit for this snapshot and is not embedded here.
Retrieve the identical bytes from the `pulse-publication-evidence` artifact and verify them against the checksum above.

</details>
'@
    return $header + "`n" + ($fallbackTail.Replace("`r`n", "`n")) + "`n"
}

function Write-RetrievalZip
{
    param([string]$Path, [object[]]$Entries)

    $file = [IO.File]::Open($Path, [IO.FileMode]::Create, [IO.FileAccess]::ReadWrite)
    $archive = [IO.Compression.ZipArchive]::new($file, [IO.Compression.ZipArchiveMode]::Create)
    try
    {
        foreach ($entry in $Entries)
        {
            $zipEntry = $archive.CreateEntry($entry.Name, [IO.Compression.CompressionLevel]::NoCompression)
            $zipEntry.LastWriteTime = [datetimeoffset]"2026-09-23T00:00:00Z"
            $stream = $zipEntry.Open()
            try
            {
                $stream.Write($entry.Bytes, 0, $entry.Bytes.Length)
            }
            finally
            {
                $stream.Dispose()
            }
        }
    }
    finally
    {
        $archive.Dispose()
        $file.Dispose()
    }
}

function Add-RetrievalArtifact
{
    param(
        [object]$Fixture,
        [string]$Id = "9001",
        [byte[]]$JsonBytes,
        [string]$Body,
        [string]$Name = "pulse-publication-evidence",
        [bool]$Expired = $false,
        [string]$RunId = "34643961191"
    )

    if ($null -eq $JsonBytes)
    {
        $JsonBytes = $Fixture.JsonBytes
    }
    if (-not $PSBoundParameters.ContainsKey("Body"))
    {
        $Body = $Fixture.PublishedBody
    }
    $zipPath = Join-Path $Fixture.Directory "artifact-$Id.zip"
    Write-RetrievalZip -Path $zipPath -Entries @(
        @{ Name = "pulse-input.json"; Bytes = $JsonBytes }
        @{ Name = "pulse-body.md"; Bytes = $utf8.GetBytes($Body) }
    )
    $Fixture.Downloads["repos/dotnet/aspnetcore/actions/artifacts/$Id/zip"] = $zipPath
    return [pscustomobject][ordered]@{
        id = [long]$Id
        name = $Name
        expired = $Expired
        created_at = "2026-09-23T19:31:00Z"
        expires_at = "2026-09-30T19:31:00Z"
        workflow_run = [pscustomobject]@{ id = [long]$RunId }
    }
}

function Set-RetrievalPages
{
    param([object]$Fixture, [object[]]$Pages)

    for ($index = 0; $index -lt $Pages.Count; $index++)
    {
        $pageNumber = $index + 1
        $path = Join-Path $Fixture.Directory "page-$pageNumber.json"
        Write-JsonFile $Pages[$index] $path
        $Fixture.Responses["repos/dotnet/aspnetcore/actions/runs/$($Fixture.Run.id)/artifacts?per_page=100&page=$pageNumber"] = $path
    }
}

function New-RetrievalFixture
{
    param(
        [object]$Pulse = (New-RetrievalPulse),
        [byte[]]$JsonBytes,
        [string]$PublishedBody,
        [string]$RunId = "34643961191",
        [string]$RunAttempt = "1",
        [switch]$Embedded
    )

    $script:retrievalFixtureCount++
    $directory = Join-Path $fixtureRoot ("fixture-{0:d3}" -f $script:retrievalFixtureCount)
    $null = New-Item -ItemType Directory -Path $directory
    if ($null -eq $JsonBytes)
    {
        $JsonBytes = $utf8.GetBytes(($Pulse | ConvertTo-Json -Depth 100 -Compress) + "`n")
    }
    if (-not $PSBoundParameters.ContainsKey("PublishedBody"))
    {
        $PublishedBody = New-RetrievalBody $JsonBytes -RunId $RunId -Attempt $RunAttempt -Embedded:$Embedded
    }
    $fixture = [pscustomobject]@{
        Directory = $directory
        Pulse = $Pulse
        JsonBytes = $JsonBytes
        PublishedBody = $PublishedBody
        Responses = @{}
        Downloads = @{}
        ReadFailures = @{}
        Calls = [Collections.Generic.List[string]]::new()
        Artifact = $null
        Run = [pscustomobject]@{
            id = [long]$RunId
            run_attempt = [long]$RunAttempt
            repository = [pscustomobject]@{ full_name = "dotnet/aspnetcore" }
            status = "completed"
            conclusion = "failure"
        }
    }
    $runPath = Join-Path $directory "run.json"
    Write-JsonFile $fixture.Run $runPath
    $fixture.Responses["repos/dotnet/aspnetcore/actions/runs/$RunId/attempts/$RunAttempt"] = $runPath
    $fixture.Artifact = Add-RetrievalArtifact $fixture -RunId $RunId
    Set-RetrievalPages $fixture @(@{ total_count = 1; artifacts = @($fixture.Artifact) })
    return $fixture
}

function Invoke-RetrievalFixture
{
    param([object]$Fixture)

    $readJson = {
        param([string]$Path)
        $Fixture.Calls.Add("GET $Path")
        if ($Fixture.ReadFailures.ContainsKey($Path))
        {
            throw $Fixture.ReadFailures[$Path]
        }
        if (-not $Fixture.Responses.ContainsKey($Path))
        {
            throw "Unexpected JSON GET: $Path"
        }
        return (Get-Content -LiteralPath $Fixture.Responses[$Path] -Raw | ConvertFrom-Json -Depth 100)
    }.GetNewClosure()
    $readZip = {
        param([string]$Path)
        $Fixture.Calls.Add("GET $Path")
        if ($Fixture.ReadFailures.ContainsKey($Path))
        {
            throw $Fixture.ReadFailures[$Path]
        }
        if (-not $Fixture.Downloads.ContainsKey($Path))
        {
            throw "Unexpected ZIP GET: $Path"
        }
        return ,[IO.File]::ReadAllBytes($Fixture.Downloads[$Path])
    }.GetNewClosure()
    return Get-PublishedPulseSnapshot -PublishedBody $Fixture.PublishedBody -ReadJson $readJson -ReadZip $readZip
}

function Assert-RetrievalPreserved
{
    param([object]$Fixture, [object]$Actual)

    $expected = $utf8.GetString($Fixture.JsonBytes) | ConvertFrom-Json -Depth 100
    Assert-True (($Actual | ConvertTo-Json -Depth 100 -Compress) -ceq ($expected | ConvertTo-Json -Depth 100 -Compress)) "Retrieval must preserve all parsed data, ordering, and additive fields."
}

$null = New-Item -ItemType Directory -Path $fixtureRoot
try
{
    $readmePath = Join-Path (Split-Path -Parent $knownParent) "pr-attention-pulse\README.md"
    $readme = [IO.File]::ReadAllText($readmePath)
    $blocks = [regex]::Matches($readme, '(?ms)^<!-- pulse-snapshot-retrieval -->\r?\n```powershell\r?\n(?<code>.*?)^```\r?$')
    Assert-True ($blocks.Count -eq 1) "The README must contain one named retrieval example."
    . ([scriptblock]::Create($blocks[0].Groups["code"].Value))

    Invoke-RetrievalCase "published tuple, run ID above Int32, and no overall-success requirement" {
        $fixture = New-RetrievalFixture
        $actual = Invoke-RetrievalFixture $fixture
        Assert-RetrievalPreserved $fixture $actual
        Assert-True ($fixture.Calls[0] -ceq "GET repos/dotnet/aspnetcore/actions/runs/34643961191/attempts/1") "The exact producing attempt must be read."
        Assert-True ($fixture.Run.id -gt [int]::MaxValue -and $fixture.Run.conclusion -ceq "failure") "The large-ID/non-success fixture must reach the assertion."
        Assert-True (-not $fixture.Artifact.PSObject.Properties["attempt"]) "REST artifacts do not have an attempt property."
    }

    Invoke-RetrievalCase "pagination finds the candidate after 100 other artifacts" {
        $fixture = New-RetrievalFixture
        $otherArtifacts = @(1..100 | ForEach-Object { [pscustomobject]@{ id = $_; name = "other-evidence"; expired = $false } })
        Set-RetrievalPages $fixture @(
            @{ total_count = 101; artifacts = $otherArtifacts }
            @{ total_count = 101; artifacts = @($fixture.Artifact) }
        )
        Assert-RetrievalPreserved $fixture (Invoke-RetrievalFixture $fixture)
        Assert-True ($fixture.Calls -ccontains "GET repos/dotnet/aspnetcore/actions/runs/34643961191/artifacts?per_page=100&page=2") "Every listing page must be read."
        Assert-True (@($fixture.Calls | Where-Object { $_ -like "*/zip" }).Count -eq 1) "Other artifact names must not be downloaded."
    }

    Invoke-RetrievalCase "duplicate names permit one exact match, not newest or first" {
        $fixture = New-RetrievalFixture
        $otherBody = New-RetrievalBody $fixture.JsonBytes -Attempt "2"
        $other = Add-RetrievalArtifact $fixture -Id "9002" -Body $otherBody
        $other.created_at = "2026-09-23T20:31:00Z"
        Set-RetrievalPages $fixture @(@{ total_count = 2; artifacts = @($other, $fixture.Artifact) })
        Assert-RetrievalPreserved $fixture (Invoke-RetrievalFixture $fixture)
        Assert-True (@($fixture.Calls | Where-Object { $_ -like "*/zip" }).Count -eq 2) "Both same-name candidates must be checked."
    }

    Invoke-RetrievalCase "multiple exact matching artifact IDs are ambiguous" {
        $fixture = New-RetrievalFixture
        $duplicate = Add-RetrievalArtifact $fixture -Id "9002"
        Set-RetrievalPages $fixture @(@{ total_count = 2; artifacts = @($fixture.Artifact, $duplicate) })
        Assert-Throws { Invoke-RetrievalFixture $fixture } "Duplicate matches must not select either artifact." "Snapshot ambiguous"
    }

    Invoke-RetrievalCase "pagination checks for a second exact match, not just first success" {
        $fixture = New-RetrievalFixture
        $duplicate = Add-RetrievalArtifact $fixture -Id "9002"
        $firstPage = @($fixture.Artifact) + @(1..99 | ForEach-Object { [pscustomobject]@{ id = $_; name = "other-evidence" } })
        Set-RetrievalPages $fixture @(
            @{ total_count = 101; artifacts = $firstPage }
            @{ total_count = 101; artifacts = @($duplicate) }
        )
        Assert-Throws { Invoke-RetrievalFixture $fixture } "A later duplicate must be detected." "Snapshot ambiguous"
    }

    Invoke-RetrievalCase "wrong attempt has identical JSON checksum but cannot match the body" {
        $fixture = New-RetrievalFixture
        $wrongBody = New-RetrievalBody $fixture.JsonBytes -Attempt "2"
        $hash = Get-RetrievalHash $fixture.JsonBytes
        Assert-True ($wrongBody.Contains($hash) -and $fixture.PublishedBody.Contains($hash)) "Both attempts must actually have the same JSON hash."
        $wrong = Add-RetrievalArtifact $fixture -Body $wrongBody
        Set-RetrievalPages $fixture @(@{ total_count = 1; artifacts = @($wrong) })
        Assert-Throws { Invoke-RetrievalFixture $fixture } "A checksum alone cannot identify an attempt." "Published body/identity mismatch"
    }

    foreach ($missing in @($false, $true))
    {
        Invoke-RetrievalCase "newer unpublished run is ignored (published artifact missing: $missing)" {
            $fixture = New-RetrievalFixture
            $newer = Add-RetrievalArtifact $fixture -Id "9999" -RunId "34643961192" -Body (New-RetrievalBody $fixture.JsonBytes -RunId "34643961192")
            $newerPath = Join-Path $fixture.Directory "newer-run-artifacts.json"
            Write-JsonFile @{ total_count = 1; artifacts = @($newer) } $newerPath
            $fixture.Responses["repos/dotnet/aspnetcore/actions/runs/34643961192/artifacts?per_page=100&page=1"] = $newerPath
            if ($missing)
            {
                Set-RetrievalPages $fixture @(@{ total_count = 0; artifacts = @() })
                Assert-Throws { Invoke-RetrievalFixture $fixture } "Missing evidence must not fall forward." "artifact is missing"
            }
            else
            {
                Assert-RetrievalPreserved $fixture (Invoke-RetrievalFixture $fixture)
            }
            Assert-True (-not ($fixture.Calls -match "34643961192|artifacts/9999")) "No newer unpublished run or artifact may be read."
        }
    }

    Invoke-RetrievalCase "downstream publication retry still selects the earlier producing attempt" {
        $fixture = New-RetrievalFixture
        $latestPath = Join-Path $fixture.Directory "latest-run.json"
        Write-JsonFile @{ id = [long]34643961191; run_attempt = 3; conclusion = "success" } $latestPath
        $fixture.Responses["repos/dotnet/aspnetcore/actions/runs/34643961191"] = $latestPath
        Assert-RetrievalPreserved $fixture (Invoke-RetrievalFixture $fixture)
        Assert-True ($fixture.Calls[0].EndsWith("/attempts/1", [StringComparison]::Ordinal)) "The published producing attempt remains authoritative."
    }

    Invoke-RetrievalCase "same run's unpublished attempt is not evidence of publication" {
        $fixture = New-RetrievalFixture
        $unpublished = Add-RetrievalArtifact $fixture -Body (New-RetrievalBody $fixture.JsonBytes -Attempt "2")
        Set-RetrievalPages $fixture @(@{ total_count = 1; artifacts = @($unpublished) })
        Assert-Throws { Invoke-RetrievalFixture $fixture } "Artifact existence must not replace the published tuple." "Published body/identity mismatch"
    }

    Invoke-RetrievalCase "missing exact artifact name is unavailable, not empty" {
        $fixture = New-RetrievalFixture
        $fixture.Artifact.name = "Pulse-publication-evidence"
        Set-RetrievalPages $fixture @(@{ total_count = 1; artifacts = @($fixture.Artifact) })
        Assert-Throws { Invoke-RetrievalFixture $fixture } "Artifact names must match ordinally." "artifact is missing"
    }

    Invoke-RetrievalCase "expired artifact is unavailable and not downloaded" {
        $fixture = New-RetrievalFixture
        $fixture.Artifact.expired = $true
        Set-RetrievalPages $fixture @(@{ total_count = 1; artifacts = @($fixture.Artifact) })
        Assert-Throws { Invoke-RetrievalFixture $fixture } "Expired evidence cannot become an empty queue." "9001: expired"
        Assert-True (@($fixture.Calls | Where-Object { $_ -like "*/zip" }).Count -eq 0) "Expired artifacts must not be downloaded."
    }

    Invoke-RetrievalCase "an expired duplicate does not replace a unique available exact match" {
        $fixture = New-RetrievalFixture
        $expired = Add-RetrievalArtifact $fixture -Id "9002" -Expired $true
        Set-RetrievalPages $fixture @(@{ total_count = 2; artifacts = @($expired, $fixture.Artifact) })
        Assert-RetrievalPreserved $fixture (Invoke-RetrievalFixture $fixture)
    }

    foreach ($failure in @(
        @{ Name = "attempt authentication"; Path = "actions/runs/34643961191/attempts/1"; Error = "HTTP 401 authentication required" }
        @{ Name = "attempt unavailable"; Path = "actions/runs/34643961191/attempts/1"; Error = "HTTP 404 missing attempt" }
        @{ Name = "listing permission"; Path = "actions/runs/34643961191/artifacts?per_page=100&page=1"; Error = "HTTP 403 read access denied" }
        @{ Name = "ZIP authentication"; Path = "actions/artifacts/9001/zip"; Error = "HTTP 401 authentication required" }
        @{ Name = "ZIP permission"; Path = "actions/artifacts/9001/zip"; Error = "HTTP 403 read access denied" }
        @{ Name = "artifact deleted before retention"; Path = "actions/artifacts/9001/zip"; Error = "HTTP 404 artifact deleted" }
        @{ Name = "download expired"; Path = "actions/artifacts/9001/zip"; Error = "HTTP 410 artifact expired" }
        @{ Name = "ZIP transport"; Path = "actions/artifacts/9001/zip"; Error = "Connection read failed" }
    ))
    {
        Invoke-RetrievalCase "$($failure.Name) failure is explicit" {
            $fixture = New-RetrievalFixture
            $fixture.ReadFailures["repos/dotnet/aspnetcore/$($failure.Path)"] = $failure.Error
            Assert-Throws { Invoke-RetrievalFixture $fixture } "Read failures must not produce data." $failure.Error
        }
    }

    Invoke-RetrievalCase "later-page read failure prevents selection" {
        $fixture = New-RetrievalFixture
        $firstPage = @($fixture.Artifact) + @(1..99 | ForEach-Object { [pscustomobject]@{ id = $_; name = "other-evidence" } })
        Set-RetrievalPages $fixture @(@{ total_count = 101; artifacts = $firstPage })
        $fixture.ReadFailures["repos/dotnet/aspnetcore/actions/runs/34643961191/artifacts?per_page=100&page=2"] = "HTTP 503 listing unavailable"
        Assert-Throws { Invoke-RetrievalFixture $fixture } "One page cannot establish uniqueness." "HTTP 503 listing unavailable"
    }

    Invoke-RetrievalCase "unreadable duplicate prevents uniqueness despite a matching candidate" {
        $fixture = New-RetrievalFixture
        $duplicate = Add-RetrievalArtifact $fixture -Id "9002"
        Set-RetrievalPages $fixture @(@{ total_count = 2; artifacts = @($fixture.Artifact, $duplicate) })
        $fixture.ReadFailures["repos/dotnet/aspnetcore/actions/artifacts/9002/zip"] = "HTTP 403"
        Assert-Throws { Invoke-RetrievalFixture $fixture } "Unreadable candidates must not be silently skipped." "cannot establish uniqueness"
    }

    foreach ($unreadablePart in @("archive", "entry"))
    {
        foreach ($unreadableFirst in @($false, $true))
        {
            Invoke-RetrievalCase "unreadable ZIP $unreadablePart prevents uniqueness (unreadable first: $unreadableFirst)" {
                $fixture = New-RetrievalFixture
                $duplicate = Add-RetrievalArtifact $fixture -Id "9002"
                $zipPath = $fixture.Downloads["repos/dotnet/aspnetcore/actions/artifacts/9002/zip"]
                if ($unreadablePart -ceq "archive")
                {
                    [IO.File]::WriteAllBytes($zipPath, $utf8.GetBytes("not a zip"))
                }
                else
                {
                    $zipBytes = [IO.File]::ReadAllBytes($zipPath)
                    Assert-True ($zipBytes[0] -eq 0x50 -and $zipBytes[1] -eq 0x4b -and
                        $zipBytes[2] -eq 0x03 -and $zipBytes[3] -eq 0x04) "The fixture must start with a local ZIP file header."
                    # Leave the central directory readable, but prevent opening the first entry.
                    $zipBytes[0] = 0
                    [IO.File]::WriteAllBytes($zipPath, $zipBytes)
                    $archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
                    try
                    {
                        Assert-True ($archive.Entries.Count -eq 2) "The unreadable-entry fixture must still list both files."
                        Assert-Throws {
                            $stream = $archive.GetEntry("pulse-input.json").Open()
                            $stream.Dispose()
                        } "The fixture must fail when the real ZIP entry is opened."
                    }
                    finally
                    {
                        $archive.Dispose()
                    }
                }
                $artifacts = if ($unreadableFirst)
                {
                    @($duplicate, $fixture.Artifact)
                }
                else
                {
                    @($fixture.Artifact, $duplicate)
                }
                Set-RetrievalPages $fixture @(@{ total_count = 2; artifacts = $artifacts })
                Assert-True ($fixture.ReadFailures.Count -eq 0) "The transport must return bytes successfully."
                Assert-Throws { Invoke-RetrievalFixture $fixture } "Unreadable ZIP candidates must prevent uniqueness." "cannot establish uniqueness"
            }
        }
    }

    foreach ($field in @("run", "attempt", "repository"))
    {
        Invoke-RetrievalCase "attempt endpoint $field mismatch" {
            $fixture = New-RetrievalFixture
            switch ($field)
            {
                "run" { $fixture.Run.id = [long]34643961192 }
                "attempt" { $fixture.Run.run_attempt = 2 }
                "repository" { $fixture.Run.repository.full_name = "dotnet/runtime" }
            }
            Write-JsonFile $fixture.Run $fixture.Responses["repos/dotnet/aspnetcore/actions/runs/34643961191/attempts/1"]
            Assert-Throws { Invoke-RetrievalFixture $fixture } "Attempt metadata must match the published tuple." "Producing attempt identity mismatch"
        }
    }

    Invoke-RetrievalCase "artifact run metadata mismatch" {
        $fixture = New-RetrievalFixture
        $fixture.Artifact.workflow_run.id = [long]34643961192
        Set-RetrievalPages $fixture @(@{ total_count = 1; artifacts = @($fixture.Artifact) })
        Assert-Throws { Invoke-RetrievalFixture $fixture } "Artifact metadata must match the run." "artifact run identity mismatch"
    }

    foreach ($listing in @(
        @{ Name = "missing count"; Page = @{ artifacts = @() }; Error = "Invalid artifact listing" }
        @{ Name = "missing array"; Page = @{ total_count = 0 }; Error = "Invalid artifact listing" }
        @{ Name = "premature empty page"; Page = @{ total_count = 1; artifacts = @() }; Error = "listing changed or is incomplete" }
    ))
    {
        Invoke-RetrievalCase "invalid listing: $($listing.Name)" {
            $fixture = New-RetrievalFixture
            Set-RetrievalPages $fixture @($listing.Page)
            Assert-Throws { Invoke-RetrievalFixture $fixture } "Malformed pages must be errors." $listing.Error
        }
    }

    foreach ($missingFile in @("pulse-input.json", "pulse-body.md"))
    {
        Invoke-RetrievalCase "ZIP missing $missingFile" {
            $fixture = New-RetrievalFixture
            $entries = @(
                @{ Name = "pulse-input.json"; Bytes = $fixture.JsonBytes }
                @{ Name = "pulse-body.md"; Bytes = $utf8.GetBytes($fixture.PublishedBody) }
            ) | Where-Object { $_.Name -cne $missingFile }
            Write-RetrievalZip $fixture.Downloads["repos/dotnet/aspnetcore/actions/artifacts/9001/zip"] @($entries)
            Assert-Throws { Invoke-RetrievalFixture $fixture } "Both archived files are required." "two exact expected files"
        }
    }

    foreach ($fileCase in @("duplicate entry", "unexpected context", "nested filename", "filename casing"))
    {
        Invoke-RetrievalCase "ZIP rejects $fileCase" {
            $fixture = New-RetrievalFixture
            $entries = @(
                @{ Name = "pulse-input.json"; Bytes = $fixture.JsonBytes }
                @{ Name = "pulse-body.md"; Bytes = $utf8.GetBytes($fixture.PublishedBody) }
            )
            switch ($fileCase)
            {
                "duplicate entry" { $entries += $entries[0] }
                "unexpected context" { $entries += @{ Name = "pulse-snapshot-context.json"; Bytes = $utf8.GetBytes("{}") } }
                "nested filename" { $entries[0].Name = "nested/pulse-input.json" }
                "filename casing" { $entries[0].Name = "Pulse-input.json" }
            }
            Write-RetrievalZip $fixture.Downloads["repos/dotnet/aspnetcore/actions/artifacts/9001/zip"] $entries
            Assert-Throws { Invoke-RetrievalFixture $fixture } "The documented file set must match." "two exact expected files"
        }
    }

    Invoke-RetrievalCase "malformed ZIP is explicit" {
        $fixture = New-RetrievalFixture
        [IO.File]::WriteAllBytes($fixture.Downloads["repos/dotnet/aspnetcore/actions/artifacts/9001/zip"], $utf8.GetBytes("not a zip"))
        Assert-Throws { Invoke-RetrievalFixture $fixture } "Malformed ZIP cannot become empty data." "cannot establish uniqueness"
    }

    foreach ($mutation in @(
        @{ Name = "area text"; From = "Captured area A."; To = "Changed area A." }
        @{ Name = "repository"; From = 'Repository: `dotnet/aspnetcore`'; To = 'Repository: `dotnet/runtime`' }
        @{ Name = "run ID"; From = 'run ID: `34643961191`'; To = 'run ID: `34643961192`' }
        @{ Name = "generation time"; From = "2026-09-23T19:30:00.0000000Z"; To = "2026-09-23T19:30:01.0000000Z" }
        @{ Name = "artifact name"; From = '`pulse-publication-evidence`'; To = '`other-evidence`' }
        @{ Name = "JSON filename"; From = '`pulse-input.json`'; To = '`other.json`' }
        @{ Name = "body filename"; From = '`pulse-body.md`'; To = '`other.md`' }
        @{ Name = "run URL"; From = "/attempts/1)"; To = "/attempts/2)" }
        @{ Name = "line endings"; From = "`n"; To = "`r`n" }
    ))
    {
        Invoke-RetrievalCase "archived body ordinal mismatch: $($mutation.Name)" {
            $fixture = New-RetrievalFixture
            $changed = $fixture.PublishedBody.Replace($mutation.From, $mutation.To)
            Assert-True ($changed -cne $fixture.PublishedBody) "The fixture mutation must take effect."
            $null = Add-RetrievalArtifact $fixture -Body $changed
            Assert-Throws { Invoke-RetrievalFixture $fixture } "No Markdown normalization is permitted." "Published body/identity mismatch"
        }
    }

    foreach ($mutation in @(
        @{ Name = "repository"; From = "dotnet/aspnetcore"; To = "dotnet/runtime" }
        @{ Name = "server"; From = "https://github.com"; To = "https://example.invalid" }
        @{ Name = "run disagreement"; From = 'run ID: `34643961191`'; To = 'run ID: `34643961192`' }
        @{ Name = "attempt disagreement"; From = 'attempt: `1`'; To = 'attempt: `2`' }
        @{ Name = "zero run"; From = "34643961191"; To = "0" }
        @{ Name = "non-ASCII run"; From = "34643961191"; To = "３４６４３９６１１９１" }
        @{ Name = "non-decimal run"; From = "34643961191"; To = "+34643961191" }
        @{ Name = "artifact name"; From = '`pulse-publication-evidence`'; To = '`other-evidence`' }
        @{ Name = "JSON filename"; From = '`pulse-input.json`'; To = '`other.json`' }
        @{ Name = "body filename"; From = '`pulse-body.md`'; To = '`other.md`' }
        @{ Name = "UTC offset instead of Z"; From = "19:30:00.0000000Z"; To = "19:30:00.0000000+00:00" }
        @{ Name = "wrong fractional precision"; From = "19:30:00.0000000Z"; To = "19:30:00.000Z" }
        @{ Name = "invalid calendar time"; From = "2026-09-23"; To = "2026-13-23" }
    ))
    {
        Invoke-RetrievalCase "published identity rejects $($mutation.Name) before any GET" {
            $fixture = New-RetrievalFixture
            $fixture.PublishedBody = $fixture.PublishedBody.Replace($mutation.From, $mutation.To)
            Assert-Throws { Invoke-RetrievalFixture $fixture } "Unsupported identity must not trigger retrieval." "Invalid published snapshot identity"
            Assert-True ($fixture.Calls.Count -eq 0) "Invalid identity must be rejected before any GET."
        }
    }

    foreach ($suffixCase in @("duplicate heading", "uppercase hash", "zero attempt"))
    {
        Invoke-RetrievalCase "published suffix rejects $suffixCase" {
            $fixture = New-RetrievalFixture
            switch ($suffixCase)
            {
                "duplicate heading" { $fixture.PublishedBody = "## Snapshot`n`n" + $fixture.PublishedBody }
                "uppercase hash"
                {
                    $hash = Get-RetrievalHash $fixture.JsonBytes
                    Assert-True ($hash -cne $hash.ToUpperInvariant()) "The hash must contain a-f for this mutation."
                    $fixture.PublishedBody = $fixture.PublishedBody.Replace($hash, $hash.ToUpperInvariant())
                }
                "zero attempt"
                {
                    $fixture.PublishedBody = (New-RetrievalBody $fixture.JsonBytes -Attempt "0")
                }
            }
            Assert-Throws { Invoke-RetrievalFixture $fixture } "Malformed suffix must fail explicitly." "Invalid published snapshot identity"
        }
    }
    Invoke-RetrievalCase "published suffix rejects trailing text after the JSON section" {
        $fixture = New-RetrievalFixture
        $fixture.PublishedBody += "Unexpected suffix`n"
        Assert-Throws { Invoke-RetrievalFixture $fixture } "Trailing text past the JSON section must fail explicitly." "Invalid published snapshot JSON section"
    }

    Invoke-RetrievalCase "embedded snapshot is verified directly with zero API calls" {
        $fixture = New-RetrievalFixture -Embedded
        $actual = Invoke-RetrievalFixture $fixture
        Assert-RetrievalPreserved $fixture $actual
        Assert-True ($fixture.Calls.Count -eq 0) "An embedded snapshot must never require an authenticated API call."
    }

    Invoke-RetrievalCase "embedded snapshot with backticks in the JSON widens the fence and still verifies" {
        $pulse = New-RetrievalPulse
        $pulse.areas[0].source | Add-Member -NotePropertyName note -NotePropertyValue 'contains ```` four backticks and ``` three' -Force
        $jsonBytes = $utf8.GetBytes(($pulse | ConvertTo-Json -Depth 100 -Compress) + "`n")
        $fixture = New-RetrievalFixture -Pulse $pulse -JsonBytes $jsonBytes -Embedded
        Assert-True ($fixture.PublishedBody -match '(?<fence>`{5,})json') "The fence must widen beyond the longest backtick run plus one."
        $actual = Invoke-RetrievalFixture $fixture
        Assert-RetrievalPreserved $fixture $actual
        Assert-True ($fixture.Calls.Count -eq 0) "A widened-fence embedded snapshot must still avoid API calls."
    }

    Invoke-RetrievalCase "embedded snapshot rejects checksum mismatch before any API call" {
        $fixture = New-RetrievalFixture -Embedded
        $hash = Get-RetrievalHash $fixture.JsonBytes
        $tamperedHash = ($hash.Substring(0, 63) + $(if ($hash[63] -ceq '0') { '1' } else { '0' }))
        $fixture.PublishedBody = $fixture.PublishedBody.Replace("JSON SHA-256: ``$hash``.", "JSON SHA-256: ``$tamperedHash``.")
        Assert-Throws { Invoke-RetrievalFixture $fixture } "A checksum mismatch on the embedded JSON must be rejected." "Embedded snapshot JSON does not match its published checksum"
        Assert-True ($fixture.Calls.Count -eq 0) "Checksum verification of embedded JSON must not require any API call."
    }

    Invoke-RetrievalCase "embedded snapshot rejects tampered JSON bytes inside the fence" {
        $fixture = New-RetrievalFixture -Embedded
        $fixture.PublishedBody = $fixture.PublishedBody.Replace('"schemaVersion":"2.0.0"', '"schemaVersion": "2.0.0"')
        Assert-Throws { Invoke-RetrievalFixture $fixture } "Whitespace-altered embedded JSON must fail the exact-byte checksum." "Embedded snapshot JSON does not match its published checksum"
    }

    Invoke-RetrievalCase "embedded snapshot rejects a fence that does not close with the same width" {
        $fixture = New-RetrievalFixture -Embedded
        Assert-True ($fixture.PublishedBody -cmatch '(?m)^```json\n') "The default fixture must use the minimum three-backtick fence."
        $threeBackticks = [string]::new([char]0x60, 3)
        $fourBackticks = [string]::new([char]0x60, 4)
        $fixture.PublishedBody = $fixture.PublishedBody -replace "(?m)^$threeBackticks\n\n</details>$", "$fourBackticks`n`n</details>"
        Assert-Throws { Invoke-RetrievalFixture $fixture } "A mismatched closing fence width must not be treated as embedded or fallback." "Invalid published snapshot JSON section"
    }

    Invoke-RetrievalCase "embedded snapshot rejects content escaping the details section via a bogus closing tag" {
        $pulse = New-RetrievalPulse
        $pulse.areas[0].source | Add-Member -NotePropertyName note -NotePropertyValue "</details><script>evil</script>" -Force
        $jsonBytes = $utf8.GetBytes(($pulse | ConvertTo-Json -Depth 100 -Compress) + "`n")
        $fixture = New-RetrievalFixture -Pulse $pulse -JsonBytes $jsonBytes -Embedded
        $actual = Invoke-RetrievalFixture $fixture
        Assert-RetrievalPreserved $fixture $actual
        Assert-True ($actual.areas[0].source.note -ceq "</details><script>evil</script>") "Untrusted-looking text inside the JSON must round-trip as data, not break the section out early."
    }

    foreach ($whitespace in @("pretty JSON", "trailing newline"))
    {
        Invoke-RetrievalCase "parsed-equal $whitespace changes fail exact-byte checksum" {
            $fixture = New-RetrievalFixture
            $changedBytes = if ($whitespace -ceq "pretty JSON")
            {
                $utf8.GetBytes(($fixture.Pulse | ConvertTo-Json -Depth 100) + "`n")
            }
            else
            {
                $utf8.GetBytes($utf8.GetString($fixture.JsonBytes) + "`n")
            }
            $originalParsed = $utf8.GetString($fixture.JsonBytes) | ConvertFrom-Json -Depth 100 | ConvertTo-Json -Depth 100 -Compress
            $changedParsed = $utf8.GetString($changedBytes) | ConvertFrom-Json -Depth 100 | ConvertTo-Json -Depth 100 -Compress
            Assert-True ($originalParsed -ceq $changedParsed) "The JSON values must remain semantically identical."
            Assert-True ((Get-RetrievalHash $fixture.JsonBytes) -cne (Get-RetrievalHash $changedBytes)) "The exact bytes must differ."
            $null = Add-RetrievalArtifact $fixture -JsonBytes $changedBytes
            Assert-Throws { Invoke-RetrievalFixture $fixture } "Re-serialization cannot substitute for the frozen bytes." "Exact-byte JSON checksum mismatch"
        }
    }

    Invoke-RetrievalCase "hash mismatch is checked before malformed JSON parsing" {
        $fixture = New-RetrievalFixture
        $null = Add-RetrievalArtifact $fixture -JsonBytes $utf8.GetBytes("{")
        Assert-Throws { Invoke-RetrievalFixture $fixture } "Parsing must happen after the checksum." "Exact-byte JSON checksum mismatch"
    }

    Invoke-RetrievalCase "malformed JSON with matching body and checksum" {
        $fixture = New-RetrievalFixture -JsonBytes $utf8.GetBytes("{")
        Assert-Throws { Invoke-RetrievalFixture $fixture } "A valid binding does not make malformed JSON usable." "Malformed snapshot JSON"
    }

    foreach ($rootKind in @("null", "array"))
    {
        Invoke-RetrievalCase "JSON $rootKind is not a combined envelope" {
            $json = $rootKind -ceq "null" ? "null" : ("[" + (New-RetrievalPulse | ConvertTo-Json -Depth 100 -Compress) + "]")
            $fixture = New-RetrievalFixture -JsonBytes $utf8.GetBytes($json)
            Assert-Throws { Invoke-RetrievalFixture $fixture } "JSON arrays must not be unwrapped as an envelope." "Invalid snapshot envelope"
        }
    }

    foreach ($version in @("1.0.0", "3.0.0"))
    {
        Invoke-RetrievalCase "unsupported combined schema $version" {
            $pulse = New-RetrievalPulse
            $pulse.schemaVersion = $version
            $fixture = New-RetrievalFixture -Pulse $pulse
            Assert-Throws { Invoke-RetrievalFixture $fixture } "Unsupported schema must not become empty results." "Unsupported combined snapshot schema"
        }
    }

    foreach ($index in @(0, 1))
    {
        Invoke-RetrievalCase "unsupported complete-area source schema at index $index" {
            $pulse = New-RetrievalPulse
            $pulse.areas[$index].source.schemaVersion = "9.0.0"
            $fixture = New-RetrievalFixture -Pulse $pulse
            Assert-Throws { Invoke-RetrievalFixture $fixture } "Every complete source schema must be checked." "Unsupported complete-area source schema"
        }
    }

    Invoke-RetrievalCase "compatible additive fields remain available" {
        $pulse = New-RetrievalPulse
        $pulse | Add-Member futureRoot ([pscustomobject]@{ enabled = $true })
        $pulse.areas[0] | Add-Member futureArea "retained"
        $pulse.areas[0].source | Add-Member futureSource @(1, 2)
        $pulse.areas[0].views.reviewNow[0] | Add-Member futureItem "retained"
        $fixture = New-RetrievalFixture -Pulse $pulse
        Assert-RetrievalPreserved $fixture (Invoke-RetrievalFixture $fixture)
    }

    foreach ($scenario in @("complete-zero", "blazor-unavailable", "repository-unavailable", "unavailable", "bounded-empty"))
    {
        Invoke-RetrievalCase "preserves $scenario data without inventing counts or views" {
            $fixture = New-RetrievalFixture -Pulse (New-RetrievalPulse $scenario)
            $actual = Invoke-RetrievalFixture $fixture
            Assert-RetrievalPreserved $fixture $actual
            foreach ($area in $actual.areas)
            {
                if ($area.status -ceq "unavailable")
                {
                    Assert-True (-not $area.candidateCountsAvailable -and -not $area.PSObject.Properties["views"] -and -not $area.source.PSObject.Properties["census"]) "Unavailable data must not be represented by empty views/counts."
                }
                elseif ($scenario -ceq "complete-zero")
                {
                    Assert-True ($area.candidateCountsAvailable -and $area.source.census.matched -eq 0 -and $area.views.reviewNow.Count -eq 0) "Complete-zero must retain known counts."
                }
                elseif ($scenario -ceq "bounded-empty")
                {
                    Assert-True ($area.source.census.matched -gt 0 -and $area.views.reviewNow.Count -eq 0 -and $area.source.discussion.unassessedReviewNowCount -gt 0) "Empty displayed views must not erase nonzero inventory."
                }
            }
        }
    }

    Invoke-RetrievalCase "complete inventory preserves bounded, truncated, unassessed evidence and rank" {
        $fixture = New-RetrievalFixture
        $actual = Invoke-RetrievalFixture $fixture
        Assert-RetrievalPreserved $fixture $actual
        $area = $actual.areas[0]
        Assert-True ($actual.status -ceq "complete" -and $area.source.query.complete) "The inventory must actually be complete."
        Assert-True (($area.views.reviewNow.number -join ",") -ceq "61002,61001" -and (($area.views.reviewNow | ForEach-Object rank) -join ",") -ceq "1,2") "View rank, not PR number, determines order."
        Assert-True (-not $area.views.verifyDiscussionBeforeReview[0].discussionAssessment.complete) "Incomplete evidence must survive."
        Assert-True ($area.views.verifyDiscussionBeforeMerge[1].mergeEligibility -ceq "not-assessed") "Unassessed merge eligibility must survive."
        Assert-True ($area.source.warnings -is [array] -and $area.source.warnings.Count -gt 0 -and $area.source.overflow.reviewNow -gt 0) "Warnings and hidden inventory must survive."
        $ready = $area.views.readyToMerge[0]
        Assert-True ($ready.mergeEligibility -ceq "eligible" -and $ready.discussionAssessment.complete -and $ready.discussionAssessment.commentEvidenceTruncated -and $ready.discussionAssessment.signals -is [array] -and $ready.discussionAssessment.signals.Count -eq 0) "Excerpt truncation alone must not rewrite established bounded eligibility."
        Assert-True ($actual.areas[1].views.reviewNow[0].number -eq $area.views.reviewNow[0].number -and $actual.areas[1].views.reviewNow[0].scopeMatch -ceq "all-repo") "Overlapping scope results must remain independent."
    }

    Invoke-RetrievalCase "legacy culture-formatted attemptedAt is neither parsed nor repaired" {
        $fixture = New-RetrievalFixture
        $actual = Invoke-RetrievalFixture $fixture
        Assert-True ($actual.areas[0].attemptedAt -ceq "09/23/2026 19:20:00") "Legacy attemptedAt must be retained."
        Assert-True ($actual.areas[1].attemptedAt -ceq "23.09.2026 19:25:00") "No locale-dependent freshness or identity conversion is allowed."
        Assert-True ($actual.areas[0].source.generatedAt -ne $actual.areas[1].source.generatedAt) "Independent source query times must not be combined."
    }

    foreach ($invalid in @("area order", "root status", "complete query", "missing view", "unavailable with views"))
    {
        Invoke-RetrievalCase "invalid envelope rejects $invalid" {
            $pulse = New-RetrievalPulse
            $expectedError = switch ($invalid)
            {
                "area order"
                {
                    $pulse.areas = @($pulse.areas[1], $pulse.areas[0])
                    "Invalid snapshot area metadata"
                }
                "root status"
                {
                    $pulse.status = "partial"
                    "Snapshot status does not match"
                }
                "complete query"
                {
                    $pulse.areas[0].source.query.complete = $false
                    "Invalid complete-area inventory contract"
                }
                "missing view"
                {
                    $pulse.areas[0].views.PSObject.Properties.Remove("verifyDiscussionBeforeMerge")
                    "Missing or invalid snapshot view"
                }
                "unavailable with views"
                {
                    $pulse.areas[0].status = "unavailable"
                    $pulse.areas[0].candidateCountsAvailable = $false
                    $pulse.areas[0] | Add-Member errorCategory "collection-failed"
                    "Invalid unavailable-area contract"
                }
            }
            $fixture = New-RetrievalFixture -Pulse $pulse
            Assert-Throws { Invoke-RetrievalFixture $fixture } "Invalid envelope must fail explicitly." $expectedError
        }
    }

    if ($includeProducerPublication)
    {
        Invoke-RetrievalCase "producer publication exact files reach the documented consumer" {
            $inputBytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $PulseInputPath).Path)
            $bodyBytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $PublishedBodyPath).Path)
            $publishedBody = $utf8.GetString($bodyBytes)

            # Read only the supplied tuple to configure mock routes; do not reconstruct the rendered body.
            $tuples = [regex]::Matches($publishedBody, '(?m)^Repository: `(?<repository>[^`]+)`; run ID: `(?<run>[1-9][0-9]*)`; attempt: `(?<attempt>[1-9][0-9]*)`\.$')
            $hashes = [regex]::Matches($publishedBody, '(?m)^JSON SHA-256: `(?<hash>[0-9a-f]{64})`\.$')
            Assert-True ($tuples.Count -eq 1 -and $hashes.Count -eq 1) "The real rendered body must supply one run tuple and checksum."
            $repository = $tuples[0].Groups["repository"].Value
            $runId = $tuples[0].Groups["run"].Value
            $runAttempt = $tuples[0].Groups["attempt"].Value
            Assert-True ($repository -ceq "dotnet/aspnetcore") "The supplied publication must identify the fixed repository."
            Assert-True ((Get-RetrievalHash $inputBytes) -ceq $hashes[0].Groups["hash"].Value) "The real rendered checksum must bind the exact supplied JSON bytes."

            $fixture = New-RetrievalFixture -JsonBytes $inputBytes -PublishedBody $publishedBody -RunId $runId -RunAttempt $runAttempt
            $zipPath = $fixture.Downloads["repos/dotnet/aspnetcore/actions/artifacts/9001/zip"]
            $archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
            try
            {
                foreach ($expected in @(
                    @{ Name = "pulse-input.json"; Bytes = $inputBytes }
                    @{ Name = "pulse-body.md"; Bytes = $bodyBytes }
                ))
                {
                    $stream = $archive.GetEntry($expected.Name).Open()
                    $content = [IO.MemoryStream]::new()
                    try
                    {
                        $stream.CopyTo($content)
                        Assert-True ([Convert]::ToBase64String($content.ToArray()) -ceq [Convert]::ToBase64String($expected.Bytes)) "The candidate ZIP must retain the supplied $($expected.Name) bytes exactly."
                        if ($expected.Name -ceq "pulse-body.md")
                        {
                            Assert-True ([string]::Equals($utf8.GetString($content.ToArray()), $publishedBody, [StringComparison]::Ordinal)) "The archived real body must remain ordinally identical."
                        }
                    }
                    finally
                    {
                        $stream.Dispose()
                        $content.Dispose()
                    }
                }
            }
            finally
            {
                $archive.Dispose()
            }

            $actual = Invoke-RetrievalFixture $fixture
            Assert-RetrievalPreserved $fixture $actual
            Assert-True ($actual.schemaVersion -ceq "2.0.0") "The real combined schema must be accepted without replacement."
            foreach ($area in $actual.areas | Where-Object status -CEQ "complete")
            {
                Assert-True ($area.source.schemaVersion -ceq "1.0.0") "Each complete area's real source schema must survive retrieval."
            }
            $isEmbedded = $publishedBody.Contains("<summary>Snapshot JSON (exact sanitized bytes)</summary>")
            $expectedCalls = if ($isEmbedded)
            {
                # An embedded snapshot is verified directly against its checksum; no API calls are required.
                @()
            }
            else
            {
                @(
                    "GET repos/$repository/actions/runs/$runId/attempts/$runAttempt"
                    "GET repos/$repository/actions/runs/$runId/artifacts?per_page=100&page=1"
                    "GET repos/$repository/actions/artifacts/9001/zip"
                )
            }
            Assert-True (($fixture.Calls -join "`n") -ceq ($expectedCalls -join "`n")) "The documented consumer must retrieve only the supplied repository/run/attempt and exact candidate ID."
            Assert-True ([string]$fixture.Artifact.workflow_run.id -ceq $runId -and [string]$fixture.Run.run_attempt -ceq $runAttempt) "The candidate metadata must retain the supplied run tuple."
        }
    }
}
finally
{
    $resolved = (Resolve-Path -LiteralPath $fixtureRoot).Path
    $leaf = Split-Path -Leaf $resolved
    $parent = Split-Path -Parent $resolved
    Assert-True ([string]::Equals($parent, $knownParent, [StringComparison]::OrdinalIgnoreCase) -and $leaf -cmatch '^\.snapshot-retrieval-[0-9a-f]{32}$') "Cleanup must remain within the known retrieval fixture directory."
    Assert-True (-not ((Get-Item -LiteralPath $resolved).Attributes -band [IO.FileAttributes]::ReparsePoint)) "Do not recursively remove a redirected fixture directory."
    Remove-Item -LiteralPath $resolved -Recurse -Force
}

if ($retrievalFailures.Count -gt 0)
{
    throw "$($retrievalFailures.Count) of $script:retrievalCaseCount local retrieval cases failed:`n$($retrievalFailures -join "`n")"
}

Write-Output "PASS all $script:retrievalCaseCount local snapshot retrieval cases. No API calls, hosted artifact claims, or producer execution."
