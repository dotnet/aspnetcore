#!/usr/bin/env pwsh
#Requires -Version 7.0

param(
    [string]$SupportRoot = (Join-Path $PSScriptRoot "..\pr-attention-pulse"),
    [string]$BaselineRoot,
    [string]$EvidencePath,
    [string]$CasePattern = "*"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Test-PRAttentionPulse.ps1") -FunctionsOnly
$testRoot = $PSScriptRoot
$supportRoot = $SupportRoot
$fixtureRoot = Join-Path $testRoot "fixtures"
$workflowRoot = Split-Path -Parent $testRoot
$queueRoot = Join-Path (Split-Path -Parent $workflowRoot) "skills\pr-attention-queue"
$queueScript = Join-Path $queueRoot "scripts\Get-PRAttentionQueue.ps1"
$queueFixtureRoot = Join-Path $queueRoot "tests\fixtures"
$sanitizerPath = Join-Path $supportRoot "Sanitize-PRAttentionPulse.ps1"
$combinerPath = Join-Path $supportRoot "Combine-PRAttentionPulse.ps1"
$rendererPath = Join-Path $supportRoot "Render-PRAttentionPulse.ps1"
$validatorPath = Join-Path $supportRoot "Validate-PRAttentionPulseOutput.ps1"
$lockPath = Join-Path $workflowRoot "pr-attention-pulse.lock.yml"
$collectorJsRoot = Join-Path (Get-GhAwExtensionRoot) "actions\setup\js"
$collectorSanitizerPath = Join-Path $collectorJsRoot "sanitize_content.cjs"
$attemptTimestamp = [datetime]"2026-09-10T20:04:56Z"
$queueSnapshot = [datetime]"2026-09-03T18:00:00Z"
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) "pulse-snapshot-$([guid]::NewGuid().ToString('N'))"
$results = [Collections.Generic.List[object]]::new()
$rejectedPublications = [Collections.Generic.List[object]]::new()
$previousUrls = [Environment]::GetEnvironmentVariable("GH_AW_SAFE_OUTPUTS_URLS", "Process")
$previousReferences = [Environment]::GetEnvironmentVariable("GH_AW_ALLOWED_GITHUB_REFS", "Process")

function Invoke-SnapshotCase
{
    param([string]$CaseName, [scriptblock]$Action)

    if ($CaseName -notlike $CasePattern)
    {
        return
    }
    try
    {
        $env:GH_AW_SAFE_OUTPUTS_URLS = "allowed-or-code-region"
        $env:GH_AW_ALLOWED_GITHUB_REFS = "dotnet/aspnetcore"
        Import-Module -Scope Local -Force (Join-Path $supportRoot "PRAttentionPulseContract.psm1")
        & $Action
        $results.Add([pscustomobject]@{ case = $CaseName; outcome = "PASS" })
        Write-Output "PASS $CaseName"
    }
    catch
    {
        $results.Add([pscustomobject]@{ case = $CaseName; outcome = "FAIL"; message = $_.Exception.Message })
        Write-Output "FAIL $CaseName`: $($_.Exception.Message)"
    }
}

function Write-SnapshotCombinedInput
{
    param([object[]]$Areas, [string]$OutputPath, [string]$Combiner = $combinerPath)

    $blazorPath = Join-Path $tempRoot "blazor.json"
    $repositoryPath = Join-Path $tempRoot "repository.json"
    Write-JsonFile $Areas[0] $blazorPath
    Write-JsonFile $Areas[1] $repositoryPath
    & $Combiner -BlazorInputPath $blazorPath -RepositoryWideInputPath $repositoryPath -OutputPath $OutputPath
    Assert-True (-not (Test-Path $blazorPath) -and -not (Test-Path $repositoryPath)) "Combination must remove both intermediate area files."
}

function Get-SnapshotPublicationPaths
{
    param([string]$Root, [string]$RunId = "34643961191", [string]$RunAttempt = "1")

    return [pscustomobject]@{
        Root = $Root
        InputPath = Join-Path $Root "private\pulse-input.json"
        BodyPath = Join-Path $Root "private\pulse-body.md"
        ContextPath = Join-Path $Root "private\pulse-snapshot-context.json"
        AgentOutputPath = Join-Path $Root "agent_output.json"
        RunId = $RunId
        RunAttempt = $RunAttempt
    }
}

function New-SnapshotPublication
{
    param([string]$Name, [string]$InputPath, [string]$RunId = "34643961191", [string]$RunAttempt = "1")

    $root = Join-Path $tempRoot $Name
    $workspace = Join-Path $root "workspace\.pr-attention-pulse"
    $private = Join-Path $root "private"
    New-Item -ItemType Directory -Force $workspace, $private | Out-Null
    $modelInput = Join-Path $workspace "pulse-input.json"
    $modelBody = Join-Path $workspace "pulse-body.md"
    $contextPath = Join-Path $workspace "pulse-snapshot-context.json"
    Copy-Item -LiteralPath $InputPath -Destination $modelInput
    & $rendererPath -InputPath $modelInput -OutputPath $modelBody -SnapshotContextPath $contextPath `
        -Repository "dotnet/aspnetcore" -ServerUrl "https://github.com" -RunId $RunId -RunAttempt $RunAttempt `
        -GeneratedAt "2026-09-23T19:30:00.0000000Z"
    $normalized = Invoke-PinnedOutputSanitizer -Content ([IO.File]::ReadAllText($modelBody))
    [IO.File]::WriteAllText($modelBody, $normalized, [Text.UTF8Encoding]::new($false))
    Write-JsonFile -Value ([ordered]@{
        issue_number = 69328; operation = "replace"; body = $normalized
    }) -Path (Join-Path $workspace "pulse-request.json")
    Copy-Item -LiteralPath $modelInput, $modelBody, $contextPath -Destination $private
    Remove-Item -LiteralPath $contextPath

    return Get-SnapshotPublicationPaths -Root $root -RunId $RunId -RunAttempt $RunAttempt
}

function Copy-SnapshotPublication
{
    param([object]$Publication, [string]$Name)

    $root = Join-Path $tempRoot $Name
    New-Item -ItemType Directory -Path $root | Out-Null
    Copy-Item -LiteralPath (Join-Path $Publication.Root "private") -Destination $root -Recurse

    return Get-SnapshotPublicationPaths -Root $root -RunId $Publication.RunId -RunAttempt $Publication.RunAttempt
}

function Invoke-SnapshotValidation
{
    param(
        [object]$Publication,
        [object]$AgentOutput,
        [string]$ExpectedFailure,
        [hashtable]$ExpectedContext = @{}
    )

    $expected = @{
        ExpectedRepository = "dotnet/aspnetcore"
        ExpectedServerUrl = "https://github.com"
        ExpectedRunId = $Publication.RunId
        ExpectedRunAttempt = $Publication.RunAttempt
    }
    foreach ($name in $ExpectedContext.Keys)
    {
        if ($ExpectedContext[$name] -ceq "")
        {
            $expected.Remove($name)
        }
        else
        {
            $expected[$name] = $ExpectedContext[$name]
        }
    }
    Write-JsonFile -Value $AgentOutput -Path $Publication.AgentOutputPath
    $rawOutputPath = Join-Path $Publication.Root "safeoutputs.jsonl"
    Write-JsonFile -Value $AgentOutput.items[0] -Path $rawOutputPath
    $messages = @(& pwsh -NoProfile -File $validatorPath `
        -AgentOutputPath $Publication.AgentOutputPath -PulseInputPath $Publication.InputPath `
        -ExpectedBodyPath $Publication.BodyPath -SnapshotContextPath $Publication.ContextPath `
        -SanitizerModulePath $collectorSanitizerPath -ExpectedIssueNumber 69328 @expected 2>&1)
    $exitCode = $LASTEXITCODE
    $retained = Get-Content -LiteralPath $Publication.AgentOutputPath -Raw | ConvertFrom-Json -Depth 100
    if ($ExpectedFailure)
    {
        Assert-True ($exitCode -ne 0) "The real private validator unexpectedly accepted the invalid snapshot."
        Assert-True (($messages -join " ").Contains($ExpectedFailure, [StringComparison]::Ordinal)) "Expected '$ExpectedFailure'; actual: $($messages -join ' ')"
        Assert-True (($retained | ConvertTo-Json -Compress) -ceq '{"items":[],"errors":[]}') "Rejection must disarm the exact collected output envelope."
        Assert-True (-not (Test-Path $rawOutputPath) -and -not (Test-Path "$($Publication.AgentOutputPath).rejected")) "Rejection must remove raw and quarantined output."
        $rejectedPublications.Add([pscustomobject]@{
            name = Split-Path -Leaf $Publication.Root
            outputPath = $Publication.AgentOutputPath
        })
    }
    else
    {
        Assert-True ($exitCode -eq 0) "The real private validator failed: $($messages -join ' ')"
        Assert-True ($retained.items.Count -eq 1 -and $retained.items[0].body -ceq [IO.File]::ReadAllText($Publication.BodyPath)) "Validation must retain exactly the canonical replacement."
    }
    Assert-True (-not (Test-Path "$($Publication.BodyPath).rendered") -and -not (Test-Path "$($Publication.BodyPath).normalized")) "Validation must clean its temporary renderings."
}

New-Item -ItemType Directory -Path $tempRoot | Out-Null
try
{
    $version = (& gh aw --version 2>&1) -join "`n"
    Assert-True ($LASTEXITCODE -eq 0 -and $version.Contains("v0.88.7")) "Snapshot coverage requires the reviewed gh-aw v0.88.7."
    Import-Module -Scope Local -Force (Join-Path $supportRoot "PRAttentionPulseContract.psm1")
    $blazor = Invoke-RealQueueFixture -FixtureName "pull-requests.json" -Scope blazor
    $repositoryWide = Invoke-RealQueueFixture -FixtureName "pull-requests.json" -Scope repository-wide
    $zero = Invoke-Sanitizer -FixtureName "complete-zero.json"
    $repositoryZeroRaw = Get-Content -LiteralPath (Join-Path $fixtureRoot "complete-zero.json") -Raw | ConvertFrom-Json -Depth 100
    $repositoryZeroRaw.filter = $repositoryWide.source.filter
    $rawZeroPath = Join-Path $tempRoot "repository-zero.json"
    Write-JsonFile $repositoryZeroRaw $rawZeroPath
    $repositoryZero = Invoke-Sanitizer -SourcePath $rawZeroPath -Scope repository-wide
    $failedBlazor = Invoke-Sanitizer -CollectionExitCode 9 -Scope blazor
    $failedRepository = Invoke-Sanitizer -CollectionExitCode 9 -Scope repository-wide
    $scenarios = [ordered]@{
        complete = @($blazor, $repositoryWide)
        "complete-zero" = @($zero, $repositoryZero)
        "partial-blazor-unavailable" = @($failedBlazor, $repositoryWide)
        "partial-repository-unavailable" = @($blazor, $failedRepository)
        unavailable = @($failedBlazor, $failedRepository)
    }
    $publications = @{}
    foreach ($name in $scenarios.Keys)
    {
        $inputPath = Join-Path $tempRoot "$name.json"
        Write-SnapshotCombinedInput -Areas $scenarios[$name] -OutputPath $inputPath
        $publications[$name] = New-SnapshotPublication -Name $name -InputPath $inputPath
        Invoke-SnapshotCase "PublishedSnapshot/$name" {
            $publication = $publications[$name]
            $body = [IO.File]::ReadAllText($publication.BodyPath)
            $pulse = Get-Content -LiteralPath $inputPath -Raw | ConvertFrom-Json -Depth 100
            $hash = (Get-FileHash -LiteralPath $inputPath -Algorithm SHA256).Hash.ToLowerInvariant()
            Assert-True ((Get-FileHash $publication.InputPath).Hash.ToLowerInvariant() -ceq $hash) "The private upload source must preserve the exact combined bytes."
            Assert-True ([regex]::Matches($body, "(?m)^## Snapshot$").Count -eq 1 -and
                [regex]::Matches($body, "(?m)^<details>$").Count -eq 4) "Exactly one snapshot, both collapsed areas, and the collapsed JSON/identity blocks must accompany the body."
            foreach ($text in @(
                'Snapshot generated: `2026-09-23T19:30:00.0000000Z`.',
                '[Producing workflow run](https://github.com/dotnet/aspnetcore/actions/runs/34643961191/attempts/1)',
                'Artifact: `pulse-publication-evidence`; files: `pulse-input.json`, `pulse-body.md`.',
                'Repository: `dotnet/aspnetcore`; run ID: `34643961191`; attempt: `1`.',
                "JSON SHA-256: ``$hash``.",
                "Capped report results, not the full queue."))
            {
                Assert-True ($body.Contains($text, [StringComparison]::Ordinal)) "The canonical snapshot lost '$text'."
            }
            $jsonMatch = [regex]::Match($body, '(?s)<summary>Snapshot JSON \(exact sanitized bytes\)</summary>\n\n(?<fence>`{3,})json\n(?<json>.*?)\n\k<fence>\n\n</details>')
            Assert-True $jsonMatch.Success "The small combined snapshot must embed the exact JSON in a collapsed section."
            $rawJson = [IO.File]::ReadAllText($inputPath)
            Assert-True ($jsonMatch.Groups["json"].Value -ceq $rawJson) "The embedded JSON must be the exact sanitized combined bytes, not a reformatted or truncated copy."
            $expectedStatus = if ($name -like "partial-*") { "partial" } elseif ($name -eq "unavailable") { "unavailable" } else { "complete" }
            Assert-True ($pulse.schemaVersion -ceq "2.0.0" -and $pulse.status -ceq $expectedStatus) "Snapshot identity must not alter the combined data state."
            foreach ($area in $pulse.areas)
            {
                $areaBody = Get-PulseAreaBlock -Body $body -Label $area.label
                if ($area.status -ceq "unavailable")
                {
                    Assert-True (-not $area.candidateCountsAvailable -and -not $area.PSObject.Properties["views"] -and
                        $null -eq $area.source.census -and $areaBody.Contains("Candidate counts unavailable.")) "Unavailable areas must not invent views or zero counts."
                }
                elseif ($name -eq "complete-zero")
                {
                    Assert-True ($area.candidateCountsAvailable -and $area.source.census.matched -eq 0 -and
                        [regex]::Matches($areaBody, "None in this complete inventory\.").Count -eq 5) "Complete zero must retain five honestly empty views."
                }
                else
                {
                    Assert-True ($area.source.census.matched -gt 0 -and $areaBody.Contains("Query coverage:")) "Successful inventory and bounded coverage must remain visible."
                }
            }
            $workspaceNames = @(Get-ChildItem -LiteralPath (Join-Path $publication.Root "workspace\.pr-attention-pulse") | Sort-Object Name | ForEach-Object Name)
            Assert-True (($workspaceNames -join ",") -ceq "pulse-body.md,pulse-input.json,pulse-request.json") "The private context must never add a fourth model-visible file."
            $output = Invoke-PinnedCollector -Body $body
            Assert-True ($output.errors.Count -eq 0 -and $output.items[0].body -ceq $body) "The pinned collector must preserve the entire snapshot."
            Invoke-SnapshotValidation -Publication $publication -AgentOutput $output
            if ($BaselineRoot)
            {
                $baselineInput = Join-Path $tempRoot "$name.baseline.json"
                Write-SnapshotCombinedInput -Areas $scenarios[$name] -OutputPath $baselineInput `
                    -Combiner (Join-Path $BaselineRoot ".github\workflows\pr-attention-pulse\Combine-PRAttentionPulse.ps1")
                Assert-True ((Get-FileHash $baselineInput).Hash -ceq (Get-FileHash $inputPath).Hash) "Same-input JSON bytes, classifications, caps and ordering must match unchanged main."
            }
        }
    }
    $publicationA = $publications.complete
    $bodyA = [IO.File]::ReadAllText($publicationA.BodyPath)
    $outputA = Invoke-PinnedCollector -Body $bodyA
    Invoke-SnapshotValidation -Publication $publicationA -AgentOutput $outputA
    $publicationB = New-SnapshotPublication -Name "publication-b" `
        -InputPath $publications."partial-blazor-unavailable".InputPath -RunId "34643961192" -RunAttempt "2"
    $bodyB = [IO.File]::ReadAllText($publicationB.BodyPath)
    $outputB = Invoke-PinnedCollector -Body $bodyB
    Invoke-SnapshotValidation -Publication $publicationB -AgentOutput $outputB

    foreach ($variant in @("newline", "whitespace", "bom"))
    {
        Invoke-SnapshotCase "ExactBytes/$variant" {
            $publication = Copy-SnapshotPublication $publicationA "bytes-$variant"
            $original = [IO.File]::ReadAllBytes($publication.InputPath)
            $json = [IO.File]::ReadAllText($publication.InputPath)
            if ($variant -eq "bom")
            {
                [IO.File]::WriteAllBytes($publication.InputPath, [byte[]](@(0xEF, 0xBB, 0xBF) + $original))
            }
            else
            {
                $changed = if ($variant -eq "newline") { $json + "`n" } else { "  " + $json }
                [IO.File]::WriteAllText($publication.InputPath, $changed, [Text.UTF8Encoding]::new($false))
            }
            $parsedBefore = $json | ConvertFrom-Json -Depth 100 | ConvertTo-Json -Depth 100 -Compress
            $parsedAfter = Get-Content -LiteralPath $publication.InputPath -Raw | ConvertFrom-Json -Depth 100 | ConvertTo-Json -Depth 100 -Compress
            Assert-True ($parsedBefore -ceq $parsedAfter) "The byte mutation must leave parsed PR data identical."
            Invoke-SnapshotValidation $publication $outputA "checksum does not match the exact input file bytes"
            $fresh = New-SnapshotPublication -Name "regenerated-$variant" -InputPath $publication.InputPath
            $freshBody = [IO.File]::ReadAllText($fresh.BodyPath)
            $actualHash = (Get-FileHash -LiteralPath $publication.InputPath -Algorithm SHA256).Hash.ToLowerInvariant()
            Assert-True ($freshBody -cne $bodyA -and $freshBody.Contains("JSON SHA-256: ``$actualHash``.")) "A newly generated snapshot must bind the changed file bytes, including encoding and newline."
            Invoke-SnapshotValidation $fresh (Invoke-PinnedCollector -Body $freshBody)
        }
    }
    foreach ($case in @(
        @{ Name = "run"; Field = "ExpectedRunId"; Value = "34643961192"; Error = "does not match the current Actions" },
        @{ Name = "attempt"; Field = "ExpectedRunAttempt"; Value = "2"; Error = "does not match the current Actions" },
        @{ Name = "repository"; Field = "ExpectedRepository"; Value = "dotnet/runtime"; Error = "requires the trusted" },
        @{ Name = "server"; Field = "ExpectedServerUrl"; Value = "https://example.invalid"; Error = "requires the trusted" },
        @{ Name = "missing-run"; Field = "ExpectedRunId"; Value = ""; Error = "positive decimal strings" },
        @{ Name = "missing-attempt"; Field = "ExpectedRunAttempt"; Value = ""; Error = "positive decimal strings" }))
    {
        Invoke-SnapshotCase "CurrentActionsIdentity/$($case.Name)" {
            $publication = Copy-SnapshotPublication $publicationA "current-$($case.Name)"
            $expected = @{ $case.Field = $case.Value }
            Invoke-SnapshotValidation $publication $outputA $case.Error -ExpectedContext $expected
        }
    }
    foreach ($case in @(
        @{ Field = "runId"; Value = "34643961192"; Error = "does not match the current Actions" },
        @{ Field = "runAttempt"; Value = "2"; Error = "does not match the current Actions" },
        @{ Field = "repository"; Value = "dotnet/runtime"; Error = "requires the trusted" },
        @{ Field = "serverUrl"; Value = "http://github.com"; Error = "requires the trusted" },
        @{ Field = "generatedAt"; Value = "2026-09-24T19:30:00.0000000Z"; Error = "trusted Pulse body does not match" },
        @{ Field = "inputSha256"; Value = ("0" * 64); Error = "checksum does not match the exact input file bytes" },
        @{ Field = "artifactName"; Value = "pulse-publication-evidence-2"; Error = "artifact and file names are invalid" },
        @{ Field = "inputFileName"; Value = "queue.json"; Error = "artifact and file names are invalid" },
        @{ Field = "bodyFileName"; Value = "report.md"; Error = "artifact and file names are invalid" }))
    {
        Invoke-SnapshotCase "FrozenIdentity/$($case.Field)" {
            $publication = Copy-SnapshotPublication $publicationA "context-$($case.Field)"
            $context = Read-PulseSnapshotContext -Path $publication.ContextPath
            $context.($case.Field) = $case.Value
            Write-JsonFile $context $publication.ContextPath
            Invoke-SnapshotValidation $publication $outputA $case.Error
        }
    }
    foreach ($case in @(
        @{ Name = "missing"; Value = $null; Error = "snapshot context is missing" },
        @{ Name = "malformed"; Value = "{not-json"; Error = "context is malformed JSON" },
        @{ Name = "null"; Value = "null"; Error = "context must be an object" },
        @{ Name = "partial"; Value = '{"repository":"dotnet/aspnetcore"}'; Error = "missing or unexpected fields" },
        @{ Name = "duplicate"; Value = '{"runId":"1","runId":"2"}'; Error = "unique string fields" },
        @{ Name = "numeric"; Value = '{"runId":34643961191}'; Error = "unique string fields" }))
    {
        Invoke-SnapshotCase "ContextShape/$($case.Name)" {
            $publication = Copy-SnapshotPublication $publicationA "shape-$($case.Name)"
            if ($null -eq $case.Value)
            {
                Remove-Item -LiteralPath $publication.ContextPath
            }
            else
            {
                [IO.File]::WriteAllText($publication.ContextPath, $case.Value)
            }
            Invoke-SnapshotValidation $publication $outputA $case.Error
        }
    }
    foreach ($field in @("runId", "runAttempt"))
    {
        foreach ($decimal in @(
            @{ Name = "zero"; Value = "0" }, @{ Name = "negative"; Value = "-1" },
            @{ Name = "leading-zero"; Value = "01" }, @{ Name = "fraction"; Value = "1.0" },
            @{ Name = "exponent"; Value = "1e2" }, @{ Name = "newline"; Value = "1`n" },
            @{ Name = "space"; Value = " 1" }, @{ Name = "path"; Value = "1/attempts/2" }))
        {
            $caseName = "$field/$($decimal.Name)"
            Invoke-SnapshotCase "ContextDecimal/$caseName" {
                $publication = Copy-SnapshotPublication $publicationA "decimal-$($results.Count)"
                $context = Read-PulseSnapshotContext -Path $publication.ContextPath
                $context.$field = $decimal.Value
                Write-JsonFile $context $publication.ContextPath
                Invoke-SnapshotValidation $publication $outputA "positive decimal strings"
            }
        }
    }
    foreach ($value in @("2026-09-23T19:30:00Z", "2026-09-23T19:30:00.0000000+00:00", "09/23/2026 19:30:00", "2026-02-30T19:30:00.0000000Z"))
    {
        Invoke-SnapshotCase "ContextTime/$value" {
            $publication = Copy-SnapshotPublication $publicationA "time-$($results.Count)"
            $context = Read-PulseSnapshotContext -Path $publication.ContextPath
            $context.generatedAt = $value
            Write-JsonFile $context $publication.ContextPath
            Invoke-SnapshotValidation $publication $outputA "invariant UTC ISO-8601 timestamp"
        }
    }
    Invoke-SnapshotCase "Replay/prior-context" {
        $publication = Copy-SnapshotPublication $publicationA "prior-context"
        $publication.RunId = $publicationB.RunId
        $publication.RunAttempt = $publicationB.RunAttempt
        Invoke-SnapshotValidation $publication $outputA "does not match the current Actions"
    }
    Invoke-SnapshotCase "ContextShape/unexpected-url" {
        $publication = Copy-SnapshotPublication $publicationA "unexpected-url"
        $context = Read-PulseSnapshotContext -Path $publication.ContextPath
        $context | Add-Member runUrl "https://github.com/dotnet/aspnetcore/actions/runs/1"
        Write-JsonFile $context $publication.ContextPath
        Invoke-SnapshotValidation $publication $outputA "missing or unexpected fields"
    }
    foreach ($hash in @(
        @{ Name = "short"; Value = "0123456789abcdef" },
        @{ Name = "uppercase"; Value = ("ABCDEF12" * 8) },
        @{ Name = "non-hex"; Value = ("g" * 64) },
        @{ Name = "long"; Value = ("a" * 65) }))
    {
        Invoke-SnapshotCase "ContextHash/$($hash.Name)" {
            $publication = Copy-SnapshotPublication $publicationA "hash-$($hash.Name)"
            $context = Read-PulseSnapshotContext -Path $publication.ContextPath
            $context.inputSha256 = $hash.Value
            Write-JsonFile $context $publication.ContextPath
            Invoke-SnapshotValidation $publication $outputA "full lowercase SHA-256"
        }
    }
    Invoke-SnapshotCase "ContextDecimal/large-identifiers" {
        $publication = New-SnapshotPublication -Name "large-identifiers" -InputPath $publicationA.InputPath `
            -RunId "123456789012345678901234567890" -RunAttempt "12345678901"
        $body = [IO.File]::ReadAllText($publication.BodyPath)
        Assert-True ($body.Contains("/123456789012345678901234567890/attempts/12345678901")) "Run identifiers must not be narrowed or rounded."
        Invoke-SnapshotValidation $publication (Invoke-PinnedCollector -Body $body)
    }
    Invoke-SnapshotCase "Timestamp/frozen-generation" {
        $publication = Copy-SnapshotPublication $publicationA "captured-time"
        $before = [datetime]::UtcNow
        & $rendererPath -InputPath $publication.InputPath -OutputPath $publication.BodyPath -SnapshotContextPath $publication.ContextPath `
            -Repository "dotnet/aspnetcore" -ServerUrl "https://github.com" -RunId $publication.RunId -RunAttempt $publication.RunAttempt
        $after = [datetime]::UtcNow
        Import-Module -Scope Local -Force (Join-Path $supportRoot "PRAttentionPulseContract.psm1")
        $context = Read-PulseSnapshotContext -Path $publication.ContextPath
        $captured = [datetime]::ParseExact($context.generatedAt, "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
            [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind)
        Assert-True ($captured -ge $before -and $captured -le $after) "The production default must capture generation time after combination."
        $body = Invoke-PinnedOutputSanitizer -Content ([IO.File]::ReadAllText($publication.BodyPath))
        [IO.File]::WriteAllText($publication.BodyPath, $body, [Text.UTF8Encoding]::new($false))
        $frozen = (Get-FileHash $publication.ContextPath).Hash
        Invoke-SnapshotValidation $publication (Invoke-PinnedCollector -Body $body)
        Assert-True ((Get-FileHash $publication.ContextPath).Hash -ceq $frozen -and
            [IO.File]::ReadAllText($publication.BodyPath) -ceq $body) "Validation must reuse frozen time without changing context or report."
    }

    Import-Module -Scope Local -Force (Join-Path $supportRoot "PRAttentionPulseContract.psm1")
    $contextA = Read-PulseSnapshotContext -Path $publicationA.ContextPath
    $pulseA = Get-Content -LiteralPath $publicationA.InputPath -Raw | ConvertFrom-Json -Depth 100
    $jsonA = [IO.File]::ReadAllText($publicationA.InputPath)
    $bodyMutations = [ordered]@{
        run = { param($body) $body.Replace("34643961191", "34643961192") }
        attempt = { param($body) $body.Replace("/attempts/1", "/attempts/2").Replace('attempt: `1`', 'attempt: `2`') }
        repository = { param($body) $body.Replace('Repository: `dotnet/aspnetcore`', 'Repository: `dotnet/runtime`') }
        time = { param($body) $body.Replace("2026-09-23T19:30:00.0000000Z", "2026-09-24T19:30:00.0000000Z") }
        checksum = { param($body) $body.Replace($contextA.inputSha256, ("f" * 64)) }
        artifact = { param($body) $body.Replace("pulse-publication-evidence", "pulse-publication-evidence-2") }
        input = { param($body) $body.Replace("pulse-input.json", "different.json") }
        report = { param($body) $body.Replace("pulse-body.md", "different.md") }
        url = { param($body) $body.Replace("/attempts/1", "/attempts/1?download=1") }
        "link-label" = { param($body) $body.Replace("Producing workflow run", "Direct download") }
        "details-expanded" = { param($body) $body.Replace("<summary>Snapshot identity", "<summary>Expanded identity") }
        "duplicate-snapshot" = { param($body) $body + "`n`n" + $body.Substring($body.IndexOf("## Snapshot", [StringComparison]::Ordinal)) }
    }
    foreach ($name in $bodyMutations.Keys)
    {
        Invoke-SnapshotCase "BodyIdentity/$name" {
            $tampered = & $bodyMutations[$name] $bodyA
            Assert-True ($tampered -cne $bodyA) "The body mutation must exercise its intended field."
            Assert-Throws {
                Assert-PRAttentionPulseOutput -AgentOutput (New-ValidAgentOutput $tampered) -Pulse $pulseA -Json $jsonA `
                    -SnapshotContext $contextA -ExpectedBody $tampered -ExpectedIssueNumber 69328
            } "Only the one regenerated snapshot may be exempted." "exactly the trusted Pulse snapshot section"
            $publication = Copy-SnapshotPublication $publicationA "body-$name"
            $collected = Invoke-PinnedCollector -Body $tampered
            Assert-True ($collected.items.Count -eq 1 -and $collected.items[0].body -cne $bodyA) "The real collector must reach an altered body."
            Invoke-SnapshotValidation $publication $collected "emitted body does not exactly match"
        }
    }
    foreach ($case in @(
        @{ Name = "run-link-outside-snapshot"; Text = "[run](https://github.com/dotnet/aspnetcore/actions/runs/34643961191/attempts/1)"; Error = "prohibited content" },
        @{ Name = "filename-outside-snapshot"; Text = "pulse-input.json"; Error = "prohibited content" },
        @{ Name = "external-domain"; Text = "example.com"; Error = "prohibited content" },
        @{ Name = "external-link"; Text = "[outside](https://example.com/)"; Error = "prohibited content" },
        @{ Name = "mention"; Text = "@someone"; Error = "prohibited content" },
        @{ Name = "extra-details"; Text = "<details>`n<summary>Extra</summary>`n</details>"; Error = "unexpected details or summary block" }))
    {
        Invoke-SnapshotCase "Restrictions/$($case.Name)" {
            $tampered = $bodyA.Replace("## Snapshot", "$($case.Text)`n`n## Snapshot")
            Assert-Throws {
                Assert-PRAttentionPulseOutput -AgentOutput (New-ValidAgentOutput $tampered) -Pulse $pulseA -Json $jsonA `
                    -SnapshotContext $contextA -ExpectedBody $tampered -ExpectedIssueNumber 69328
            } "General body restrictions must remain enforced." $case.Error
            $publication = Copy-SnapshotPublication $publicationA "restriction-$($case.Name)"
            Invoke-SnapshotValidation $publication (Invoke-PinnedCollector -Body $tampered) "emitted body does not exactly match"
        }
    }

    Invoke-SnapshotCase "Archive/exact-upload-bytes" {
        $upload = Join-Path $tempRoot "upload"
        New-Item -ItemType Directory -Path $upload | Out-Null
        Copy-Item -LiteralPath $publicationA.InputPath, $publicationA.BodyPath -Destination $upload
        $zipPath = Join-Path $tempRoot "evidence.zip"
        [IO.Compression.ZipFile]::CreateFromDirectory($upload, $zipPath)
        $archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
        try
        {
            Assert-True ((@($archive.Entries.FullName | Sort-Object) -join ",") -ceq "pulse-body.md,pulse-input.json") "The artifact must contain only the existing canonical pair."
            foreach ($entry in $archive.Entries)
            {
                $destination = Join-Path $tempRoot "archived-$($entry.Name)"
                [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination)
                Assert-True ((Get-FileHash $destination).Hash -ceq (Get-FileHash (Join-Path $upload $entry.Name)).Hash) "Archive content must retain exact upload bytes."
            }
            Assert-True ((Get-FileHash (Join-Path $tempRoot "archived-pulse-input.json")).Hash.ToLowerInvariant() -ceq $contextA.inputSha256) "The report checksum must identify JSON bytes, not the ZIP."
        }
        finally
        {
            $archive.Dispose()
        }
    }
    foreach ($length in @(65000, 65001))
    {
        Invoke-SnapshotCase "BodyLimit/$length" {
            $body = ("x" * ($length - $bodyA.Length)) + $bodyA
            $action = {
                Assert-PRAttentionPulseOutput -AgentOutput (New-ValidAgentOutput $body) -Pulse $pulseA -Json $jsonA `
                    -SnapshotContext $contextA -ExpectedBody $body -ExpectedIssueNumber 69328
            }
            if ($length -eq 65000)
            {
                & $action
            }
            else
            {
                Assert-Throws $action "The body limit must not increase." "outside the allowed size range"
            }
        }
    }
    foreach ($size in @(131072, 131073))
    {
        Invoke-SnapshotCase "CombinedLimit/$size" {
            $areas = $scenarios.complete | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100
            $areas[0].source.warnings = @("x")
            $path = Join-Path $tempRoot "limit-$size.json"
            Write-SnapshotCombinedInput -Areas $areas -OutputPath $path
            $baseSize = [Text.Encoding]::UTF8.GetByteCount([IO.File]::ReadAllText($path).TrimEnd("`r", "`n"))
            $areas[0].source.warnings = @("x" * ($size - $baseSize + 1))
            if ($size -eq 131072)
            {
                Write-SnapshotCombinedInput -Areas $areas -OutputPath $path
                Assert-True ([Text.Encoding]::UTF8.GetByteCount([IO.File]::ReadAllText($path).TrimEnd("`r", "`n")) -eq $size) "Exercise the actual combined JSON byte threshold, excluding its existing final newline."
            }
            else
            {
                Assert-Throws { Write-SnapshotCombinedInput -Areas $areas -OutputPath $path } "The combined input limit must not increase." "exceeds the configured size limit"
            }
        }
    }
    Invoke-SnapshotCase "Publication/atomic-replacement-and-rejection" {
        $manifestPath = Join-Path $tempRoot "publications.json"
        Write-JsonFile -Value ([ordered]@{
            first = @{ outputPath = $publicationA.AgentOutputPath; bodyPath = $publicationA.BodyPath }
            second = @{ outputPath = $publicationB.AgentOutputPath; bodyPath = $publicationB.BodyPath }
            rejected = @($rejectedPublications)
        }) -Path $manifestPath
        & node (Join-Path $testRoot "Test-PulseIssueUpdate.cjs") $collectorJsRoot $lockPath $publicationA.BodyPath $manifestPath
        Assert-True ($LASTEXITCODE -eq 0) "The real pinned dispatcher and issue handler must atomically replace or retain the whole report and reference."
    }
    Invoke-SnapshotCase "Retrieval/documented-consumer" {
        & pwsh -NoProfile -File (Join-Path $testRoot "Test-PulseSnapshotRetrieval.ps1") `
            -PulseInputPath $publicationA.InputPath -PublishedBodyPath $publicationA.BodyPath
        Assert-True ($LASTEXITCODE -eq 0) "The documented published-reference retrieval algorithm must pass every local API/ZIP fixture."
    }
    Assert-True ($results.Count -gt 0) "No snapshot cases matched '$CasePattern'."
    Assert-True (@($results | Where-Object outcome -CEQ "FAIL").Count -eq 0) "Snapshot cases failed; inspect each named outcome above."
    Write-Output "All $($results.Count) Pulse snapshot cases passed."
}
finally
{
    [Environment]::SetEnvironmentVariable("GH_AW_SAFE_OUTPUTS_URLS", $previousUrls, "Process")
    [Environment]::SetEnvironmentVariable("GH_AW_ALLOWED_GITHUB_REFS", $previousReferences, "Process")
    if ($EvidencePath)
    {
        Write-JsonFile -Value ([pscustomobject]@{
            boundary = "Local fixture production, exact bytes, pinned collector, private validator and issue-handler mocked transport; no hosted publication."
            cases = @($results)
        }) -Path $EvidencePath
    }
    Remove-Item -LiteralPath $tempRoot -Recurse -Force
}
