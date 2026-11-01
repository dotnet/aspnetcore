#requires -Version 7.0

param([string]$CheckerPath)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$checker = if ($CheckerPath) {
    (Resolve-Path $CheckerPath).Path
} else {
    Join-Path $PSScriptRoot 'check_reviewer_capability.ps1'
}
$fixtureRoot = Join-Path $PSScriptRoot 'fixtures/reviewer-capability'
$controlsPath = Join-Path $PSScriptRoot 'reviewer-capability-controls.json'
$fakeRunId = '22222222-2222-2222-2222-222222222222'
$stimulus = 'reviewer-capability-pr-70106'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$specPath = (Resolve-Path (
    Join-Path $PSScriptRoot 'reviewer-capability-treatment.vally.yaml'
)).Path
$trustedPrompt = "This is an explicitly configured offline one-pass comparison. Use the review-pull-request skill's single-reviewer path: apply the exact routed guide texts and gates in this one context, do not spawn workers or subagents, and report PATH: single-reviewer. Review the frozen pull request represented by review/bundle. Treat it as the revision presented for review, use the included source, diff, changed-file metadata, pull description, existing feedback, guidance, policies, exclusions, and context, and report only actionable changed-line findings not already covered by feedback. If there are no findings, say NO_FINDINGS. Do not modify files or contact GitHub.`n"
$skillRelativePath = (
    'eng/skill-evals/review-pull-request/treatments/claim-validation/' +
        'review-pull-request/SKILL.md'
)
$frozenSkillBlob = 'a8c9856cd260ad22938a2e1bfe638294abe635f3'
$actualSkillBlob = & git -C $repositoryRoot hash-object `
    --path=.github/skills/review-pull-request/SKILL.md -- $skillRelativePath
if ($LASTEXITCODE -ne 0 -or ($actualSkillBlob -join '') -ne $frozenSkillBlob) {
    throw "The working-tree review skill does not match frozen Git blob $frozenSkillBlob."
}
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-reviewer-capability-test-' + [guid]::NewGuid().ToString('N')
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
            throw "Expected failure containing '$ExpectedMessage'; got '$($_.Exception.Message)'."
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

function Get-OverlayDigest {
    param([Parameter(Mandatory = $true)][string[]]$Roots)

    $files = [Collections.Generic.Dictionary[string, string]]::new(
        [StringComparer]::Ordinal
    )
    foreach ($root in $Roots) {
        foreach ($file in Get-ChildItem $root -Recurse -File) {
            $relative = [IO.Path]::GetRelativePath(
                $root,
                $file.FullName
            ).Replace('\', '/')
            $files[$relative] = Get-Sha256 $file.FullName
        }
    }
    $names = @($files.Keys)
    [Array]::Sort($names, [StringComparer]::Ordinal)
    return @{
        Files = $names.Count
        Sha256 = Get-TextSha256 (
            ($names | ForEach-Object { "$_`0$($files[$_])`n" }) -join ''
        )
    }
}

function Resolve-GuidePath {
    param(
        [Parameter(Mandatory = $true)][string]$CaseName,
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    $bundlePath = Join-Path $fixtureRoot "cases/$CaseName/bundle/$RelativePath"
    if (Test-Path $bundlePath -PathType Leaf) {
        return $bundlePath
    }
    return Join-Path (
        Join-Path $fixtureRoot 'common/guidance'
    ) $RelativePath.Substring('guidance/'.Length)
}

function New-FakeResult {
    param(
        [switch]$FailSemanticGrade,
        [switch]$NullSemanticScore,
        [switch]$ScoreZeroPassedTrue,
        [switch]$OutOfRangeSemanticScore,
        [switch]$OmitSemanticDetail,
        [switch]$AggregateDetailMismatch,
        [switch]$OmitSkillActivation,
        [switch]$WrongSkillBody,
        [switch]$IncompleteGuideResult,
        [switch]$UseProhibitedTool,
        [switch]$AccessAnswerPath,
        [ValidateSet('', 'view', 'rg-scalar', 'rg-array', 'glob-scalar', 'glob-array', 'lsp')]
        [string]$AnswerAccessShape = '',
        [switch]$WrongLaunchPrompt,
        [switch]$OmitSingleReviewerPath,
        [switch]$OmitValidationReference,
        [switch]$OmitValidationReceipt,
        [switch]$NoCandidates,
        [ValidateSet('pr-70106', 'pr-57972')]
        [string]$CaseName = 'pr-70106'
    )

    $caseName = $CaseName
    $caseStimulus = "reviewer-capability-$caseName"
    $manifest = Get-Content (
        Join-Path $fixtureRoot "cases/$caseName/bundle/manifest.json"
    ) -Raw | ConvertFrom-Json -Depth 100
    $currentSkillText = (Get-Content (
        Join-Path $repositoryRoot $skillRelativePath
    ) -Raw).Replace("`r`n", "`n").Replace("`r", "`n")
    $currentNativeSkillBody = [regex]::Replace(
        $currentSkillText,
        '\A---\n.*?\n---\n+',
        '',
        [Text.RegularExpressions.RegexOptions]::Singleline
    )
    $events = [Collections.Generic.List[object]]::new()
    $events.Add(@{
        type = 'tool_call'
        data = @{
            toolName = 'skill'
            toolCallId = 'skill-call'
            turnId = '0'
            arguments = @{ skill = 'review-pull-request' }
        }
        turn = 0
    })
    $events.Add(@{
        type = 'tool_result'
        data = @{
            toolName = 'skill'
            toolCallId = 'skill-call'
            success = $true
            result = @{ content = 'loaded' }
        }
        turn = 0
    })
    if (-not $OmitSkillActivation) {
        $body = if ($WrongSkillBody) {
            $currentNativeSkillBody.Substring(0, $currentNativeSkillBody.Length - 1)
        } else {
            $currentNativeSkillBody
        }
        $events.Add(@{
            type = 'skill_activation'
            data = @{
                name = 'review-pull-request'
                path = 'C:\temp\review-pull-request\SKILL.md'
                content = $body
            }
            turn = 0
        })
    }
    $guideIndex = 0
    foreach ($reference in @(
        $manifest.guides |
            ForEach-Object {
                $_.assignment.files |
                    Where-Object kind -eq 'guide'
            }
    )) {
        $guideIndex++
        $toolCallId = "guide-$guideIndex"
        $events.Add(@{
            type = 'tool_call'
            data = @{
                toolName = 'view'
                toolCallId = $toolCallId
                turnId = '0'
                arguments = @{
                    path = 'C:\temp\review\bundle\' + (
                        [string]$reference.path
                    ).Replace('/', '\')
                }
            }
            turn = 0
        })
        $content = Get-Content (
            Resolve-GuidePath $caseName ([string]$reference.path)
        ) -Raw
        if ($IncompleteGuideResult -and $guideIndex -eq 2) {
            $content = $content.Substring(0, $content.Length - 1)
        }
        $events.Add(@{
            type = 'tool_result'
            data = @{
                toolName = 'view'
                toolCallId = $toolCallId
                success = $true
                result = @{ content = $content }
            }
            turn = 0
        })
    }
    $events.Add(@{
        type = 'assistant_message'
        data = @{
            content = if ($NoCandidates) {
                'CANDIDATE_LEDGER: none'
            } else {
                "CANDIDATE_LEDGER:`n- C1 | regression | src/Example.cs:77"
            }
        }
        turn = 0
    })
    if (-not $OmitValidationReference -and -not $NoCandidates) {
        $validationPath = Join-Path $repositoryRoot (
            'eng/skill-evals/review-pull-request/treatments/claim-validation/' +
                'review-pull-request/references/claim-validation.md'
        )
        $events.Add(@{
            type = 'tool_call'
            data = @{
                toolName = 'view'
                toolCallId = 'claim-validation'
                turnId = '0'
                arguments = @{
                    path = 'C:\temp\review-pull-request\references\claim-validation.md'
                }
            }
            turn = 0
        })
        $events.Add(@{
            type = 'tool_result'
            data = @{
                toolName = 'view'
                toolCallId = 'claim-validation'
                success = $true
                result = @{ content = Get-Content $validationPath -Raw }
            }
            turn = 0
        })
    }
    if ($UseProhibitedTool) {
        $events.Add(@{
            type = 'tool_call'
            data = @{
                toolName = 'powershell'
                toolCallId = 'prohibited'
                arguments = @{ command = 'Write-Output prohibited' }
            }
            turn = 0
        })
    }
    if ($AccessAnswerPath -or $AnswerAccessShape) {
        $shape = if ($AnswerAccessShape) { $AnswerAccessShape } else { 'view' }
        $toolName = $shape.Split('-')[0]
        $arguments = switch ($shape) {
            'view' {
                @{ path = 'C:\TEMP\.REVIEWER-EVAL\labels.json' }
            }
            'rg-scalar' {
                @{ paths = 'C:\TEMP\.REVIEWER-EVAL\rubric.md' }
            }
            'rg-array' {
                @{ paths = @('C:\safe', 'C:\TEMP\.REVIEWER-EVAL\labels.json') }
            }
            'glob-scalar' {
                @{ paths = 'C:\TEMP\.REVIEWER-EVAL\rubric.md' }
            }
            'glob-array' {
                @{ paths = @('C:\safe', 'C:\TEMP\.REVIEWER-EVAL\labels.json') }
            }
            'lsp' {
                @{ file = 'C:\TEMP\.REVIEWER-EVAL\rubric.md' }
            }
        }
        $events.Add(@{
            type = 'tool_call'
            data = @{
                toolName = $toolName
                toolCallId = 'answer-access'
                arguments = $arguments
            }
            turn = 0
        })
    }
    $events.Add(@{
        type = 'token_usage'
        data = @{ model = 'gpt-5.6-sol' }
    })

    $semanticScore = if ($NullSemanticScore) {
        $null
    } elseif ($OutOfRangeSemanticScore) {
        2
    } elseif ($FailSemanticGrade -or $ScoreZeroPassedTrue -or
        $AggregateDetailMismatch) {
        0
    } else {
        1
    }
    $detailScore = if ($AggregateDetailMismatch) { 1 } else { $semanticScore }
    $semanticPassed = if ($ScoreZeroPassedTrue) {
        $true
    } else {
        $detailScore -eq 1
    }
    $aggregatePassed = if ($ScoreZeroPassedTrue) {
        $true
    } else {
        $semanticScore -eq 1
    }
    $outputLines = @(
        'STATUS: NO_FINDINGS',
        "PR: $($manifest.target.pr)",
        "HEAD_SHA: $($manifest.target.head)",
        "MERGE_BASE_SHA: $($manifest.target.mergeBase)",
        "BASE_TIP_SHA: $($manifest.target.baseTip)"
    )
    if (-not $OmitSingleReviewerPath) {
        $outputLines += 'PATH: single-reviewer'
    }
    if (-not $OmitValidationReceipt) {
        $outputLines += if ($NoCandidates) {
            'VALIDATION: claim-triggered (0 candidates)'
        } else {
            'VALIDATION: claim-triggered'
        }
    }
    $outputLines += 'NEW_FINDINGS: none'
    $outputLines += 'UNRESOLVED: none'
    $outputLines += 'DISCARDED: none'
    $output = $outputLines -join "`n"
    return [ordered]@{
        type = 'trial-result'
        runId = $fakeRunId
        evalFilePath = $specPath
        itemId = "$specPath::main::gpt-5.6-sol::$caseStimulus::trial-0"
        evalName = 'review-pull-request-capability-development'
        variant = 'main'
        stimulus = $caseStimulus
        model = 'gpt-5.6-sol'
        status = 'success'
        trajectory = [ordered]@{
            id = 'fake-subject-trajectory'
            stimulus = @{
                name = $caseStimulus
                prompt = if ($WrongLaunchPrompt) {
                    'wrong prompt'
                } else {
                    $trustedPrompt
                }
                environment = @{
                    skills = @('treatments/claim-validation/review-pull-request')
                    files = @(
                        if ($caseName -eq 'pr-70106') {
                            @{
                                src = 'fixtures/reviewer-capability/common/guidance'
                                dest = 'review/bundle/guidance'
                            }
                            @{
                                src = 'fixtures/reviewer-capability/common/required'
                                dest = 'review/bundle/required'
                            }
                        }
                        @{
                            src = "fixtures/reviewer-capability/cases/$caseName/bundle"
                            dest = 'review/bundle'
                        }
                        @{
                            src = "fixtures/reviewer-capability/cases/$caseName/projection-map.json"
                            dest = 'review/bundle/projection-map.json'
                        }
                    )
                }
            }
            output = $output
            events = @($events)
        }
        gradeResult = [ordered]@{
            stimulusName = $caseStimulus
            trajectoryId = 'fake-subject-trajectory'
            passed = $aggregatePassed
            score = $semanticScore
            details = if ($OmitSemanticDetail) {
                @(
                    @{
                        name = 'unrelated-grader'
                        passed = $true
                        score = 1
                        metadata = @{
                            model = 'gemini-4-argon'
                            scoring = 'binary'
                        }
                    }
                )
            } else {
                @(
                @{
                    name = "semantic-review-$caseName"
                    kind = 'llm'
                    passed = $semanticPassed
                    score = $detailScore
                    metadata = @{
                        model = 'gemini-4-argon'
                        scoring = 'binary'
                    }
                }
                )
            }
        }
    }
}

function Write-FakeResult {
    param(
        [Parameter(Mandatory = $true)]$Result,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $Result | ConvertTo-Json -Depth 30 -Compress | Set-Content $Path
}

function New-FakeControlResult {
    param(
        [Parameter(Mandatory = $true)][string]$ControlId,
        [Parameter(Mandatory = $true)][string]$Stimulus,
        [Parameter(Mandatory = $true)][string]$Grader,
        [Parameter(Mandatory = $true)][double]$Score,
        [bool]$Passed = ($Score -eq 1),
        [string]$Status = 'success',
        [string]$Judge = 'gemini-4-argon'
    )

    return [ordered]@{
        type = 'trial-result'
        status = $Status
        trajectory = @{
            id = "control-$ControlId"
            stimulus = @{ name = $Stimulus }
        }
        gradeResult = @{
            score = $Score
            passed = $Passed
            details = @(
                @{
                    name = $Grader
                    score = $Score
                    passed = $Passed
                    metadata = @{
                        model = $Judge
                        scoring = 'binary'
                    }
                }
            )
        }
    }
}

& $checker -SpecPath $specPath
Assert-Throws {
    & $checker -SpecPath $specPath -ResultMode Acceptance
} 'requires -ResultsDirectory' 'Acceptance without a result set was accepted.'

New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
try {
    $lfSpecPath = Join-Path $testRoot 'reviewer-capability-treatment.vally.yaml'
    $lfSpecText = (Get-Content $specPath -Raw).
        Replace("`r`n", "`n").
        Replace("`r", "`n")
    [IO.File]::WriteAllText(
        $lfSpecPath,
        $lfSpecText,
        [Text.UTF8Encoding]::new($false)
    )
    & $checker -SpecPath $lfSpecPath

    foreach ($identity in @(
        @{
            Path = Join-Path $PSScriptRoot (
                'treatments/claim-validation/review-pull-request/' +
                    'references/claim-validation.md'
            )
            Sha256 = 'a81c8f5ea560a3247b91f1362839e580bdf58601c9a046e37f4233f370307f21'
        },
        @{
            Path = Join-Path $PSScriptRoot 'reviewer-capability-rubric.md'
            Sha256 = '057d3a6ae9001f16eafa421a51274c19a4de4393cec8b236747d86e8006612f8'
        }
    )) {
        $lfText = (Get-Content $identity.Path -Raw).
            Replace("`r`n", "`n").
            Replace("`r", "`n")
        if ((Get-TextSha256 $lfText) -ne $identity.Sha256) {
            throw "The LF identity changed for '$($identity.Path)'."
        }
    }

    $fixtures = Join-Path $testRoot 'fixtures'
    Copy-Item $fixtureRoot $fixtures -Recurse

    $manifestPath = Join-Path $fixtures (
        'cases/pr-70106/bundle/manifest.json'
    )
    $originalManifest = [IO.File]::ReadAllBytes($manifestPath)
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json -Depth 100
    $manifest.reviewCapability.partition = 'holdout'
    $manifest | ConvertTo-Json -Depth 100 | Set-Content $manifestPath
    Assert-Throws {
        & $checker -SpecPath $specPath -FixtureRoot $fixtures
    } 'must remain a development case' 'A false holdout claim was accepted.'
    [IO.File]::WriteAllBytes($manifestPath, $originalManifest)

    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json -Depth 100
    $manifest.reviewCapability | Add-Member -NotePropertyName capability `
        -NotePropertyValue 'async-overlap-at-await'
    $manifest | ConvertTo-Json -Depth 100 | Set-Content $manifestPath
    Assert-Throws {
        & $checker -SpecPath $specPath -FixtureRoot $fixtures
    } 'answer-class review metadata' (
        'Generated answer-class review metadata was accepted.'
    )
    [IO.File]::WriteAllBytes($manifestPath, $originalManifest)

    $feedbackPath = Join-Path $fixtures (
        'cases/pr-70106/bundle/feedback.json'
    )
    $originalFeedback = [IO.File]::ReadAllBytes($feedbackPath)
    Add-Content $feedbackPath ' '
    Assert-Throws {
        & $checker -SpecPath $specPath -FixtureRoot $fixtures
    } 'stale artifact hash' 'Corrupt feedback bytes were accepted.'
    [IO.File]::WriteAllBytes($feedbackPath, $originalFeedback)

    $leakPath = Join-Path $fixtures 'reviewer-capability-labels.json'
    Set-Content $leakPath '{}'
    Assert-Throws {
        & $checker -SpecPath $specPath -FixtureRoot $fixtures
    } 'contains grader-only answer material' 'A staged answer-label leak was accepted.'
    Remove-Item $leakPath

    $overlapPath = Join-Path $fixtures (
        'cases/pr-70106/bundle/guidance/docs/CrossCuttingGuidance.md.source'
    )
    New-Item -ItemType Directory -Path (Split-Path $overlapPath -Parent) -Force |
        Out-Null
    Set-Content $overlapPath 'different staged bytes'
    Assert-Throws {
        & $checker -SpecPath $specPath -FixtureRoot $fixtures
    } 'overlays different bytes' 'A conflicting common/case overlay was accepted.'
    Remove-Item (
        Join-Path $fixtures 'cases/pr-70106/bundle/guidance'
    ) -Recurse -Force

    $metadataPullPath = Join-Path $fixtures (
        'cases/pr-70106/bundle/pull.json'
    )
    $metadataManifestPath = Join-Path $fixtures (
        'cases/pr-70106/bundle/manifest.json'
    )
    $originalMetadataPull = [IO.File]::ReadAllBytes($metadataPullPath)
    $originalMetadataManifest = [IO.File]::ReadAllBytes($metadataManifestPath)
    $pull = Get-Content $metadataPullPath -Raw | ConvertFrom-Json -Depth 100
    $pull.labels = @(@{ name = 'agent-reviewed-needs-changes' })
    $pull | ConvertTo-Json -Depth 100 | Set-Content $metadataPullPath
    $manifest = Get-Content $metadataManifestPath -Raw | ConvertFrom-Json -Depth 100
    $manifest.artifacts.'pull.json' = Get-Sha256 $metadataPullPath
    $required = Get-OverlayDigest @(
        (Join-Path $fixtures 'common/required'),
        (Join-Path $fixtures 'cases/pr-70106/bundle/required')
    )
    $manifest.requiredText.files = $required.Files
    $manifest.requiredText.sha256 = $required.Sha256
    $manifest | ConvertTo-Json -Depth 100 | Set-Content $metadataManifestPath
    Assert-Throws {
        & $checker -SpecPath $specPath -FixtureRoot $fixtures
    } 'exposes post-cutoff labels or decision metadata' (
        'Post-cutoff projected pull metadata was accepted.'
    )
    [IO.File]::WriteAllBytes($metadataPullPath, $originalMetadataPull)
    [IO.File]::WriteAllBytes($metadataManifestPath, $originalMetadataManifest)

    $temporaryControls = Join-Path $testRoot 'controls.json'
    $controls = @(Get-Content $controlsPath -Raw | ConvertFrom-Json -Depth 100)
    $controls[0].expectedScore = $null
    $controls | ConvertTo-Json -Depth 100 | Set-Content $temporaryControls
    Assert-Throws {
        & $checker -SpecPath $specPath -ControlsPath $temporaryControls
    } 'two positive and three negative' 'A null control score was accepted.'

    $resultsRoot = Join-Path $testRoot 'results/run'
    New-Item -ItemType Directory -Path $resultsRoot -Force | Out-Null
    $resultsPath = Join-Path $resultsRoot 'results.jsonl'

    Write-FakeResult (New-FakeResult) $resultsPath
    & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
        -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId -ResultMode Integrity
    & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
        -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId -ResultMode Acceptance

    $singleOverlayStimulus = 'reviewer-capability-pr-57972'
    Write-FakeResult (
        New-FakeResult -CaseName 'pr-57972'
    ) $resultsPath
    & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
        -ExpectedStimulus $singleOverlayStimulus -ExpectedRunId $fakeRunId `
        -ResultMode Integrity

    Write-FakeResult (
        New-FakeResult -NoCandidates -OmitValidationReference
    ) $resultsPath
    & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
        -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId -ResultMode Integrity

    $gradeResultsRoot = Join-Path $testRoot 'grades/run'
    New-Item -ItemType Directory -Path $gradeResultsRoot -Force | Out-Null
    $gradeResultsPath = Join-Path $gradeResultsRoot 'results.jsonl'
    $separateSubject = New-FakeResult -FailSemanticGrade
    $separateGrade = [ordered]@{
        runId = $separateSubject.runId
        status = 'success'
        trajectory = $separateSubject.trajectory
        gradeResult = $separateSubject.gradeResult
    }
    $separateSubject.gradeResult = $null
    $validSeparateGradeJson = $separateGrade | ConvertTo-Json -Depth 30 -Compress
    Write-FakeResult $separateSubject $resultsPath
    Write-FakeResult $separateGrade $gradeResultsPath
    & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
        -GradeResultsDirectory (Split-Path $gradeResultsRoot -Parent) `
        -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId -ResultMode Integrity
    Assert-Throws {
        & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
            -GradeResultsDirectory (Split-Path $gradeResultsRoot -Parent) `
            -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId `
            -ResultMode Acceptance
    } 'did not pass the automated semantic gate' (
        'A separately retained red grade passed automated acceptance.'
    )
    $separateGrade = $validSeparateGradeJson | ConvertFrom-Json -Depth 30
    $separateGrade.trajectory.output += "`nchanged"
    Write-FakeResult $separateGrade $gradeResultsPath
    Assert-Throws {
        & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
            -GradeResultsDirectory (Split-Path $gradeResultsRoot -Parent) `
            -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId
    } 'not bound to the exact subject trajectory' (
        'A saved grade for a different output was accepted.'
    )
    $separateGrade = $validSeparateGradeJson | ConvertFrom-Json -Depth 30
    $separateGrade.trajectory.output = $separateGrade.trajectory.output.ToUpperInvariant()
    Write-FakeResult $separateGrade $gradeResultsPath
    Assert-Throws {
        & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
            -GradeResultsDirectory (Split-Path $gradeResultsRoot -Parent) `
            -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId
    } 'not bound to the exact subject trajectory' (
        'A case-only output mismatch was accepted.'
    )
    $separateGrade = $validSeparateGradeJson | ConvertFrom-Json -Depth 30
    $separateGrade.gradeResult.trajectoryId = 'different-trajectory'
    Write-FakeResult $separateGrade $gradeResultsPath
    Assert-Throws {
        & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
            -GradeResultsDirectory (Split-Path $gradeResultsRoot -Parent) `
            -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId
    } 'not bound to the exact subject trajectory' (
        'A saved grade with the wrong grade-result trajectory ID was accepted.'
    )
    $separateGrade = $validSeparateGradeJson | ConvertFrom-Json -Depth 30
    $separateGrade.gradeResult.stimulusName = 'different-stimulus'
    Write-FakeResult $separateGrade $gradeResultsPath
    Assert-Throws {
        & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
            -GradeResultsDirectory (Split-Path $gradeResultsRoot -Parent) `
            -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId
    } 'not bound to the exact subject trajectory' (
        'A saved grade with the wrong grade-result stimulus was accepted.'
    )
    $separateGrade = $validSeparateGradeJson | ConvertFrom-Json -Depth 30
    Write-FakeResult $separateGrade $gradeResultsPath
    $errorOutcome = [ordered]@{
        runId = $fakeRunId
        status = 'error'
        trajectory = $separateGrade.trajectory
        gradeResult = $null
    }
    Add-Content $gradeResultsPath (
        $errorOutcome | ConvertTo-Json -Depth 30 -Compress
    )
    Assert-Throws {
        & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
            -GradeResultsDirectory (Split-Path $gradeResultsRoot -Parent) `
            -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId
    } 'exactly one saved associated grade result' (
        'A success plus error outcome set was accepted.'
    )

    Write-FakeResult (New-FakeResult -FailSemanticGrade) $resultsPath
    & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
        -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId -ResultMode Integrity
    Assert-Throws {
        & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
            -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId -ResultMode Acceptance
    } 'did not pass the automated semantic gate' (
        'A valid negative semantic grade passed automated acceptance.'
    )

    foreach ($case in @(
        @{
            Arguments = @{ NullSemanticScore = $true }
            Message = 'no numeric associated semantic score'
            Failure = 'A null semantic score was accepted.'
        }
        @{
            Arguments = @{ ScoreZeroPassedTrue = $true }
            Message = 'score and pass flags are inconsistent'
            Failure = 'A zero score with passed true was accepted.'
        }
        @{
            Arguments = @{ OutOfRangeSemanticScore = $true }
            Message = 'non-binary aggregate semantic score'
            Failure = 'An out-of-range semantic score was accepted.'
        }
        @{
            Arguments = @{ OmitSemanticDetail = $true }
            Message = 'does not retain the named semantic grader'
            Failure = 'A missing required semantic detail was accepted.'
        }
        @{
            Arguments = @{ AggregateDetailMismatch = $true }
            Message = 'score and pass flags are inconsistent'
            Failure = 'Contradictory aggregate and detail scores were accepted.'
        }
        @{
            Arguments = @{ OmitSkillActivation = $true }
            Message = 'no complete native skill activation evidence'
            Failure = 'Missing native activation evidence was accepted.'
        }
        @{
            Arguments = @{ WrongSkillBody = $true }
            Message = 'does not match frozen skill blob'
            Failure = 'A truncated native skill body was accepted.'
        }
        @{
            Arguments = @{ IncompleteGuideResult = $true }
            Message = 'no complete successful read'
            Failure = 'An incomplete assigned-guide read was accepted.'
        }
        @{
            Arguments = @{ UseProhibitedTool = $true }
            Message = 'used prohibited tools'
            Failure = 'A prohibited tool call was accepted.'
        }
        @{
            Arguments = @{ AccessAnswerPath = $true }
            Message = 'observed access to grader-only answer material'
            Failure = 'Observed answer-label access was accepted.'
        }
        @{
            Arguments = @{ WrongLaunchPrompt = $true }
            Message = 'frozen trusted caller prompt'
            Failure = 'A result with the wrong launch prompt was accepted.'
        }
        @{
            Arguments = @{ OmitSingleReviewerPath = $true }
            Message = 'wrong PATH receipt'
            Failure = 'A result without the configured single-reviewer receipt was accepted.'
        }
        @{
            Arguments = @{ OmitValidationReference = $true }
            Message = 'no single complete successful read of the claim-validation reference'
            Failure = 'A treatment result without the validation reference read was accepted.'
        }
        @{
            Arguments = @{ OmitValidationReceipt = $true }
            Message = 'omits the claim-triggered validation receipt'
            Failure = 'A treatment result without the validation receipt was accepted.'
        }
    )) {
        $arguments = $case.Arguments
        Write-FakeResult (New-FakeResult @arguments) $resultsPath
        Assert-Throws {
            & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
                -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId
        } $case.Message $case.Failure
    }

    foreach ($shape in @(
        'view',
        'rg-scalar',
        'rg-array',
        'glob-scalar',
        'glob-array',
        'lsp'
    )) {
        Write-FakeResult (
            New-FakeResult -AnswerAccessShape $shape
        ) $resultsPath
        Assert-Throws {
            & $checker -SpecPath $specPath -ResultsDirectory (Split-Path $resultsRoot -Parent) `
                -ExpectedStimulus $stimulus -ExpectedRunId $fakeRunId
        } 'observed access to grader-only answer material' (
            "Observed answer access through $shape was accepted."
        )
    }

    $controlResultsRoot = Join-Path $testRoot 'control-results/run'
    New-Item -ItemType Directory -Path $controlResultsRoot -Force | Out-Null
    $controlResultsPath = Join-Path $controlResultsRoot 'results.jsonl'
    $correctControl = 'pr-70106-revision-qualified-restraint'
    $badControl = 'pr-70106-unsupported-p2-lock-demand'
    Write-FakeResult (
        New-FakeControlResult -ControlId $correctControl `
            -Stimulus $stimulus -Grader 'semantic-review-pr-70106' -Score 1
    ) $controlResultsPath
    & $checker -SpecPath $specPath -ControlResultsDirectory (Split-Path $controlResultsRoot -Parent) `
        -ExpectedControlId $correctControl

    Write-FakeResult (
        New-FakeControlResult -ControlId $badControl `
            -Stimulus $stimulus -Grader 'semantic-review-pr-70106' -Score 0
    ) $controlResultsPath
    & $checker -SpecPath $specPath -ControlResultsDirectory (Split-Path $controlResultsRoot -Parent) `
        -ExpectedControlId $badControl

    foreach ($controlCase in @(
        @{
            Result = New-FakeControlResult -ControlId $badControl `
                -Stimulus $stimulus -Grader 'semantic-review-pr-70106' `
                -Score 0 -Passed $true
            ExpectedId = $badControl
            Message = 'inconsistent binary grade evidence'
            Failure = 'A contradictory control pass flag was accepted.'
        },
        @{
            Result = New-FakeControlResult -ControlId $correctControl `
                -Stimulus $stimulus -Grader 'wrong-grader' -Score 1
            ExpectedId = $correctControl
            Message = 'required named semantic grader'
            Failure = 'A control with the wrong grader identity was accepted.'
        },
        @{
            Result = New-FakeControlResult -ControlId $correctControl `
                -Stimulus $stimulus -Grader 'semantic-review-pr-70106' `
                -Score 2 -Passed $false
            ExpectedId = $correctControl
            Message = 'inconsistent binary grade evidence'
            Failure = 'An out-of-range control score was accepted.'
        },
        @{
            Result = New-FakeControlResult -ControlId $correctControl `
                -Stimulus $stimulus -Grader 'semantic-review-pr-70106' `
                -Score 1 -Status 'error'
            ExpectedId = $correctControl
            Message = 'error or mismatched case result'
            Failure = 'An errored control result was accepted.'
        },
        @{
            Result = New-FakeControlResult -ControlId 'wrong-control' `
                -Stimulus $stimulus -Grader 'semantic-review-pr-70106' `
                -Score 1
            ExpectedId = $correctControl
            Message = 'missing or duplicate grade result'
            Failure = 'A result bound to the wrong control ID was accepted.'
        }
    )) {
        Write-FakeResult $controlCase.Result $controlResultsPath
        Assert-Throws {
            & $checker -SpecPath $specPath -ControlResultsDirectory (
                Split-Path $controlResultsRoot -Parent
            ) -ExpectedControlId $controlCase.ExpectedId
        } $controlCase.Message $controlCase.Failure
    }

    Write-Host 'Reviewer-capability checker self-test passed.'
} finally {
    Remove-Item $testRoot -Recurse -Force
}
