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
        $integrityPassed = $executed -and $tierValid -and $reportedSurfaced -eq $derivedSurfaced

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
        }
        $details.Add((New-Detail "execution/$id" $executed "Result row was present exactly once: $executed." $common))
        $details.Add((New-Detail "surfaced/$id" $surfacedPassed "Expected surfaced=$expectedSurfaced; actual surfaced=$derivedSurfaced." $common))
        $details.Add((New-Detail "tier-justified/$id" $tierPassed "Allowed tiers: $(@($expected.allowedTiers) -join ', '); actual tier: $tier." $common))
        $details.Add((New-Detail "acceptance/$id" $acceptancePassed "Execution, surfaced, tier, and integrity checks completed." $common))
        $details.Add((New-Detail "integrity/$id" $integrityPassed "Tier was valid and surfaced agreed with the tier adapter." $common))
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
    $evidence = if ($adjudicationPending) {
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
