#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deterministic, read-only collector for exactly one open dotnet/aspnetcore test-quarantine issue.

.DESCRIPTION
    Gathers public evidence for a single quarantine issue -- the issue body itself, Azure DevOps
    build metadata, GitHub "Build Analysis" check-run snapshots (corroborating only, never
    authoritative), and capped/redacted raw Helix evidence -- then emits either:
      * a "candidate" object that independently satisfies test-quarantine-kbe-shadow-candidate.schema.json
        and is ready for Evaluate-TestQuarantineKbeCandidate.ps1, or
      * a structured "incomplete" outcome explaining exactly which evidence could not be
        established, without inferring a pass, a recurrence, or a signature from anything missing
        or expired.

    This script makes no repository-state mutations. It only reads public GitHub/Azure DevOps/Helix
    endpoints (or, in fixture mode, a local fixture file) and writes local files: the dossier, an
    optional candidate, and capped evidence text files under -EvidenceRoot.

.PARAMETER IssueNumber
    The dotnet/aspnetcore issue number to evaluate. Must be the canonical, currently open
    test-quarantine issue for the test(s) it names.

.PARAMETER Signature
    Optional manual failure-signature override. Supply this when the issue body does not contain
    a deterministically extractable "## Error Message" (or equivalent) code block. When omitted and
    extraction is ambiguous, the collector fails closed with reason code
    'signature-extraction-ambiguous' rather than guessing.

.PARAMETER OutputFile
    Path to write the dossier JSON (always written, whether the outcome is 'candidate' or
    'incomplete').

.PARAMETER CandidateFile
    Path to write the candidate JSON. Only written when the dossier outcome is 'candidate'.

.PARAMETER EvidenceRoot
    Directory to materialize capped/redacted raw evidence text files into. Created if missing.

.PARAMETER FixtureRoot
    Optional path to a directory containing a single consolidated 'fixture.json' file that stands
    in for every live network call (GitHub issue/check-runs/search, Azure DevOps build metadata,
    Helix evidence). Used by the test harness and by the shadow workflow's self-test mode so this
    script's deterministic logic can be exercised with zero network access. See README.md for the
    fixture.json shape.

.PARAMETER GitHubToken
    Optional GitHub token for the GitHub REST calls (issue, check-runs, search). Falls back to the
    GITHUB_TOKEN environment variable, then to unauthenticated requests.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [int]$IssueNumber,

    [string]$Signature,

    [Parameter(Mandatory = $true)]
    [string]$OutputFile,

    [Parameter(Mandatory = $true)]
    [string]$CandidateFile,

    [Parameter(Mandatory = $true)]
    [string]$EvidenceRoot,

    [string]$FixtureRoot,

    [string]$GitHubToken = $env:GITHUB_TOKEN,

    [string]$Repository = "dotnet/aspnetcore",

    [string]$RepositoryRoot = "$PSScriptRoot/../../../..",

    [string]$DossierSchemaFile = "$PSScriptRoot/test-quarantine-kbe-shadow-dossier.schema.json",

    [string]$CandidateSchemaFile = "$PSScriptRoot/test-quarantine-kbe-shadow-candidate.schema.json",

    [int]$RecurrenceScanBuildCap = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ado = "https://dev.azure.com/dnceng-public/public/_apis"
$vstmr = "https://vstmr.dev.azure.com/dnceng-public/public/_apis"
$helix = "https://helix.dot.net/api/2019-06-17"
$pipelineDefinitionIds = @(83, 87)
$canonicalQuarantineLabel = "test-failure"
$minimumFailureBuilds = 2
$minimumNegativeLogs = 1
$excerptCap = 2000
$rawLogCap = 8000

# Same secret-shaped patterns the production test-quarantine.md deterministic collector scrubs
# before surfacing captured CI text. Helix work-item upload steps can log a live Azure DevOps
# bearer JWT on failure; the raw evidence gathered here is untrusted input and must never leak
# a live credential into an uploaded artifact.
$secretPatterns = @(
    [regex]'eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{6,}\.[A-Za-z0-9_-]{6,}'
    [regex]'eyJ[A-Za-z0-9_-]{20,}'
    [regex]'\bgh[pousr]_[A-Za-z0-9]{20,}\b'
    [regex]'\bgithub_pat_[A-Za-z0-9_]{20,}\b'
    [regex]'(?i)\bbearer\s+[A-Za-z0-9._~+/=-]{20,}'
    [regex]'(?i)\bhttps?://[^/\s:@"]+:[^@\s/"]{6,}@'
    [regex]'(?i)[?&]sig=[A-Za-z0-9%/+_=-]{20,}'
    [regex]'(?i)\b(?:AccountKey|SharedAccessKey|AccessKey|Password|Pwd)=[^;\s"'']{12,}'
    [regex]'[A-Za-z0-9][A-Za-z0-9+/=_-]{51,}'
)

function ConvertTo-Redacted
{
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value)

    $result = $Value
    foreach ($pattern in $secretPatterns)
    {
        $result = $pattern.Replace($result, "[REDACTED]")
    }

    return $result
}

function ConvertTo-Iso8601String
{
    # ConvertFrom-Json (and Invoke-RestMethod, which uses it internally) auto-converts
    # ISO-8601-shaped JSON string values into [datetime] instances. A bare [string] cast
    # on one of those then renders using the current culture (e.g. "08/22/2026 03:28:01"),
    # silently dropping the 'Z' suffix and sub-second precision and producing a value that
    # is no longer schema-valid 'date-time' text. Route every timestamp read from parsed
    # JSON through this so the emitted dossier/candidate always carries a real ISO-8601
    # UTC string regardless of whether the runtime parsed it into a DateTime or left it
    # as a string.
    param([AllowNull()]$Value)

    if ($null -eq $Value)
    {
        return $null
    }
    if ($Value -is [datetime])
    {
        return $Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
    }
    return [string]$Value
}

function Get-Sha256String
{
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)

    return [System.Convert]::ToHexString($hash).ToLowerInvariant()
}

function Get-CappedExcerpt
{
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value,
        [int]$Cap = $excerptCap,
        # Phrases (e.g. the quarantined test's fully qualified name) that must survive
        # redaction verbatim. The "long high-entropy run" secret pattern intentionally
        # has no notion of word structure -- a descriptive xUnit test name easily exceeds
        # its 52-character threshold and would otherwise be redacted to "[REDACTED]",
        # silently destroying the exact marker Evaluate-TestQuarantineKbeCandidate.ps1
        # needs to associate a failure line with this test. Each phrase is swapped for a
        # private-use-area sentinel before redaction runs and restored immediately after.
        [string[]]$ProtectedPhrases = @()
    )

    $working = $Value
    $placeholders = [ordered]@{}
    $tokenIndex = 0
    foreach ($phrase in $ProtectedPhrases)
    {
        if ([string]::IsNullOrEmpty($phrase) -or -not $working.Contains($phrase))
        {
            continue
        }
        $token = "`u{E000}PROTECTED-$tokenIndex`u{E000}"
        $placeholders[$token] = $phrase
        $working = $working.Replace($phrase, $token)
        $tokenIndex++
    }

    $redacted = ConvertTo-Redacted -Value $working
    foreach ($token in $placeholders.Keys)
    {
        $redacted = $redacted.Replace($token, [string]$placeholders[$token])
    }

    $normalized = [System.Text.RegularExpressions.Regex]::Replace($redacted, "[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "?")
    if ($normalized.Length -gt $Cap)
    {
        $normalized = $normalized.Substring(0, $Cap)
    }

    return $normalized
}

function Add-MissingEvidence
{
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.Generic.List[object]]$List,
        [Parameter(Mandatory = $true)][string]$Kind,
        [Parameter(Mandatory = $true)][string]$Detail
    )

    $null = $List.Add([ordered]@{ kind = $Kind; detail = $Detail })
}

# ---------------------------------------------------------------------------
# Fixture / live network abstraction. Fixture mode reads one consolidated JSON
# document; live mode calls the public GitHub/Azure DevOps/Helix REST APIs
# documented in test-quarantine.md's "API Reference (Azure DevOps & Helix)".
# ---------------------------------------------------------------------------

$isFixtureMode = -not [string]::IsNullOrEmpty($FixtureRoot)
$fixture = $null
if ($isFixtureMode)
{
    $fixturePath = Join-Path $FixtureRoot "fixture.json"
    if (-not (Test-Path -LiteralPath $fixturePath))
    {
        throw "Fixture mode requested but '$fixturePath' does not exist."
    }
    $fixture = Get-Content -LiteralPath $fixturePath -Raw | ConvertFrom-Json -Depth 32
}

function Get-GitHubIssue
{
    param([Parameter(Mandatory = $true)][int]$Number)

    if ($isFixtureMode)
    {
        if ([int]$fixture.issue.number -ne $Number)
        {
            throw "Fixture issue number $($fixture.issue.number) does not match requested issue $Number."
        }
        return $fixture.issue
    }

    $headers = @{ Accept = "application/vnd.github+json"; "User-Agent" = "aspnetcore-test-quarantine-kbe-shadow" }
    if (-not [string]::IsNullOrEmpty($GitHubToken))
    {
        $headers["Authorization"] = "Bearer $GitHubToken"
    }
    return Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/issues/$Number" -Headers $headers -Method Get -TimeoutSec 30
}

function Get-AzdoBuild
{
    param([Parameter(Mandatory = $true)][int]$BuildId)

    if ($isFixtureMode)
    {
        $key = [string]$BuildId
        if ($fixture.azdo_builds.PSObject.Properties.Name -contains $key)
        {
            return $fixture.azdo_builds.$key
        }
        return $null
    }

    try
    {
        return Invoke-RestMethod -Uri "$ado/build/builds/${BuildId}?api-version=7.1" -Method Get -TimeoutSec 30
    }
    catch
    {
        return $null
    }
}

function Get-AzdoRecurrenceCandidateBuilds
{
    param([Parameter(Mandatory = $true)][int]$DefinitionId)

    if ($isFixtureMode)
    {
        $key = [string]$DefinitionId
        if ($fixture.recurrence_scan.PSObject.Properties.Name -contains $key)
        {
            return @($fixture.recurrence_scan.$key)
        }
        return @()
    }

    try
    {
        $result = Invoke-RestMethod -Uri "$ado/build/builds?definitions=$DefinitionId&branchName=refs/heads/main&resultFilter=failed&`$top=$RecurrenceScanBuildCap&api-version=7.1" -Method Get -TimeoutSec 30
        return @($result.value)
    }
    catch
    {
        return @()
    }
}

function Get-AzdoNegativeCandidateBuilds
{
    param([Parameter(Mandatory = $true)][int]$DefinitionId)

    if ($isFixtureMode)
    {
        $key = [string]$DefinitionId
        if ($fixture.negative_scan.PSObject.Properties.Name -contains $key)
        {
            return @($fixture.negative_scan.$key)
        }
        return @()
    }

    try
    {
        $result = Invoke-RestMethod -Uri "$ado/build/builds?definitions=$DefinitionId&branchName=refs/heads/main&resultFilter=succeeded&`$top=$RecurrenceScanBuildCap&api-version=7.1" -Method Get -TimeoutSec 30
        return @($result.value)
    }
    catch
    {
        return @()
    }
}

function Get-VstmrTestOutcome
{
    param(
        [Parameter(Mandatory = $true)][int]$BuildId,
        [Parameter(Mandatory = $true)][string]$TestName
    )

    if ($isFixtureMode)
    {
        $key = "$BuildId"
        if ($fixture.vstmr_results.PSObject.Properties.Name -contains $key)
        {
            return $fixture.vstmr_results.$key
        }
        return $null
    }

    try
    {
        $result = Invoke-RestMethod -Uri "$vstmr/testresults/resultsbyBuild?buildId=$BuildId&api-version=7.1-preview.1" -Method Get -TimeoutSec 60
        $match = @($result.value) | Where-Object { $_.automatedTestName -eq $TestName -or $_.testCaseTitle -eq $TestName } | Select-Object -First 1
        if ($null -eq $match)
        {
            return $null
        }
        return [ordered]@{
            outcome = $match.outcome
            comment = $match.comment
            errorMessage = $match.errorMessage
            stackTrace = $match.stackTrace
        }
    }
    catch
    {
        return $null
    }
}

function Get-HelixEvidence
{
    param(
        [Parameter(Mandatory = $true)][int]$BuildId,
        [string]$HelixJob,
        [string]$HelixWorkItem
    )

    if ($isFixtureMode)
    {
        $key = "$BuildId"
        if ($fixture.helix_evidence.PSObject.Properties.Name -contains $key)
        {
            return $fixture.helix_evidence.$key
        }
        return $null
    }

    if ([string]::IsNullOrEmpty($HelixJob) -or [string]::IsNullOrEmpty($HelixWorkItem))
    {
        return $null
    }

    try
    {
        $files = Invoke-RestMethod -Uri "$helix/jobs/$HelixJob/workitems/$HelixWorkItem/files" -Method Get -TimeoutSec 60
        $consoleFile = @($files) | Where-Object { $_.Name -like "console.*" } | Select-Object -First 1
        if ($null -eq $consoleFile)
        {
            return [ordered]@{ found = $false; expired = $true }
        }
        $content = Invoke-RestMethod -Uri $consoleFile.Link -Method Get -TimeoutSec 60
        return [ordered]@{ found = $true; expired = $false; console_excerpt = [string]$content }
    }
    catch
    {
        return [ordered]@{ found = $false; expired = $true }
    }
}

function Get-CheckRunsForSha
{
    param([Parameter(Mandatory = $true)][string]$Sha)

    if ($isFixtureMode)
    {
        if ($fixture.check_runs.PSObject.Properties.Name -contains $Sha)
        {
            return @($fixture.check_runs.$Sha)
        }
        return @()
    }

    $headers = @{ Accept = "application/vnd.github+json"; "User-Agent" = "aspnetcore-test-quarantine-kbe-shadow" }
    if (-not [string]::IsNullOrEmpty($GitHubToken))
    {
        $headers["Authorization"] = "Bearer $GitHubToken"
    }
    try
    {
        $result = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/commits/$Sha/check-runs" -Headers $headers -Method Get -TimeoutSec 30
        return @($result.check_runs)
    }
    catch
    {
        return @()
    }
}

function Get-DuplicateSearch
{
    param(
        [Parameter(Mandatory = $true)][string]$Category,
        [Parameter(Mandatory = $true)][string]$Query
    )

    if ($isFixtureMode)
    {
        if ($fixture.duplicate_search.PSObject.Properties.Name -contains $Category)
        {
            $entry = $fixture.duplicate_search.$Category
            return [ordered]@{ complete = [bool]$entry.complete; result_numbers = @($entry.result_numbers) }
        }
        return [ordered]@{ complete = $false; result_numbers = @() }
    }

    $headers = @{ Accept = "application/vnd.github+json"; "User-Agent" = "aspnetcore-test-quarantine-kbe-shadow" }
    if (-not [string]::IsNullOrEmpty($GitHubToken))
    {
        $headers["Authorization"] = "Bearer $GitHubToken"
    }
    try
    {
        $encoded = [System.Uri]::EscapeDataString($Query)
        $result = Invoke-RestMethod -Uri "https://api.github.com/search/issues?q=$encoded&per_page=20" -Headers $headers -Method Get -TimeoutSec 30
        if ([bool]$result.incomplete_results)
        {
            return [ordered]@{ complete = $false; result_numbers = @() }
        }
        return [ordered]@{ complete = $true; result_numbers = @($result.items | ForEach-Object { [int]$_.number }) }
    }
    catch
    {
        return [ordered]@{ complete = $false; result_numbers = @() }
    }
}

# ---------------------------------------------------------------------------
# Step 1: validate the canonical, open quarantine issue.
# ---------------------------------------------------------------------------

$missingEvidence = [System.Collections.Generic.List[object]]::new()
$reasonCodes = [System.Collections.Generic.List[string]]::new()

$issue = Get-GitHubIssue -Number $IssueNumber
$issueUrl = "https://github.com/$Repository/issues/$IssueNumber"
$issueLabels = @($issue.labels | ForEach-Object { if ($_ -is [string]) { $_ } else { [string]$_.name } })
$issueState = [string]$issue.state

if ($issueLabels -notcontains $canonicalQuarantineLabel)
{
    $reasonCodes.Add("issue-not-canonical-quarantine")
    Add-MissingEvidence -List $missingEvidence -Kind "quarantine-label" -Detail "Issue #$IssueNumber does not carry the canonical '$canonicalQuarantineLabel' label."
}

if ($issueState -ne "open")
{
    $reasonCodes.Add("issue-not-open")
    Add-MissingEvidence -List $missingEvidence -Kind "issue-state" -Detail "Issue #$IssueNumber is '$issueState', not 'open'."
}

$issueBody = [string]$issue.body

# ---------------------------------------------------------------------------
# Step 2: deterministically parse the issue body for the test name, referenced
# builds, and (when unambiguous) a failure signature. Every quarantine issue
# produced by the production workflow carries a '## Failing Test(s)' section
# and at least one 'buildId=<id>' link; both formats in use (the strict
# 50_test_failure.md template and the freeform '## Details' variant) satisfy
# these two invariants, so this parsing does not depend on section ordering.
# ---------------------------------------------------------------------------

$testName = $null
$failingTestMatch = [regex]::Match($issueBody, "##\s*Failing Test\(s\)\s*\r?\n(.*?)(?=\r?\n##\s|\z)", [System.Text.RegularExpressions.RegexOptions]::Singleline)
if ($failingTestMatch.Success)
{
    $backtickMatch = [regex]::Match($failingTestMatch.Groups[1].Value, '`([^`]+)`')
    if ($backtickMatch.Success)
    {
        $testName = $backtickMatch.Groups[1].Value.Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($testName) -or $testName.Length -lt 3 -or $testName.Length -gt 1024 -or $testName -match "[\r\n]")
{
    $reasonCodes.Add("test-name-unresolvable")
    Add-MissingEvidence -List $missingEvidence -Kind "test-name" -Detail "Could not deterministically extract a single backtick-quoted fully qualified test name from '## Failing Test(s)'."
    $testName = $null
}

$citedBuildIds = @(
    [regex]::Matches($issueBody, "buildId=(\d+)") |
        ForEach-Object { [int]$_.Groups[1].Value } |
        Select-Object -Unique
)

if ($citedBuildIds.Count -eq 0)
{
    $reasonCodes.Add("build-reference-unresolvable")
    Add-MissingEvidence -List $missingEvidence -Kind "build-reference" -Detail "No 'buildId=<id>' reference found in the issue body."
}

$manualSignatureProvided = -not [string]::IsNullOrWhiteSpace($Signature)
$extractedSignature = $null
$errorMessageMatch = [regex]::Match($issueBody, '##\s*Error Message\s*\r?\n```(?:text)?\r?\n(.*?)```', [System.Text.RegularExpressions.RegexOptions]::Singleline)
if ($errorMessageMatch.Success)
{
    $firstLine = ($errorMessageMatch.Groups[1].Value -split "\r?\n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
    if (-not [string]::IsNullOrWhiteSpace($firstLine))
    {
        $extractedSignature = $firstLine.Trim()
    }
}

$effectiveSignature = if ($manualSignatureProvided) { $Signature.Trim() } else { $extractedSignature }
if ([string]::IsNullOrWhiteSpace($effectiveSignature) -or
    $effectiveSignature.Length -lt 8 -or $effectiveSignature.Length -gt 2048 -or
    $effectiveSignature -match "[\r\n]")
{
    $reasonCodes.Add("signature-extraction-ambiguous")
    Add-MissingEvidence -List $missingEvidence -Kind "signature" -Detail "No deterministic '## Error Message' code block was found and no valid -Signature override was supplied."
    $effectiveSignature = $null
}

# ---------------------------------------------------------------------------
# Step 3: resolve Azure DevOps build metadata for every issue-cited build.
# Builds whose metadata has aged out of Azure DevOps retention are recorded as
# not-found, never fabricated (see aspnetcore#68945).
# ---------------------------------------------------------------------------

$retrievedUtc = [System.DateTimeOffset]::UtcNow.ToString("O")
$azdoBuildRecords = [System.Collections.Generic.List[object]]::new()
$resolvedBuilds = [System.Collections.Generic.List[object]]::new()

foreach ($buildId in $citedBuildIds)
{
    $build = Get-AzdoBuild -BuildId $buildId
    if ($null -eq $build)
    {
        $null = $azdoBuildRecords.Add([ordered]@{
            id = $buildId
            found = $false
            retrieved_utc = $retrievedUtc
            source = "issue-body-reference"
            note = "Build metadata was not retrievable; it may have aged out of Azure DevOps retention."
        })
        Add-MissingEvidence -List $missingEvidence -Kind "azdo-build" -Detail "Build $buildId metadata could not be retrieved."
        continue
    }

    $definitionId = [int]$build.definition.id
    $sourceVersion = [string]$build.sourceVersion
    $record = [ordered]@{
        id = $buildId
        found = $true
        retrieved_utc = $retrievedUtc
        source = "issue-body-reference"
        definition_id = $definitionId
        source_version = $sourceVersion
        started_utc = ConvertTo-Iso8601String -Value $build.startTime
        finished_utc = ConvertTo-Iso8601String -Value $build.finishTime
        result = [string]$build.result
    }
    $null = $azdoBuildRecords.Add($record)
    $null = $resolvedBuilds.Add(($record + @{ intended_role = "failure" }))
}

# ---------------------------------------------------------------------------
# Step 4: if fewer than two distinct resolved builds are available, perform a
# capped supplementary recurrence scan across the same pipeline definition(s)
# on 'main' -- direct raw AzDO/VSTMR evidence, never the Build Analysis
# abstraction, per the architecture consensus that Build Analysis is
# corroborating only and cannot establish exact recurrence.
# ---------------------------------------------------------------------------

if ($resolvedBuilds.Count -lt $minimumFailureBuilds -and $null -ne $testName -and $null -ne $effectiveSignature)
{
    $scanDefinitionIds = if ($resolvedBuilds.Count -gt 0)
    {
        @($resolvedBuilds | ForEach-Object { $_.definition_id } | Select-Object -Unique)
    }
    else
    {
        $pipelineDefinitionIds
    }

    foreach ($definitionId in $scanDefinitionIds)
    {
        if ($resolvedBuilds.Count -ge $minimumFailureBuilds)
        {
            break
        }

        $candidates = Get-AzdoRecurrenceCandidateBuilds -DefinitionId $definitionId
        foreach ($candidate in $candidates)
        {
            if ($resolvedBuilds.Count -ge $minimumFailureBuilds)
            {
                break
            }

            $candidateId = [int]$candidate.id
            if ($citedBuildIds -contains $candidateId)
            {
                continue
            }

            $outcome = Get-VstmrTestOutcome -BuildId $candidateId -TestName $testName
            if ($null -eq $outcome -or [string]$outcome.outcome -ne "Failed")
            {
                continue
            }

            $signatureText = "$($outcome.errorMessage) $($outcome.stackTrace)"
            if ($signatureText -notlike "*$effectiveSignature*")
            {
                continue
            }

            $record = [ordered]@{
                id = $candidateId
                found = $true
                retrieved_utc = $retrievedUtc
                source = "recurrence-scan"
                definition_id = $definitionId
                source_version = [string]$candidate.sourceVersion
                started_utc = ConvertTo-Iso8601String -Value $candidate.startTime
                finished_utc = ConvertTo-Iso8601String -Value $candidate.finishTime
                result = [string]$candidate.result
            }
            $null = $azdoBuildRecords.Add($record)
            $null = $resolvedBuilds.Add(($record + @{ intended_role = "failure" }))
        }
    }
}

if ($null -ne $testName -and $null -ne $effectiveSignature -and $resolvedBuilds.Count -lt $minimumFailureBuilds)
{
    $reasonCodes.Add("recurrence-single-build-only")
    Add-MissingEvidence -List $missingEvidence -Kind "recurrence" -Detail "Only $($resolvedBuilds.Count) distinct build(s) with matching failure evidence were found; at least $minimumFailureBuilds are required and none could be added by the supplementary recurrence scan."
}

# ---------------------------------------------------------------------------
# Step 4.5: gather at least one authoritative negative (passed/skipped)
# occurrence of the same test on the same pipeline(s). This is what lets the
# evaluator confirm the failure is not a consistent regression -- a missing
# negative is recorded as insufficient evidence, never inferred as a pass.
# ---------------------------------------------------------------------------

$negativeBuilds = [System.Collections.Generic.List[object]]::new()

if ($null -ne $testName -and $null -ne $effectiveSignature)
{
    $negativeScanDefinitionIds = @($resolvedBuilds | ForEach-Object { $_.definition_id } | Select-Object -Unique)
    if ($negativeScanDefinitionIds.Count -eq 0)
    {
        $negativeScanDefinitionIds = $pipelineDefinitionIds
    }

    foreach ($definitionId in $negativeScanDefinitionIds)
    {
        if ($negativeBuilds.Count -ge $minimumNegativeLogs)
        {
            break
        }

        $candidates = Get-AzdoNegativeCandidateBuilds -DefinitionId $definitionId
        foreach ($candidate in $candidates)
        {
            if ($negativeBuilds.Count -ge $minimumNegativeLogs)
            {
                break
            }

            $candidateId = [int]$candidate.id
            $outcome = Get-VstmrTestOutcome -BuildId $candidateId -TestName $testName
            if ($null -eq $outcome -or [string]$outcome.outcome -notin @("Passed", "Skipped"))
            {
                continue
            }

            $record = [ordered]@{
                id = $candidateId
                found = $true
                retrieved_utc = $retrievedUtc
                source = "negative-scan"
                definition_id = $definitionId
                source_version = [string]$candidate.sourceVersion
                started_utc = ConvertTo-Iso8601String -Value $candidate.startTime
                finished_utc = ConvertTo-Iso8601String -Value $candidate.finishTime
                result = [string]$candidate.result
            }
            $null = $azdoBuildRecords.Add($record)
            $null = $negativeBuilds.Add(($record + @{ intended_role = "negative" }))
        }
    }
}

# ---------------------------------------------------------------------------
# Step 5: for each resolved build, gather raw failure/negative evidence
# (Helix console log) and materialize it locally so Evaluate-TestQuarantineKbeCandidate.ps1
# can hash-verify it. An artifact that once existed but is no longer
# retrievable is recorded as found=false, expired=true -- never silently
# dropped and never treated as a pass (see aspnetcore#68945).
# ---------------------------------------------------------------------------

[System.IO.Directory]::CreateDirectory($EvidenceRoot) | Out-Null

$rawEvidenceRecords = [System.Collections.Generic.List[object]]::new()
$rawLogs = [System.Collections.Generic.List[object]]::new()
$failureBuildIdSet = [System.Collections.Generic.HashSet[int]]::new()
$negativeCount = 0
$evidenceIndex = 0

$evidenceBuilds = @($resolvedBuilds) + @($negativeBuilds)
foreach ($build in $evidenceBuilds)
{
    if ($null -eq $testName)
    {
        break
    }

    $role = [string]$build.intended_role
    $vstmrOutcome = Get-VstmrTestOutcome -BuildId $build.id -TestName $testName
    $helixJob = $null
    $helixWorkItem = $null
    if ($null -ne $vstmrOutcome -and -not [string]::IsNullOrEmpty($vstmrOutcome.comment))
    {
        $commentMatch = ($vstmrOutcome.comment | ConvertFrom-Json -ErrorAction SilentlyContinue)
        if ($null -ne $commentMatch)
        {
            $helixJob = [string]$commentMatch.HelixJobId
            $helixWorkItem = [string]$commentMatch.HelixWorkItemName
        }
    }

    # Defensive consistency check: the intended role (why this build was selected)
    # must match what VSTMR actually reports for this test in this build. A
    # mismatch means the evidence is stale or inconsistent -- skip it rather than
    # writing a role/outcome pair that contradicts the authoritative test result.
    $outcomeValue = if ($null -eq $vstmrOutcome) { $null }
        elseif ([string]$vstmrOutcome.outcome -eq "Failed") { "failed" }
        elseif ([string]$vstmrOutcome.outcome -eq "Passed") { "passed" }
        elseif ([string]$vstmrOutcome.outcome -eq "Skipped") { "skipped" }
        else { $null }

    $roleOutcomeConsistent =
        ($role -eq "failure" -and $outcomeValue -eq "failed") -or
        ($role -eq "negative" -and $outcomeValue -in @("passed", "skipped"))

    if (-not $roleOutcomeConsistent)
    {
        Add-MissingEvidence -List $missingEvidence -Kind "vstmr-consistency" -Detail "Build $($build.id): recorded test outcome did not match the reason this build was selected as $role evidence."
        continue
    }

    $helixEvidence = Get-HelixEvidence -BuildId $build.id -HelixJob $helixJob -HelixWorkItem $helixWorkItem
    $found = ($null -ne $helixEvidence -and [bool]$helixEvidence.found)
    $expired = ($null -ne $helixEvidence -and [bool]$helixEvidence.expired) -or ($null -eq $helixEvidence -and $null -ne $vstmrOutcome)

    if (-not $found)
    {
        $null = $rawEvidenceRecords.Add([ordered]@{
            build_id = $build.id
            role = $role
            found = $false
            expired = $expired
            captured_utc = $retrievedUtc
            note = "Helix console evidence for build $($build.id) was not retrievable."
        })
        Add-MissingEvidence -List $missingEvidence -Kind "helix-evidence" -Detail "Build $($build.id): no retrievable Helix console evidence."
        if ($expired)
        {
            $reasonCodes.Add("raw-evidence-expired")
        }
        continue
    }

    $evidenceIndex += 1
    $fileName = "issue-$IssueNumber-build-$($build.id)-$role.log"
    $evidencePath = Join-Path $EvidenceRoot $fileName
    $cappedContent = Get-CappedExcerpt -Value ([string]$helixEvidence.console_excerpt) -Cap $rawLogCap -ProtectedPhrases @($testName)
    [System.IO.File]::WriteAllText($evidencePath, $cappedContent)
    $sha256 = (Get-FileHash -LiteralPath $evidencePath -Algorithm SHA256).Hash.ToLowerInvariant()

    $null = $rawEvidenceRecords.Add([ordered]@{
        build_id = $build.id
        role = $role
        kind = "helix-console-log"
        helix_job = $(if ($helixJob) { $helixJob } else { "" })
        helix_workitem = $(if ($helixWorkItem) { $helixWorkItem } else { "" })
        found = $true
        expired = $false
        captured_utc = $retrievedUtc
        sha256 = $sha256
        evidence_path = $fileName
    })

    if ($role -eq "failure")
    {
        $null = $failureBuildIdSet.Add([int]$build.id)
    }
    else
    {
        $negativeCount += 1
    }

    $null = $rawLogs.Add([ordered]@{
        id = "evidence-$evidenceIndex"
        role = $role
        outcome = $outcomeValue
        path = $fileName
        source_url = "https://dev.azure.com/dnceng-public/public/_build/results?buildId=$($build.id)&view=results"
        sha256 = $sha256
        build = [ordered]@{
            id = [int]$build.id
            pipeline_definition_id = [int]$build.definition_id
            source_version = [string]$build.source_version
            started_utc = [string]$build.started_utc
            platform = "Linux"
            configuration = "Release"
        }
    })
}

if ($null -ne $testName -and $failureBuildIdSet.Count -lt $minimumFailureBuilds)
{
    if ($reasonCodes -notcontains "recurrence-single-build-only")
    {
        $reasonCodes.Add("raw-evidence-insufficient")
        Add-MissingEvidence -List $missingEvidence -Kind "raw-evidence" -Detail "Only $($failureBuildIdSet.Count) distinct build(s) produced retrievable failure evidence; at least $minimumFailureBuilds are required."
    }
}

if ($null -ne $testName -and $negativeCount -lt $minimumNegativeLogs)
{
    $reasonCodes.Add("raw-evidence-insufficient")
    Add-MissingEvidence -List $missingEvidence -Kind "raw-evidence" -Detail "No retrievable negative (passed/skipped) evidence was found; at least $minimumNegativeLogs is required."
}

# ---------------------------------------------------------------------------
# Step 6: fetch Build Analysis check-run snapshots. Advisory/corroborating
# only: recorded regardless of outcome, and a missing or generic snapshot
# never overrides raw evidence gathered above.
# ---------------------------------------------------------------------------

$checkRunRecords = [System.Collections.Generic.List[object]]::new()
$distinctShas = @($resolvedBuilds | ForEach-Object { $_.source_version } | Where-Object { $_ } | Select-Object -Unique)
$corroboratingContext = [System.Collections.Generic.List[object]]::new()

foreach ($sha in $distinctShas)
{
    $checkRuns = Get-CheckRunsForSha -Sha $sha
    $buildAnalysis = @($checkRuns) | Where-Object { [string]$_.name -eq "Build Analysis" } | Select-Object -First 1

    if ($null -eq $buildAnalysis)
    {
        $null = $checkRunRecords.Add([ordered]@{
            source_version = $sha
            found = $false
            retrieved_utc = $retrievedUtc
            exact_test_referenced = $false
            known_issue_referenced = $false
        })
        continue
    }

    $text = [string]$buildAnalysis.output.text
    $textSha256 = Get-Sha256String -Value $text
    $shortMethodName = if ($testName) { ($testName -split '\.')[-1] } else { $null }
    $exactTestReferenced = ($null -ne $testName -and $text.Contains($testName)) -or ($null -ne $shortMethodName -and $text.Contains($shortMethodName))
    $knownIssueReferenced = $text -match "(?i)known issue"

    $null = $checkRunRecords.Add([ordered]@{
        source_version = $sha
        found = $true
        retrieved_utc = $retrievedUtc
        check_id = [int]$buildAnalysis.id
        conclusion = [string]$buildAnalysis.conclusion
        title = Get-CappedExcerpt -Value ([string]$buildAnalysis.output.title) -Cap 512
        text_sha256 = $textSha256
        text_excerpt = Get-CappedExcerpt -Value $text -Cap $excerptCap
        html_url = [string]$buildAnalysis.html_url
        exact_test_referenced = $exactTestReferenced
        known_issue_referenced = $knownIssueReferenced
    })
    $null = $corroboratingContext.Add([ordered]@{ source = "build-analysis"; url = [string]$buildAnalysis.html_url })
}

# ---------------------------------------------------------------------------
# Step 7: categorized duplicate KBE / fix-PR search.
# ---------------------------------------------------------------------------

$shortName = if ($testName) { ($testName -split '\.')[-1] } else { $IssueNumber.ToString() }
$duplicateQueries = @(
    @{ category = "open-kbe"; query = "repo:$Repository is:issue is:open label:`"Known Build Error`" $shortName" }
    @{ category = "recently-closed-kbe"; query = "repo:$Repository is:issue is:closed label:`"Known Build Error`" $shortName" }
    @{ category = "open-fix-pr"; query = "repo:$Repository is:pr is:open $shortName" }
    @{ category = "recently-merged-fix-pr"; query = "repo:$Repository is:pr is:merged $shortName" }
)

$duplicateQueryResults = [System.Collections.Generic.List[object]]::new()
$duplicateReferences = [System.Collections.Generic.List[string]]::new()
$kbeNumbers = [System.Collections.Generic.List[int]]::new()
$fixPrNumbers = [System.Collections.Generic.List[int]]::new()
$allQueriesComplete = $true

foreach ($q in $duplicateQueries)
{
    $searchResult = Get-DuplicateSearch -Category $q.category -Query $q.query
    if (-not [bool]$searchResult.complete)
    {
        $allQueriesComplete = $false
    }
    $resultNumbers = @($searchResult.result_numbers)
    $null = $duplicateQueryResults.Add([ordered]@{
        category = $q.category
        query = $q.query
        complete = [bool]$searchResult.complete
        result_numbers = $resultNumbers
    })

    $isKbeCategory = $q.category -in @("open-kbe", "recently-closed-kbe")
    foreach ($n in $resultNumbers)
    {
        if ($isKbeCategory)
        {
            $null = $kbeNumbers.Add($n)
            $null = $duplicateReferences.Add("issue:$n")
        }
        else
        {
            $null = $fixPrNumbers.Add($n)
            $null = $duplicateReferences.Add("pull-request:$n")
        }
    }
}

$duplicateStatus = if (-not $allQueriesComplete)
{
    "not-evaluated"
}
elseif ($kbeNumbers.Count -gt 0)
{
    "existing-kbe"
}
elseif ($fixPrNumbers.Count -gt 0)
{
    "existing-fix-pr"
}
else
{
    "none"
}

if (-not $allQueriesComplete)
{
    $reasonCodes.Add("duplicate-search-incomplete")
    Add-MissingEvidence -List $missingEvidence -Kind "duplicate-search" -Detail "At least one duplicate KBE/fix-PR search category returned incomplete results."
}

$duplicateCheck = [ordered]@{
    status = $duplicateStatus
    checked_utc = $retrievedUtc
    coverage = [ordered]@{
        open_kbes = $true
        recently_closed_kbes = $true
        open_fix_prs = $true
        recently_merged_fix_prs = $true
    }
    references = @($duplicateReferences | Select-Object -Unique)
    queries = @($duplicateQueryResults)
}

# ---------------------------------------------------------------------------
# Step 8: assemble the dossier -- and the candidate, when every gate passed.
# ---------------------------------------------------------------------------

$reasonCodes = @($reasonCodes | Select-Object -Unique)
$outcome = if ($reasonCodes.Count -gt 0) { "incomplete" } else { "candidate" }

$candidate = $null
if ($outcome -eq "candidate")
{
    $proposedClassification = switch ($duplicateStatus)
    {
        "existing-kbe" { "reuse-existing-kbe"; break }
        "existing-fix-pr" { "quarantine-only"; break }
        default
        {
            if ($effectiveSignature -match "(?i)timeout|WebDriverException|TaskCanceledException")
            {
                "timeout-needs-classification"
            }
            else
            {
                "new-kbe-candidate"
            }
        }
    }

    $null = $corroboratingContext.Add([ordered]@{ source = "quarantine-issue"; url = $issueUrl })

    $repoHeadSha = (& git -C $RepositoryRoot rev-parse HEAD).Trim()
    $candidate = [ordered]@{
        schema_version = 1
        repository = "dotnet/aspnetcore"
        repository_ref = [ordered]@{
            branch = "main"
            commit_sha = $repoHeadSha
        }
        issue = [ordered]@{
            number = $IssueNumber
            url = $issueUrl
        }
        test = [ordered]@{
            fully_qualified_name = $testName
        }
        signature = [ordered]@{
            kind = "ErrorMessage"
            values = @($effectiveSignature)
            build_retry = $false
            exclude_console_log = $false
        }
        policy = [ordered]@{
            minimum_failure_logs = $minimumFailureBuilds
            minimum_negative_logs = $minimumNegativeLogs
        }
        evidence = [ordered]@{
            raw_logs = @($rawLogs)
            corroborating_context = @($corroboratingContext)
        }
        duplicate_check = $duplicateCheck
        proposed_classification = $proposedClassification
    }

    $candidateJson = $candidate | ConvertTo-Json -Depth 32
    if (-not ($candidateJson | Test-Json -SchemaFile $CandidateSchemaFile))
    {
        throw "Collector produced a candidate that does not satisfy the versioned candidate schema."
    }

    $candidateDirectory = Split-Path -Parent $CandidateFile
    if ($candidateDirectory)
    {
        [System.IO.Directory]::CreateDirectory($candidateDirectory) | Out-Null
    }
    [System.IO.File]::WriteAllText($CandidateFile, $candidateJson + [System.Environment]::NewLine)
}

$incomplete = $null
if ($outcome -eq "incomplete")
{
    $incomplete = [ordered]@{
        reason_codes = @($reasonCodes)
        message = "Collector could not produce a validated candidate for issue #$IssueNumber : " + ($reasonCodes -join ", ") + "."
        missing_evidence = @($missingEvidence)
    }
}

$dossier = [ordered]@{
    schema_version = 1
    repository = "dotnet/aspnetcore"
    collector = [ordered]@{
        name = "Collect-TestQuarantineKbeEvidence.ps1"
        version = 1
        generated_utc = $retrievedUtc
        fixture_mode = $isFixtureMode
        manual_signature_provided = $manualSignatureProvided
    }
    issue = [ordered]@{
        number = $IssueNumber
        url = $issueUrl
        state = $issueState
        labels = @($issueLabels)
    }
    outcome = $outcome
    provenance = [ordered]@{
        azdo_builds = @($azdoBuildRecords)
        check_run_snapshots = @($checkRunRecords)
        raw_evidence_sources = @($rawEvidenceRecords)
        duplicate_search = $duplicateCheck
    }
    candidate = $candidate
    incomplete = $incomplete
}

$dossierJson = $dossier | ConvertTo-Json -Depth 32
if (-not ($dossierJson | Test-Json -SchemaFile $DossierSchemaFile))
{
    throw "Generated dossier does not satisfy the versioned dossier schema."
}

$outputDirectory = Split-Path -Parent $OutputFile
if ($outputDirectory)
{
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}
[System.IO.File]::WriteAllText($OutputFile, $dossierJson + [System.Environment]::NewLine)

Write-Host "Wrote '$outcome' dossier for issue #$IssueNumber to $OutputFile"
