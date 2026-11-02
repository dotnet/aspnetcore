#requires -Version 7.0
param(
    [Parameter(Mandatory)]
    [int]$PullRequest
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

function New-Detail {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Evidence,
        [hashtable]$Metadata = @{}
    )

    return [ordered]@{
        name = $Name
        kind = 'code'
        passed = $Passed
        score = if ($Passed) { 1 } else { 0 }
        evidence = $Evidence
        metadata = $Metadata
    }
}

function New-ErrorResult {
    param([string]$Evidence)

    return [ordered]@{
        name = 'reviewer-regression'
        kind = 'code'
        status = 'error'
        passed = $false
        score = 0
        evidence = $Evidence
        details = @()
    }
}

function Get-ArtifactSections {
    param([string]$Artifact)

    $sectionNames = 'STATUS|PR|HEAD_SHA|MERGE_BASE_SHA|BASE_TIP_SHA|GUIDANCE_SOURCE|' +
        'GUIDES|UNCOVERED|PATH|NEW_FINDINGS|EXISTING_FEEDBACK_COVERAGE|UNRESOLVED|' +
        'DISCARDED|QUESTIONS?|INTENT_QUESTIONS|QUESTIONS_FOR_AUTHOR|TEST_BOUNDARY|LIMITATIONS'
    $headers = @([regex]::Matches(
        $Artifact,
        "(?m)^(?<name>$sectionNames):[ \t]*(?<inline>[^\r\n]*)"
    ))
    $sections = @{}
    for ($index = 0; $index -lt $headers.Count; $index++) {
        $header = $headers[$index]
        $name = $header.Groups['name'].Value
        $contentStart = $header.Index + $header.Length
        $contentEnd = if ($index + 1 -lt $headers.Count) {
            $headers[$index + 1].Index
        } else {
            $Artifact.Length
        }
        $content = (
            $header.Groups['inline'].Value + "`n" +
            $Artifact.Substring($contentStart, $contentEnd - $contentStart)
        ).Trim()
        if ($sections.ContainsKey($name)) {
            $sections[$name] = "$($sections[$name])`n$content"
        } else {
            $sections[$name] = $content
        }
    }
    return $sections
}

function Test-CandidateMention {
    param(
        [string]$Text,
        [object]$Candidate
    )

    $idPattern = "(?<![A-Za-z0-9])$([regex]::Escape([string]$Candidate.id))(?![A-Za-z0-9])"
    if ([regex]::IsMatch($Text, $idPattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        return $true
    }

    $pathPattern = [regex]::Escape([string]$Candidate.path).Replace('/', '[/\\]')
    $line = [int]$Candidate.line
    $anchorPattern = "$pathPattern(?:(?:\s*[:#]\s*L?$line\b)|(?:[^\r\n]{0,60}\bline\s+$line\b))"
    return [regex]::IsMatch(
        $Text,
        $anchorPattern,
        [Text.RegularExpressions.RegexOptions]::IgnoreCase
    )
}

try {
    $graderInput = Get-Content $env:EVALUATE_GRADER_INPUT -Raw | ConvertFrom-Json -Depth 100
    $labels = Get-Content (Join-Path $env:EVALUATE_WORKSPACE '.reviewer-eval/labels.json') -Raw |
        ConvertFrom-Json -Depth 100
    $output = [string]$graderInput.trajectory.output

    $match = [regex]::Match(
        $output,
        '(?ms)^REVIEWER_REGRESSION_RESULT\s*\r?\n```json\s*\r?\n(?<json>\{.*?\})\s*\r?\n```'
    )
    if (-not $match.Success) {
        New-ErrorResult 'Missing REVIEWER_REGRESSION_RESULT JSON block.' |
            ConvertTo-Json -Depth 20 -Compress
        exit 0
    }

    try {
        $result = $match.Groups['json'].Value | ConvertFrom-Json -Depth 100
    } catch {
        New-ErrorResult "Malformed REVIEWER_REGRESSION_RESULT JSON: $($_.Exception.Message)" |
            ConvertTo-Json -Depth 20 -Compress
        exit 0
    }

    $artifact = $output.Substring(0, $match.Index).TrimEnd()
    $sections = Get-ArtifactSections $artifact
    $requiredSections = @('NEW_FINDINGS', 'UNRESOLVED', 'DISCARDED')
    $missingSections = @($requiredSections | Where-Object { -not $sections.ContainsKey($_) })
    $questionSections = @($sections.Keys | Where-Object {
        $_ -match '(^|_)QUESTIONS?($|_)'
    })
    $publishedText = @(
        if ($sections.ContainsKey('NEW_FINDINGS')) {
            $sections['NEW_FINDINGS']
        }
        foreach ($name in $questionSections) {
            $sections[$name]
        }
    ) -join "`n"
    $unresolvedText = if ($sections.ContainsKey('UNRESOLVED')) {
        $sections['UNRESOLVED']
    } else {
        ''
    }
    $discardedText = if ($sections.ContainsKey('DISCARDED')) {
        $sections['DISCARDED']
    } else {
        ''
    }

    $expectedByPullRequest = @{
        70106 = @('V21', 'V32', 'V36', 'L69')
        69782 = @('V23')
    }
    if (-not $expectedByPullRequest.ContainsKey($PullRequest)) {
        throw "Unsupported pull request: $PullRequest."
    }
    $expectedIds = @($expectedByPullRequest[$PullRequest])
    $expectedRows = @($labels.rows | Where-Object id -in $expectedIds)
    if ($expectedRows.Count -ne $expectedIds.Count) {
        throw 'The hidden labels do not cover the trusted expected-case set.'
    }
    $actualRows = @($result.results)
    $actualIds = @($actualRows.id)
    $missingIds = @($expectedIds | Where-Object { $_ -notin $actualIds })
    $duplicateIds = @($actualIds | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name)
    $unexpectedIds = @($actualIds | Where-Object { $_ -notin $expectedIds })
    $validTiers = @('VERIFIED', 'QUESTION', 'PLAUSIBLE', 'DISCARDED')
    $details = [Collections.Generic.List[object]]::new()
    $executionFailed = [Collections.Generic.List[string]]::new()
    $acceptanceFailed = [Collections.Generic.List[string]]::new()
    $integrityFailed = [Collections.Generic.List[string]]::new()

    foreach ($expected in $expectedRows) {
        $id = [string]$expected.id
        $rows = @($actualRows | Where-Object id -eq $id)
        $executed = $rows.Count -eq 1
        $tier = if ($executed) { [string]$rows[0].tier } else { '' }
        $tierValid = $tier -in $validTiers
        $derivedSurfaced = $tier -in @('VERIFIED', 'QUESTION')
        $reportedSurfaced = if ($executed -and $null -ne $rows[0].surfaced) {
            [bool]$rows[0].surfaced
        } else {
            $false
        }
        $publishedMention = Test-CandidateMention $publishedText $expected
        $unresolvedMention = Test-CandidateMention $unresolvedText $expected
        $discardedMention = Test-CandidateMention $discardedText $expected
        $dispositionMentions = @(
            if ($publishedMention) { 'published' }
            if ($unresolvedMention) { 'unresolved' }
            if ($discardedMention) { 'discarded' }
        )
        $artifactMatchesTier = switch ($tier) {
            'VERIFIED' { $publishedMention }
            'QUESTION' { $publishedMention }
            'PLAUSIBLE' { $unresolvedMention }
            'DISCARDED' { $discardedMention }
            default { $false }
        }
        $artifactIntegrityPassed = $missingSections.Count -eq 0 -and
            $dispositionMentions.Count -eq 1 -and $artifactMatchesTier
        $integrityPassed = $executed -and $tierValid -and
            $reportedSurfaced -eq $derivedSurfaced -and $artifactIntegrityPassed

        $expectedSurfaced = $expected.expectedSurfaced
        $surfacedPassed = if ($null -eq $expectedSurfaced) {
            $true
        } else {
            $derivedSurfaced -eq [bool]$expectedSurfaced
        }
        $tierPassed = if (@($expected.allowedTiers).Count -eq 0) {
            $true
        } else {
            $tier -in @($expected.allowedTiers)
        }
        $acceptancePassed = if ($expected.reportOnly) {
            $executed -and $integrityPassed
        } else {
            $executed -and $integrityPassed -and $surfacedPassed -and $tierPassed
        }

        if (-not $executed) {
            $executionFailed.Add($id)
        }
        if (-not $expected.reportOnly -and -not $acceptancePassed) {
            $acceptanceFailed.Add($id)
        }
        if (-not $integrityPassed) {
            $integrityFailed.Add($id)
        }

        $common = @{
            caseId = $id
            tier = $tier
            required = [bool]$expected.required
            reportOnly = [bool]$expected.reportOnly
            enforced = -not [bool]$expected.reportOnly
            artifactDisposition = $dispositionMentions -join ','
            missingArtifactSections = $missingSections
        }
        $details.Add((New-Detail "execution/$id" $executed "Result row was present exactly once: $executed." $common))
        $details.Add((New-Detail "surfaced/$id" $surfacedPassed "Expected surfaced=$expectedSurfaced; actual surfaced=$derivedSurfaced." $common))
        $details.Add((New-Detail "tier-justified/$id" $tierPassed "Allowed tiers: $(@($expected.allowedTiers) -join ', '); actual tier: $tier." $common))
        $details.Add((New-Detail "acceptance/$id" $acceptancePassed "Execution, surfaced, tier, and integrity checks completed." $common))
        $integrityEvidence = if ($integrityPassed) {
            'The convenience block agrees with the authoritative review artifact.'
        } else {
            "INTEGRITY_FAIL: tier=$tier; artifact disposition=$($dispositionMentions -join ','); missing sections=$($missingSections -join ',')."
        }
        $details.Add((New-Detail "integrity/$id" $integrityPassed $integrityEvidence $common))
    }

    $prPassed = [int]$result.pr -eq $PullRequest
    $schemaPassed = [int]$result.schemaVersion -eq 1
    $shapePassed = $missingIds.Count -eq 0 -and $duplicateIds.Count -eq 0 -and
        $unexpectedIds.Count -eq 0 -and $prPassed -and $schemaPassed
    $details.Add((New-Detail 'integrity/result-shape' $shapePassed 'The result block must cover exactly the expected IDs.' @{
        missingIds = $missingIds
        duplicateIds = $duplicateIds
        unexpectedIds = $unexpectedIds
        expectedPullRequest = $PullRequest
        actualPullRequest = $result.pr
    }))

    $novel = @($result.novelFindings)
    $adjudicationPending = $novel.Count -gt 0
    $requiredRows = @($expectedRows | Where-Object required)
    $requiredFailed = @($requiredRows.id | Where-Object { $_ -in $acceptanceFailed })
    $passed = $executionFailed.Count -eq 0 -and $acceptanceFailed.Count -eq 0 -and
        $integrityFailed.Count -eq 0 -and $shapePassed -and -not $adjudicationPending
    $score = if ($passed) { 1 } else { 0 }
    $evidence = if ($integrityFailed.Count -gt 0) {
        "INTEGRITY_FAIL: the convenience block disagrees with the authoritative review artifact for $($integrityFailed -join ', ')."
    } elseif ($adjudicationPending) {
        'ADJUDICATION_PENDING: novel plausible findings require manual adjudication.'
    } elseif ($passed) {
        'All enforced deterministic reviewer regression checks passed.'
    } else {
        'One or more deterministic reviewer regression checks failed.'
    }

    [ordered]@{
        name = 'reviewer-regression'
        kind = 'code'
        passed = $passed
        score = $score
        evidence = $evidence
        details = $details
        metadata = [ordered]@{
            pullRequest = $PullRequest
            expectedCaseIds = $expectedIds
            execution = [ordered]@{
                expectedIds = $expectedIds
                failedIds = @($executionFailed)
                missingIds = $missingIds
            }
            acceptance = [ordered]@{
                expectedIds = $expectedIds
                failedIds = @($acceptanceFailed)
                requiredFailedIds = $requiredFailed
            }
            integrity = [ordered]@{
                expectedIds = $expectedIds
                failedIds = @($integrityFailed)
                missingIds = $missingIds
                duplicateIds = $duplicateIds
                unexpectedIds = $unexpectedIds
            }
            adjudicationPending = $adjudicationPending
            novelFindingCount = $novel.Count
        }
    } | ConvertTo-Json -Depth 100 -Compress
} catch {
    New-ErrorResult $_.Exception.Message | ConvertTo-Json -Depth 20 -Compress
}
