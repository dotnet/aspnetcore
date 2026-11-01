#requires -Version 7.0
param([switch]$KeepArtifacts)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$checker = Join-Path $PSScriptRoot 'check_evidence_scout.ps1'
$capabilityChecker = Join-Path $PSScriptRoot 'check_reviewer_capability.ps1'
$fixtureRoot = Join-Path $PSScriptRoot 'fixtures/reviewer-capability'
$discoverySpec = (Resolve-Path (
    Join-Path $PSScriptRoot 'reviewer-capability-evidence-scout.vally.yaml'
)).Path
$discoverySkill = Join-Path (
    $PSScriptRoot
) 'treatments/evidence-scout/discovery/review-pull-request/SKILL.md'
$finalizerSkillRoot = (Resolve-Path (
    Join-Path $PSScriptRoot 'treatments/evidence-scout/finalizer/review-pull-request'
)).Path
$finalizerSkill = Join-Path $finalizerSkillRoot 'SKILL.md'
$finalizerReference = Join-Path (
    $finalizerSkillRoot
) 'references/evidence-scout.md'
$labelsPath = (Resolve-Path (
    Join-Path $PSScriptRoot 'reviewer-capability-labels.json'
)).Path
$rubricPath = (Resolve-Path (
    Join-Path $PSScriptRoot 'reviewer-capability-rubric.md'
)).Path
$caseName = 'pr-70106'
$runId = '33333333-3333-3333-3333-333333333333'
$finalizerRunId = '44444444-4444-4444-4444-444444444444'
$prompt = "This is an explicitly configured offline evidence-scout phase. Use the review-pull-request skill's claim-neutral evidence-scout path: inspect the complete frozen projection under review/bundle, do not perform a pull request review, do not form defect conclusions, and do not spawn workers or subagents. Produce only the exact JSON evidence inventory required by the installed skill. Do not modify files or contact GitHub.`n"
$finalizerPrompt = "This is an explicitly configured offline one-pass comparison. Use the review-pull-request skill's single-reviewer path: apply the exact routed guide texts and gates in this one context, do not spawn workers or subagents, and report PATH: single-reviewer. Review the frozen pull request represented by review/bundle. Treat it as the revision presented for review, use the included source, diff, changed-file metadata, pull description, existing feedback, guidance, policies, exclusions, and context, and report only actionable changed-line findings not already covered by feedback. If there are no findings, say NO_FINDINGS. Do not modify files or contact GitHub.`n"
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-evidence-scout-test-' + [guid]::NewGuid().ToString('N')
)

function Assert-Throws {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)][string]$ExpectedMessage,
        [Parameter(Mandatory = $true)][string]$FailureMessage
    )

    $threw = $false
    try {
        & $Action
    } catch {
        $threw = $true
        if ($_.Exception.Message -notlike "*$ExpectedMessage*") {
            throw "Expected '$ExpectedMessage'; got '$($_.Exception.Message)'."
        }
    }
    if (-not $threw) {
        throw $FailureMessage
    }
}

function Get-Sha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    return (Get-FileHash -Algorithm SHA256 $Path).Hash.ToLowerInvariant()
}

function Get-TextSha256 {
    param([Parameter(Mandatory = $true)][string]$Text)

    return [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData(
            [Text.Encoding]::UTF8.GetBytes($Text)
        )
    ).ToLowerInvariant()
}

function Get-NativeSkillBody {
    param([Parameter(Mandatory = $true)][string]$Path)

    $text = (Get-Content $Path -Raw).
        Replace("`r`n", "`n").
        Replace("`r", "`n")
    return [regex]::Replace(
        $text,
        '\A---\n.*?\n---\n+',
        '',
        [Text.RegularExpressions.RegexOptions]::Singleline
    )
}

function Get-CaseRoot {
    return Join-Path $fixtureRoot "cases/$caseName/bundle"
}

function Get-Manifest {
    return Get-Content (
        Join-Path (Get-CaseRoot) 'manifest.json'
    ) -Raw | ConvertFrom-Json -Depth 100
}

function Resolve-BundleFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path (Get-CaseRoot) $RelativePath
    if (Test-Path $path -PathType Leaf) {
        return $path
    }
    if ($RelativePath.StartsWith('guidance/', [StringComparison]::Ordinal)) {
        return Join-Path (
            Join-Path $fixtureRoot 'common/guidance'
        ) $RelativePath.Substring('guidance/'.Length)
    }
    if ($RelativePath.StartsWith('required/', [StringComparison]::Ordinal)) {
        return Join-Path (
            Join-Path $fixtureRoot 'common/required'
        ) $RelativePath.Substring('required/'.Length)
    }
    throw "Cannot resolve '$RelativePath'."
}

function Add-ReadEvents {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [Collections.Generic.List[object]]$Events,
        [Parameter(Mandatory = $true)][string]$ToolPath,
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string]$Id
    )

    $Events.Add(@{
        type = 'tool_call'
        data = @{
            toolName = 'view'
            toolCallId = $Id
            turnId = '0'
            arguments = @{
                path = $ToolPath
                forceReadLargeFiles = $true
            }
        }
        turn = 0
    })
    $Events.Add(@{
        type = 'tool_result'
        data = @{
            toolName = 'view'
            toolCallId = $Id
            success = $true
            result = @{
                content = Get-Content $FilePath -Raw
            }
        }
        turn = 0
    })
}

function New-ValidInventory {
    $manifest = Get-Manifest
    $headRoot = Join-Path (Get-CaseRoot) $manifest.sources.head.root
    $paths = @(
        Get-ChildItem $headRoot -Recurse -File |
            ForEach-Object {
                $relative = [IO.Path]::GetRelativePath(
                    $headRoot,
                    $_.FullName
                ).Replace('\', '/')
                $relative.Substring(0, $relative.Length - '.source'.Length)
            } |
            Sort-Object
    )
    $evidencePath = $paths[0]
    $evidenceFile = Join-Path $headRoot "$evidencePath.source"
    $lines = @(Get-Content $evidenceFile)
    $lineNumber = 1
    while ($lineNumber -le $lines.Count -and
        [string]::IsNullOrWhiteSpace($lines[$lineNumber - 1])) {
        $lineNumber++
    }
    $excerpt = [string]$lines[$lineNumber - 1]
    $symbol = [regex]::Match($excerpt, '[A-Za-z_][A-Za-z0-9_]*').Value
    if ([string]::IsNullOrWhiteSpace($symbol)) {
        throw 'The test fixture cannot select a source token.'
    }
    $coverage = @(
        foreach ($path in $paths) {
            $mergeFile = Join-Path (
                Join-Path (Get-CaseRoot) $manifest.sources.mergeBase.root
            ) "$path.source"
            $baseTipFile = Join-Path (
                Join-Path (Get-CaseRoot) $manifest.sources.baseTip.root
            ) "$path.source"
            [ordered]@{
                path = $path
                head = 'scanned'
                mergeBase = if (Test-Path $mergeFile -PathType Leaf) {
                    'scanned'
                } else {
                    'absent'
                }
                baseTip = if (Test-Path $baseTipFile -PathType Leaf) {
                    'scanned'
                } else {
                    'absent'
                }
                evidenceIds = @(
                    if ($path -eq $evidencePath) {
                        'E001'
                    }
                )
            }
        }
    )
    $citations = [Collections.Generic.List[object]]::new()
    $citations.Add([ordered]@{
        id = 'E001-C01'
        role = 'head'
        path = $evidencePath
        startLine = $lineNumber
        endLine = $lineNumber
        excerpt = $excerpt
        excerptSha256 = Get-TextSha256 $excerpt
    })
    $mergeFile = Join-Path (
        Join-Path (Get-CaseRoot) $manifest.sources.mergeBase.root
    ) "$evidencePath.source"
    if (Test-Path $mergeFile -PathType Leaf) {
        $mergeExcerpt = [string]@(Get-Content $mergeFile)[$lineNumber - 1]
        if ($mergeExcerpt.Contains($symbol, [StringComparison]::Ordinal)) {
            $citations.Add([ordered]@{
                id = 'E001-C02'
                role = 'mergeBase'
                path = $evidencePath
                startLine = $lineNumber
                endLine = $lineNumber
                excerpt = $mergeExcerpt
                excerptSha256 = Get-TextSha256 $mergeExcerpt
            })
        }
    }
    return [ordered]@{
        schemaVersion = 1
        role = 'claim-neutral-evidence-scout'
        target = [ordered]@{
            pr = 70106
            head = '2edf3761692afe15475075eb2fc0088566162045'
            mergeBase = '10bc886bfa321718276619d1077cc14eaf48456f'
            baseTip = '4f556f73a610b386e5ec29355a300e493d30c4c5'
        }
        completedGuides = @($manifest.guides.path | Sort-Object)
        coverage = $coverage
        evidence = @(
            [ordered]@{
                id = 'E001'
                category = 'resource_declaration'
                symbol = $symbol
                citations = @($citations)
            }
        )
        unresolvedReferences = @()
    }
}

function New-DiscoveryEvents {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [Collections.Generic.List[object]]$Events,
        [switch]$AccessAnswerPath,
        [switch]$OmitRoutingRead
    )

    $Events.Add(@{
        type = 'tool_call'
        data = @{
            toolName = 'skill'
            toolCallId = 'skill'
            turnId = '0'
            arguments = @{ skill = 'review-pull-request' }
        }
        turn = 0
    })
    $Events.Add(@{
        type = 'tool_result'
        data = @{
            toolName = 'skill'
            toolCallId = 'skill'
            success = $true
            result = @{ content = 'loaded' }
        }
        turn = 0
    })
    $Events.Add(@{
        type = 'skill_activation'
        data = @{
            name = 'review-pull-request'
            path = 'C:\temp\review-pull-request\SKILL.md'
            content = Get-NativeSkillBody $discoverySkill
        }
        turn = 0
    })

    $manifest = Get-Manifest
    $readIndex = 0
    foreach ($relative in @('manifest.json', 'pull.json', 'diff.patch', 'files.json')) {
        $readIndex++
        Add-ReadEvents $Events "C:\temp\review\bundle\$relative" (
            Resolve-BundleFile $relative
        ) "read-$readIndex"
    }
    $pull = Get-Content (
        Resolve-BundleFile 'pull.json'
    ) -Raw | ConvertFrom-Json -Depth 100
    $readIndex++
    Add-ReadEvents $Events (
        "C:\temp\review\bundle\$($pull.body.path.Replace('/', '\'))"
    ) (
        Resolve-BundleFile ([string]$pull.body.path)
    ) "read-$readIndex"
    $routingRelative = "guidance/$($manifest.routing.path).source"
    if (-not $OmitRoutingRead) {
        $readIndex++
        Add-ReadEvents $Events (
            "C:\temp\review\bundle\$($routingRelative.Replace('/', '\'))"
        ) (Resolve-BundleFile $routingRelative) "read-$readIndex"
    }
    $assignedPaths = @(
        $manifest.guides |
            ForEach-Object { $_.assignment.files } |
            ForEach-Object { [string]$_.path } |
            Sort-Object -Unique
    )
    foreach ($relative in $assignedPaths) {
        $readIndex++
        Add-ReadEvents $Events (
            "C:\temp\review\bundle\$($relative.Replace('/', '\'))"
        ) (Resolve-BundleFile $relative) "read-$readIndex"
    }
    foreach ($role in @('baseTip', 'head', 'mergeBase')) {
        $root = Join-Path (Get-CaseRoot) $manifest.sources.$role.root
        foreach ($file in Get-ChildItem $root -Recurse -File) {
            $relative = [IO.Path]::GetRelativePath(
                $root,
                $file.FullName
            )
            $readIndex++
            Add-ReadEvents $Events (
                "C:\temp\review\bundle\$($manifest.sources.$role.root.Replace('/', '\'))\$relative"
            ) $file.FullName "read-$readIndex"
        }
    }
    if ($AccessAnswerPath) {
        $Events.Add(@{
            type = 'tool_call'
            data = @{
                toolName = 'view'
                toolCallId = 'answer-access'
                arguments = @{
                    path = 'C:\temp\.reviewer-eval\labels.json'
                }
            }
            turn = 0
        })
    }
    $Events.Add(@{
        type = 'token_usage'
        data = @{ model = 'gpt-5.6-sol' }
    })
}

function Write-DiscoveryResult {
    param(
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)]$Inventory,
        [switch]$AccessAnswerPath,
        [switch]$OmitRoutingRead
    )

    New-Item $Directory -ItemType Directory -Force | Out-Null
    $events = [Collections.Generic.List[object]]::new()
    New-DiscoveryEvents $events `
        -AccessAnswerPath:$AccessAnswerPath `
        -OmitRoutingRead:$OmitRoutingRead
    $output = $Inventory | ConvertTo-Json -Depth 100
    $environmentFiles = [Collections.Generic.List[object]]::new()
    $environmentFiles.Add(@{
        src = 'fixtures/reviewer-capability/common/guidance'
        dest = 'review/bundle/guidance'
    })
    $environmentFiles.Add(@{
        src = 'fixtures/reviewer-capability/common/required'
        dest = 'review/bundle/required'
    })
    $environmentFiles.Add(@{
        src = 'fixtures/reviewer-capability/cases/pr-70106/bundle'
        dest = 'review/bundle'
    })
    $environmentFiles.Add(@{
        src = 'fixtures/reviewer-capability/cases/pr-70106/projection-map.json'
        dest = 'review/bundle/projection-map.json'
    })
    $trajectory = @{
        id = 'discovery-trajectory'
        stimulus = @{
            name = 'reviewer-evidence-scout-pr-70106'
            prompt = $prompt
            environment = @{
                skills = @(
                    'treatments/evidence-scout/discovery/review-pull-request'
                )
                files = @($environmentFiles)
            }
        }
        output = $output
        events = @($events)
    }
    $result = @{
        type = 'trial-result'
        status = 'success'
        evalName = 'review-pull-request-evidence-scout-development'
        variant = 'main'
        model = 'gpt-5.6-sol'
        stimulus = 'reviewer-evidence-scout-pr-70106'
        runId = $runId
        itemId = (
            "$discoverySpec::main::gpt-5.6-sol::" +
            'reviewer-evidence-scout-pr-70106::trial-0'
        )
        evalFilePath = $discoverySpec
        trajectory = $trajectory
        gradeResult = $null
    }
    [IO.File]::WriteAllText(
        (Join-Path $Directory 'results.jsonl'),
        ($result | ConvertTo-Json -Depth 100 -Compress),
        [Text.UTF8Encoding]::new($false)
    )
}

function New-FinalizerEvents {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [Collections.Generic.List[object]]$Events,
        [Parameter(Mandatory = $true)][string]$InventoryPath,
        [Parameter(Mandatory = $true)]$Inventory,
        [switch]$ScoutBeforeCore,
        [switch]$OmitCitedSourceRead,
        [string]$OmitCitedRole
    )

    $Events.Add(@{
        type = 'tool_call'
        data = @{
            toolName = 'skill'
            toolCallId = 'skill'
            turnId = '0'
            arguments = @{ skill = 'review-pull-request' }
        }
        turn = 0
    })
    $Events.Add(@{
        type = 'tool_result'
        data = @{
            toolName = 'skill'
            toolCallId = 'skill'
            success = $true
            result = @{ content = 'loaded' }
        }
        turn = 0
    })
    $Events.Add(@{
        type = 'skill_activation'
        data = @{
            name = 'review-pull-request'
            path = 'C:\temp\review-pull-request\SKILL.md'
            content = Get-NativeSkillBody $finalizerSkill
        }
        turn = 0
    })

    if ($ScoutBeforeCore) {
        Add-ReadEvents $Events (
            'C:\temp\.reviewer-eval-input\evidence-inventory.json'
        ) $InventoryPath 'scout'
    }

    $readIndex = 0
    foreach ($relative in @('manifest.json', 'pull.json', 'diff.patch', 'files.json')) {
        $readIndex++
        Add-ReadEvents $Events "C:\temp\review\bundle\$relative" (
            Resolve-BundleFile $relative
        ) "core-$readIndex"
    }
    $pull = Get-Content (
        Resolve-BundleFile 'pull.json'
    ) -Raw | ConvertFrom-Json -Depth 100
    $readIndex++
    Add-ReadEvents $Events (
        "C:\temp\review\bundle\$($pull.body.path.Replace('/', '\'))"
    ) (
        Resolve-BundleFile ([string]$pull.body.path)
    ) "core-$readIndex"
    $manifest = Get-Manifest
    foreach ($guide in @($manifest.guides)) {
        $reference = @(
            $guide.assignment.files | Where-Object kind -eq 'guide'
        )[0]
        $readIndex++
        Add-ReadEvents $Events (
            "C:\temp\review\bundle\$($reference.path.Replace('/', '\'))"
        ) (
            Resolve-BundleFile ([string]$reference.path)
        ) "core-$readIndex"
    }
    Add-ReadEvents $Events (
        'C:\temp\review-pull-request\references\evidence-scout.md'
    ) $finalizerReference 'reference'
    if (-not $ScoutBeforeCore) {
        Add-ReadEvents $Events (
            'C:\temp\.reviewer-eval-input\evidence-inventory.json'
        ) $InventoryPath 'scout'
    }
    if (-not $OmitCitedSourceRead) {
        $verificationIndex = 0
        foreach ($citation in @(
            $Inventory.evidence.citations |
                Where-Object role -ne 'pull' |
                Sort-Object role, path -Unique
        )) {
            if ([string]$citation.role -eq $OmitCitedRole) {
                continue
            }
            $verificationIndex++
            $sourceFile = Join-Path (
                Join-Path (
                    Get-CaseRoot
                ) $manifest.sources.([string]$citation.role).root
            ) "$($citation.path).source"
            Add-ReadEvents $Events (
                "C:\temp\review\bundle\$($manifest.sources.([string]$citation.role).root.Replace('/', '\'))\$($citation.path.Replace('/', '\')).source"
            ) $sourceFile "verify-source-$verificationIndex"
        }
    }
    $Events.Add(@{
        type = 'token_usage'
        data = @{ model = 'gpt-5.6-sol' }
    })
}

function Write-FinalizerResults {
    param(
        [Parameter(Mandatory = $true)][string]$ResultsDirectory,
        [Parameter(Mandatory = $true)][string]$GradeDirectory,
        [Parameter(Mandatory = $true)]$Binding,
        [Parameter(Mandatory = $true)]$Inventory,
        [switch]$ScoutBeforeCore,
        [switch]$OmitCitedSourceRead,
        [string]$OmitCitedRole
    )

    New-Item $ResultsDirectory -ItemType Directory -Force | Out-Null
    New-Item $GradeDirectory -ItemType Directory -Force | Out-Null
    $events = [Collections.Generic.List[object]]::new()
    New-FinalizerEvents $events ([string]$Binding.inventoryPath) $Inventory `
        -ScoutBeforeCore:$ScoutBeforeCore `
        -OmitCitedSourceRead:$OmitCitedSourceRead `
        -OmitCitedRole $OmitCitedRole
    $output = @"
STATUS: NO_FINDINGS
PR: 70106
HEAD_SHA: 2edf3761692afe15475075eb2fc0088566162045
MERGE_BASE_SHA: 10bc886bfa321718276619d1077cc14eaf48456f
BASE_TIP_SHA: 4f556f73a610b386e5ec29355a300e493d30c4c5
GUIDANCE_SOURCE: frozen
GUIDES: complete
UNCOVERED: none
PATH: single-reviewer
NEW_FINDINGS: none
EXISTING_FEEDBACK_COVERAGE: none
UNRESOLVED: none
DISCARDED: none
TEST_BOUNDARY: source-only
LIMITATIONS: development fixture
"@
    $environmentFiles = [Collections.Generic.List[object]]::new()
    $environmentFiles.Add(@{
        src = (Resolve-Path (
            Join-Path $fixtureRoot 'common/guidance'
        )).Path
        dest = 'review/bundle/guidance'
    })
    $environmentFiles.Add(@{
        src = (Resolve-Path (
            Join-Path $fixtureRoot 'common/required'
        )).Path
        dest = 'review/bundle/required'
    })
    $environmentFiles.Add(@{
        src = (Resolve-Path (Get-CaseRoot)).Path
        dest = 'review/bundle'
    })
    $environmentFiles.Add(@{
        src = (Resolve-Path (
            Join-Path $fixtureRoot 'cases/pr-70106/projection-map.json'
        )).Path
        dest = 'review/bundle/projection-map.json'
    })
    $environmentFiles.Add(@{
        src = [string]$Binding.inventoryPath
        dest = '.reviewer-eval-input/evidence-inventory.json'
    })
    $trajectory = @{
        id = 'finalizer-trajectory'
        stimulus = @{
            name = 'reviewer-capability-pr-70106'
            prompt = $finalizerPrompt
            environment = @{
                skills = @($finalizerSkillRoot)
                files = @($environmentFiles)
            }
        }
        output = $output
        events = @($events)
    }
    $subject = @{
        type = 'trial-result'
        status = 'success'
        evalName = 'review-pull-request-capability-development'
        variant = 'main'
        model = 'gpt-5.6-sol'
        stimulus = 'reviewer-capability-pr-70106'
        runId = $finalizerRunId
        itemId = (
            "$($Binding.finalizerSpecPath)::main::gpt-5.6-sol::" +
            'reviewer-capability-pr-70106::trial-0'
        )
        evalFilePath = [string]$Binding.finalizerSpecPath
        trajectory = $trajectory
        gradeResult = $null
    }
    [IO.File]::WriteAllText(
        (Join-Path $ResultsDirectory 'results.jsonl'),
        ($subject | ConvertTo-Json -Depth 100 -Compress),
        [Text.UTF8Encoding]::new($false)
    )
    $gradeResult = @{
        score = 1
        passed = $true
        trajectoryId = 'finalizer-trajectory'
        stimulusName = 'reviewer-capability-pr-70106'
        details = @(
            @{
                name = 'semantic-review-pr-70106'
                score = 1
                passed = $true
                metadata = @{
                    model = 'gemini-4-argon'
                    scoring = 'binary'
                }
            }
        )
    }
    $grade = @{
        status = 'success'
        runId = $finalizerRunId
        trajectory = $trajectory
        gradeResult = $gradeResult
    }
    [IO.File]::WriteAllText(
        (Join-Path $GradeDirectory 'results.jsonl'),
        ($grade | ConvertTo-Json -Depth 100 -Compress),
        [Text.UTF8Encoding]::new($false)
    )
}

try {
    New-Item $testRoot -ItemType Directory -Force | Out-Null
    & $checker -Mode Layout

    $validInventory = New-ValidInventory
    $validRun = Join-Path $testRoot 'valid-discovery-run'
    $generated = Join-Path $testRoot 'generated'
    Write-DiscoveryResult $validRun $validInventory
    & $checker -Mode Discovery -Case $caseName `
        -ResultsDirectory $validRun -ExpectedRunId $runId `
        -OutputDirectory $generated
    $bindingPath = Join-Path $generated 'post-discovery-binding.json'
    $binding = Get-Content $bindingPath -Raw | ConvertFrom-Json -Depth 100

    $missingCoverage = New-ValidInventory
    $missingCoverage.coverage = @($missingCoverage.coverage | Select-Object -Skip 1)
    $missingCoverageRun = Join-Path $testRoot 'missing-coverage-run'
    Write-DiscoveryResult $missingCoverageRun $missingCoverage
    Assert-Throws {
        & $checker -Mode Discovery -Case $caseName `
            -ResultsDirectory $missingCoverageRun -ExpectedRunId $runId `
            -OutputDirectory (Join-Path $testRoot 'missing-coverage-output')
    } 'wrong projected-file coverage count' (
        'Missing projected-file coverage unexpectedly passed.'
    )

    $badCitation = New-ValidInventory
    $badCitation.evidence[0].citations[0].excerptSha256 = '0' * 64
    $badCitationRun = Join-Path $testRoot 'bad-citation-run'
    Write-DiscoveryResult $badCitationRun $badCitation
    Assert-Throws {
        & $checker -Mode Discovery -Case $caseName `
            -ResultsDirectory $badCitationRun -ExpectedRunId $runId `
            -OutputDirectory (Join-Path $testRoot 'bad-citation-output')
    } 'does not bind exact frozen bytes' (
        'A bad citation hash unexpectedly passed.'
    )

    $traversalCitation = New-ValidInventory
    $traversalCitation.evidence[0].citations[0].path = '../outside'
    $traversalRun = Join-Path $testRoot 'traversal-citation-run'
    Write-DiscoveryResult $traversalRun $traversalCitation
    Assert-Throws {
        & $checker -Mode Discovery -Case $caseName `
            -ResultsDirectory $traversalRun -ExpectedRunId $runId `
            -OutputDirectory (Join-Path $testRoot 'traversal-citation-output')
    } 'not a canonical projected path' (
        'A source citation path traversal unexpectedly passed.'
    )

    $extraProperty = New-ValidInventory
    $extraProperty.evidence[0]['verdict'] = 'none'
    $extraPropertyRun = Join-Path $testRoot 'extra-property-run'
    Write-DiscoveryResult $extraPropertyRun $extraProperty
    Assert-Throws {
        & $checker -Mode Discovery -Case $caseName `
            -ResultsDirectory $extraPropertyRun -ExpectedRunId $runId `
            -OutputDirectory (Join-Path $testRoot 'extra-property-output')
    } 'missing, reordered, or unexpected properties' (
        'A model-authored verdict field unexpectedly passed.'
    )

    $answerAccessRun = Join-Path $testRoot 'answer-access-run'
    Write-DiscoveryResult $answerAccessRun (New-ValidInventory) -AccessAnswerPath
    Assert-Throws {
        & $checker -Mode Discovery -Case $caseName `
            -ResultsDirectory $answerAccessRun -ExpectedRunId $runId `
            -OutputDirectory (Join-Path $testRoot 'answer-access-output')
    } 'grader-only answer material' (
        'Observed answer-key access unexpectedly passed.'
    )

    $missingRoutingRun = Join-Path $testRoot 'missing-routing-run'
    Write-DiscoveryResult $missingRoutingRun (
        New-ValidInventory
    ) -OmitRoutingRead
    Assert-Throws {
        & $checker -Mode Discovery -Case $caseName `
            -ResultsDirectory $missingRoutingRun -ExpectedRunId $runId `
            -OutputDirectory (Join-Path $testRoot 'missing-routing-output')
    } 'routing.md.source' (
        'A discovery execution that skipped routing unexpectedly passed.'
    )

    $validFinalizerRun = Join-Path $testRoot 'valid-finalizer-run'
    $validGradeRun = Join-Path $testRoot 'valid-grade-run'
    Write-FinalizerResults $validFinalizerRun $validGradeRun $binding (
        $validInventory
    )
    & $capabilityChecker `
        -SpecPath ([string]$binding.finalizerSpecPath) `
        -ExpectedEvalFilePath ([string]$binding.finalizerSpecPath) `
        -EvidenceScoutBindingPath $bindingPath `
        -ResultsDirectory $validFinalizerRun `
        -GradeResultsDirectory $validGradeRun `
        -ExpectedStimulus 'reviewer-capability-pr-70106' `
        -ExpectedRunId $finalizerRunId `
        -ResultMode Acceptance

    $earlyScoutRun = Join-Path $testRoot 'early-scout-run'
    $earlyScoutGrade = Join-Path $testRoot 'early-scout-grade'
    Write-FinalizerResults $earlyScoutRun $earlyScoutGrade $binding (
        $validInventory
    ) -ScoutBeforeCore
    Assert-Throws {
        & $capabilityChecker `
            -SpecPath ([string]$binding.finalizerSpecPath) `
            -ExpectedEvalFilePath ([string]$binding.finalizerSpecPath) `
            -EvidenceScoutBindingPath $bindingPath `
            -ResultsDirectory $earlyScoutRun `
            -GradeResultsDirectory $earlyScoutGrade `
            -ExpectedStimulus 'reviewer-capability-pr-70106' `
            -ExpectedRunId $finalizerRunId
    } 'frozen order' (
        'A scout read before core inputs unexpectedly passed.'
    )

    $missingVerificationRun = Join-Path $testRoot 'missing-verification-run'
    $missingVerificationGrade = Join-Path $testRoot 'missing-verification-grade'
    Write-FinalizerResults $missingVerificationRun $missingVerificationGrade (
        $binding
    ) $validInventory -OmitCitedSourceRead
    Assert-Throws {
        & $capabilityChecker `
            -SpecPath ([string]$binding.finalizerSpecPath) `
            -ExpectedEvalFilePath ([string]$binding.finalizerSpecPath) `
            -EvidenceScoutBindingPath $bindingPath `
            -ResultsDirectory $missingVerificationRun `
            -GradeResultsDirectory $missingVerificationGrade `
            -ExpectedStimulus 'reviewer-capability-pr-70106' `
            -ExpectedRunId $finalizerRunId
    } 'did not verify cited source' (
        'A finalizer that skipped source verification unexpectedly passed.'
    )

    if (@($validInventory.evidence[0].citations).Count -gt 1) {
        $missingRoleRun = Join-Path $testRoot 'missing-role-verification-run'
        $missingRoleGrade = Join-Path $testRoot 'missing-role-verification-grade'
        Write-FinalizerResults $missingRoleRun $missingRoleGrade $binding (
            $validInventory
        ) -OmitCitedRole 'mergeBase'
        Assert-Throws {
            & $capabilityChecker `
                -SpecPath ([string]$binding.finalizerSpecPath) `
                -ExpectedEvalFilePath ([string]$binding.finalizerSpecPath) `
                -EvidenceScoutBindingPath $bindingPath `
                -ResultsDirectory $missingRoleRun `
                -GradeResultsDirectory $missingRoleGrade `
                -ExpectedStimulus 'reviewer-capability-pr-70106' `
                -ExpectedRunId $finalizerRunId
        } 'mergeBase:' (
            'A finalizer that skipped one cited source role unexpectedly passed.'
        )
    }

    Write-Host 'Evidence-scout checker self-tests passed.'
} finally {
    if ($KeepArtifacts) {
        Write-Host "Retained evidence-scout self-test artifacts: $testRoot"
    } elseif (Test-Path $testRoot) {
        Remove-Item $testRoot -Recurse -Force
    }
}
