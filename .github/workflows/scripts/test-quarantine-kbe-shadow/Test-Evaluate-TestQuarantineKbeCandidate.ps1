#!/usr/bin/env pwsh

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$evaluator = "$PSScriptRoot/Evaluate-TestQuarantineKbeCandidate.ps1"
$candidateSchema = "$PSScriptRoot/test-quarantine-kbe-shadow-candidate.schema.json"
$receiptSchema = "$PSScriptRoot/test-quarantine-kbe-shadow-receipt.schema.json"
$repositoryRoot = (Resolve-Path "$PSScriptRoot/../../../..").Path
$repositoryHead = (& git -C $repositoryRoot rev-parse HEAD).Trim()
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "aspnetcore-kbe-shadow-$([System.Guid]::NewGuid().ToString('N'))"
$outsideRoot = "$tempRoot-outside"
$symbolicLinkPath = Join-Path $tempRoot "linked-evidence"

function Assert-Equal
{
    param(
        [Parameter(Mandatory = $true)]$Actual,
        [Parameter(Mandatory = $true)]$Expected,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($Actual -ne $Expected)
    {
        throw "$Message Expected '$Expected', actual '$Actual'."
    }
}

function Write-Candidate
{
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string[]]$Signature,
        [Parameter(Mandatory = $true)][object[]]$Logs,
        [string]$SignatureKind = "ErrorMessage",
        [string]$DuplicateStatus = "none",
        [bool]$CompleteCoverage = $true,
        [string]$ProposedClassification = "new-kbe-candidate"
    )

    $candidate = [ordered]@{
        schema_version = 1
        repository = "dotnet/aspnetcore"
        repository_ref = [ordered]@{
            branch = "main"
            commit_sha = $repositoryHead
        }
        issue = [ordered]@{
            number = 12345
            url = "https://github.com/dotnet/aspnetcore/issues/12345"
        }
        test = [ordered]@{
            fully_qualified_name = "Microsoft.AspNetCore.Example.Tests.SampleTests.Completes"
        }
        signature = [ordered]@{
            kind = $SignatureKind
            values = $Signature
            build_retry = $false
            exclude_console_log = $false
        }
        policy = [ordered]@{
            minimum_failure_logs = 2
            minimum_negative_logs = 1
        }
        evidence = [ordered]@{
            raw_logs = $Logs
            corroborating_context = @(
                [ordered]@{
                    source = "quarantine-issue"
                    url = "https://github.com/dotnet/aspnetcore/issues/12345"
                }
            )
        }
        duplicate_check = [ordered]@{
            status = $DuplicateStatus
            checked_utc = [System.DateTimeOffset]::UtcNow.ToString("O")
            coverage = [ordered]@{
                open_kbes = $CompleteCoverage
                recently_closed_kbes = $CompleteCoverage
                open_fix_prs = $CompleteCoverage
                recently_merged_fix_prs = $CompleteCoverage
            }
            references = @()
            queries = @(
                [ordered]@{
                    category = "open-kbe"
                    query = "repo:dotnet/aspnetcore is:issue label:`"Known Build Error`" SampleTests"
                    complete = $CompleteCoverage
                    result_numbers = @()
                },
                [ordered]@{
                    category = "recently-closed-kbe"
                    query = "repo:dotnet/aspnetcore is:issue is:closed SampleTests"
                    complete = $CompleteCoverage
                    result_numbers = @()
                },
                [ordered]@{
                    category = "open-fix-pr"
                    query = "repo:dotnet/aspnetcore is:pr is:open SampleTests"
                    complete = $CompleteCoverage
                    result_numbers = @()
                },
                [ordered]@{
                    category = "recently-merged-fix-pr"
                    query = "repo:dotnet/aspnetcore is:pr is:merged SampleTests"
                    complete = $CompleteCoverage
                    result_numbers = @()
                }
            )
        }
        proposed_classification = $ProposedClassification
    }

    $candidate | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $Path
}

function New-LogEntry
{
    param(
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][string]$Role,
        [Parameter(Mandatory = $true)][string]$Outcome,
        [Parameter(Mandatory = $true)][string]$Path
    )

    return [ordered]@{
        id = $Id
        role = $Role
        outcome = $Outcome
        path = $Path
        source_url = "https://example.invalid/$Path"
        sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot $Path) -Algorithm SHA256).Hash.ToLowerInvariant()
        build = [ordered]@{
            id = switch ($Id)
            {
                "failure-1" { 1001; break }
                "failure-2" { 1002; break }
                default { 1003 }
            }
            pipeline_definition_id = 83
            source_branch = "refs/heads/main"
            source_version = switch ($Id)
            {
                "failure-1" { "2222222222222222222222222222222222222222"; break }
                "failure-2" { "3333333333333333333333333333333333333333"; break }
                default { "4444444444444444444444444444444444444444" }
            }
            started_utc = switch ($Id)
            {
                "failure-1" { "2026-08-20T12:00:00Z"; break }
                "failure-2" { "2026-08-21T12:00:00Z"; break }
                default { "2026-08-20T18:00:00Z" }
            }
            status = "completed"
            result = if ($Role -eq "failure") { "failed" } else { "succeeded" }
            test_run_identity = "quarantine-mono-linux-release-xunit"
            platform = "Linux"
            configuration = "Release"
        }
    }
}

try
{
    [System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null
    $signature = "Expected completion signal before deterministic deadline"
    Set-Content -LiteralPath (Join-Path $tempRoot "failure-1.log") -Value @(
        "2026-08-22T04:28:28.1901712Z [xUnit.net 00:48:54.05]     Microsoft.AspNetCore.Example.Tests.SampleTests.Completes [FAIL]"
        "2026-08-22T04:28:28.1991498Z Xunit.Sdk.TrueException: $signature"
    )
    1..60 | ForEach-Object { Add-Content -LiteralPath (Join-Path $tempRoot "failure-1.log") -Value "Diagnostic line $_" }
    Add-Content -LiteralPath (Join-Path $tempRoot "failure-1.log") -Value "Repeated diagnostic: $signature"
    Set-Content -LiteralPath (Join-Path $tempRoot "failure-2.log") -Value @(
        "Failed Microsoft.AspNetCore.Example.Tests.SampleTests.Completes [2 s]"
        "Xunit.Sdk.TrueException: $signature"
    )
    Set-Content -LiteralPath (Join-Path $tempRoot "negative.log") -Value @(
        "[PASS] Microsoft.AspNetCore.Example.Tests.SampleTests.Completes"
        "Finished normally"
    )

    $logs = @(
        (New-LogEntry -Id "failure-1" -Role "failure" -Outcome "failed" -Path "failure-1.log"),
        (New-LogEntry -Id "failure-2" -Role "failure" -Outcome "failed" -Path "failure-2.log"),
        (New-LogEntry -Id "negative" -Role "negative" -Outcome "passed" -Path "negative.log")
    )

    $candidatePath = Join-Path $tempRoot "candidate.json"
    $receiptPath = Join-Path $tempRoot "receipt.json"
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "validated" -Message "Valid candidate status mismatch."
    Assert-Equal -Actual $receipt.shadow_recommendation -Expected "new-kbe-candidate" -Message "Valid candidate recommendation mismatch."
    Assert-Equal -Actual $receipt.eligible_for_kbe_enrichment -Expected $false -Message "Shadow evaluator must not authorize enrichment from unverified provenance."
    Assert-Equal -Actual $receipt.evidence_provenance_verified -Expected $false -Message "Shadow evidence provenance must remain unverified."

    $logs[2].build.started_utc = "2026-08-19T12:00:00Z"
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs
    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "incomplete" -Message "Predating pass evidence status mismatch."
    if (-not (($receipt.reasons -join "`n").Contains("strictly between")))
    {
        throw "Predating pass evidence must report the interleaving gate."
    }

    $logs[2].build.started_utc = "2026-08-22T12:00:00Z"
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs
    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "incomplete" -Message "Pass-after-last-failure status mismatch."
    if (-not (($receipt.reasons -join "`n").Contains("strictly between")))
    {
        throw "Pass-after-last evidence must report the interleaving gate."
    }

    $logs[2].build.started_utc = "2026-08-20T18:00:00Z"
    $logs[2].build.test_run_identity = "quarantine-coreclr-linux-release-xunit"
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs
    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "incomplete" -Message "Different TestRun identity pass evidence status mismatch."
    if (-not (($receipt.reasons -join "`n").Contains("pipeline definition, canonical TestRun identity, platform, and configuration")))
    {
        throw "Mono-failure/CoreCLR-pass evidence must report the environment gate."
    }
    $logs[2].build.test_run_identity = "quarantine-mono-linux-release-xunit"

    Set-Content -LiteralPath (Join-Path $tempRoot "negative.log") -Value @(
        "[SKIP] Microsoft.AspNetCore.Example.Tests.SampleTests.Completes"
        "Skipped by test infrastructure"
    )
    $logs[2].outcome = "skipped"
    $logs[2].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "negative.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "incomplete" -Message "Skip-only negative evidence status mismatch."
    Assert-Equal -Actual $receipt.evidence.distinct_negative_build_count -Expected 0 -Message "Skipped evidence must not satisfy the authoritative Passed build gate."

    Set-Content -LiteralPath (Join-Path $tempRoot "negative.log") -Value @(
        "[PASS] Microsoft.AspNetCore.Example.Tests.SampleTests.Completes"
        "Finished normally"
    )
    $logs[2].outcome = "passed"
    $logs[2].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "negative.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    $logs[0].build.platform = "unknown"
    $logs[0].build.configuration = "unknown"
    $logs[0].build.test_run_identity = "unknown"
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "incomplete" -Message "Unknown environment evidence status mismatch."
    if (-not (($receipt.reasons -join "`n").Contains("unknown platform")) -or
        -not (($receipt.reasons -join "`n").Contains("unknown configuration")) -or
        -not (($receipt.reasons -join "`n").Contains("unknown canonical TestRun identity")))
    {
        throw "Unknown environment evidence must report every missing dimension."
    }
    $logs[0].build.platform = "Linux"
    $logs[0].build.configuration = "Release"
    $logs[0].build.test_run_identity = "quarantine-mono-linux-release-xunit"

    Set-Content -LiteralPath (Join-Path $tempRoot "failure-2.log") -Value @(
        "Starting an unrelated test"
        "Xunit.Sdk.TrueException: $signature"
    )
    $logs[1].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-2.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Unassociated failure evidence status mismatch."
    Assert-Equal -Actual $receipt.evidence.logs[1].failed_test_detected -Expected $false -Message "Unassociated failure evidence marker mismatch."

    Set-Content -LiteralPath (Join-Path $tempRoot "failure-2.log") -Value @(
        "Failed Microsoft.AspNetCore.Example.Tests.SampleTests.Completes [2 s]"
        "Xunit.Sdk.TrueException: $signature"
    )
    $logs[1].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-2.log") -Algorithm SHA256).Hash.ToLowerInvariant()

    Add-Content -LiteralPath (Join-Path $tempRoot "negative.log") -Value "[SKIP] $signature"
    $logs[2].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "negative.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Collision candidate status mismatch."
    Assert-Equal -Actual $receipt.shadow_recommendation -Expected "human-review" -Message "Collision candidate recommendation mismatch."
    Assert-Equal -Actual $receipt.evidence.pass_or_skip_collision_count -Expected 1 -Message "Pass/skip collision count mismatch."

    Set-Content -LiteralPath (Join-Path $tempRoot "negative.log") -Value "Passed Other.Tests.Completes [1 ms] $signature"
    $logs[2].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "negative.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "VSTest pass collision status mismatch."
    Assert-Equal -Actual $receipt.evidence.pass_or_skip_collision_count -Expected 1 -Message "VSTest pass collision count mismatch."

    Set-Content -LiteralPath (Join-Path $tempRoot "negative.log") -Value "[PASS] SampleTests.Completes"
    $logs[2].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "negative.log") -Algorithm SHA256).Hash.ToLowerInvariant()

    $multiValueSignature = @(
        "Expected completion signal",
        "before deterministic deadline"
    )
    Set-Content -LiteralPath (Join-Path $tempRoot "failure-1.log") -Value @(
        "[FAIL] Microsoft.AspNetCore.Example.Tests.SampleTests.Completes"
        $multiValueSignature[0]
        $multiValueSignature[1]
        "[PASS] $($multiValueSignature[0])"
        "[PASS] $($multiValueSignature[1])"
    )
    Set-Content -LiteralPath (Join-Path $tempRoot "failure-2.log") -Value @(
        "Failed Microsoft.AspNetCore.Example.Tests.SampleTests.Completes [2 s]"
        $multiValueSignature[0]
        $multiValueSignature[1]
    )
    $logs[0].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-1.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    $logs[1].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-2.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Candidate -Path $candidatePath -Signature $multiValueSignature -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Multi-value pass collision status mismatch."
    Assert-Equal -Actual $receipt.evidence.pass_or_skip_collision_count -Expected 2 -Message "Multi-value pass collision count mismatch."

    Set-Content -LiteralPath (Join-Path $tempRoot "failure-1.log") -Value @(
        "[FAIL] Microsoft.AspNetCore.Example.Tests.SampleTests.Completes"
        "Xunit.Sdk.TrueException: $signature"
    )
    Set-Content -LiteralPath (Join-Path $tempRoot "failure-2.log") -Value @(
        "Failed Microsoft.AspNetCore.Example.Tests.SampleTests.Completes [2 s]"
        "Xunit.Sdk.TrueException: $signature"
    )
    $logs[0].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-1.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    $logs[1].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-2.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs -CompleteCoverage $false

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "incomplete" -Message "Incomplete duplicate check status mismatch."
    Assert-Equal -Actual $receipt.shadow_recommendation -Expected "insufficient-evidence" -Message "Incomplete duplicate check recommendation mismatch."

    Copy-Item -LiteralPath (Join-Path $tempRoot "failure-1.log") -Destination (Join-Path $tempRoot "failure-2.log") -Force
    $logs[1].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-2.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "incomplete" -Message "Duplicate failure evidence status mismatch."
    Assert-Equal -Actual $receipt.evidence.distinct_failure_log_count -Expected 1 -Message "Distinct failure evidence count mismatch."

    Set-Content -LiteralPath (Join-Path $tempRoot "failure-2.log") -Value @(
        "Failed Microsoft.AspNetCore.Example.Tests.SampleTests.Completes [2 s]"
        "Xunit.Sdk.TrueException: $signature"
    )
    $logs[1].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-2.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    $logs[1].build.id = $logs[0].build.id
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "incomplete" -Message "Same-build failure evidence status mismatch."
    Assert-Equal -Actual $receipt.evidence.distinct_failure_log_count -Expected 2 -Message "Same-build distinct log count mismatch."
    Assert-Equal -Actual $receipt.evidence.distinct_failure_build_count -Expected 1 -Message "Same-build distinct build count mismatch."
    $logs[1].build.id = 1002

    Write-Candidate `
        -Path $candidatePath `
        -Signature @("Microsoft.AspNetCore.Example.Tests.SampleTests.Completes") `
        -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Bare test identifier status mismatch."
    Assert-Equal -Actual $receipt.eligible_for_kbe_enrichment -Expected $false -Message "Bare test identifier eligibility mismatch."

    Write-Candidate `
        -Path $candidatePath `
        -Signature @("SampleTests.Completes") `
        -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Test identifier fragment status mismatch."

    Write-Candidate `
        -Path $candidatePath `
        -Signature @("SampleTests\.Completes") `
        -SignatureKind "ErrorPattern" `
        -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Regex test identifier fragment status mismatch."

    Set-Content -LiteralPath (Join-Path $tempRoot "failure-1.log") -Value @(
        "[FAIL] Microsoft.AspNetCore.Example.Tests.SampleTests.CompletesAsync"
        "Xunit.Sdk.TrueException: $signature"
    )
    Set-Content -LiteralPath (Join-Path $tempRoot "failure-2.log") -Value @(
        "Failed Microsoft.AspNetCore.Example.Tests.SampleTests.CompletesAsync [2 s]"
        "Xunit.Sdk.TrueException: $signature"
    )
    $logs[0].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-1.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    $logs[1].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-2.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Prefixed test name association status mismatch."
    Assert-Equal -Actual $receipt.evidence.logs[0].failed_test_detected -Expected $false -Message "Prefixed test name was treated as the declared test."

    Set-Content -LiteralPath (Join-Path $tempRoot "failure-1.log") -Value @(
        "Exception message: `"[FAIL] Microsoft.AspNetCore.Example.Tests.SampleTests.Completes`""
        "Xunit.Sdk.TrueException: $signature"
    )
    Set-Content -LiteralPath (Join-Path $tempRoot "failure-2.log") -Value @(
        "Diagnostic text mentions [FAILED] Microsoft.AspNetCore.Example.Tests.SampleTests.Completes"
        "Xunit.Sdk.TrueException: $signature"
    )
    $logs[0].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-1.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    $logs[1].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-2.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Candidate -Path $candidatePath -Signature @($signature) -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Embedded failure marker status mismatch."
    Assert-Equal -Actual $receipt.evidence.logs[0].failed_test_detected -Expected $false -Message "Embedded failure text was treated as a failed-test record."

    Set-Content -LiteralPath (Join-Path $tempRoot "failure-1.log") -Value @(
        "[FAIL] Microsoft.AspNetCore.Example.Tests.SampleTests.Completes"
        "Xunit.Sdk.TrueException: $signature"
    )
    Set-Content -LiteralPath (Join-Path $tempRoot "failure-2.log") -Value @(
        "Failed Microsoft.AspNetCore.Example.Tests.SampleTests.Completes [2 s]"
        "Xunit.Sdk.TrueException: $signature"
    )
    $logs[0].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-1.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    $logs[1].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-2.log") -Algorithm SHA256).Hash.ToLowerInvariant()

    Write-Candidate `
        -Path $candidatePath `
        -Signature @("^Xunit\.Sdk\.TrueException: Expected completion signal before deterministic deadline$") `
        -SignatureKind "ErrorPattern" `
        -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "validated" -Message "Regex candidate status mismatch."
    Assert-Equal -Actual $receipt.shadow_recommendation -Expected "new-kbe-candidate" -Message "Regex candidate recommendation mismatch."

    Write-Candidate `
        -Path $candidatePath `
        -Signature @($signature) `
        -Logs $logs `
        -DuplicateStatus "existing-kbe" `
        -ProposedClassification "reuse-existing-kbe"

    $existingCandidate = Get-Content -LiteralPath $candidatePath -Raw | ConvertFrom-Json -Depth 32
    $existingCandidate.duplicate_check.references = @("issue:54321")
    $existingCandidate.duplicate_check.queries[0].result_numbers = @(54321)
    $existingCandidate | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $candidatePath

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "validated" -Message "Existing KBE status mismatch."
    Assert-Equal -Actual $receipt.shadow_recommendation -Expected "reuse-existing-kbe" -Message "Existing KBE recommendation mismatch."

    Write-Candidate `
        -Path $candidatePath `
        -Signature @($signature) `
        -Logs $logs `
        -DuplicateStatus "existing-fix-pr" `
        -ProposedClassification "quarantine-only"
    $existingFixCandidate = Get-Content -LiteralPath $candidatePath -Raw | ConvertFrom-Json -Depth 32
    $existingFixCandidate.duplicate_check.references = @("pull-request:54322")
    $existingFixCandidate.duplicate_check.queries[2].result_numbers = @(54322)
    $existingFixCandidate | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $candidatePath

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "incomplete" -Message "Unsupported existing fix PR status mismatch."
    if (-not (($receipt.reasons -join "`n").Contains("closing-link and changed-file relevance")))
    {
        throw "Unsupported existing fix PR must report the missing proof."
    }

    $existingCandidate.duplicate_check.queries[0].result_numbers = @()
    $existingCandidate | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $candidatePath

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Unsubstantiated existing KBE reference status mismatch."
    Assert-Equal -Actual $receipt.shadow_recommendation -Expected "human-review" -Message "Unsubstantiated existing KBE reference recommendation mismatch."

    Write-Candidate `
        -Path $candidatePath `
        -Signature @($signature) `
        -Logs $logs `
        -ProposedClassification "reuse-existing-kbe"

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Inconsistent duplicate classification status mismatch."
    Assert-Equal -Actual $receipt.shadow_recommendation -Expected "human-review" -Message "Inconsistent duplicate classification recommendation mismatch."

    Write-Candidate `
        -Path $candidatePath `
        -Signature @($signature) `
        -Logs $logs
    $contradictoryDuplicateCandidate = Get-Content -LiteralPath $candidatePath -Raw | ConvertFrom-Json -Depth 32
    $contradictoryDuplicateCandidate.duplicate_check.queries[0].result_numbers = @(54321)
    $contradictoryDuplicateCandidate | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $candidatePath

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Contradictory no-duplicate status mismatch."
    Assert-Equal -Actual $receipt.shadow_recommendation -Expected "human-review" -Message "Contradictory no-duplicate recommendation mismatch."

    $testName = "Microsoft.AspNetCore.Example.Tests.SampleTests.Completes"
    Set-Content -LiteralPath (Join-Path $tempRoot "failure-1.log") -Value @(
        "[FAIL] $testName"
        "First unrelated failure"
    )
    Set-Content -LiteralPath (Join-Path $tempRoot "failure-2.log") -Value @(
        "[FAIL] $testName"
        "Second unrelated failure"
    )
    $logs[0].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-1.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    $logs[1].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-2.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Candidate `
        -Path $candidatePath `
        -Signature @("Microsoft\.AspNetCore\.Example\.Tests\.SampleTests\.Completes") `
        -SignatureKind "ErrorPattern" `
        -Logs $logs

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "rejected" -Message "Regex-escaped test identifier status mismatch."
    Assert-Equal -Actual $receipt.eligible_for_kbe_enrichment -Expected $false -Message "Regex-escaped test identifier eligibility mismatch."

    Set-Content -LiteralPath (Join-Path $tempRoot "failure-1.log") -Value @(
        "[FAIL] Microsoft.AspNetCore.Example.Tests.SampleTests.Completes"
        "Xunit.Sdk.TrueException: $signature"
    )
    Set-Content -LiteralPath (Join-Path $tempRoot "failure-2.log") -Value @(
        "Failed Microsoft.AspNetCore.Example.Tests.SampleTests.Completes [2 s]"
        "Xunit.Sdk.TrueException: $signature"
    )
    $logs[0].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-1.log") -Algorithm SHA256).Hash.ToLowerInvariant()
    $logs[1].sha256 = (Get-FileHash -LiteralPath (Join-Path $tempRoot "failure-2.log") -Algorithm SHA256).Hash.ToLowerInvariant()

    Write-Candidate `
        -Path $candidatePath `
        -Signature @($signature) `
        -Logs $logs
    $staleCandidate = Get-Content -LiteralPath $candidatePath -Raw | ConvertFrom-Json -Depth 32
    $staleCandidate.duplicate_check.checked_utc = [System.DateTimeOffset]::UtcNow.AddHours(-30).ToString("o")
    $staleCandidate | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $candidatePath

    & $evaluator `
        -CandidateFile $candidatePath `
        -EvidenceRoot $tempRoot `
        -OutputFile $receiptPath `
        -RepositoryRoot $repositoryRoot `
        -CandidateSchemaFile $candidateSchema `
        -ReceiptSchemaFile $receiptSchema

    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json -Depth 32
    Assert-Equal -Actual $receipt.deterministic_status -Expected "incomplete" -Message "Stale duplicate search status mismatch."
    Assert-Equal -Actual $receipt.shadow_recommendation -Expected "insufficient-evidence" -Message "Stale duplicate search recommendation mismatch."

    Write-Candidate `
        -Path $candidatePath `
        -Signature @($signature) `
        -Logs $logs
    $wrongRepositoryCandidate = Get-Content -LiteralPath $candidatePath -Raw | ConvertFrom-Json -Depth 32
    $wrongRepositoryCandidate.repository_ref.commit_sha = "1111111111111111111111111111111111111111"
    $wrongRepositoryCandidate | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $candidatePath
    $repositoryMismatchRejected = $false
    try
    {
        & $evaluator `
            -CandidateFile $candidatePath `
            -EvidenceRoot $tempRoot `
            -OutputFile $receiptPath `
            -RepositoryRoot $repositoryRoot `
            -CandidateSchemaFile $candidateSchema `
            -ReceiptSchemaFile $receiptSchema
    }
    catch
    {
        $repositoryMismatchRejected = $_.Exception.Message -match "repository commit does not match"
    }

    Assert-Equal -Actual $repositoryMismatchRejected -Expected $true -Message "Repository commit mismatch was not rejected."
    Assert-Equal -Actual (Test-Path -LiteralPath $receiptPath) -Expected $false -Message "A stale receipt survived repository rejection."

    [System.IO.Directory]::CreateDirectory($outsideRoot) | Out-Null
    $outsideEvidencePath = Join-Path $outsideRoot "failure.log"
    Copy-Item -LiteralPath (Join-Path $tempRoot "failure-1.log") -Destination $outsideEvidencePath
    $symbolicLinkCreated = $false
    try
    {
        [System.IO.Directory]::CreateSymbolicLink($symbolicLinkPath, $outsideRoot) | Out-Null
        $symbolicLinkCreated = $true
    }
    catch
    {
        Write-Warning "Skipping symbolic-link regression because the platform denied link creation."
    }

    if ($symbolicLinkCreated)
    {
        Write-Candidate `
            -Path $candidatePath `
            -Signature @($signature) `
            -Logs $logs
        $linkedCandidate = Get-Content -LiteralPath $candidatePath -Raw | ConvertFrom-Json -Depth 32
        $linkedCandidate.evidence.raw_logs[0].path = "linked-evidence/failure.log"
        $linkedCandidate.evidence.raw_logs[0].sha256 = (Get-FileHash -LiteralPath $outsideEvidencePath -Algorithm SHA256).Hash.ToLowerInvariant()
        $linkedCandidate | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $candidatePath
        $symbolicLinkRejected = $false
        try
        {
            & $evaluator `
                -CandidateFile $candidatePath `
                -EvidenceRoot $tempRoot `
                -OutputFile $receiptPath `
                -RepositoryRoot $repositoryRoot `
                -CandidateSchemaFile $candidateSchema `
                -ReceiptSchemaFile $receiptSchema
        }
        catch
        {
            $symbolicLinkRejected = $_.Exception.Message -match "must not traverse a symbolic link"
        }

        Assert-Equal -Actual $symbolicLinkRejected -Expected $true -Message "A symlinked evidence parent was not rejected."
        Assert-Equal -Actual (Test-Path -LiteralPath $receiptPath) -Expected $false -Message "A stale receipt survived symbolic-link rejection."
    }

    Write-Candidate `
        -Path $candidatePath `
        -Signature @($signature) `
        -Logs $logs
    $tamperedCandidate = Get-Content -LiteralPath $candidatePath -Raw | ConvertFrom-Json -Depth 32
    $tamperedCandidate.evidence.raw_logs[0].sha256 = "0" * 64
    $tamperedCandidate | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $candidatePath
    $hashMismatchRejected = $false
    try
    {
        & $evaluator `
            -CandidateFile $candidatePath `
            -EvidenceRoot $tempRoot `
            -OutputFile $receiptPath `
            -RepositoryRoot $repositoryRoot `
            -CandidateSchemaFile $candidateSchema `
            -ReceiptSchemaFile $receiptSchema
    }
    catch
    {
        $hashMismatchRejected = $_.Exception.Message -match "Evidence hash mismatch"
    }

    Assert-Equal -Actual $hashMismatchRejected -Expected $true -Message "Tampered evidence was not rejected."
    Assert-Equal -Actual (Test-Path -LiteralPath $receiptPath) -Expected $false -Message "A stale receipt survived evidence rejection."

    Write-Host "All test-quarantine KBE shadow evaluator tests passed."
}
finally
{
    if (Test-Path -LiteralPath $symbolicLinkPath)
    {
        Remove-Item -LiteralPath $symbolicLinkPath -Force
    }

    if (Test-Path -LiteralPath $tempRoot)
    {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }

    if (Test-Path -LiteralPath $outsideRoot)
    {
        Remove-Item -LiteralPath $outsideRoot -Recurse -Force
    }
}
