#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deterministic, read-only collector for exactly one open dotnet/aspnetcore test-quarantine issue.

.DESCRIPTION
    Gathers public evidence for a single quarantine issue -- the issue body itself, Azure DevOps
    build metadata, authoritative VSTMR test-result detail (errorMessage/stackTrace), and GitHub
    "Build Analysis" check-run snapshots (corroborating only, never authoritative) -- then emits
    either:
      * a "candidate" object that independently satisfies test-quarantine-kbe-shadow-candidate.schema.json
        and is ready for Evaluate-TestQuarantineKbeCandidate.ps1, or
      * a structured "incomplete" outcome explaining exactly which evidence could not be
        established, without inferring a pass, a recurrence, a signature, a platform/configuration,
        or a validated duplicate from anything missing, ambiguous, or unverifiable.

    This script makes no repository-state mutations. It only reads public GitHub/Azure DevOps
    endpoints (or, in fixture mode, a local fixture file) and writes local files: the dossier, an
    optional candidate, and capped/redacted evidence text files under -EvidenceRoot.

.PARAMETER IssueNumber
    The dotnet/aspnetcore issue number to evaluate. Must be the canonical, currently open,
    automation-generated test-quarantine issue for the test(s) it names.

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
    in for every live network call (GitHub issue/check-runs/search/commits and Azure DevOps
    build/VSTMR data). Used by the test suite and by the shadow workflow's self-test mode so this
    script's deterministic logic can be exercised with zero network access. See README.md for the
    fixture.json shape.

.PARAMETER GitHubToken
    GitHub token for the GitHub REST calls (issue, check-runs, search, commits). Falls back to the
    GITHUB_TOKEN environment variable. Authenticated calls are strongly preferred: the search API's
    unauthenticated rate limit (10 requests/minute) is exhausted by a single run's four duplicate
    searches plus one retry.
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

    [int]$RecurrenceScanBuildCap = 20,

    [int]$DuplicateSearchWindowDays = 90,

    [int]$DuplicateSearchPageSize = 100,

    [int]$DuplicateSearchMaxPages = 3
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ado = "https://dev.azure.com/dnceng-public/public/_apis"
$vstmr = "https://vstmr.dev.azure.com/dnceng-public/public/_apis"
$pipelineDefinitionIds = @(83, 87)
$canonicalQuarantineLabel = "test-failure"
$workflowMarkers = @(
    "<!-- gh-aw-workflow-id: test-quarantine -->",
    "<!-- gh-aw-workflow-call-id: dotnet/aspnetcore/test-quarantine -->"
)
$minimumFailureBuilds = 2
$minimumNegativeLogs = 1
$excerptCap = 2000
$rawLogCap = 12000

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

    # The evidence text this function builds always places the failed/passed-test marker line
    # and the (already-extracted) signature first, so capping to the first $Cap characters here
    # can only ever truncate the tail of a long stack trace -- never the marker or signature the
    # evaluator's association window needs. See Assemble-EvidenceText below.
    $normalized = [System.Text.RegularExpressions.Regex]::Replace($redacted, "[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "?")
    if ($normalized.Length -gt $Cap)
    {
        $normalized = $normalized.Substring(0, $Cap)
    }

    return $normalized
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

function Test-HasProperty
{
    # `$obj.PSObject.Properties.Name -contains $name` throws "The property 'Name' cannot be
    # found on this object" under Set-StrictMode -Version Latest whenever the object has zero
    # properties (an empty JSON object `{}}`, which every fixture.json category not exercised by
    # a given test/fixture legitimately is). Iterating .PSObject.Properties directly is safe for
    # both the empty and non-empty case.
    param([AllowNull()]$Object, [Parameter(Mandatory = $true)][string]$Name)

    if ($null -eq $Object)
    {
        return $false
    }
    foreach ($property in $Object.PSObject.Properties)
    {
        if ($property.Name -eq $Name)
        {
            return $true
        }
    }
    return $false
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
# document; live mode calls the public GitHub/Azure DevOps REST APIs.
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

function Get-GitHubHeaders
{
    # A real bearer token, never a literal placeholder. Authenticated GitHub REST/Search calls
    # get a materially higher rate limit (up to 30 search requests/minute vs. 10 unauthenticated)
    # and are required in practice: a single run's four duplicate-search categories alone can
    # exhaust the unauthenticated search quota.
    $headers = @{ Accept = "application/vnd.github+json"; "User-Agent" = "aspnetcore-test-quarantine-kbe-shadow" }
    if (-not [string]::IsNullOrEmpty($GitHubToken))
    {
        $headers["Authorization"] = "Bearer $GitHubToken"
    }
    return $headers
}

function Write-RateLimitDiagnostic
{
    # Non-blocking, informational only: surfaces the authenticated GitHub rate-limit headroom in
    # the workflow log so a maintainer can see at a glance whether the token is actually being
    # used and how much quota remains. Never fails the run if the header is absent.
    param($ResponseHeaders)

    if ($null -eq $ResponseHeaders)
    {
        return
    }
    $remaining = $ResponseHeaders["X-RateLimit-Remaining"]
    $limit = $ResponseHeaders["X-RateLimit-Limit"]
    if ($remaining -and $limit)
    {
        Write-Host "GitHub API rate limit: $remaining/$limit remaining."
    }
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

    $headers = Get-GitHubHeaders
    $responseHeaders = $null
    $result = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/issues/$Number" -Headers $headers -Method Get -TimeoutSec 30 -ResponseHeadersVariable responseHeaders
    Write-RateLimitDiagnostic -ResponseHeaders $responseHeaders
    return $result
}

function Get-TrustedMainShaResult
{
    # Returns @{ Checked; TrustedSha }. `Checked = $false` only in fixture mode when the fixture
    # does not model this dimension at all (the three real pilot fixtures do not); live mode
    # always performs the check. A failed live lookup still counts as Checked = $true with a
    # null TrustedSha, which fails the comparison closed rather than silently skipping it.
    if ($isFixtureMode)
    {
        if (Test-HasProperty -Object $fixture -Name "main_branch")
        {
            return [ordered]@{ Checked = $true; TrustedSha = [string]$fixture.main_branch.sha }
        }
        return [ordered]@{ Checked = $false; TrustedSha = $null }
    }

    try
    {
        $headers = Get-GitHubHeaders
        $result = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/commits/main" -Headers $headers -Method Get -TimeoutSec 30
        return [ordered]@{ Checked = $true; TrustedSha = [string]$result.sha }
    }
    catch
    {
        return [ordered]@{ Checked = $true; TrustedSha = $null }
    }
}

function Get-AzdoBuild
{
    param([Parameter(Mandatory = $true)][int]$BuildId)

    if ($isFixtureMode)
    {
        $key = [string]$BuildId
        if (Test-HasProperty -Object $fixture.azdo_builds -Name $key)
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

function Merge-AzdoBuildLists
{
    # Pure, side-effect-free merge/dedupe used by the live branch of
    # Get-AzdoRecurrenceCandidateBuilds below. Extracted so the merge/dedupe/cap semantics can be
    # unit-tested directly with synthetic input, without any network access, independent of
    # whichever live resultFilter values happen to be queried.
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Lists,
        [Parameter(Mandatory = $true)][int]$Cap
    )

    $merged = [ordered]@{}
    foreach ($list in $Lists)
    {
        foreach ($build in @($list))
        {
            $merged[[string]$build.id] = $build
        }
    }

    return @($merged.Values | Sort-Object -Property startTime -Descending | Select-Object -First $Cap)
}

function Get-AzdoRecurrenceCandidateBuilds
{
    # Azure DevOps' `resultFilter` does not support a comma-separated multi-value combination
    # (verified live: "resultFilter=failed,partiallySucceeded" silently behaves like a single,
    # different filter, not their union). ASP.NET Core test failures routinely land in a
    # `partiallySucceeded` build (confirmed for aspnetcore#68947's own cited build 1551326), so a
    # `failed`-only query misses real recurrence evidence. Issue one request per result value and
    # merge, deduping by build id, via Merge-AzdoBuildLists.
    param([Parameter(Mandatory = $true)][int]$DefinitionId)

    if ($isFixtureMode)
    {
        $key = [string]$DefinitionId
        if (Test-HasProperty -Object $fixture.recurrence_scan -Name $key)
        {
            return @($fixture.recurrence_scan.$key)
        }
        return @()
    }

    $resultLists = [System.Collections.Generic.List[object]]::new()
    foreach ($resultFilter in @("failed", "partiallySucceeded"))
    {
        try
        {
            $result = Invoke-RestMethod -Uri "$ado/build/builds?definitions=$DefinitionId&branchName=refs/heads/main&resultFilter=$resultFilter&`$top=$RecurrenceScanBuildCap&api-version=7.1" -Method Get -TimeoutSec 30
            $null = $resultLists.Add(@($result.value))
        }
        catch
        {
            continue
        }
    }

    return Merge-AzdoBuildLists -Lists @($resultLists) -Cap $RecurrenceScanBuildCap
}

function Get-AzdoNegativeCandidateBuilds
{
    param([Parameter(Mandatory = $true)][int]$DefinitionId)

    if ($isFixtureMode)
    {
        $key = [string]$DefinitionId
        if (Test-HasProperty -Object $fixture.negative_scan -Name $key)
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

function Get-VstmrSummaryRows
{
    # The `resultsbyBuild` summary rows carry only identity + outcome (id, runId,
    # automatedTestName, outcome) for ordinary xUnit tests -- verified live against
    # aspnetcore#68947's own cited build: no comment/errorMessage/stackTrace field is present.
    # Use this only to locate the (runId, resultId) pair; fetch Get-VstmrDetail for the actual
    # error text.
    param(
        [Parameter(Mandatory = $true)][int]$BuildId,
        [Parameter(Mandatory = $true)][string]$TestName
    )

    if ($isFixtureMode)
    {
        $key = "$BuildId"
        if (Test-HasProperty -Object $fixture.vstmr_summary -Name $key)
        {
            return @($fixture.vstmr_summary.$key | Where-Object { [string]$_.automatedTestName -eq $TestName })
        }
        return @()
    }

    try
    {
        $result = Invoke-RestMethod -Uri "$vstmr/testresults/resultsbyBuild?buildId=$BuildId&api-version=7.1-preview.1" -Method Get -TimeoutSec 60
        return @($result.value | Where-Object { [string]$_.automatedTestName -eq $TestName })
    }
    catch
    {
        return @()
    }
}

$vstmrDetailCache = @{}

function Get-VstmrDetail
{
    # The authoritative source for a specific result's errorMessage/stackTrace (and, when
    # present, Helix job/work-item coordinates via a `comment` field -- only ever populated on a
    # Helix work item's own crash/'.WorkItemExecution' pseudo-test row, not on ordinary xUnit
    # test rows). This is the "detailed VSTMR result" endpoint, distinct from resultsbyBuild.
    param(
        [Parameter(Mandatory = $true)][int]$RunId,
        [Parameter(Mandatory = $true)][int]$ResultId
    )

    $cacheKey = "${RunId}:${ResultId}"
    if ($vstmrDetailCache.ContainsKey($cacheKey))
    {
        return $vstmrDetailCache[$cacheKey]
    }

    $detail = if ($isFixtureMode)
    {
        if (Test-HasProperty -Object $fixture.vstmr_detail -Name $cacheKey)
        {
            $fixture.vstmr_detail.$cacheKey
        }
        else
        {
            $null
        }
    }
    else
    {
        try
        {
            Invoke-RestMethod -Uri "$ado/test/Runs/$RunId/results/${ResultId}?api-version=7.1" -Method Get -TimeoutSec 30
        }
        catch
        {
            $null
        }
    }

    $vstmrDetailCache[$cacheKey] = $detail
    return $detail
}

$vstmrRunCache = @{}

function Get-VstmrRunName
{
    # The TestRun's `name` (e.g. "Quarantine-Mono-Linux-Release-xunit") is the only authoritative,
    # cheaply-available signal for which platform/configuration leg a result ran on --
    # `buildConfiguration.platform`/`.flavor` are empty strings on every run observed live.
    param([Parameter(Mandatory = $true)][int]$RunId)

    if ($vstmrRunCache.ContainsKey($RunId))
    {
        return $vstmrRunCache[$RunId]
    }

    $name = if ($isFixtureMode)
    {
        $key = "$RunId"
        if (Test-HasProperty -Object $fixture.vstmr_runs -Name $key)
        {
            [string]$fixture.vstmr_runs.$key.name
        }
        else
        {
            $null
        }
    }
    else
    {
        try
        {
            [string](Invoke-RestMethod -Uri "$ado/test/runs/${RunId}?api-version=7.1" -Method Get -TimeoutSec 30).name
        }
        catch
        {
            $null
        }
    }

    $vstmrRunCache[$RunId] = $name
    return $name
}

function Get-PlatformConfigurationFromRunName
{
    param([AllowNull()][string]$RunName)

    $platform = "unknown"
    $configuration = "unknown"
    if ([string]::IsNullOrEmpty($RunName))
    {
        return [ordered]@{ Platform = $platform; Configuration = $configuration }
    }

    if ($RunName -match "(?i)\bwindows\b") { $platform = "Windows" }
    elseif ($RunName -match "(?i)\blinux\b") { $platform = "Linux" }
    elseif ($RunName -match "(?i)\b(?:macos|osx)\b") { $platform = "macOS" }

    if ($RunName -match "(?i)\bdebug\b") { $configuration = "Debug" }
    elseif ($RunName -match "(?i)\brelease\b") { $configuration = "Release" }

    return [ordered]@{ Platform = $platform; Configuration = $configuration }
}

function Get-CheckRunsForSha
{
    param([Parameter(Mandatory = $true)][string]$Sha)

    if ($isFixtureMode)
    {
        if (Test-HasProperty -Object $fixture.check_runs -Name $Sha)
        {
            return @($fixture.check_runs.$Sha)
        }
        return @()
    }

    try
    {
        $headers = Get-GitHubHeaders
        $result = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/commits/$Sha/check-runs" -Headers $headers -Method Get -TimeoutSec 30
        return @($result.check_runs)
    }
    catch
    {
        return @()
    }
}

function Search-GitHubIssues
{
    # Returns @{ Complete; Numbers; TotalCount }. `Complete` requires BOTH that GitHub reported
    # incomplete_results=false AND that every matching item (per total_count) was actually
    # retrieved across the paginated fetch -- a `total_count` larger than what a single
    # per_page=100 page returns previously went unnoticed and was still labeled complete.
    param([Parameter(Mandatory = $true)][string]$Query)

    if ($isFixtureMode)
    {
        return $null
    }

    $headers = Get-GitHubHeaders
    $numbers = [System.Collections.Generic.List[int]]::new()
    $totalCount = 0
    $complete = $true
    $encoded = [System.Uri]::EscapeDataString($Query)

    for ($page = 1; $page -le $DuplicateSearchMaxPages; $page++)
    {
        try
        {
            $responseHeaders = $null
            $result = Invoke-RestMethod -Uri "https://api.github.com/search/issues?q=$encoded&per_page=$DuplicateSearchPageSize&page=$page" -Headers $headers -Method Get -TimeoutSec 30 -ResponseHeadersVariable responseHeaders
            Write-RateLimitDiagnostic -ResponseHeaders $responseHeaders
        }
        catch
        {
            $complete = $false
            break
        }

        $totalCount = [int]$result.total_count
        if ([bool]$result.incomplete_results)
        {
            $complete = $false
        }
        foreach ($item in @($result.items))
        {
            $null = $numbers.Add([int]$item.number)
        }

        if ($numbers.Count -ge $totalCount)
        {
            break
        }
        if ($page -eq $DuplicateSearchMaxPages -and $numbers.Count -lt $totalCount)
        {
            $complete = $false
        }
    }

    return [ordered]@{ Complete = $complete; Numbers = @($numbers); TotalCount = $totalCount }
}

function Get-DuplicateCandidateText
{
    # Fetches the searched-up issue or PR's title+body so its exact test identity can be
    # verified before it is ever treated as a validated duplicate. `/issues/{number}` is a
    # unified GitHub endpoint that also resolves pull requests.
    param([Parameter(Mandatory = $true)][int]$Number)

    if ($isFixtureMode)
    {
        $key = "$Number"
        if (Test-HasProperty -Object $fixture.duplicate_candidate_text -Name $key)
        {
            return [string]$fixture.duplicate_candidate_text.$key
        }
        return $null
    }

    try
    {
        $headers = Get-GitHubHeaders
        $result = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/issues/$Number" -Headers $headers -Method Get -TimeoutSec 30
        return "$($result.title)`n$($result.body)"
    }
    catch
    {
        return $null
    }
}

# ---------------------------------------------------------------------------
# Step 1: validate the canonical, open quarantine issue. The 'test-failure' label alone is not
# proof an issue was generated by quarantine automation (any contributor can apply it to an
# ordinary bug report); also require the trusted HTML-comment marker the production workflow
# stamps into every issue it creates.
# ---------------------------------------------------------------------------

$missingEvidence = [System.Collections.Generic.List[object]]::new()
$reasonCodes = [System.Collections.Generic.List[string]]::new()

$issue = Get-GitHubIssue -Number $IssueNumber
$issueUrl = "https://github.com/$Repository/issues/$IssueNumber"
$issueLabels = @($issue.labels | ForEach-Object { if ($_ -is [string]) { $_ } else { [string]$_.name } })
$issueState = [string]$issue.state
$issueBody = [string]$issue.body
$hasWorkflowMarker = @($workflowMarkers | Where-Object { $issueBody.Contains($_, [System.StringComparison]::Ordinal) }).Count -gt 0

if ($issueLabels -notcontains $canonicalQuarantineLabel -or -not $hasWorkflowMarker)
{
    $reasonCodes.Add("issue-not-canonical-quarantine")
    if ($issueLabels -notcontains $canonicalQuarantineLabel)
    {
        Add-MissingEvidence -List $missingEvidence -Kind "quarantine-label" -Detail "Issue #$IssueNumber does not carry the canonical '$canonicalQuarantineLabel' label."
    }
    if (-not $hasWorkflowMarker)
    {
        Add-MissingEvidence -List $missingEvidence -Kind "quarantine-workflow-marker" -Detail "Issue #$IssueNumber body does not contain a trusted 'gh-aw-workflow-id: test-quarantine' / 'gh-aw-workflow-call-id: dotnet/aspnetcore/test-quarantine' marker; the label alone is not proof this issue was generated by quarantine automation."
    }
}

if ($issueState -ne "open")
{
    $reasonCodes.Add("issue-not-open")
    Add-MissingEvidence -List $missingEvidence -Kind "issue-state" -Detail "Issue #$IssueNumber is '$issueState', not 'open'."
}

# ---------------------------------------------------------------------------
# Step 2: deterministically parse the issue body for the test name, referenced builds, and (when
# unambiguous) a failure signature. Every quarantine issue produced by the production workflow
# carries a '## Failing Test(s)' section and at least one 'buildId=<id>' link; both formats in use
# (the strict 50_test_failure.md template and the freeform '## Details' variant) satisfy these two
# invariants, so this parsing does not depend on section ordering.
#
# A '## Failing Test(s)' section can legitimately name more than one concrete test identity (e.g.
# aspnetcore#68724 names both a base test and its server-execution subclass override, and live
# data shows only the override actually failed while the base identity passed). Silently picking
# the first one risks binding evidence to the wrong identity. This collector fails closed unless
# exactly one concrete identity can be unambiguously selected; evaluating every listed identity
# independently is a reasonable extension left for a follow-up, since this PR targets one
# issue/one root cause at a time.
# ---------------------------------------------------------------------------

$testName = $null
$failingTestMatch = [regex]::Match($issueBody, "##\s*Failing Test\(s\)\s*\r?\n(.*?)(?=\r?\n##\s|\z)", [System.Text.RegularExpressions.RegexOptions]::Singleline)
$distinctIdentities = @()
if ($failingTestMatch.Success)
{
    $distinctIdentities = @(
        [regex]::Matches($failingTestMatch.Groups[1].Value, '`([^`]+)`') |
            ForEach-Object { $_.Groups[1].Value.Trim() } |
            Where-Object { $_.Length -ge 3 -and $_.Length -le 1024 -and $_ -notmatch "[\r\n]" } |
            Select-Object -Unique
    )
}

if ($distinctIdentities.Count -eq 0)
{
    $reasonCodes.Add("test-name-unresolvable")
    Add-MissingEvidence -List $missingEvidence -Kind "test-name" -Detail "Could not deterministically extract any backtick-quoted fully qualified test name from '## Failing Test(s)'."
}
elseif ($distinctIdentities.Count -gt 1)
{
    $reasonCodes.Add("multiple-test-identities-unresolved")
    Add-MissingEvidence -List $missingEvidence -Kind "test-name" -Detail "'## Failing Test(s)' names $($distinctIdentities.Count) distinct test identities ($($distinctIdentities -join '; ')); this collector requires exactly one unambiguous identity per run rather than guessing which one actually failed."
}
else
{
    $testName = $distinctIdentities[0]
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
# Step 2.5: confirm the repository checkout this collector and the evaluator are running against
# is genuinely dotnet/aspnetcore's 'main' branch tip, via a trusted GitHub API response -- never
# label a non-main checkout (e.g. this very prototype PR's branch) as 'main'.
# ---------------------------------------------------------------------------

$repoHeadSha = (& git -C $RepositoryRoot rev-parse HEAD).Trim()
$mainShaResult = Get-TrustedMainShaResult
if (-not [bool]$mainShaResult.Checked)
{
    # Fixture does not model this dimension: trust the checkout (used by fixtures that are not
    # specifically exercising this guard).
    $trustedMainSha = $repoHeadSha
    $matchesMain = $true
}
else
{
    $trustedMainSha = $mainShaResult.TrustedSha
    $matchesMain = ($null -ne $trustedMainSha) -and $repoHeadSha.Equals($trustedMainSha, [System.StringComparison]::OrdinalIgnoreCase)
    if (-not $matchesMain)
    {
        $reasonCodes.Add("repository-ref-not-main")
        $trustedDisplay = if ($trustedMainSha) { $trustedMainSha } else { "(lookup failed)" }
        Add-MissingEvidence -List $missingEvidence -Kind "repository-ref" -Detail "Checked-out commit $repoHeadSha does not match the trusted dotnet/aspnetcore main SHA $trustedDisplay; refusing to label repository_ref.branch as 'main'."
    }
}

# ---------------------------------------------------------------------------
# Step 3: resolve Azure DevOps build metadata for every issue-cited build. Builds whose metadata
# has aged out of Azure DevOps retention are recorded as not-found, never fabricated (see
# aspnetcore#68945).
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
# Step 4: if fewer than two distinct resolved builds are available, perform a capped
# supplementary recurrence scan across the same pipeline definition(s) on 'main' -- direct raw
# AzDO/VSTMR evidence, never the Build Analysis abstraction, per the architecture consensus that
# Build Analysis is corroborating only and cannot establish exact recurrence. Signature matching
# uses ordinal substring containment, never `-like`/`-notlike`: a literal ErrorMessage containing
# `*`, `?`, or `[` would otherwise be misinterpreted as a wildcard pattern instead of literal text.
# ---------------------------------------------------------------------------

function Test-SignatureMatch
{
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Haystack,
        [Parameter(Mandatory = $true)][string]$Signature
    )

    return $Haystack.Contains($Signature, [System.StringComparison]::Ordinal)
}

function Get-MatchingFailureDetail
{
    # Finds the first summary row for $BuildId/$TestName whose VSTMR detail errorMessage+stackTrace
    # contains $Signature (ordinal substring). Returns $null when no such row exists.
    param(
        [Parameter(Mandatory = $true)]$BuildId,
        [Parameter(Mandatory = $true)][string]$TestName,
        [Parameter(Mandatory = $true)][string]$Signature
    )

    foreach ($row in @(Get-VstmrSummaryRows -BuildId $BuildId -TestName $TestName))
    {
        if ([string]$row.outcome -ne "Failed")
        {
            continue
        }
        $detail = Get-VstmrDetail -RunId ([int]$row.runId) -ResultId ([int]$row.id)
        if ($null -eq $detail)
        {
            continue
        }
        $haystack = "$($detail.errorMessage) $($detail.stackTrace)"
        if (Test-SignatureMatch -Haystack $haystack -Signature $Signature)
        {
            return $row
        }
    }

    return $null
}

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

            $matchingRow = Get-MatchingFailureDetail -BuildId $candidateId -TestName $testName -Signature $effectiveSignature
            if ($null -eq $matchingRow)
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
# Step 4.5: gather at least one authoritative negative (passed/skipped) occurrence of the same
# test on the same pipeline(s). This is what lets the evaluator confirm the failure is not a
# consistent regression -- a missing negative is recorded as insufficient evidence, never
# inferred as a pass.
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
            $rows = @(Get-VstmrSummaryRows -BuildId $candidateId -TestName $testName | Where-Object { [string]$_.outcome -in @("Passed", "Skipped") })
            if ($rows.Count -eq 0)
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
# Step 5: for each resolved build, materialize authoritative raw evidence from the VSTMR detail
# result -- never a blindly-capped raw console log. Helix job/work-item coordinates are recorded
# as metadata only when a work item's own crash/'.WorkItemExecution' row happens to carry a
# `comment` field (rare for ordinary xUnit tests, confirmed live); otherwise `helix_unavailable` is
# recorded explicitly and the VSTMR detail text remains authoritative on its own, per aspnetcore's
# real API shape. Platform/configuration are parsed from the authoritative TestRun name, never
# fabricated; "unknown" is recorded when no recognized token is present.
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
    $expectedOutcome = if ($role -eq "failure") { @("Failed") } else { @("Passed", "Skipped") }
    $matchedRow = $null
    $matchedDetail = $null

    foreach ($row in @(Get-VstmrSummaryRows -BuildId $build.id -TestName $testName))
    {
        if ([string]$row.outcome -notin $expectedOutcome)
        {
            continue
        }
        $detail = Get-VstmrDetail -RunId ([int]$row.runId) -ResultId ([int]$row.id)
        if ($null -eq $detail)
        {
            continue
        }
        if ($role -eq "failure" -and $null -ne $effectiveSignature)
        {
            $haystack = "$($detail.errorMessage) $($detail.stackTrace)"
            if (-not (Test-SignatureMatch -Haystack $haystack -Signature $effectiveSignature))
            {
                continue
            }
        }
        $matchedRow = $row
        $matchedDetail = $detail
        break
    }

    if ($null -eq $matchedRow -or $null -eq $matchedDetail)
    {
        $null = $rawEvidenceRecords.Add([ordered]@{
            build_id = $build.id
            role = $role
            found = $false
            captured_utc = $retrievedUtc
            note = "No VSTMR result for build $($build.id) matched the expected outcome/signature for this test."
        })
        Add-MissingEvidence -List $missingEvidence -Kind "vstmr-evidence" -Detail "Build $($build.id): no matching, retrievable VSTMR result detail."
        continue
    }

    $runId = [int]$matchedRow.runId
    $resultId = [int]$matchedRow.id

    $helixJob = $null
    $helixWorkItem = $null
    if ((Test-HasProperty -Object $matchedDetail -Name "comment") -and -not [string]::IsNullOrEmpty($matchedDetail.comment))
    {
        $commentObject = $matchedDetail.comment | ConvertFrom-Json -ErrorAction SilentlyContinue
        if ($null -ne $commentObject -and (Test-HasProperty -Object $commentObject -Name "HelixJobId"))
        {
            $helixJob = [string]$commentObject.HelixJobId
            $helixWorkItem = [string]$commentObject.HelixWorkItemName
        }
    }
    $helixUnavailable = [string]::IsNullOrEmpty($helixJob) -or [string]::IsNullOrEmpty($helixWorkItem)

    $runName = Get-VstmrRunName -RunId $runId
    $platformConfiguration = Get-PlatformConfigurationFromRunName -RunName $runName

    $evidenceIndex += 1
    $fileName = "issue-$IssueNumber-build-$($build.id)-$role.log"
    $evidencePath = Join-Path $EvidenceRoot $fileName

    # The marker line (which Evaluate-TestQuarantineKbeCandidate.ps1 requires to associate a
    # signature match with the declared test) and, for failures, the already-extracted signature
    # are always placed first -- capping to $rawLogCap can then only ever truncate the tail of a
    # long stack trace, never the lines the evaluator actually needs.
    $markerLine = if ($role -eq "failure") { "Failed $testName [reported by VSTMR result $resultId]" } else { "$([string]$matchedRow.outcome) $testName [reported by VSTMR result $resultId]" }
    $bodyText = if ($role -eq "failure")
    {
        "$($matchedDetail.errorMessage)`n$($matchedDetail.stackTrace)"
    }
    else
    {
        "(no error: VSTMR outcome was $([string]$matchedRow.outcome))"
    }
    # Build id is appended defensively so two builds that legitimately share identical evidence
    # text (as small synthetic fixtures sometimes do) never collide to the same content hash.
    $rawContent = "$markerLine`n$bodyText`n(observed in build $($build.id))"
    $cappedContent = Get-CappedExcerpt -Value $rawContent -Cap $rawLogCap -ProtectedPhrases @($testName)
    [System.IO.File]::WriteAllText($evidencePath, $cappedContent)
    $sha256 = (Get-FileHash -LiteralPath $evidencePath -Algorithm SHA256).Hash.ToLowerInvariant()

    $evidenceRecord = [ordered]@{
        build_id = $build.id
        role = $role
        kind = "vstmr-detail"
        run_id = $runId
        result_id = $resultId
        helix_unavailable = $helixUnavailable
        platform = $platformConfiguration.Platform
        configuration = $platformConfiguration.Configuration
        found = $true
        captured_utc = $retrievedUtc
        sha256 = $sha256
        evidence_path = $fileName
    }
    if (-not $helixUnavailable)
    {
        $evidenceRecord["helix_job"] = $helixJob
        $evidenceRecord["helix_workitem"] = $helixWorkItem
    }
    $null = $rawEvidenceRecords.Add($evidenceRecord)

    if ($role -eq "failure")
    {
        $null = $failureBuildIdSet.Add([int]$build.id)
    }
    else
    {
        $negativeCount += 1
    }

    $outcomeValue = switch ([string]$matchedRow.outcome)
    {
        "Failed" { "failed"; break }
        "Passed" { "passed"; break }
        default { "skipped" }
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
            platform = $platformConfiguration.Platform
            configuration = $platformConfiguration.Configuration
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
# Step 6: fetch Build Analysis check-run snapshots. Advisory/corroborating only: recorded
# regardless of outcome, and a missing or generic snapshot never overrides raw evidence gathered
# above. `exact_test_referenced` requires the FULL fully-qualified test name, never a bare method
# name (which commonly collides with unrelated tests); `known_issue_referenced` requires a
# concrete dotnet/aspnetcore issue number/URL near the phrase, not the bare phrase alone (a
# heading or table column label reading "Known Issue" with no associated reference must not set
# this true).
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
            short_name_referenced = $false
            known_issue_referenced = $false
            known_issue_numbers = @()
        })
        continue
    }

    $text = [string]$buildAnalysis.output.text
    $textSha256 = Get-Sha256String -Value $text
    $shortMethodName = if ($testName) { ($testName -split '\.')[-1] } else { $null }
    $exactTestReferenced = ($null -ne $testName) -and $text.Contains($testName, [System.StringComparison]::Ordinal)
    $shortNameReferenced = (-not $exactTestReferenced) -and ($null -ne $shortMethodName) -and $text.Contains($shortMethodName, [System.StringComparison]::Ordinal)

    $knownIssueNumbers = [System.Collections.Generic.List[int]]::new()
    foreach ($phraseMatch in [regex]::Matches($text, "(?i)known issue"))
    {
        $windowStart = $phraseMatch.Index
        $windowLength = [System.Math]::Min(200, $text.Length - $windowStart)
        $window = $text.Substring($windowStart, $windowLength)
        foreach ($numberMatch in [regex]::Matches($window, "dotnet/aspnetcore(?:#|/issues/)(\d+)|(?<![\w/])#(\d+)"))
        {
            $numberText = if ($numberMatch.Groups[1].Success) { $numberMatch.Groups[1].Value } else { $numberMatch.Groups[2].Value }
            $null = $knownIssueNumbers.Add([int]$numberText)
        }
    }
    $knownIssueNumbers = @($knownIssueNumbers | Select-Object -Unique)

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
        short_name_referenced = $shortNameReferenced
        known_issue_referenced = $knownIssueNumbers.Count -gt 0
        known_issue_numbers = $knownIssueNumbers
    })
    $null = $corroboratingContext.Add([ordered]@{ source = "build-analysis"; url = [string]$buildAnalysis.html_url })
}

# ---------------------------------------------------------------------------
# Step 7: categorized duplicate KBE / fix-PR search. A search hit is a discovery candidate only --
# it is fetched and required to contain the exact fully-qualified test name before it is ever
# treated as a validated duplicate; otherwise it is recorded as an unvalidated candidate and never
# contributes to an existing-kbe/existing-fix-pr status. "Recently" closed/merged categories carry
# an explicit time-window qualifier so the label matches what the query actually searches.
# ---------------------------------------------------------------------------

$shortName = if ($testName) { ($testName -split '\.')[-1] } else { $IssueNumber.ToString() }
$recentWindowDate = [System.DateTimeOffset]::UtcNow.AddDays(-$DuplicateSearchWindowDays).ToString("yyyy-MM-dd")
$duplicateQueries = @(
    @{ category = "open-kbe"; query = "repo:$Repository is:issue is:open label:`"Known Build Error`" $shortName" }
    @{ category = "recently-closed-kbe"; query = "repo:$Repository is:issue is:closed closed:>=$recentWindowDate label:`"Known Build Error`" $shortName" }
    @{ category = "open-fix-pr"; query = "repo:$Repository is:pr is:open $shortName" }
    @{ category = "recently-merged-fix-pr"; query = "repo:$Repository is:pr is:merged merged:>=$recentWindowDate $shortName" }
)

$duplicateQueryResults = [System.Collections.Generic.List[object]]::new()
$duplicateReferences = [System.Collections.Generic.List[string]]::new()
$unvalidatedCandidates = [System.Collections.Generic.List[object]]::new()
$kbeNumbers = [System.Collections.Generic.List[int]]::new()
$fixPrNumbers = [System.Collections.Generic.List[int]]::new()
$allQueriesComplete = $true

foreach ($q in $duplicateQueries)
{
    $searchResult = if ($isFixtureMode)
    {
        $categoryName = [string]$q.category
        if (Test-HasProperty -Object $fixture.duplicate_search -Name $categoryName)
        {
            $entry = $fixture.duplicate_search.$categoryName
            [ordered]@{ Complete = [bool]$entry.complete; Numbers = @($entry.result_numbers); TotalCount = [int]$(if (Test-HasProperty -Object $entry -Name "total_count") { $entry.total_count } else { @($entry.result_numbers).Count }) }
        }
        else
        {
            [ordered]@{ Complete = $false; Numbers = @(); TotalCount = 0 }
        }
    }
    else
    {
        Search-GitHubIssues -Query $q.query
    }

    if (-not [bool]$searchResult.Complete)
    {
        $allQueriesComplete = $false
    }

    $isKbeCategory = $q.category -in @("open-kbe", "recently-closed-kbe")
    foreach ($n in @($searchResult.Numbers))
    {
        $candidateText = Get-DuplicateCandidateText -Number $n
        $validated = ($null -ne $testName) -and ($null -ne $candidateText) -and $candidateText.Contains($testName, [System.StringComparison]::Ordinal)
        if ($validated)
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
        else
        {
            $reason = if ($null -eq $testName) { "no resolved test identity to validate against" }
                elseif ($null -eq $candidateText) { "could not fetch issue/PR #$n to validate test identity" }
                else { "issue/PR #$n does not contain the exact fully-qualified test name" }
            $null = $unvalidatedCandidates.Add([ordered]@{ category = $q.category; number = $n; reason = $reason })
        }
    }

    $null = $duplicateQueryResults.Add([ordered]@{
        category = $q.category
        query = $q.query
        complete = [bool]$searchResult.Complete
        result_numbers = @($searchResult.Numbers)
        total_count = [int]$searchResult.TotalCount
    })
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
    Add-MissingEvidence -List $missingEvidence -Kind "duplicate-search" -Detail "At least one duplicate KBE/fix-PR search category returned incomplete or truncated results."
}

$duplicateQueriesForCandidate = @($duplicateQueryResults | ForEach-Object {
    [ordered]@{
        category = $_.category
        query = $_.query
        complete = $_.complete
        result_numbers = $_.result_numbers
    }
})

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
    queries = $duplicateQueriesForCandidate
}

# The dossier's own duplicate_search $def additionally carries total_count and
# unvalidated_candidates -- both new, dossier-only provenance fields. candidate.duplicate_check
# above deliberately keeps the exact, unmodified shape test-quarantine-kbe-shadow-candidate.schema.json
# requires (additionalProperties: false; no total_count/unvalidated_candidates there).
$duplicateCheckWithUnvalidated = [ordered]@{
    status = $duplicateStatus
    checked_utc = $retrievedUtc
    coverage = $duplicateCheck.coverage
    references = $duplicateCheck.references
    queries = @($duplicateQueryResults)
    unvalidated_candidates = @($unvalidatedCandidates)
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
        has_workflow_marker = $hasWorkflowMarker
    }
    outcome = $outcome
    provenance = [ordered]@{
        repository_ref_verification = [ordered]@{
            checkout_sha = $repoHeadSha
            trusted_main_sha = $trustedMainSha
            matches_main = $matchesMain
        }
        azdo_builds = @($azdoBuildRecords)
        check_run_snapshots = @($checkRunRecords)
        raw_evidence_sources = @($rawEvidenceRecords)
        duplicate_search = $duplicateCheckWithUnvalidated
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
