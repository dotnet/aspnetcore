#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deterministic, offline tests for Collect-TestQuarantineKbeEvidence.ps1.

.DESCRIPTION
    Exercises the collector entirely in -FixtureRoot mode (zero network access) against the
    three real pilot quarantine issues recorded in fixtures/, and against a handful of small
    synthetic fixtures for edge cases that are not represented by those three issues. Golden
    dossier comparisons exclude collector-generated timestamps and the running repository's
    HEAD commit SHA, both of which are expected to differ between runs/commits; every other
    field must match exactly.

    Where the collector's outcome is 'candidate', the resulting candidate.json is also fed
    through the unmodified, already-tested Evaluate-TestQuarantineKbeCandidate.ps1 to prove the
    two scripts reconcile: the collector's output is accepted as-is by the existing evaluator
    contract with no changes to that script or its schemas.
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

# The three collector-generated timestamps and the running checkout's HEAD commit SHA are
# expected to differ run-to-run and commit-to-commit; every golden fixture below was captured
# with these fields already replaced by this same sentinel.
$volatileKeys = @("generated_utc", "retrieved_utc", "captured_utc", "checked_utc", "commit_sha")
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
        [string]$Signature
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

try
{
    [System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null

    # ------------------------------------------------------------------
    # Pilot 1 -- aspnetcore#68724: deterministic '## Error Message' extraction,
    # a supplementary recurrence-scan build, and reuse of an existing KBE.
    # ------------------------------------------------------------------
    $result68724 = Invoke-Collector -IssueNumber 68724 -FixtureRoot "$fixturesRoot/68724" -WorkDirectory "$tempRoot/68724"
    Assert-Equal -Actual $result68724.Dossier.outcome -Expected "candidate" -Message "#68724 outcome mismatch."
    Assert-Equal -Actual $result68724.Dossier.candidate.proposed_classification -Expected "reuse-existing-kbe" -Message "#68724 proposed_classification mismatch."
    Assert-GoldenDossier -IssueDirectory "$fixturesRoot/68724" -ActualDossier $result68724.Dossier

    $receiptPath68724 = Join-Path "$tempRoot/68724" "receipt.json"
    & $evaluator -CandidateFile $result68724.CandidatePath -EvidenceRoot $result68724.EvidenceRoot -OutputFile $receiptPath68724 -CandidateSchemaFile $candidateSchema
    $receipt68724 = Get-Content -LiteralPath $receiptPath68724 -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt68724.deterministic_status -Expected "validated" -Message "#68724 deterministic_status mismatch."
    Assert-Equal -Actual $receipt68724.shadow_recommendation -Expected "reuse-existing-kbe" -Message "#68724 shadow_recommendation mismatch."
    Assert-Equal -Actual $receipt68724.eligible_for_kbe_enrichment -Expected $false -Message "#68724 must never authorize enrichment."
    Assert-Equal -Actual $receipt68724.evidence_provenance_verified -Expected $false -Message "#68724 provenance must remain unverified."

    $summaryPath68724 = Join-Path "$tempRoot/68724" "summary.md"
    & $summaryGenerator -DossierFile $result68724.DossierPath -ReceiptFile $receiptPath68724 -OutputFile $summaryPath68724
    $summaryText68724 = Get-Content -LiteralPath $summaryPath68724 -Raw
    if (-not $summaryText68724.Contains("reuse-existing-kbe"))
    {
        throw "#68724 summary must mention the shadow_recommendation."
    }

    # ------------------------------------------------------------------
    # Pilot 2 -- aspnetcore#68947: the issue body has no fenced '## Error
    # Message' block, so deterministic extraction is ambiguous without a
    # manual signature.
    # ------------------------------------------------------------------
    $result68947NoSig = Invoke-Collector -IssueNumber 68947 -FixtureRoot "$fixturesRoot/68947" -WorkDirectory "$tempRoot/68947-no-signature"
    Assert-Equal -Actual $result68947NoSig.Dossier.outcome -Expected "incomplete" -Message "#68947 (no signature) outcome mismatch."
    Assert-Contains -Collection @($result68947NoSig.Dossier.incomplete.reason_codes) -Value "signature-extraction-ambiguous" -Message "#68947 (no signature) reason codes mismatch."

    # With the manual override supplied, recurrence is established via the collector's
    # supplementary scan (the issue's second cited build has itself aged out of Azure DevOps
    # retention) and the outcome is a validated, generic-timeout candidate.
    $result68947 = Invoke-Collector -IssueNumber 68947 -FixtureRoot "$fixturesRoot/68947" -WorkDirectory "$tempRoot/68947" -Signature "OpenQA.Selenium.WebDriverException: TaskCanceledException"
    Assert-Equal -Actual $result68947.Dossier.outcome -Expected "candidate" -Message "#68947 outcome mismatch."
    Assert-Equal -Actual $result68947.Dossier.candidate.proposed_classification -Expected "timeout-needs-classification" -Message "#68947 proposed_classification mismatch."
    Assert-GoldenDossier -IssueDirectory "$fixturesRoot/68947" -ActualDossier $result68947.Dossier

    $receiptPath68947 = Join-Path "$tempRoot/68947" "receipt.json"
    & $evaluator -CandidateFile $result68947.CandidatePath -EvidenceRoot $result68947.EvidenceRoot -OutputFile $receiptPath68947 -CandidateSchemaFile $candidateSchema
    $receipt68947 = Get-Content -LiteralPath $receiptPath68947 -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt68947.deterministic_status -Expected "validated" -Message "#68947 deterministic_status mismatch."
    Assert-Equal -Actual $receipt68947.shadow_recommendation -Expected "timeout-needs-classification" -Message "#68947 shadow_recommendation mismatch."

    # ------------------------------------------------------------------
    # Pilot 3 -- aspnetcore#68945: both cited builds' Azure DevOps metadata still
    # resolves, but the second build's Helix console-log artifact has expired.
    # Recurrence therefore falls back to a single usable failure log and the
    # collector must fail closed rather than infer a pass.
    # ------------------------------------------------------------------
    $result68945 = Invoke-Collector -IssueNumber 68945 -FixtureRoot "$fixturesRoot/68945" -WorkDirectory "$tempRoot/68945" -Signature "System.Threading.Tasks.TaskCanceledException: The operation was canceled."
    Assert-Equal -Actual $result68945.Dossier.outcome -Expected "incomplete" -Message "#68945 outcome mismatch."
    Assert-Contains -Collection @($result68945.Dossier.incomplete.reason_codes) -Value "raw-evidence-expired" -Message "#68945 reason codes must record the expired artifact."
    Assert-Equal -Actual $result68945.Dossier.candidate -Expected $null -Message "#68945 must not emit a candidate."
    Assert-GoldenDossier -IssueDirectory "$fixturesRoot/68945" -ActualDossier $result68945.Dossier

    $summaryPath68945 = Join-Path "$tempRoot/68945" "summary.md"
    & $summaryGenerator -DossierFile $result68945.DossierPath -OutputFile $summaryPath68945
    $summaryText68945 = Get-Content -LiteralPath $summaryPath68945 -Raw
    if (-not $summaryText68945.Contains("raw-evidence-expired"))
    {
        throw "#68945 summary must mention the raw-evidence-expired reason code."
    }

    # ------------------------------------------------------------------
    # Edge cases not represented by the three pilots: a closed issue, an issue
    # missing the canonical quarantine label, and conservative check-run
    # substring extraction actually flipping to true when warranted.
    # ------------------------------------------------------------------
    $closedFixtureDir = Join-Path $tempRoot "closed-issue-fixture"
    [System.IO.Directory]::CreateDirectory($closedFixtureDir) | Out-Null
    @{
        issue = @{
            number = 1
            state = "closed"
            labels = @("test-failure")
            body = "## Failing Test(s)`n`` Sample.Tests.Closed ``"
        }
        azdo_builds = @{}
        recurrence_scan = @{}
        negative_scan = @{}
        vstmr_results = @{}
        helix_evidence = @{}
        check_runs = @{}
        duplicate_search = @{
            "open-kbe" = @{ complete = $true; result_numbers = @() }
            "recently-closed-kbe" = @{ complete = $true; result_numbers = @() }
            "open-fix-pr" = @{ complete = $true; result_numbers = @() }
            "recently-merged-fix-pr" = @{ complete = $true; result_numbers = @() }
        }
    } | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath (Join-Path $closedFixtureDir "fixture.json")
    $resultClosed = Invoke-Collector -IssueNumber 1 -FixtureRoot $closedFixtureDir -WorkDirectory (Join-Path $tempRoot "closed-issue")
    Assert-Equal -Actual $resultClosed.Dossier.outcome -Expected "incomplete" -Message "Closed-issue outcome mismatch."
    Assert-Contains -Collection @($resultClosed.Dossier.incomplete.reason_codes) -Value "issue-not-open" -Message "Closed-issue reason codes mismatch."

    $unlabeledFixtureDir = Join-Path $tempRoot "unlabeled-issue-fixture"
    [System.IO.Directory]::CreateDirectory($unlabeledFixtureDir) | Out-Null
    @{
        issue = @{
            number = 2
            state = "open"
            labels = @("area-blazor")
            body = "## Failing Test(s)`n`` Sample.Tests.Unlabeled ``"
        }
        azdo_builds = @{}
        recurrence_scan = @{}
        negative_scan = @{}
        vstmr_results = @{}
        helix_evidence = @{}
        check_runs = @{}
        duplicate_search = @{
            "open-kbe" = @{ complete = $true; result_numbers = @() }
            "recently-closed-kbe" = @{ complete = $true; result_numbers = @() }
            "open-fix-pr" = @{ complete = $true; result_numbers = @() }
            "recently-merged-fix-pr" = @{ complete = $true; result_numbers = @() }
        }
    } | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath (Join-Path $unlabeledFixtureDir "fixture.json")
    $resultUnlabeled = Invoke-Collector -IssueNumber 2 -FixtureRoot $unlabeledFixtureDir -WorkDirectory (Join-Path $tempRoot "unlabeled-issue")
    Assert-Equal -Actual $resultUnlabeled.Dossier.outcome -Expected "incomplete" -Message "Unlabeled-issue outcome mismatch."
    Assert-Contains -Collection @($resultUnlabeled.Dossier.incomplete.reason_codes) -Value "issue-not-canonical-quarantine" -Message "Unlabeled-issue reason codes mismatch."

    # The three pilots' real Build Analysis snapshots were all generic (matching the
    # documented architecture-consensus finding that this signal is corroborating only), so
    # none of them exercise a positive substring match. Prove that path separately: a snapshot
    # whose text literally names the test and a known issue must flip both conservative flags.
    $exactMatchFixtureDir = Join-Path $tempRoot "exact-match-fixture"
    [System.IO.Directory]::CreateDirectory($exactMatchFixtureDir) | Out-Null
    $exactMatchSha = "bf6e1566a2433f298c3adc8b6ecc3358b99d5d3f"
    $exactMatchSignature = "System.InvalidOperationException: Sample failure for exact-match testing."
    @{
        issue = @{
            number = 3
            state = "open"
            labels = @("test-failure")
            body = "## Failing Test(s)`n`` Sample.Tests.ExactMatchCase ``" + "`n`n## Error Message`n``````text`n$exactMatchSignature`n``````" + "`n`n## Build`nhttps://dev.azure.com/dnceng-public/public/_build/results?buildId=5000001"
        }
        azdo_builds = @{
            "5000001" = @{ definition = @{ id = 83 }; sourceVersion = $exactMatchSha; startTime = "2026-08-01T00:00:00Z"; finishTime = "2026-08-01T01:00:00Z"; result = "failed" }
        }
        recurrence_scan = @{
            "83" = @(@{ id = 5000002; sourceVersion = "c1b304785ea05e7c92030583e1cb658c50630102"; startTime = "2026-07-30T00:00:00Z"; finishTime = "2026-07-30T01:00:00Z"; result = "failed" })
        }
        negative_scan = @{
            "83" = @(@{ id = 5000003; sourceVersion = "52bcd78ab0d7a1df3834306cc1c56a21f86a9fd2"; startTime = "2026-07-29T00:00:00Z"; finishTime = "2026-07-29T01:00:00Z"; result = "succeeded" })
        }
        vstmr_results = @{
            "5000001" = @{ outcome = "Failed"; comment = '{"HelixJobId":"job-a","HelixWorkItemName":"wi-a"}'; errorMessage = $exactMatchSignature; stackTrace = "at Sample.Tests.ExactMatchCase..." }
            "5000002" = @{ outcome = "Failed"; comment = '{"HelixJobId":"job-b","HelixWorkItemName":"wi-b"}'; errorMessage = $exactMatchSignature; stackTrace = "at Sample.Tests.ExactMatchCase..." }
            "5000003" = @{ outcome = "Passed"; comment = '{"HelixJobId":"job-c","HelixWorkItemName":"wi-c"}'; errorMessage = $null; stackTrace = $null }
        }
        helix_evidence = @{
            "5000001" = @{ found = $true; expired = $false; console_excerpt = "Failed Sample.Tests.ExactMatchCase [1 s]`n$exactMatchSignature (build 5000001)" }
            "5000002" = @{ found = $true; expired = $false; console_excerpt = "Failed Sample.Tests.ExactMatchCase [1 s]`n$exactMatchSignature (build 5000002)" }
            "5000003" = @{ found = $true; expired = $false; console_excerpt = "[PASS] Sample.Tests.ExactMatchCase" }
        }
        check_runs = @{
            $exactMatchSha = @(@{
                name = "Build Analysis"
                id = 700001
                conclusion = "failure"
                output = @{
                    title = "1 failing test"
                    text = "Sample.Tests.ExactMatchCase failed. This matches a Known Issue: https://github.com/dotnet/aspnetcore/issues/70000."
                }
                html_url = "https://github.com/dotnet/aspnetcore/runs/700001"
            })
        }
        duplicate_search = @{
            "open-kbe" = @{ complete = $true; result_numbers = @() }
            "recently-closed-kbe" = @{ complete = $true; result_numbers = @() }
            "open-fix-pr" = @{ complete = $true; result_numbers = @() }
            "recently-merged-fix-pr" = @{ complete = $true; result_numbers = @() }
        }
    } | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath (Join-Path $exactMatchFixtureDir "fixture.json")
    $resultExactMatch = Invoke-Collector -IssueNumber 3 -FixtureRoot $exactMatchFixtureDir -WorkDirectory (Join-Path $tempRoot "exact-match")
    Assert-Equal -Actual $resultExactMatch.Dossier.outcome -Expected "candidate" -Message "Exact-match fixture outcome mismatch."
    $matchingSnapshot = @($resultExactMatch.Dossier.provenance.check_run_snapshots | Where-Object { $_.source_version -eq $exactMatchSha })[0]
    Assert-Equal -Actual $matchingSnapshot.exact_test_referenced -Expected $true -Message "exact_test_referenced must flip true when the check-run text names the test."
    Assert-Equal -Actual $matchingSnapshot.known_issue_referenced -Expected $true -Message "known_issue_referenced must flip true when the check-run text names a Known Issue."

    Write-Host "All test-quarantine-kbe-shadow collector tests passed."
}
finally
{
    if (Test-Path -LiteralPath $tempRoot)
    {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
