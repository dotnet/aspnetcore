#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$InputPath,

    [Parameter(Mandatory)]
    [string]$OutputPath,

    [Parameter(Mandatory)]
    [datetime]$AttemptTimestamp,

    [int]$CollectionExitCode = 0,

    [string]$ExpectedRepository = "dotnet/aspnetcore",

    [string]$ExpectedSchemaVersion = "1.0.0",

    [ValidateRange(4096, 262144)]
    [int]$MaxOutputBytes = 65536
)

$ErrorActionPreference = "Stop"
$pulseSchemaVersion = "1.0.0"

function Throw-ContractError
{
    param([Parameter(Mandatory)][string]$Category)

    throw [System.IO.InvalidDataException]::new($Category)
}

function Test-OrdinalEqual
{
    param(
        [AllowNull()][string]$Left,
        [AllowNull()][string]$Right
    )

    return [string]::Equals($Left, $Right, [StringComparison]::Ordinal)
}

function Test-OrdinalIn
{
    param(
        [AllowNull()][string]$Value,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$AllowedValues
    )

    foreach ($allowedValue in $AllowedValues)
    {
        if (Test-OrdinalEqual -Left $Value -Right $allowedValue)
        {
            return $true
        }
    }

    return $false
}

function Get-RequiredProperty
{
    param(
        [Parameter(Mandatory)][object]$Object,
        [Parameter(Mandatory)][string]$Name
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property)
    {
        Throw-ContractError "invalid-contract"
    }

    if ($property.Value -is [array])
    {
        Write-Output -NoEnumerate $property.Value
    }
    else
    {
        return $property.Value
    }
}

function Get-RequiredString
{
    param(
        [Parameter(Mandatory)][object]$Object,
        [Parameter(Mandatory)][string]$Name,
        [switch]$AllowEmpty
    )

    $value = Get-RequiredProperty -Object $Object -Name $Name
    if ($value -isnot [string] -or (-not $AllowEmpty -and [string]::IsNullOrWhiteSpace($value)))
    {
        Throw-ContractError "invalid-contract"
    }

    return $value
}

function Get-RequiredBoolean
{
    param(
        [Parameter(Mandatory)][object]$Object,
        [Parameter(Mandatory)][string]$Name
    )

    $value = Get-RequiredProperty -Object $Object -Name $Name
    if ($value -isnot [bool])
    {
        Throw-ContractError "invalid-contract"
    }

    return $value
}

function Get-RequiredInteger
{
    param(
        [Parameter(Mandatory)][object]$Object,
        [Parameter(Mandatory)][string]$Name,
        [int]$Minimum = 0
    )

    $value = Get-RequiredProperty -Object $Object -Name $Name
    if ($value -isnot [byte] -and
        $value -isnot [int16] -and
        $value -isnot [int32] -and
        $value -isnot [int64])
    {
        Throw-ContractError "invalid-contract"
    }

    $result = [int64]$value
    if ($result -lt $Minimum -or $result -gt [int]::MaxValue)
    {
        Throw-ContractError "invalid-contract"
    }

    return [int]$result
}

function Get-RequiredNullableInteger
{
    param(
        [Parameter(Mandatory)][object]$Object,
        [Parameter(Mandatory)][string]$Name,
        [int]$Minimum = 0
    )

    $value = Get-RequiredProperty -Object $Object -Name $Name
    if ($null -eq $value)
    {
        return $null
    }

    if ($value -isnot [byte] -and
        $value -isnot [int16] -and
        $value -isnot [int32] -and
        $value -isnot [int64])
    {
        Throw-ContractError "invalid-contract"
    }

    $result = [int64]$value
    if ($result -lt $Minimum -or $result -gt [int]::MaxValue)
    {
        Throw-ContractError "invalid-contract"
    }

    return [int]$result
}

function Get-RequiredArray
{
    param(
        [Parameter(Mandatory)][object]$Object,
        [Parameter(Mandatory)][string]$Name,
        [ValidateRange(0, 10000)][int]$MaximumCount = 10000
    )

    $value = Get-RequiredProperty -Object $Object -Name $Name
    if ($value -isnot [array] -or $value.Count -gt $MaximumCount)
    {
        Throw-ContractError "invalid-contract"
    }

    Write-Output -NoEnumerate $value
}

function ConvertTo-SafeDisplayText
{
    param(
        [Parameter(Mandatory)][string]$Value,
        [ValidateRange(1, 2000)][int]$MaximumLength,
        [switch]$AllowEmpty
    )

    $text = $Value.Normalize([Text.NormalizationForm]::FormKC)
    $text = [regex]::Replace($text, "(?i)\b[A-Za-z][A-Za-z0-9+.-]*://\S+|\bwww\.\S+", "[link removed]")
    $text = [regex]::Replace($text, "(?i)(?<![\w.-])(?:[A-Z0-9-]+\.)+[A-Z]{2,}(?::[0-9]+)?(?:[/#?]\S*)?", "[link removed]")
    $text = [regex]::Replace($text, "(?i)\bGH-([0-9]+)\b", "GH number `$1")
    $text = [regex]::Replace($text, "(?i)(?<![0-9A-Z])(?=[0-9A-F]{7,40}(?![0-9A-Z]))(?=[0-9A-F]{0,39}[A-F])(?=[0-9A-F]{0,39}[0-9])[0-9A-F]{7,40}(?![0-9A-Z])", "[reference removed]")
    $text = [regex]::Replace($text, "[\u0000-\u001F\u007F]+", " ")
    $text = [regex]::Replace($text, "\s+", " ").Trim()
    $text = $text.Replace([char]96, [char]39)
    $text = $text.Replace("\", "/")
    $text = $text.Replace("@", "(at)")
    $text = $text.Replace("#", "number ")
    $text = $text.Replace("[", "(").Replace("]", ")")
    $text = $text.Replace("<", "(").Replace(">", ")")
    $text = $text.Replace("|", "/")
    $text = $text.Replace("*", "").Replace("_", "-").Replace("~", "")
    $text = $text.Replace("&", "and")
    $text = [regex]::Replace($text, "\s+", " ").Trim()

    if (-not $AllowEmpty -and [string]::IsNullOrWhiteSpace($text))
    {
        Throw-ContractError "invalid-contract"
    }

    if ($text.Length -gt $MaximumLength)
    {
        $text = $text.Substring(0, $MaximumLength - 3).TrimEnd() + "..."
    }

    return $text
}

function ConvertTo-SafeAuthor
{
    param([Parameter(Mandatory)][string]$Value)

    $author = $Value.Trim().TrimStart("@")
    if ($author -notmatch "^[A-Za-z0-9](?:[A-Za-z0-9-]{0,38})$")
    {
        Throw-ContractError "invalid-contract"
    }

    return $author
}

function ConvertTo-StableCodes
{
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Values,
        [ValidateRange(0, 100)][int]$MaximumCount = 20
    )

    if ($Values.Count -gt $MaximumCount)
    {
        Throw-ContractError "invalid-contract"
    }

    $result = @(
        foreach ($value in $Values)
        {
            if ($value -isnot [string] -or $value -cnotmatch "^[a-z0-9-]+$")
            {
                Throw-ContractError "invalid-contract"
            }

            $value
        }
    )

    Write-Output -NoEnumerate $result
}

function ConvertTo-SafeDisplayArray
{
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Values,
        [ValidateRange(0, 100)][int]$MaximumCount,
        [ValidateRange(1, 2000)][int]$MaximumLength
    )

    if ($Values.Count -gt $MaximumCount)
    {
        Throw-ContractError "invalid-contract"
    }

    $result = @(
        foreach ($value in $Values)
        {
            if ($value -isnot [string])
            {
                Throw-ContractError "invalid-contract"
            }

            ConvertTo-SafeDisplayText -Value $value -MaximumLength $MaximumLength
        }
    )

    Write-Output -NoEnumerate $result
}

function ConvertTo-IsoTimestamp
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
        Throw-ContractError "invalid-contract"
    }

    return $timestamp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", [Globalization.CultureInfo]::InvariantCulture)
}

function New-FailureEnvelope
{
    param([Parameter(Mandatory)][string]$ErrorCategory)

    return [ordered]@{
        schemaVersion = $pulseSchemaVersion
        status = "unavailable"
        attemptedAt = $AttemptTimestamp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", [Globalization.CultureInfo]::InvariantCulture)
        source = [ordered]@{
            repository = $ExpectedRepository
        }
        errorCategory = $ErrorCategory
        candidateCountsAvailable = $false
    }
}

function ConvertTo-SanitizedDiscussion
{
    param([Parameter(Mandatory)][object]$Assessment)

    $threads = Get-RequiredProperty -Object $Assessment -Name "threads"
    return [ordered]@{
        state = ConvertTo-SafeDisplayText `
            -Value (Get-RequiredString -Object $Assessment -Name "state") `
            -MaximumLength 80
        complete = Get-RequiredBoolean -Object $Assessment -Name "complete"
        signals = ConvertTo-StableCodes `
            -Values (Get-RequiredArray -Object $Assessment -Name "signals" -MaximumCount 20)
        commentTotalCount = Get-RequiredInteger -Object $Assessment -Name "commentTotalCount"
        commentEvidenceTruncated = Get-RequiredBoolean -Object $Assessment -Name "commentEvidenceTruncated"
        threads = [ordered]@{
            totalCount = Get-RequiredInteger -Object $threads -Name "totalCount"
            returnedCount = Get-RequiredInteger -Object $threads -Name "returnedCount"
            complete = Get-RequiredBoolean -Object $threads -Name "complete"
            unresolvedCount = Get-RequiredInteger -Object $threads -Name "unresolvedCount"
            outdatedUnresolvedCount = Get-RequiredInteger -Object $threads -Name "outdatedUnresolvedCount"
        }
    }
}

function ConvertTo-SanitizedItem
{
    param(
        [Parameter(Mandatory)][object]$Item,
        [Parameter(Mandatory)][string]$RankProperty,
        [switch]$IncludeDiscussion
    )

    $number = Get-RequiredInteger -Object $Item -Name "number" -Minimum 1
    $rank = Get-RequiredNullableInteger -Object $Item -Name $RankProperty -Minimum 1
    if ($null -eq $rank)
    {
        Throw-ContractError "invalid-contract"
    }

    $result = [ordered]@{
        number = $number
        title = ConvertTo-SafeDisplayText `
            -Value (Get-RequiredString -Object $Item -Name "title") `
            -MaximumLength 240
        author = ConvertTo-SafeAuthor -Value (Get-RequiredString -Object $Item -Name "author")
        bucket = ConvertTo-SafeDisplayText `
            -Value (Get-RequiredString -Object $Item -Name "bucket") `
            -MaximumLength 80
        rank = $rank
        nextActor = ConvertTo-SafeDisplayText `
            -Value (Get-RequiredString -Object $Item -Name "nextActor") `
            -MaximumLength 100
        reasonCodes = ConvertTo-StableCodes `
            -Values (Get-RequiredArray -Object $Item -Name "reasonCodes" -MaximumCount 20)
        blockers = ConvertTo-SafeDisplayArray `
            -Values (Get-RequiredArray -Object $Item -Name "blockers" -MaximumCount 20) `
            -MaximumCount 20 `
            -MaximumLength 300
        ageDays = Get-RequiredInteger -Object $Item -Name "ageDays"
        idleDays = Get-RequiredInteger -Object $Item -Name "idleDays"
        scopeMatch = ConvertTo-SafeDisplayText `
            -Value (Get-RequiredString -Object $Item -Name "scopeMatch") `
            -MaximumLength 80
    }

    if ($IncludeDiscussion)
    {
        $assessmentSource = Get-RequiredProperty -Object $Item -Name "discussionAssessment"
        if ($null -eq $assessmentSource)
        {
            Throw-ContractError "invalid-contract"
        }

        $result["discussionAssessment"] = ConvertTo-SanitizedDiscussion -Assessment $assessmentSource
    }

    return $result
}

function ConvertTo-SanitizedView
{
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Items,
        [Parameter(Mandatory)][string]$Bucket,
        [Parameter(Mandatory)][string]$ShownProperty,
        [Parameter(Mandatory)][string]$RankProperty,
        [Parameter(Mandatory)][int]$MaximumCount,
        [switch]$IncludeDiscussion
    )

    $selected = @(
        foreach ($item in $Items)
        {
            $itemBucket = Get-RequiredString -Object $item -Name "bucket"
            $isShown = Get-RequiredBoolean -Object $item -Name $ShownProperty
            $null = Get-RequiredNullableInteger -Object $item -Name $RankProperty

            if ($isShown -and (Test-OrdinalEqual -Left $itemBucket -Right $Bucket))
            {
                ConvertTo-SanitizedItem -Item $item -RankProperty $RankProperty -IncludeDiscussion:$IncludeDiscussion
            }
        }
    )

    if ($selected.Count -gt $MaximumCount)
    {
        Throw-ContractError "invalid-contract"
    }

    $duplicateRanks = @($selected | Group-Object { $_["rank"] } | Where-Object Count -gt 1)
    if ($duplicateRanks.Count -gt 0)
    {
        Throw-ContractError "invalid-contract"
    }

    $result = @($selected | Sort-Object { $_["rank"] })
    for ($index = 0; $index -lt $result.Count; $index++)
    {
        if ($result[$index]["rank"] -ne ($index + 1))
        {
            Throw-ContractError "invalid-contract"
        }
    }

    Write-Output -NoEnumerate $result
}

function ConvertTo-SanitizedCensus
{
    param([Parameter(Mandatory)][object]$Census)

    $byBucketSource = Get-RequiredProperty -Object $Census -Name "byBucket"
    $byBucket = [ordered]@{}
    foreach ($bucketName in @(
        "ReviewNow",
        "NeedsRescue",
        "ReadyToMerge",
        "WaitingOnAuthor",
        "WaitingOnCI",
        "DesignDecision",
        "Draft",
        "Excluded"))
    {
        $byBucket[$bucketName] = Get-RequiredInteger -Object $byBucketSource -Name $bucketName
    }

    return [ordered]@{
        openPullRequests = Get-RequiredInteger -Object $Census -Name "openPullRequests"
        matched = Get-RequiredInteger -Object $Census -Name "matched"
        labelOnly = Get-RequiredInteger -Object $Census -Name "labelOnly"
        pathOnly = Get-RequiredInteger -Object $Census -Name "pathOnly"
        labelAndPath = Get-RequiredInteger -Object $Census -Name "labelAndPath"
        incidentalPathExcluded = Get-RequiredInteger -Object $Census -Name "incidentalPathExcluded"
        unresolvedMergeable = Get-RequiredInteger -Object $Census -Name "unresolvedMergeable"
        byBucket = $byBucket
    }
}

function ConvertTo-SanitizedResult
{
    param([Parameter(Mandatory)][object]$Raw)

    if (-not (Test-OrdinalEqual -Left (Get-RequiredString -Object $Raw -Name "schemaVersion") -Right $ExpectedSchemaVersion))
    {
        Throw-ContractError "incompatible-schema"
    }

    if (-not (Test-OrdinalEqual -Left (Get-RequiredString -Object $Raw -Name "repository") -Right $ExpectedRepository))
    {
        Throw-ContractError "unexpected-repository"
    }

    $query = Get-RequiredProperty -Object $Raw -Name "query"
    if (-not (Get-RequiredBoolean -Object $query -Name "complete"))
    {
        Throw-ContractError "incomplete-query"
    }

    $filter = Get-RequiredProperty -Object $Raw -Name "filter"
    if (-not (Get-RequiredBoolean -Object $filter -Name "allRepositoryPullRequests") -or
        -not (Test-OrdinalEqual -Left (Get-RequiredString -Object $filter -Name "coverage") -Right "all-repo"))
    {
        Throw-ContractError "invalid-contract"
    }

    $items = Get-RequiredArray -Object $Raw -Name "items" -MaximumCount 10000
    $knownBuckets = @(
        "ReviewNow",
        "NeedsRescue",
        "ReadyToMerge",
        "WaitingOnAuthor",
        "WaitingOnCI",
        "DesignDecision",
        "Draft",
        "Excluded"
    )
    $reviewNowCandidateCount = 0
    $assessedCandidateCount = 0
    $verificationNeededCount = 0
    $unassessedReviewNowCount = 0
    $itemNumbers = @(
        foreach ($item in $items)
        {
            $bucket = Get-RequiredString -Object $item -Name "bucket"
            if (-not (Test-OrdinalIn -Value $bucket -AllowedValues $knownBuckets))
            {
                Throw-ContractError "invalid-contract"
            }

            $shownInDigest = Get-RequiredBoolean -Object $item -Name "shownInDigest"
            $digestRank = Get-RequiredNullableInteger -Object $item -Name "digestRank" -Minimum 1
            if ($shownInDigest -ne ($null -ne $digestRank) -or
                ($shownInDigest -and -not (Test-OrdinalIn -Value $bucket -AllowedValues @("ReviewNow", "NeedsRescue", "ReadyToMerge"))))
            {
                Throw-ContractError "invalid-contract"
            }

            $shownInDiscussion = Get-RequiredBoolean -Object $item -Name "shownInDiscussionVerification"
            $discussionRank = Get-RequiredNullableInteger -Object $item -Name "discussionVerificationRank" -Minimum 1
            $assessment = Get-RequiredProperty -Object $item -Name "discussionAssessment"
            $assessmentState = $null
            if (Test-OrdinalEqual -Left $bucket -Right "ReviewNow")
            {
                $reviewNowCandidateCount++
                if ($null -ne $assessment)
                {
                    $null = ConvertTo-SanitizedDiscussion -Assessment $assessment
                    $assessmentState = Get-RequiredString -Object $assessment -Name "state"
                    if (-not (Test-OrdinalIn -Value $assessmentState -AllowedValues @("clear", "actionable", "verification-needed", "not-assessed")))
                    {
                        Throw-ContractError "invalid-contract"
                    }
                    if (Test-OrdinalEqual -Left $assessmentState -Right "not-assessed")
                    {
                        $unassessedReviewNowCount++
                    }
                    else
                    {
                        $assessedCandidateCount++
                    }
                    if (Test-OrdinalEqual -Left $assessmentState -Right "verification-needed")
                    {
                        $verificationNeededCount++
                    }
                }
            }
            elseif ($null -ne $assessment)
            {
                Throw-ContractError "invalid-contract"
            }
            if ($shownInDiscussion -ne ($null -ne $discussionRank) -or
                ($shownInDiscussion -and -not (Test-OrdinalEqual -Left $assessmentState -Right "verification-needed")) -or
                ($shownInDigest -and
                    (Test-OrdinalEqual -Left $bucket -Right "ReviewNow") -and
                    (Test-OrdinalIn -Value $assessmentState -AllowedValues @("verification-needed", "not-assessed"))))
            {
                Throw-ContractError "invalid-contract"
            }

            Get-RequiredInteger -Object $item -Name "number" -Minimum 1
        }
    )
    if (@($itemNumbers | Select-Object -Unique).Count -ne $itemNumbers.Count)
    {
        Throw-ContractError "invalid-contract"
    }

    $returnedCount = Get-RequiredInteger -Object $query -Name "returnedPullRequestCount"
    $openCount = Get-RequiredInteger -Object $query -Name "openPullRequestCount"
    if ($returnedCount -ne $items.Count -or $openCount -ne $returnedCount)
    {
        Throw-ContractError "invalid-contract"
    }

    $capsSource = Get-RequiredProperty -Object $Raw -Name "caps"
    $caps = [ordered]@{
        reviewNow = Get-RequiredInteger -Object $capsSource -Name "reviewNow"
        needsRescue = Get-RequiredInteger -Object $capsSource -Name "needsRescue"
        readyToMerge = Get-RequiredInteger -Object $capsSource -Name "readyToMerge"
        reviewNowPerAuthor = Get-RequiredInteger -Object $capsSource -Name "reviewNowPerAuthor"
    }

    $discussion = Get-RequiredProperty -Object $Raw -Name "discussion"
    $discussionCandidateLimit = Get-RequiredInteger -Object $discussion -Name "candidateLimit"
    $sourceAssessedCandidateCount = Get-RequiredInteger -Object $discussion -Name "assessedCandidateCount"
    $sourceVerificationNeededCount = Get-RequiredInteger -Object $discussion -Name "verificationNeededCount"
    $sourceUnassessedReviewNowCount = Get-RequiredInteger -Object $discussion -Name "unassessedReviewNowCount"
    if ($sourceAssessedCandidateCount -ne $assessedCandidateCount -or
        $sourceAssessedCandidateCount -gt $discussionCandidateLimit -or
        $sourceVerificationNeededCount -ne $verificationNeededCount -or
        $sourceUnassessedReviewNowCount -ne $unassessedReviewNowCount -or
        ($sourceAssessedCandidateCount + $sourceUnassessedReviewNowCount) -gt $reviewNowCandidateCount)
    {
        Throw-ContractError "invalid-contract"
    }

    $reviewNow = ConvertTo-SanitizedView `
        -Items $items `
        -Bucket "ReviewNow" `
        -ShownProperty "shownInDigest" `
        -RankProperty "digestRank" `
        -MaximumCount $caps.reviewNow
    $verifyDiscussion = ConvertTo-SanitizedView `
        -Items $items `
        -Bucket "ReviewNow" `
        -ShownProperty "shownInDiscussionVerification" `
        -RankProperty "discussionVerificationRank" `
        -MaximumCount $discussionCandidateLimit `
        -IncludeDiscussion
    $needsRescue = ConvertTo-SanitizedView `
        -Items $items `
        -Bucket "NeedsRescue" `
        -ShownProperty "shownInDigest" `
        -RankProperty "digestRank" `
        -MaximumCount $caps.needsRescue
    $readyToMerge = ConvertTo-SanitizedView `
        -Items $items `
        -Bucket "ReadyToMerge" `
        -ShownProperty "shownInDigest" `
        -RankProperty "digestRank" `
        -MaximumCount $caps.readyToMerge

    $selectedNumbers = @(
        @($reviewNow) + @($verifyDiscussion) + @($needsRescue) + @($readyToMerge) |
            ForEach-Object { $_["number"] }
    )
    if (@($selectedNumbers | Select-Object -Unique).Count -ne $selectedNumbers.Count)
    {
        Throw-ContractError "invalid-contract"
    }

    if ($verifyDiscussion.Count -gt $sourceVerificationNeededCount)
    {
        Throw-ContractError "invalid-contract"
    }

    $census = ConvertTo-SanitizedCensus -Census (Get-RequiredProperty -Object $Raw -Name "census")
    if ($census.openPullRequests -ne $openCount -or $census.matched -ne $items.Count)
    {
        Throw-ContractError "invalid-contract"
    }
    $bucketTotal = 0
    foreach ($bucketCount in $census.byBucket.Values)
    {
        $bucketTotal += $bucketCount
    }
    if ($bucketTotal -ne $census.matched)
    {
        Throw-ContractError "invalid-contract"
    }

    $overflowSource = Get-RequiredProperty -Object $Raw -Name "overflow"
    $warnings = ConvertTo-SafeDisplayArray `
        -Values (Get-RequiredArray -Object $Raw -Name "warnings" -MaximumCount 20) `
        -MaximumCount 20 `
        -MaximumLength 500

    return [ordered]@{
        schemaVersion = $pulseSchemaVersion
        status = "complete"
        attemptedAt = $AttemptTimestamp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", [Globalization.CultureInfo]::InvariantCulture)
        candidateCountsAvailable = $true
        source = [ordered]@{
            repository = $ExpectedRepository
            schemaVersion = $ExpectedSchemaVersion
            generatedAt = ConvertTo-IsoTimestamp -Value (Get-RequiredProperty -Object $Raw -Name "generatedAt")
            query = [ordered]@{
                openPullRequestCount = $openCount
                returnedPullRequestCount = $returnedCount
                complete = $true
            }
            filter = [ordered]@{
                name = ConvertTo-SafeDisplayText `
                    -Value (Get-RequiredString -Object $filter -Name "name") `
                    -MaximumLength 80
                description = ConvertTo-SafeDisplayText `
                    -Value (Get-RequiredString -Object $filter -Name "description") `
                    -MaximumLength 200
                coverage = "all-repo"
                selection = ConvertTo-SafeDisplayText `
                    -Value (Get-RequiredString -Object $filter -Name "selection") `
                    -MaximumLength 200
                allRepositoryPullRequests = $true
            }
            discussion = [ordered]@{
                candidateLimit = $discussionCandidateLimit
                assessedCandidateCount = $sourceAssessedCandidateCount
                verificationNeededCount = $sourceVerificationNeededCount
                unassessedReviewNowCount = $sourceUnassessedReviewNowCount
            }
            census = $census
            overflow = [ordered]@{
                reviewNow = Get-RequiredInteger -Object $overflowSource -Name "reviewNow"
                needsRescue = Get-RequiredInteger -Object $overflowSource -Name "needsRescue"
                readyToMerge = Get-RequiredInteger -Object $overflowSource -Name "readyToMerge"
            }
            caps = $caps
            warnings = $warnings
        }
        views = [ordered]@{
            reviewNow = @($reviewNow)
            verifyDiscussionBeforeReview = @($verifyDiscussion)
            needsRescue = @($needsRescue)
            readyToMerge = @($readyToMerge)
        }
    }
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory))
{
    New-Item -ItemType Directory -Force $outputDirectory | Out-Null
}

$result = $null
try
{
    if ($CollectionExitCode -ne 0)
    {
        $result = New-FailureEnvelope -ErrorCategory "collection-failed"
    }
    elseif (-not (Test-Path -LiteralPath $InputPath))
    {
        $result = New-FailureEnvelope -ErrorCategory "collection-output-missing"
    }
    else
    {
        try
        {
            $raw = Get-Content -LiteralPath $InputPath -Raw | ConvertFrom-Json -Depth 100
        }
        catch
        {
            $result = New-FailureEnvelope -ErrorCategory "malformed-json"
        }

        if ($null -eq $result)
        {
            try
            {
                $result = ConvertTo-SanitizedResult -Raw $raw
            }
            catch [System.IO.InvalidDataException]
            {
                $result = New-FailureEnvelope -ErrorCategory $_.Exception.Message
            }
            catch
            {
                $result = New-FailureEnvelope -ErrorCategory "invalid-contract"
            }
        }
    }
}
finally
{
    Remove-Item -LiteralPath $InputPath -Force -ErrorAction SilentlyContinue
}

$json = $result | ConvertTo-Json -Depth 20 -Compress
if ([Text.Encoding]::UTF8.GetByteCount($json) -gt $MaxOutputBytes)
{
    $result = New-FailureEnvelope -ErrorCategory "sanitized-output-too-large"
    $json = $result | ConvertTo-Json -Depth 10 -Compress
}

[IO.File]::WriteAllText($OutputPath, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
