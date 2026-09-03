#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deterministic, offline tests for Collect-TestQuarantineKbeEvidence.ps1.

.DESCRIPTION
    Exercises the collector entirely in -FixtureRoot mode (zero network access) against the
    three real pilot quarantine issues recorded in fixtures/, against a battery of synthetic
    edge-case fixtures for gates not represented by those three issues, and against
    Merge-AzdoBuildLists directly as a pure unit. Golden dossier comparisons exclude
    collector-generated timestamps and the running repository's HEAD commit SHA, both of which
    are expected to differ between runs/commits; every other field must match exactly.

    Where the collector's outcome is 'candidate', the resulting candidate.json is also fed
    through Evaluate-TestQuarantineKbeCandidate.ps1 to prove the two scripts reconcile: the
    collector's output is accepted as-is by the versioned evaluator contract.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$collector = "$PSScriptRoot/Collect-TestQuarantineKbeEvidence.ps1"
$evaluator = "$PSScriptRoot/Evaluate-TestQuarantineKbeCandidate.ps1"
$summaryGenerator = "$PSScriptRoot/New-TestQuarantineKbeSummary.ps1"
$dossierSchema = "$PSScriptRoot/test-quarantine-kbe-shadow-dossier.schema.json"
$candidateSchema = "$PSScriptRoot/test-quarantine-kbe-shadow-candidate.schema.json"
$fixturesRoot = "$PSScriptRoot/fixtures"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "aspnetcore-kbe-shadow-collector-$([System.Guid]::NewGuid().ToString('N'))"
$repositoryRoot = (Resolve-Path "$PSScriptRoot/../../../..").Path
$repositoryHead = (& git -C $repositoryRoot rev-parse HEAD).Trim()
$fixtureEventRef = "refs/heads/main"
$fixtureEventSha = $repositoryHead
$originalGitHubRef = $env:GITHUB_REF
$originalGitHubSha = $env:GITHUB_SHA

function Assert-Equal
{
    param(
        [Parameter(Mandatory = $true)][AllowNull()]$Actual,
        [Parameter(Mandatory = $true)][AllowNull()]$Expected,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ("$Actual" -ne "$Expected")
    {
        throw "$Message Expected '$Expected', actual '$Actual'."
    }
}

function Assert-Contains
{
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Collection,
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (@($Collection | Where-Object { "$_" -eq "$Value" }).Count -eq 0)
    {
        throw "$Message Expected collection to contain '$Value'; actual: $($Collection -join ', ')."
    }
}

function Assert-NotContains
{
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Collection,
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (@($Collection | Where-Object { "$_" -eq "$Value" }).Count -gt 0)
    {
        throw "$Message Expected collection NOT to contain '$Value'; actual: $($Collection -join ', ')."
    }
}

# The collector-generated timestamps and the running checkout's HEAD commit SHA are expected to
# differ run-to-run and commit-to-commit; every golden fixture below was captured with these
# fields already replaced by this same sentinel.
$volatileKeys = @("generated_utc", "retrieved_utc", "captured_utc", "checked_utc", "commit_sha", "event_sha", "checkout_sha", "current_main_sha")
$volatileSentinel = "<GENERATED>"

function ConvertTo-NormalizedObject
{
    param($Value)

    if ($null -eq $Value)
    {
        return $null
    }
    if ($Value -is [System.Management.Automation.PSCustomObject])
    {
        $result = [ordered]@{}
        foreach ($prop in $Value.PSObject.Properties)
        {
            $result[$prop.Name] = if ($volatileKeys -contains $prop.Name) { $volatileSentinel } else { ConvertTo-NormalizedObject -Value $prop.Value }
        }
        return [PSCustomObject]$result
    }
    if ($Value -is [array])
    {
        return ,@($Value | ForEach-Object { ConvertTo-NormalizedObject -Value $_ })
    }
    return $Value
}

function Test-DeepEqual
{
    param($Expected, $Actual, [string]$Path = "`$")

    if ($null -eq $Expected -or $null -eq $Actual)
    {
        if ($null -ne $Expected -or $null -ne $Actual)
        {
            throw "Golden mismatch at ${Path}: expected '$Expected', actual '$Actual'."
        }
        return
    }

    if ($Expected -is [System.Management.Automation.PSCustomObject] -and $Actual -is [System.Management.Automation.PSCustomObject])
    {
        $expectedNames = @($Expected.PSObject.Properties.Name | Sort-Object)
        $actualNames = @($Actual.PSObject.Properties.Name | Sort-Object)
        $diff = Compare-Object -ReferenceObject $expectedNames -DifferenceObject $actualNames
        if ($null -ne $diff)
        {
            throw "Golden mismatch at ${Path}: property set differs (expected: $($expectedNames -join ','); actual: $($actualNames -join ','))."
        }
        foreach ($name in $expectedNames)
        {
            Test-DeepEqual -Expected $Expected.$name -Actual $Actual.$name -Path "$Path.$name"
        }
        return
    }

    if ($Expected -is [array] -and $Actual -is [array])
    {
        if ($Expected.Count -ne $Actual.Count)
        {
            throw "Golden mismatch at ${Path}: array length differs (expected $($Expected.Count), actual $($Actual.Count))."
        }
        for ($i = 0; $i -lt $Expected.Count; $i++)
        {
            Test-DeepEqual -Expected $Expected[$i] -Actual $Actual[$i] -Path "$Path[$i]"
        }
        return
    }

    if ("$Expected" -ne "$Actual")
    {
        throw "Golden mismatch at ${Path}: expected '$Expected', actual '$Actual'."
    }
}

function Invoke-Collector
{
    param(
        [Parameter(Mandatory = $true)][int]$IssueNumber,
        [Parameter(Mandatory = $true)][string]$FixtureRoot,
        [Parameter(Mandatory = $true)][string]$WorkDirectory,
        [string]$Signature,
        [string]$EventRef,
        [string]$EventSha
    )

    [System.IO.Directory]::CreateDirectory($WorkDirectory) | Out-Null
    $dossierPath = Join-Path $WorkDirectory "dossier.json"
    $candidatePath = Join-Path $WorkDirectory "candidate.json"
    $evidenceRoot = Join-Path $WorkDirectory "evidence"

    $params = @{
        IssueNumber = $IssueNumber
        OutputFile = $dossierPath
        CandidateFile = $candidatePath
        EvidenceRoot = $evidenceRoot
        FixtureRoot = $FixtureRoot
        RepositoryRoot = $repositoryRoot
        EventRef = if ([string]::IsNullOrEmpty($EventRef)) { $fixtureEventRef } else { $EventRef }
        EventSha = if ([string]::IsNullOrEmpty($EventSha)) { $fixtureEventSha } else { $EventSha }
        DossierSchemaFile = $dossierSchema
        CandidateSchemaFile = $candidateSchema
    }
    if (-not [string]::IsNullOrEmpty($Signature))
    {
        $params["Signature"] = $Signature
    }
    & $collector @params | Out-Null

    return [ordered]@{
        DossierPath = $dossierPath
        CandidatePath = $candidatePath
        EvidenceRoot = $evidenceRoot
        Dossier = (Get-Content -LiteralPath $dossierPath -Raw | ConvertFrom-Json -Depth 32)
    }
}

function Assert-GoldenDossier
{
    param(
        [Parameter(Mandatory = $true)][string]$IssueDirectory,
        [Parameter(Mandatory = $true)]$ActualDossier
    )

    $goldenPath = Join-Path $IssueDirectory "expected-dossier.json"
    $golden = Get-Content -LiteralPath $goldenPath -Raw | ConvertFrom-Json -Depth 32
    $normalizedActual = ConvertTo-NormalizedObject -Value $ActualDossier
    Test-DeepEqual -Expected $golden -Actual $normalizedActual
}

function New-SyntheticFixture
{
    # Writes a minimal-but-complete fixture.json (every top-level key the collector expects to
    # be able to read present, even if empty) to a fresh directory under $tempRoot and returns
    # its path.
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][hashtable]$Overrides
    )

    $base = [ordered]@{
        issue = $null
        main_branch = $null
        azdo_builds = [ordered]@{}
        recurrence_scan = [ordered]@{}
        negative_scan = [ordered]@{}
        vstmr_summary = [ordered]@{}
        vstmr_detail = [ordered]@{}
        vstmr_runs = [ordered]@{}
        check_runs = [ordered]@{}
        duplicate_search = [ordered]@{}
        duplicate_candidate_text = [ordered]@{}
    }
    foreach ($key in $Overrides.Keys)
    {
        $base[$key] = $Overrides[$key]
    }
    if ($null -ne $base.issue -and -not $base.issue.Contains("user"))
    {
        $base.issue["user"] = [ordered]@{ login = "app/github-actions" }
    }
    if ($null -eq $base["main_branch"])
    {
        $base.Remove("main_branch")
    }

    $fixtureBuilds = @($base.azdo_builds.Values)
    foreach ($scan in @($base.recurrence_scan, $base.negative_scan))
    {
        foreach ($entry in $scan.Values)
        {
            $fixtureBuilds += @($entry)
        }
    }
    foreach ($build in $fixtureBuilds)
    {
        if (-not $build.Contains("sourceBranch"))
        {
            $build["sourceBranch"] = "refs/heads/main"
        }
        if (-not $build.Contains("status"))
        {
            $build["status"] = "completed"
        }
    }

    $directory = Join-Path $tempRoot "fixture-$Name"
    [System.IO.Directory]::CreateDirectory($directory) | Out-Null
    $base | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath (Join-Path $directory "fixture.json")
    return $directory
}

function New-DerivedFixture
{
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][scriptblock]$Mutate
    )

    $fixtureObject = Get-Content -LiteralPath (Join-Path $Source "fixture.json") -Raw | ConvertFrom-Json -Depth 32
    & $Mutate $fixtureObject
    $directory = Join-Path $tempRoot "fixture-$Name"
    [System.IO.Directory]::CreateDirectory($directory) | Out-Null
    $fixtureObject | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath (Join-Path $directory "fixture.json")
    return $directory
}

$workflowMarker = @"
<!-- gh-aw-agentic-workflow: Daily Test Quarantine Management, engine: copilot, model: auto, id: 123456789, workflow_id: test-quarantine, run: https://github.com/dotnet/aspnetcore/actions/runs/123456789 -->
<!-- gh-aw-workflow-id: test-quarantine -->
<!-- gh-aw-workflow-call-id: dotnet/aspnetcore/test-quarantine -->
"@
# Built from a single-quoted literal (no backtick-escape processing) rather than embedding
# repeated backtick-escape sequences directly in double-quoted synthetic issue bodies below,
# which is easy to miscount (a stray extra backtick silently becomes an unrelated `t`/`n`/etc.
# escape sequence instead of a literal backtick).
$codeFence = '```'
$defaultDuplicateSearch = [ordered]@{
    "open-kbe" = [ordered]@{ complete = $true; result_numbers = @(); total_count = 0 }
    "recently-closed-kbe" = [ordered]@{ complete = $true; result_numbers = @(); total_count = 0 }
    "open-fix-pr" = [ordered]@{ complete = $true; result_numbers = @(); total_count = 0 }
    "recently-merged-fix-pr" = [ordered]@{ complete = $true; result_numbers = @(); total_count = 0 }
}

try
{
    [System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null
    $env:GITHUB_REF = "refs/pull/69021/merge"
    $env:GITHUB_SHA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"

    # ------------------------------------------------------------------
    # Pilot 1 -- aspnetcore#68724: '## Failing Test(s)' names two distinct concrete test
    # identities (a base test and its server-execution subclass override). Live data shows only
    # the override actually failed while the base identity passed -- the collector must fail
    # closed rather than silently bind evidence to the first name it sees.
    # ------------------------------------------------------------------
    $result68724 = Invoke-Collector -IssueNumber 68724 -FixtureRoot "$fixturesRoot/68724" -WorkDirectory "$tempRoot/68724"
    Assert-Equal -Actual $result68724.Dossier.outcome -Expected "incomplete" -Message "#68724 outcome mismatch."
    Assert-Contains -Collection @($result68724.Dossier.incomplete.reason_codes) -Value "multiple-test-identities-unresolved" -Message "#68724 reason codes mismatch."
    Assert-NotContains -Collection @($result68724.Dossier.incomplete.reason_codes) -Value "workflow-dispatch-ref-not-main" -Message "Ambient GITHUB_REF must not affect fixture collection."
    Assert-Equal -Actual $result68724.Dossier.provenance.repository_ref_verification.event_ref -Expected $fixtureEventRef -Message "Fixture event ref must be explicit and deterministic."
    Assert-Equal -Actual $result68724.Dossier.provenance.repository_ref_verification.event_sha -Expected $fixtureEventSha -Message "Fixture event SHA must be explicit and deterministic."
    Assert-GoldenDossier -IssueDirectory "$fixturesRoot/68724" -ActualDossier $result68724.Dossier

    # ------------------------------------------------------------------
    # Pilot 2 -- aspnetcore#68947: the issue body has no fenced '## Error Message' block, so
    # deterministic extraction is ambiguous without a manual signature. Its own cited second
    # build (1537561) has aged out of Azure DevOps retention; recurrence is instead established
    # by the collector's supplementary recurrence scan, which must also query
    # resultFilter=partiallySucceeded (the cited build 1551326's own real result value) in
    # addition to resultFilter=failed.
    # ------------------------------------------------------------------
    $result68947NoSig = Invoke-Collector -IssueNumber 68947 -FixtureRoot "$fixturesRoot/68947" -WorkDirectory "$tempRoot/68947-no-signature"
    Assert-Equal -Actual $result68947NoSig.Dossier.outcome -Expected "incomplete" -Message "#68947 (no signature) outcome mismatch."
    Assert-Contains -Collection @($result68947NoSig.Dossier.incomplete.reason_codes) -Value "signature-extraction-ambiguous" -Message "#68947 (no signature) reason codes mismatch."

    $signature68947 = "OpenQA.Selenium.WebDriverException : The HTTP request to the remote WebDriver server"
    $result68947 = Invoke-Collector -IssueNumber 68947 -FixtureRoot "$fixturesRoot/68947" -WorkDirectory "$tempRoot/68947" -Signature $signature68947
    Assert-Equal -Actual $result68947.Dossier.outcome -Expected "incomplete" -Message "#68947 outcome mismatch."
    Assert-Contains -Collection @($result68947.Dossier.incomplete.reason_codes) -Value "passed-evidence-not-interleaved" -Message "#68947 must not count its pass that predates both collected failures."
    Assert-Equal -Actual $result68947.Dossier.candidate -Expected $null -Message "#68947 must not emit a candidate without a contemporaneous environment-matched pass."
    Assert-GoldenDossier -IssueDirectory "$fixturesRoot/68947" -ActualDossier $result68947.Dossier

    $summaryPath68947 = Join-Path "$tempRoot/68947" "summary.md"
    & $summaryGenerator -DossierFile $result68947.DossierPath -OutputFile $summaryPath68947
    $summaryText68947 = Get-Content -LiteralPath $summaryPath68947 -Raw
    if (-not $summaryText68947.Contains("passed-evidence-not-interleaved"))
    {
        throw "#68947 summary must mention the chronology failure."
    }

    # ------------------------------------------------------------------
    # Pilot 3 -- aspnetcore#68945: both cited builds' Azure DevOps build-level metadata still
    # resolves, but the second build's historical VSTMR test-result data is no longer queryable,
    # leaving only one usable failure log below the two-build recurrence floor. The collector
    # must fail closed rather than infer a pass or fabricate evidence.
    # ------------------------------------------------------------------
    $result68945 = Invoke-Collector -IssueNumber 68945 -FixtureRoot "$fixturesRoot/68945" -WorkDirectory "$tempRoot/68945" -Signature "System.Threading.Tasks.TaskCanceledException: The operation was canceled."
    Assert-Equal -Actual $result68945.Dossier.outcome -Expected "incomplete" -Message "#68945 outcome mismatch."
    Assert-Contains -Collection @($result68945.Dossier.incomplete.reason_codes) -Value "raw-evidence-insufficient" -Message "#68945 reason codes must record the insufficient evidence."
    Assert-Equal -Actual $result68945.Dossier.candidate -Expected $null -Message "#68945 must not emit a candidate."
    Assert-GoldenDossier -IssueDirectory "$fixturesRoot/68945" -ActualDossier $result68945.Dossier

    $summaryPath68945 = Join-Path "$tempRoot/68945" "summary.md"
    & $summaryGenerator -DossierFile $result68945.DossierPath -OutputFile $summaryPath68945
    $summaryText68945 = Get-Content -LiteralPath $summaryPath68945 -Raw
    if (-not $summaryText68945.Contains("raw-evidence-insufficient"))
    {
        throw "#68945 summary must mention the raw-evidence-insufficient reason code."
    }

    # ------------------------------------------------------------------
    # Edge case: a closed issue must fail closed regardless of label/marker.
    # ------------------------------------------------------------------
    $closedDir = New-SyntheticFixture -Name "closed-issue" -Overrides @{
        issue = [ordered]@{
            number = 1
            state = "closed"
            labels = @("test-failure")
            body = "## Failing Test(s)`n`` Sample.Tests.Closed ``$([System.Environment]::NewLine)$workflowMarker"
        }
        duplicate_search = $defaultDuplicateSearch
    }
    $resultClosed = Invoke-Collector -IssueNumber 1 -FixtureRoot $closedDir -WorkDirectory (Join-Path $tempRoot "closed-issue")
    Assert-Equal -Actual $resultClosed.Dossier.outcome -Expected "incomplete" -Message "Closed-issue outcome mismatch."
    Assert-Contains -Collection @($resultClosed.Dossier.incomplete.reason_codes) -Value "issue-not-open" -Message "Closed-issue reason codes mismatch."

    # ------------------------------------------------------------------
    # Edge case: an issue missing the canonical 'test-failure' label must fail closed.
    # ------------------------------------------------------------------
    $unlabeledDir = New-SyntheticFixture -Name "unlabeled-issue" -Overrides @{
        issue = [ordered]@{
            number = 2
            state = "open"
            labels = @("area-blazor")
            body = "## Failing Test(s)`n`` Sample.Tests.Unlabeled ``$([System.Environment]::NewLine)$workflowMarker"
        }
        duplicate_search = $defaultDuplicateSearch
    }
    $resultUnlabeled = Invoke-Collector -IssueNumber 2 -FixtureRoot $unlabeledDir -WorkDirectory (Join-Path $tempRoot "unlabeled-issue")
    Assert-Equal -Actual $resultUnlabeled.Dossier.outcome -Expected "incomplete" -Message "Unlabeled-issue outcome mismatch."
    Assert-Contains -Collection @($resultUnlabeled.Dossier.incomplete.reason_codes) -Value "issue-not-canonical-quarantine" -Message "Unlabeled-issue reason codes mismatch."

    # ------------------------------------------------------------------
    # Edge case (item 5): the 'test-failure' label alone is not proof of quarantine automation --
    # an issue carrying the label and open state, but with no trusted gh-aw workflow marker in
    # its body, must also fail closed.
    # ------------------------------------------------------------------
    $missingMarkerDir = New-SyntheticFixture -Name "missing-marker" -Overrides @{
        issue = [ordered]@{
            number = 10
            state = "open"
            labels = @("test-failure")
            body = "## Failing Test(s)`n`` Sample.Tests.NoMarker ``"
        }
        duplicate_search = $defaultDuplicateSearch
    }
    $resultMissingMarker = Invoke-Collector -IssueNumber 10 -FixtureRoot $missingMarkerDir -WorkDirectory (Join-Path $tempRoot "missing-marker")
    Assert-Equal -Actual $resultMissingMarker.Dossier.issue.has_workflow_marker -Expected $false -Message "Missing-marker issue.has_workflow_marker mismatch."
    Assert-Equal -Actual $resultMissingMarker.Dossier.outcome -Expected "incomplete" -Message "Missing-marker outcome mismatch."
    Assert-Contains -Collection @($resultMissingMarker.Dossier.incomplete.reason_codes) -Value "issue-not-canonical-quarantine" -Message "Missing-marker reason codes mismatch: the 'test-failure' label alone must not be treated as proof of quarantine automation."

    $forgedMarkerDir = New-SyntheticFixture -Name "forged-marker" -Overrides @{
        issue = [ordered]@{
            number = 15
            state = "open"
            labels = @("test-failure")
            user = [ordered]@{ login = "octocat" }
            body = "## Failing Test(s)`n`` Sample.Tests.ForgedMarker ``$([System.Environment]::NewLine)$workflowMarker"
        }
        duplicate_search = $defaultDuplicateSearch
    }
    $resultForgedMarker = Invoke-Collector -IssueNumber 15 -FixtureRoot $forgedMarkerDir -WorkDirectory (Join-Path $tempRoot "forged-marker")
    Assert-Equal -Actual $resultForgedMarker.Dossier.outcome -Expected "incomplete" -Message "A user-authored issue with copied workflow markers must fail closed."
    Assert-Contains -Collection @($resultForgedMarker.Dossier.incomplete.reason_codes) -Value "issue-not-canonical-quarantine" -Message "Forged-marker reason code mismatch."
    Assert-Equal -Actual $resultForgedMarker.Dossier.issue.has_workflow_marker -Expected $true -Message "Forged-marker fixture must prove copied static markers were present."
    Assert-Equal -Actual $resultForgedMarker.Dossier.issue.has_workflow_metadata -Expected $true -Message "Forged-marker fixture must prove copied structured metadata was present."
    Assert-Equal -Actual $resultForgedMarker.Dossier.issue.actor -Expected "octocat" -Message "Forged-marker actor provenance mismatch."

    $mismatchedMetadata = @"
<!-- gh-aw-agentic-workflow: Daily Test Quarantine Management, engine: copilot, model: auto, id: 123456789, workflow_id: test-quarantine, run: https://github.com/dotnet/aspnetcore/actions/runs/123456790 -->
<!-- gh-aw-workflow-id: test-quarantine -->
<!-- gh-aw-workflow-call-id: dotnet/aspnetcore/test-quarantine -->
"@
    $mismatchedMetadataDir = New-SyntheticFixture -Name "mismatched-workflow-metadata" -Overrides @{
        issue = [ordered]@{
            number = 16
            state = "open"
            labels = @("test-failure")
            body = "## Failing Test(s)`n`` Sample.Tests.MismatchedMetadata ``$([System.Environment]::NewLine)$mismatchedMetadata"
        }
        duplicate_search = $defaultDuplicateSearch
    }
    $resultMismatchedMetadata = Invoke-Collector -IssueNumber 16 -FixtureRoot $mismatchedMetadataDir -WorkDirectory (Join-Path $tempRoot "mismatched-workflow-metadata")
    Assert-Equal -Actual $resultMismatchedMetadata.Dossier.outcome -Expected "incomplete" -Message "Mismatched workflow metadata must fail closed."
    Assert-Equal -Actual $resultMismatchedMetadata.Dossier.issue.has_workflow_metadata -Expected $false -Message "Mismatched workflow run IDs must not validate."

    # ------------------------------------------------------------------
    # The immutable dispatch SHA must be confirmed as a member of main. A deliberately unrelated
    # current-main SHA must fail closed rather than mislabel the checkout.
    # ------------------------------------------------------------------
    $repoRefMismatchDir = New-SyntheticFixture -Name "repo-ref-mismatch" -Overrides @{
        issue = [ordered]@{
            number = 13
            state = "open"
            labels = @("test-failure")
            body = "## Failing Test(s)`n`` Sample.Tests.RepoRefMismatch ``$([System.Environment]::NewLine)$workflowMarker"
        }
        main_branch = [ordered]@{ sha = "0000000000000000000000000000000000000000" }
        duplicate_search = $defaultDuplicateSearch
    }
    $resultRepoRefMismatch = Invoke-Collector -IssueNumber 13 -FixtureRoot $repoRefMismatchDir -WorkDirectory (Join-Path $tempRoot "repo-ref-mismatch")
    Assert-Equal -Actual $resultRepoRefMismatch.Dossier.outcome -Expected "incomplete" -Message "Repository-ref-mismatch outcome mismatch."
    Assert-Contains -Collection @($resultRepoRefMismatch.Dossier.incomplete.reason_codes) -Value "repository-ref-not-main" -Message "Repository-ref-mismatch reason codes mismatch."
    Assert-Equal -Actual $resultRepoRefMismatch.Dossier.provenance.repository_ref_verification.matches_main -Expected $false -Message "Repository-ref-mismatch matches_main mismatch."

    # ------------------------------------------------------------------
    # Edge case (item 6): a Build Analysis snapshot naming only the bare method name (which
    # commonly collides with unrelated tests) must never set exact_test_referenced; a generic
    # "Known Issues" heading with no associated concrete issue number/URL must never set
    # known_issue_referenced. Only the full fully-qualified name, and only a concrete issue
    # reference, may set these flags.
    # ------------------------------------------------------------------
    $flagsTestName = "Sample.Tests.ExactMatchCase"
    $flagsSignature = "System.InvalidOperationException: Sample failure for exact-match testing."
    $flagsShaA = "f4d9777d7b9a3d45c88e1ca1b10609e412cc4ade"
    $flagsShaB = "e06ef94591aaa5a8dc3f84926f8664b2964bf0ea"
    $flagsShaC = "51e066210e1643430dccb9986c42500d3e706638"
    $flagsDir = New-SyntheticFixture -Name "check-run-flags" -Overrides @{
        main_branch = [ordered]@{ sha = ("b" * 40); contains_event_sha = $true }
        issue = [ordered]@{
            number = 11
            state = "open"
            labels = @("test-failure")
            body = "## Failing Test(s)`n`` $flagsTestName ``$([System.Environment]::NewLine)## Error Message$([System.Environment]::NewLine)${codeFence}text$([System.Environment]::NewLine)$flagsSignature$([System.Environment]::NewLine)$codeFence$([System.Environment]::NewLine)## Build$([System.Environment]::NewLine)https://dev.azure.com/dnceng-public/public/_build/results?buildId=6100001$([System.Environment]::NewLine)$workflowMarker"
        }
        azdo_builds = [ordered]@{
            "6100001" = [ordered]@{ definition = [ordered]@{ id = 83 }; sourceVersion = $flagsShaA; startTime = "2026-08-01T00:00:00Z"; finishTime = "2026-08-01T01:00:00Z"; result = "failed" }
        }
        recurrence_scan = [ordered]@{
            "83" = @([ordered]@{ id = 6100002; sourceVersion = $flagsShaB; startTime = "2026-07-30T00:00:00Z"; finishTime = "2026-07-30T01:00:00Z"; result = "failed" })
        }
        negative_scan = [ordered]@{
            "83" = @([ordered]@{ id = 6100003; sourceVersion = $flagsShaC; startTime = "2026-07-31T00:00:00Z"; finishTime = "2026-07-31T01:00:00Z"; result = "succeeded" })
        }
        vstmr_summary = [ordered]@{
            "6100001" = @([ordered]@{ id = 1; runId = 7100001; outcome = "Failed"; automatedTestName = $flagsTestName })
            "6100002" = @([ordered]@{ id = 2; runId = 7100002; outcome = "Failed"; automatedTestName = $flagsTestName })
            "6100003" = @([ordered]@{ id = 3; runId = 7100003; outcome = "Passed"; automatedTestName = $flagsTestName })
        }
        vstmr_detail = [ordered]@{
            "7100001:1" = [ordered]@{ outcome = "Failed"; errorMessage = $flagsSignature; stackTrace = "at $flagsTestName.Run() (build a)" }
            "7100002:2" = [ordered]@{ outcome = "Failed"; errorMessage = $flagsSignature; stackTrace = "at $flagsTestName.Run() (build b)" }
            "7100003:3" = [ordered]@{ outcome = "Passed"; errorMessage = $null; stackTrace = $null }
        }
        vstmr_runs = [ordered]@{
            "7100001" = [ordered]@{ name = "Quarantine-Mono-Windows-Debug-xunit" }
            "7100002" = [ordered]@{ name = "Quarantine-Mono-Windows-Debug-xunit" }
            "7100003" = [ordered]@{ name = "Quarantine-Mono-Windows-Debug-xunit" }
        }
        check_runs = [ordered]@{
            $flagsShaA = @([ordered]@{
                name = "Build Analysis"; id = 1; conclusion = "failure"
                output = [ordered]@{ title = "1 failing test"; text = "$flagsTestName failed. This matches a Known Issue: dotnet/aspnetcore#70000." }
                html_url = "https://github.com/dotnet/aspnetcore/runs/1"
            })
            $flagsShaB = @([ordered]@{
                name = "Build Analysis"; id = 2; conclusion = "failure"
                # Only the bare method name appears (embedded in an unrelated identifier, not the
                # full FQN), and "Known Issues" is a generic heading with no associated number.
                output = [ordered]@{ title = "1 failing test"; text = "## Known Issues`nSomeOtherExactMatchCaseVariant failed for unrelated reasons. See the table above." }
                html_url = "https://github.com/dotnet/aspnetcore/runs/2"
            })
        }
        duplicate_search = $defaultDuplicateSearch
    }
    $resultFlags = Invoke-Collector -IssueNumber 11 -FixtureRoot $flagsDir -WorkDirectory (Join-Path $tempRoot "check-run-flags")
    Assert-Equal -Actual $resultFlags.Dossier.outcome -Expected "candidate" -Message "check-run-flags outcome mismatch."
    Assert-Equal -Actual $resultFlags.Dossier.provenance.repository_ref_verification.dispatch_sha_on_main -Expected $true -Message "A dispatch SHA that is an ancestor/member of advanced main must validate."
    Assert-Equal -Actual $resultFlags.Dossier.provenance.repository_ref_verification.current_main_sha -Expected ("b" * 40) -Message "Current main SHA provenance mismatch."
    Assert-Equal -Actual $resultFlags.Dossier.provenance.repository_ref_verification.checkout_sha -Expected $repositoryHead -Message "Dispatch checkout SHA provenance mismatch."
    $snapshotA = @($resultFlags.Dossier.provenance.check_run_snapshots | Where-Object { $_.source_version -eq $flagsShaA })[0]
    $snapshotB = @($resultFlags.Dossier.provenance.check_run_snapshots | Where-Object { $_.source_version -eq $flagsShaB })[0]
    Assert-Equal -Actual $snapshotA.exact_test_referenced -Expected $true -Message "exact_test_referenced must be true when the full FQN appears verbatim."
    Assert-Equal -Actual $snapshotA.known_issue_referenced -Expected $true -Message "known_issue_referenced must be true when a concrete issue number follows 'Known Issue'."
    Assert-Contains -Collection @($snapshotA.known_issue_numbers) -Value 70000 -Message "known_issue_numbers must record the referenced issue."
    Assert-Equal -Actual $snapshotB.exact_test_referenced -Expected $false -Message "exact_test_referenced must stay false for a bare-method-name collision."
    Assert-Equal -Actual $snapshotB.short_name_referenced -Expected $true -Message "short_name_referenced must record the bare-method-name match."
    Assert-Equal -Actual $snapshotB.known_issue_referenced -Expected $false -Message "known_issue_referenced must stay false for a generic 'Known Issues' heading with no associated number."

    $flagsReceiptPath = Join-Path "$tempRoot/check-run-flags" "receipt.json"
    & $evaluator -CandidateFile $resultFlags.CandidatePath -EvidenceRoot $resultFlags.EvidenceRoot -OutputFile $flagsReceiptPath -RepositoryRoot $repositoryRoot -CandidateSchemaFile $candidateSchema
    $flagsReceipt = Get-Content -LiteralPath $flagsReceiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $flagsReceipt.deterministic_status -Expected "validated" -Message "Collector/evaluator pass-eligibility rules must reconcile."

    $multiplePassRowsDir = New-DerivedFixture -Name "multiple-pass-environments" -Source $flagsDir -Mutate {
        param($fixtureObject)
        $fixtureObject.vstmr_summary.'6100003' = @(
            [PSCustomObject]@{ id = 4; runId = 7100004; outcome = "Passed"; automatedTestName = $flagsTestName },
            $fixtureObject.vstmr_summary.'6100003'[0]
        )
        $fixtureObject.vstmr_detail | Add-Member -NotePropertyName '7100004:4' -NotePropertyValue ([PSCustomObject]@{ outcome = "Passed"; errorMessage = $null; stackTrace = $null })
        $fixtureObject.vstmr_runs | Add-Member -NotePropertyName '7100004' -NotePropertyValue ([PSCustomObject]@{ name = "Quarantine-Mono-Linux-Release-xunit" })
    }
    $resultMultiplePassRows = Invoke-Collector -IssueNumber 11 -FixtureRoot $multiplePassRowsDir -WorkDirectory (Join-Path $tempRoot "multiple-pass-environments")
    Assert-Equal -Actual $resultMultiplePassRows.Dossier.outcome -Expected "candidate" -Message "Collector must prefer an environment-matched pass row from a multi-run build."
    $selectedPassSource = @($resultMultiplePassRows.Dossier.provenance.raw_evidence_sources | Where-Object { $_.role -eq "negative" })[0]
    Assert-Equal -Actual $selectedPassSource.run_id -Expected 7100003 -Message "Collector selected the wrong pass TestRun environment."

    $nonMainResult = Invoke-Collector `
        -IssueNumber 11 `
        -FixtureRoot $flagsDir `
        -WorkDirectory (Join-Path $tempRoot "non-main-dispatch") `
        -EventRef "refs/heads/feature/quarantine" `
        -EventSha $repositoryHead
    Assert-Equal -Actual $nonMainResult.Dossier.outcome -Expected "incomplete" -Message "A non-main workflow dispatch must fail closed."
    Assert-Contains -Collection @($nonMainResult.Dossier.incomplete.reason_codes) -Value "workflow-dispatch-ref-not-main" -Message "Non-main dispatch reason code mismatch."

    $skipOnlyDir = New-DerivedFixture -Name "skip-only-negative" -Source $flagsDir -Mutate {
        param($fixtureObject)
        $fixtureObject.vstmr_summary.'6100003'[0].outcome = "Skipped"
        $fixtureObject.vstmr_detail.'7100003:3'.outcome = "Skipped"
    }
    $resultSkipOnly = Invoke-Collector -IssueNumber 11 -FixtureRoot $skipOnlyDir -WorkDirectory (Join-Path $tempRoot "skip-only-negative")
    Assert-Equal -Actual $resultSkipOnly.Dossier.outcome -Expected "incomplete" -Message "Skip-only evidence must not satisfy intermittency eligibility."
    Assert-Contains -Collection @($resultSkipOnly.Dossier.incomplete.reason_codes) -Value "raw-evidence-insufficient" -Message "Skip-only evidence reason code mismatch."

    $unknownEnvironmentDir = New-DerivedFixture -Name "unknown-environment" -Source $flagsDir -Mutate {
        param($fixtureObject)
        $fixtureObject.vstmr_runs.'7100001'.name = "Quarantine-Mono-xunit"
    }
    $resultUnknownEnvironment = Invoke-Collector -IssueNumber 11 -FixtureRoot $unknownEnvironmentDir -WorkDirectory (Join-Path $tempRoot "unknown-environment")
    Assert-Equal -Actual $resultUnknownEnvironment.Dossier.outcome -Expected "incomplete" -Message "Unknown required environment dimensions must fail closed."
    Assert-Contains -Collection @($resultUnknownEnvironment.Dossier.incomplete.reason_codes) -Value "evidence-platform-unknown" -Message "Unknown platform reason code mismatch."
    Assert-Contains -Collection @($resultUnknownEnvironment.Dossier.incomplete.reason_codes) -Value "evidence-configuration-unknown" -Message "Unknown configuration reason code mismatch."

    $unknownExecutionLegDir = New-DerivedFixture -Name "unknown-execution-leg" -Source $flagsDir -Mutate {
        param($fixtureObject)
        $fixtureObject.vstmr_runs.'7100001'.name = "Quarantine-Windows-Debug-xunit"
    }
    $resultUnknownExecutionLeg = Invoke-Collector -IssueNumber 11 -FixtureRoot $unknownExecutionLegDir -WorkDirectory (Join-Path $tempRoot "unknown-execution-leg")
    Assert-Equal -Actual $resultUnknownExecutionLeg.Dossier.outcome -Expected "incomplete" -Message "Unknown required execution leg must fail closed."
    Assert-Contains -Collection @($resultUnknownExecutionLeg.Dossier.incomplete.reason_codes) -Value "evidence-execution-leg-unknown" -Message "Unknown execution leg reason code mismatch."

    $unknownPassExecutionLegDir = New-DerivedFixture -Name "unknown-pass-execution-leg" -Source $flagsDir -Mutate {
        param($fixtureObject)
        $fixtureObject.vstmr_runs.'7100003'.name = "Quarantine-Windows-Debug-xunit"
    }
    $resultUnknownPassExecutionLeg = Invoke-Collector -IssueNumber 11 -FixtureRoot $unknownPassExecutionLegDir -WorkDirectory (Join-Path $tempRoot "unknown-pass-execution-leg")
    Assert-Equal -Actual $resultUnknownPassExecutionLeg.Dossier.outcome -Expected "incomplete" -Message "Unknown pass execution leg must fail closed."
    Assert-Contains -Collection @($resultUnknownPassExecutionLeg.Dossier.incomplete.reason_codes) -Value "evidence-execution-leg-unknown" -Message "Unknown pass execution leg reason code mismatch."

    $differentPassEnvironmentDir = New-DerivedFixture -Name "different-pass-environment" -Source $flagsDir -Mutate {
        param($fixtureObject)
        $fixtureObject.vstmr_runs.'7100003'.name = "Quarantine-Mono-Linux-Release-xunit"
    }
    $resultDifferentPassEnvironment = Invoke-Collector -IssueNumber 11 -FixtureRoot $differentPassEnvironmentDir -WorkDirectory (Join-Path $tempRoot "different-pass-environment")
    Assert-Equal -Actual $resultDifferentPassEnvironment.Dossier.outcome -Expected "incomplete" -Message "A pass from a different environment must not prove intermittency."
    Assert-Contains -Collection @($resultDifferentPassEnvironment.Dossier.incomplete.reason_codes) -Value "passed-evidence-environment-mismatch" -Message "Different pass environment reason code mismatch."

    $differentExecutionLegDir = New-DerivedFixture -Name "different-execution-leg" -Source $flagsDir -Mutate {
        param($fixtureObject)
        $fixtureObject.vstmr_runs.'7100003'.name = "Quarantine-CoreCLR-Windows-Debug-xunit"
    }
    $resultDifferentExecutionLeg = Invoke-Collector -IssueNumber 11 -FixtureRoot $differentExecutionLegDir -WorkDirectory (Join-Path $tempRoot "different-execution-leg")
    Assert-Equal -Actual $resultDifferentExecutionLeg.Dossier.outcome -Expected "incomplete" -Message "A CoreCLR pass must not prove Mono failures intermittent."
    Assert-Contains -Collection @($resultDifferentExecutionLeg.Dossier.incomplete.reason_codes) -Value "passed-evidence-environment-mismatch" -Message "Different execution leg reason code mismatch."

    $compositeExecutionLegDir = New-DerivedFixture -Name "composite-execution-leg" -Source $flagsDir -Mutate {
        param($fixtureObject)
        $fixtureObject.vstmr_runs.'7100001'.name = "Quarantine-Mono-WebAssembly-Windows-Debug-xunit"
        $fixtureObject.vstmr_runs.'7100002'.name = "Quarantine-Mono-WebAssembly-Windows-Debug-xunit"
    }
    $resultCompositeExecutionLeg = Invoke-Collector -IssueNumber 11 -FixtureRoot $compositeExecutionLegDir -WorkDirectory (Join-Path $tempRoot "composite-execution-leg")
    Assert-Equal -Actual $resultCompositeExecutionLeg.Dossier.outcome -Expected "incomplete" -Message "A Mono pass must not match a distinct Mono+WebAssembly failure leg."
    Assert-Contains -Collection @($resultCompositeExecutionLeg.Dossier.incomplete.reason_codes) -Value "passed-evidence-environment-mismatch" -Message "Composite execution leg reason code mismatch."

    $passAfterLastDir = New-DerivedFixture -Name "pass-after-last-failure" -Source $flagsDir -Mutate {
        param($fixtureObject)
        $fixtureObject.negative_scan.'83'[0].startTime = "2026-08-02T00:00:00Z"
        $fixtureObject.negative_scan.'83'[0].finishTime = "2026-08-02T01:00:00Z"
    }
    $resultPassAfterLast = Invoke-Collector -IssueNumber 11 -FixtureRoot $passAfterLastDir -WorkDirectory (Join-Path $tempRoot "pass-after-last-failure")
    Assert-Equal -Actual $resultPassAfterLast.Dossier.outcome -Expected "incomplete" -Message "A pass after the last failure must not prove active intermittency."
    Assert-Contains -Collection @($resultPassAfterLast.Dossier.incomplete.reason_codes) -Value "passed-evidence-not-interleaved" -Message "Pass-after-last reason code mismatch."

    $invalidBuildCases = @(
        [ordered]@{
            Name = "wrong-definition"
            ReasonCode = "azdo-build-definition-not-allowed"
            Mutate = { param($fixtureObject) $fixtureObject.azdo_builds.'6100001'.definition.id = 999 }
        },
        [ordered]@{
            Name = "wrong-branch"
            ReasonCode = "azdo-build-source-branch-not-main"
            Mutate = { param($fixtureObject) $fixtureObject.azdo_builds.'6100001'.sourceBranch = "refs/pull/123/merge" }
        },
        [ordered]@{
            Name = "incomplete-build"
            ReasonCode = "azdo-build-not-completed"
            Mutate = { param($fixtureObject) $fixtureObject.azdo_builds.'6100001'.status = "inProgress" }
        },
        [ordered]@{
            Name = "wrong-result"
            ReasonCode = "azdo-build-result-incompatible"
            Mutate = { param($fixtureObject) $fixtureObject.azdo_builds.'6100001'.result = "succeeded" }
        }
    )
    foreach ($invalidBuildCase in $invalidBuildCases)
    {
        $invalidBuildDir = New-DerivedFixture -Name $invalidBuildCase.Name -Source $flagsDir -Mutate $invalidBuildCase.Mutate
        $invalidBuildResult = Invoke-Collector -IssueNumber 11 -FixtureRoot $invalidBuildDir -WorkDirectory (Join-Path $tempRoot $invalidBuildCase.Name)
        Assert-Equal -Actual $invalidBuildResult.Dossier.outcome -Expected "incomplete" -Message "$($invalidBuildCase.Name) build must fail closed."
        Assert-Contains -Collection @($invalidBuildResult.Dossier.incomplete.reason_codes) -Value $invalidBuildCase.ReasonCode -Message "$($invalidBuildCase.Name) reason code mismatch."
    }

    # ------------------------------------------------------------------
    # A duplicate-search hit is discovery only. Even the same FQN must remain unvalidated when the
    # documented KBE signature is incompatible with the authoritative failure evidence.
    # ------------------------------------------------------------------
    $dupTestName = "Sample.Tests.DuplicateValidationCase"
    $dupSignature = "System.InvalidOperationException: Sample failure for duplicate validation testing."
    $dupShaA = "c78cc5badc905159a96a0d4bb0686acadaddc5c3"
    $dupShaB = "439036f2881b7046fe9b9c3953bff60ed45dda6a"
    $dupShaC = "48bf6fbc5f5aa8484a773f612851e95a4f52973a"
    $dupDir = New-SyntheticFixture -Name "duplicate-unvalidated" -Overrides @{
        issue = [ordered]@{
            number = 12
            state = "open"
            labels = @("test-failure")
            body = "## Failing Test(s)`n`` $dupTestName ``$([System.Environment]::NewLine)## Error Message$([System.Environment]::NewLine)${codeFence}text$([System.Environment]::NewLine)$dupSignature$([System.Environment]::NewLine)$codeFence$([System.Environment]::NewLine)## Build$([System.Environment]::NewLine)https://dev.azure.com/dnceng-public/public/_build/results?buildId=6200001$([System.Environment]::NewLine)$workflowMarker"
        }
        azdo_builds = [ordered]@{
            "6200001" = [ordered]@{ definition = [ordered]@{ id = 83 }; sourceVersion = $dupShaA; startTime = "2026-08-01T00:00:00Z"; finishTime = "2026-08-01T01:00:00Z"; result = "failed" }
        }
        recurrence_scan = [ordered]@{
            "83" = @([ordered]@{ id = 6200002; sourceVersion = $dupShaB; startTime = "2026-07-30T00:00:00Z"; finishTime = "2026-07-30T01:00:00Z"; result = "failed" })
        }
        negative_scan = [ordered]@{
            "83" = @([ordered]@{ id = 6200003; sourceVersion = $dupShaC; startTime = "2026-07-31T00:00:00Z"; finishTime = "2026-07-31T01:00:00Z"; result = "succeeded" })
        }
        vstmr_summary = [ordered]@{
            "6200001" = @([ordered]@{ id = 1; runId = 7200001; outcome = "Failed"; automatedTestName = $dupTestName })
            "6200002" = @([ordered]@{ id = 2; runId = 7200002; outcome = "Failed"; automatedTestName = $dupTestName })
            "6200003" = @([ordered]@{ id = 3; runId = 7200003; outcome = "Passed"; automatedTestName = $dupTestName })
        }
        vstmr_detail = [ordered]@{
            "7200001:1" = [ordered]@{ outcome = "Failed"; errorMessage = $dupSignature; stackTrace = "at $dupTestName.Run() (build 1)" }
            "7200002:2" = [ordered]@{ outcome = "Failed"; errorMessage = $dupSignature; stackTrace = "at $dupTestName.Run() (build 2)" }
            "7200003:3" = [ordered]@{ outcome = "Passed"; errorMessage = $null; stackTrace = $null }
        }
        vstmr_runs = [ordered]@{
            "7200001" = [ordered]@{ name = "Quarantine-Mono-Linux-Release-xunit" }
            "7200002" = [ordered]@{ name = "Quarantine-Mono-Linux-Release-xunit" }
            "7200003" = [ordered]@{ name = "Quarantine-Mono-Linux-Release-xunit" }
        }
        duplicate_search = [ordered]@{
            "open-kbe" = [ordered]@{ complete = $true; result_numbers = @(99999); total_count = 1 }
            "recently-closed-kbe" = [ordered]@{ complete = $true; result_numbers = @(); total_count = 0 }
            "open-fix-pr" = [ordered]@{ complete = $true; result_numbers = @(); total_count = 0 }
            "recently-merged-fix-pr" = [ordered]@{ complete = $true; result_numbers = @(); total_count = 0 }
        }
        duplicate_candidate_text = [ordered]@{
            "99999" = "Known Build Error for $dupTestName`n## Error Message$([System.Environment]::NewLine)${codeFence}json$([System.Environment]::NewLine){ `"ErrorMessage`": `"System.InvalidOperationException: A different root cause.`", `"BuildRetry`": false, `"ExcludeConsoleLog`": false }$([System.Environment]::NewLine)$codeFence"
        }
    }
    $resultDup = Invoke-Collector -IssueNumber 12 -FixtureRoot $dupDir -WorkDirectory (Join-Path $tempRoot "duplicate-unvalidated")
    Assert-Equal -Actual $resultDup.Dossier.outcome -Expected "candidate" -Message "duplicate-unvalidated outcome mismatch."
    Assert-Equal -Actual $resultDup.Dossier.candidate.duplicate_check.status -Expected "none" -Message "A same-FQN KBE with an incompatible signature must never set duplicate_check.status to existing-kbe."
    Assert-Equal -Actual (@($resultDup.Dossier.candidate.duplicate_check.references)).Count -Expected 0 -Message "An unvalidated search hit must never appear in duplicate_check.references."
    $unvalidated = @($resultDup.Dossier.provenance.duplicate_search.unvalidated_candidates)
    if ($unvalidated.Count -eq 0 -or -not (@($unvalidated | Where-Object { $_.number -eq 99999 }).Count -gt 0))
    {
        throw "Expected an unvalidated_candidates entry for issue #99999."
    }

    $compatibleKbeDir = New-DerivedFixture -Name "duplicate-compatible-kbe" -Source $dupDir -Mutate {
        param($fixtureObject)
        $fixtureObject.duplicate_candidate_text.'99999' = "Known Build Error for $dupTestName`n## Error Message$([System.Environment]::NewLine)${codeFence}json$([System.Environment]::NewLine){ `"ErrorMessage`": `"$dupSignature`", `"BuildRetry`": false, `"ExcludeConsoleLog`": false }$([System.Environment]::NewLine)$codeFence"
    }
    $resultCompatibleKbe = Invoke-Collector -IssueNumber 12 -FixtureRoot $compatibleKbeDir -WorkDirectory (Join-Path $tempRoot "duplicate-compatible-kbe")
    Assert-Equal -Actual $resultCompatibleKbe.Dossier.candidate.duplicate_check.status -Expected "existing-kbe" -Message "An exact-FQN KBE with a compatible documented signature should validate."

    $detailFetchFailureDir = New-DerivedFixture -Name "duplicate-detail-fetch-failure" -Source $dupDir -Mutate {
        param($fixtureObject)
        $fixtureObject.duplicate_candidate_text.PSObject.Properties.Remove("99999")
    }
    $resultDetailFetchFailure = Invoke-Collector -IssueNumber 12 -FixtureRoot $detailFetchFailureDir -WorkDirectory (Join-Path $tempRoot "duplicate-detail-fetch-failure")
    Assert-Equal -Actual $resultDetailFetchFailure.Dossier.outcome -Expected "incomplete" -Message "A failed duplicate candidate-detail fetch must make the collector incomplete."
    Assert-Contains -Collection @($resultDetailFetchFailure.Dossier.incomplete.reason_codes) -Value "duplicate-detail-fetch-incomplete" -Message "Candidate-detail fetch failure reason code mismatch."
    Assert-Equal -Actual $resultDetailFetchFailure.Dossier.provenance.duplicate_search.coverage.open_kbes -Expected $false -Message "The affected duplicate query coverage must be incomplete."
    $failedDetailQuery = @($resultDetailFetchFailure.Dossier.provenance.duplicate_search.queries | Where-Object { $_.category -eq "open-kbe" })[0]
    Assert-Equal -Actual $failedDetailQuery.complete -Expected $false -Message "The affected duplicate query must be marked incomplete."

    $fixPrUnvalidatedDir = New-DerivedFixture -Name "fix-pr-unvalidated" -Source $dupDir -Mutate {
        param($fixtureObject)
        $fixtureObject.duplicate_search.'open-kbe'.result_numbers = @()
        $fixtureObject.duplicate_search.'open-kbe'.total_count = 0
        $fixtureObject.duplicate_search.'open-fix-pr'.result_numbers = @(99999)
        $fixtureObject.duplicate_search.'open-fix-pr'.total_count = 1
        $fixtureObject.duplicate_candidate_text.'99999' = "Fix $dupTestName without a compatible signature or linked issue."
    }
    $resultFixPrUnvalidated = Invoke-Collector -IssueNumber 12 -FixtureRoot $fixPrUnvalidatedDir -WorkDirectory (Join-Path $tempRoot "fix-pr-unvalidated")
    Assert-Equal -Actual $resultFixPrUnvalidated.Dossier.candidate.duplicate_check.status -Expected "none" -Message "An exact-FQN fix PR without compatible association must remain unvalidated."

    $fixPrMentionOnlyDir = New-DerivedFixture -Name "fix-pr-mention-only" -Source $dupDir -Mutate {
        param($fixtureObject)
        $fixtureObject.duplicate_search.'open-kbe'.result_numbers = @()
        $fixtureObject.duplicate_search.'open-kbe'.total_count = 0
        $fixtureObject.duplicate_search.'open-fix-pr'.result_numbers = @(99999)
        $fixtureObject.duplicate_search.'open-fix-pr'.total_count = 1
        $fixtureObject.duplicate_candidate_text.'99999' = "Fix $dupTestName`nRoot cause: $dupSignature"
    }
    $resultFixPrMentionOnly = Invoke-Collector -IssueNumber 12 -FixtureRoot $fixPrMentionOnlyDir -WorkDirectory (Join-Path $tempRoot "fix-pr-mention-only")
    Assert-Equal -Actual $resultFixPrMentionOnly.Dossier.candidate.duplicate_check.status -Expected "none" -Message "FQN/signature co-occurrence alone must not classify a search hit as a fix PR."
    $fixPrReason = [string](@($resultFixPrMentionOnly.Dossier.provenance.duplicate_search.unvalidated_candidates | Where-Object { $_.number -eq 99999 })[0].reason)
    if (-not $fixPrReason.Contains("closing-link and changed-file relevance"))
    {
        throw "Unvalidated fix PR must record the unsupported proof limitation."
    }

    # ------------------------------------------------------------------
    # Edge case (item 11): a literal ErrorMessage containing '*', '?', and '[' must be matched via
    # ordinal substring containment, never PowerShell -like/-notlike wildcard semantics. A decoy
    # build sharing only the literal '[' character (but not the full signature text) must NOT be
    # picked up as a second recurrence match.
    # ------------------------------------------------------------------
    $wildTestName = "Sample.Tests.WildcardSignatureCase"
    $wildSignature = "Assert.Equal() Failure: Array index [0] was *unexpected*, value? did not match."
    $wildShaA = "11c785d7c74de87dc8dabb59c500da8af9254f81"
    $wildShaB = "1d386dc40f508a46c0b76768cdbb1226cb6fe626"
    $wildShaDecoy = "7ab2248eea6ca9ec8fa5c10cabd3f5c520edd126"
    $wildShaNeg = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
    $wildDir = New-SyntheticFixture -Name "wildcard-signature" -Overrides @{
        issue = [ordered]@{
            number = 14
            state = "open"
            labels = @("test-failure")
            body = "## Failing Test(s)`n`` $wildTestName ``$([System.Environment]::NewLine)## Error Message$([System.Environment]::NewLine)${codeFence}text$([System.Environment]::NewLine)$wildSignature$([System.Environment]::NewLine)$codeFence$([System.Environment]::NewLine)## Build$([System.Environment]::NewLine)https://dev.azure.com/dnceng-public/public/_build/results?buildId=6400001$([System.Environment]::NewLine)$workflowMarker"
        }
        azdo_builds = [ordered]@{
            "6400001" = [ordered]@{ definition = [ordered]@{ id = 83 }; sourceVersion = $wildShaA; startTime = "2026-08-01T00:00:00Z"; finishTime = "2026-08-01T01:00:00Z"; result = "failed" }
        }
        recurrence_scan = [ordered]@{
            "83" = @(
                [ordered]@{ id = 6400099; sourceVersion = $wildShaDecoy; startTime = "2026-07-31T00:00:00Z"; finishTime = "2026-07-31T01:00:00Z"; result = "failed" },
                [ordered]@{ id = 6400002; sourceVersion = $wildShaB; startTime = "2026-07-30T00:00:00Z"; finishTime = "2026-07-30T01:00:00Z"; result = "failed" }
            )
        }
        negative_scan = [ordered]@{
            "83" = @([ordered]@{ id = 6400003; sourceVersion = $wildShaNeg; startTime = "2026-07-31T00:00:00Z"; finishTime = "2026-07-31T01:00:00Z"; result = "succeeded" })
        }
        vstmr_summary = [ordered]@{
            "6400001" = @([ordered]@{ id = 1; runId = 7400001; outcome = "Failed"; automatedTestName = $wildTestName })
            "6400099" = @([ordered]@{ id = 99; runId = 7400099; outcome = "Failed"; automatedTestName = $wildTestName })
            "6400002" = @([ordered]@{ id = 2; runId = 7400002; outcome = "Failed"; automatedTestName = $wildTestName })
            "6400003" = @([ordered]@{ id = 3; runId = 7400003; outcome = "Passed"; automatedTestName = $wildTestName })
        }
        vstmr_detail = [ordered]@{
            "7400001:1" = [ordered]@{ outcome = "Failed"; errorMessage = $wildSignature; stackTrace = "at $wildTestName.Run() (build 1)" }
            "7400099:99" = [ordered]@{ outcome = "Failed"; errorMessage = "Some unrelated failure referencing array index [5] elsewhere."; stackTrace = "at Sample.Tests.Unrelated.Run()" }
            "7400002:2" = [ordered]@{ outcome = "Failed"; errorMessage = $wildSignature; stackTrace = "at $wildTestName.Run() (build 2)" }
            "7400003:3" = [ordered]@{ outcome = "Passed"; errorMessage = $null; stackTrace = $null }
        }
        vstmr_runs = [ordered]@{
            "7400001" = [ordered]@{ name = "Quarantine-Mono-Linux-Debug-xunit" }
            "7400099" = [ordered]@{ name = "Quarantine-Mono-Linux-Debug-xunit" }
            "7400002" = [ordered]@{ name = "Quarantine-Mono-Linux-Debug-xunit" }
            "7400003" = [ordered]@{ name = "Quarantine-Mono-Linux-Debug-xunit" }
        }
        duplicate_search = $defaultDuplicateSearch
    }
    $resultWildcard = Invoke-Collector -IssueNumber 14 -FixtureRoot $wildDir -WorkDirectory (Join-Path $tempRoot "wildcard-signature")
    Assert-Equal -Actual $resultWildcard.Dossier.outcome -Expected "candidate" -Message "wildcard-signature outcome mismatch."
    $wildFailureBuildIds = @($resultWildcard.Dossier.candidate.evidence.raw_logs | Where-Object { $_.role -eq "failure" } | ForEach-Object { $_.build.id })
    Assert-Equal -Actual $wildFailureBuildIds.Count -Expected 2 -Message "wildcard-signature must gather exactly two failure builds."
    Assert-Contains -Collection $wildFailureBuildIds -Value 6400002 -Message "wildcard-signature must include the real matching recurrence build."
    Assert-NotContains -Collection $wildFailureBuildIds -Value 6400099 -Message "wildcard-signature must NOT include the decoy build that only shares the literal '[' character, proving ordinal (not -like) matching."

    # ------------------------------------------------------------------
    # Merge-AzdoBuildLists: pure unit coverage for the failed+partiallySucceeded merge/dedupe
    # (item 2) independent of any network access. Dot-source the collector (satisfying its
    # mandatory parameters with the already-validated missing-marker fixture, which exits after
    # Step 1) purely to bring the function into scope.
    # ------------------------------------------------------------------
    . $collector -IssueNumber 10 -OutputFile (Join-Path $tempRoot "merge-unit-dossier.json") -CandidateFile (Join-Path $tempRoot "merge-unit-candidate.json") -EvidenceRoot (Join-Path $tempRoot "merge-unit-evidence") -FixtureRoot $missingMarkerDir -RepositoryRoot $repositoryRoot -DossierSchemaFile $dossierSchema -CandidateSchemaFile $candidateSchema | Out-Null

    # Real Azure DevOps build objects deserialize as PSCustomObject (via Invoke-RestMethod /
    # ConvertFrom-Json); Sort-Object -Property only resolves a plain hashtable's "properties" via
    # PSCustomObject-style member resolution, not dictionary key lookup, so PSCustomObject here
    # matches production shape and is required for the -Property startTime sort below to work.
    $failedList = @(
        [PSCustomObject]@{ id = 1; startTime = "2026-01-01T00:00:00Z" },
        [PSCustomObject]@{ id = 2; startTime = "2026-01-02T00:00:00Z" }
    )
    $partiallySucceededList = @(
        [PSCustomObject]@{ id = 2; startTime = "2026-01-02T00:00:00Z" },
        [PSCustomObject]@{ id = 3; startTime = "2026-01-03T00:00:00Z" }
    )
    $merged = @(Merge-AzdoBuildLists -Lists @($failedList, $partiallySucceededList) -Cap 10)
    Assert-Equal -Actual $merged.Count -Expected 3 -Message "Merge-AzdoBuildLists must dedupe the build shared by both resultFilter queries."
    Assert-Equal -Actual $merged[0].id -Expected 3 -Message "Merge-AzdoBuildLists must sort by startTime descending (most recent first)."
    Assert-Equal -Actual $merged[2].id -Expected 1 -Message "Merge-AzdoBuildLists must preserve the oldest build last."

    $cappedMerged = @(Merge-AzdoBuildLists -Lists @($failedList, $partiallySucceededList) -Cap 2)
    Assert-Equal -Actual $cappedMerged.Count -Expected 2 -Message "Merge-AzdoBuildLists must honor the cap."

    $emptyMerged = @(Merge-AzdoBuildLists -Lists @(@(), @()) -Cap 5)
    Assert-Equal -Actual $emptyMerged.Count -Expected 0 -Message "Merge-AzdoBuildLists must return a real empty array (not collapse to null) when both inputs are empty."

    $windowedQueryUri = [System.Uri]::UnescapeDataString((Get-AzdoNegativeBuildQueryUri `
        -DefinitionId 83 `
        -MinimumStartTime ([System.DateTimeOffset]"2026-07-30T00:00:00Z") `
        -MaximumStartTime ([System.DateTimeOffset]"2026-08-01T00:00:00Z") `
        -Cap 20))
    foreach ($expectedQueryPart in @(
        "queryOrder=startTimeDescending",
        "minTime=2026-07-30T00:00:00.0000000+00:00",
        "maxTime=2026-08-01T00:00:00.0000000+00:00",
        "`$top=20"))
    {
        if (-not $windowedQueryUri.Contains($expectedQueryPart, [System.StringComparison]::Ordinal))
        {
            throw "Windowed Passed-build query is missing '$expectedQueryPart': $windowedQueryUri"
        }
    }

    Write-Host "All test-quarantine-kbe-shadow collector tests passed."
}
finally
{
    $env:GITHUB_REF = $originalGitHubRef
    $env:GITHUB_SHA = $originalGitHubSha

    if (Test-Path -LiteralPath $tempRoot)
    {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
