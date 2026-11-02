#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

& (Join-Path $PSScriptRoot 'check_reviewer_regression.ps1') -Quiet

$temp = Join-Path ([IO.Path]::GetTempPath()) "reviewer-regression-$([guid]::NewGuid().ToString('N'))"
New-Item $temp -ItemType Directory | Out-Null
try {
    $graderWorkspace = Join-Path $temp 'grader'
    New-Item (Join-Path $graderWorkspace '.reviewer-eval') -ItemType Directory -Force | Out-Null
    Copy-Item (
        Join-Path $PSScriptRoot 'reviewer-regression.labels.json'
    ) (Join-Path $graderWorkspace '.reviewer-eval/labels.json')
    $result70106 = [ordered]@{
        schemaVersion = 1
        pr = 70106
        results = @(
            [ordered]@{ id = 'V21'; tier = 'DISCARDED'; surfaced = $false; summary = 'not supported' }
            [ordered]@{ id = 'V32'; tier = 'DISCARDED'; surfaced = $false; summary = 'report only' }
            [ordered]@{ id = 'V36'; tier = 'DISCARDED'; surfaced = $false; summary = 'report only' }
            [ordered]@{ id = 'L69'; tier = 'DISCARDED'; surfaced = $false; summary = 'not supported' }
        )
        novelFindings = @()
    }
    $consistent70106 = @'
STATUS: NO_FINDINGS
PR: 70106
NEW_FINDINGS: none
EXISTING_FEEDBACK_COVERAGE: none
UNRESOLVED: none
DISCARDED:
- V21 source-disproved candidate
- V32 source-disproved candidate
- V36 source-disproved candidate
- L69 source-disproved candidate
TEST_BOUNDARY: source-only
LIMITATIONS: none
'@
    $graderInput = [ordered]@{
        trajectory = [ordered]@{ output = '' }
        stimulus = [ordered]@{ name = 'reviewer-regression-pr-70106' }
    }
    $graderInputPath = Join-Path $graderWorkspace 'input.json'
    $env:EVALUATE_WORKSPACE = $graderWorkspace
    $env:EVALUATE_GRADER_INPUT = $graderInputPath
    try {
        function Invoke-TestGrade {
            param(
                [int]$PullRequest,
                [string]$Artifact,
                [object]$Block
            )

            $graderInput.trajectory.output = "$Artifact`nREVIEWER_REGRESSION_RESULT`n``````json`n$($Block | ConvertTo-Json -Depth 20 -Compress)`n``````"
            Set-Content $graderInputPath ($graderInput | ConvertTo-Json -Depth 20) -Encoding utf8NoBOM
            return & (Join-Path $PSScriptRoot 'grade_reviewer_regression.ps1') -PullRequest $PullRequest |
                ConvertFrom-Json -Depth 100
        }

        $block = $result70106 | ConvertTo-Json -Depth 20 | ConvertFrom-Json -Depth 20
        $grade = Invoke-TestGrade 70106 $consistent70106 $block
        if (-not $grade.passed) {
            throw "Deterministic grader expected the consistent artifact and index to pass: $($grade | ConvertTo-Json -Depth 20 -Compress)"
        }

        $boldArtifact = @'
STATUS: NO_FINDINGS
PR: 70106
**NEW_FINDINGS:** None.
**EXISTING_FEEDBACK_COVERAGE:** None.
**UNRESOLVED**
None.
**DISCARDED:**
- **V21** source-disproved candidate
- **V32** source-disproved candidate
- **V36** source-disproved candidate
- **L69** source-disproved candidate
**TEST_BOUNDARY:** source-only
**LIMITATIONS:** none
'@
        $grade = Invoke-TestGrade 70106 $boldArtifact $block
        if (-not $grade.passed) {
            throw 'Markdown-bold headings with optional colons did not pass.'
        }

        $inlineDispositions70106 = @'
STATUS: FINDINGS
PR: 70106
NEW_FINDINGS:
1. Claim: [DISCARDED] V21 concurrent SendAsync calls can overwrite _lastToken.
   file:line: src/Components/WebAssembly/WebAssembly.Authentication/src/Services/AuthorizationMessageHandler.cs:69
   severity: P2
   confidence: discarded
   Why discarded: Same await RequestAccessToken() interleaving existed at merge-base and WebAssembly handler has no concurrency contract.
2. Claim: [DISCARDED] L69 concurrent SendAsync race on _lastToken.
   file:line: src/Components/WebAssembly/WebAssembly.Authentication/src/Services/AuthorizationMessageHandler.cs:69
   severity: P2
   confidence: discarded
   Why discarded: Pre-existing behavior on single-threaded WebAssembly.
EXISTING_FEEDBACK_COVERAGE: none
UNRESOLVED: none
DISCARDED:
- V21 source-disproved candidate
- V32 source-disproved candidate
- V36 source-disproved candidate
- L69 source-disproved candidate
TEST_BOUNDARY: source-only
LIMITATIONS: none
'@
        $grade = Invoke-TestGrade 70106 $inlineDispositions70106 $block
        if (-not $grade.passed) {
            throw "Inline [DISCARDED] entries in NEW_FINDINGS did not pass deterministic integrity: $($grade | ConvertTo-Json -Depth 20 -Compress)"
        }

        $narrativeMentionArtifact = @'
STATUS: FINDINGS
NEW_FINDINGS:
1. **P2 — V21: published candidate.**
2. **P2 — L69: published candidate.**
3. **[V32, V36] published candidate.**
UNRESOLVED: none
DISCARDED:
- The changed test does not exercise V21, L69, V32, or V36 concurrent orderings.
'@
        $publishedBlock = $result70106 | ConvertTo-Json -Depth 20 | ConvertFrom-Json -Depth 20
        foreach ($row in $publishedBlock.results) {
            $row.tier = 'VERIFIED'
            $row.surfaced = $true
        }
        $grade = Invoke-TestGrade 70106 $narrativeMentionArtifact $publishedBlock
        foreach ($id in 'V21', 'V32', 'V36', 'L69') {
            $integrity = $grade.details | Where-Object name -eq "integrity/$id"
            if (-not $integrity.passed -or $integrity.metadata.artifactDisposition -ne 'published') {
                throw "Narrative mention was incorrectly treated as a disposition for ${id}: $($integrity | ConvertTo-Json -Depth 10 -Compress)"
            }
        }

        $publishedArtifact = $consistent70106.Replace(
            'NEW_FINDINGS: none',
            'NEW_FINDINGS: P2 src/Components/WebAssembly/WebAssembly.Authentication/src/Services/AuthorizationMessageHandler.cs:65 - published candidate'
        )
        $publishedArtifact = [regex]::Replace(
            $publishedArtifact,
            '(?m)^- V32 source-disproved candidate\r?\n?',
            ''
        )
        $grade = Invoke-TestGrade 70106 $publishedArtifact $block
        $integrity = $grade.details | Where-Object name -eq 'integrity/V32'
        if ($grade.passed -or $integrity.passed) {
            throw 'Artifact-published/index-discarded mismatch did not fail integrity.'
        }

        $mismatchedBlock = $result70106 | ConvertTo-Json -Depth 20 | ConvertFrom-Json -Depth 20
        $v32 = $mismatchedBlock.results | Where-Object id -eq 'V32'
        $v32.tier = 'VERIFIED'
        $v32.surfaced = $true
        $grade = Invoke-TestGrade 70106 $consistent70106 $mismatchedBlock
        $integrity = $grade.details | Where-Object name -eq 'integrity/V32'
        if ($grade.passed -or $integrity.passed) {
            throw 'Artifact-discarded/index-published mismatch did not fail integrity.'
        }

        $missingRowBlock = $result70106 | ConvertTo-Json -Depth 20 | ConvertFrom-Json -Depth 20
        $missingRowBlock.results = @($missingRowBlock.results | Where-Object id -ne 'L69')
        $grade = Invoke-TestGrade 70106 $consistent70106 $missingRowBlock
        if ($grade.passed) {
            throw 'Deterministic grader self-test expected a missing required row to fail.'
        }

        $missingTierBlock = $result70106 | ConvertTo-Json -Depth 20 | ConvertFrom-Json -Depth 20
        $missingTierBlock.novelFindings = @(
            [pscustomobject]@{
                path = 'src/example.cs'
                line = 10
                summary = 'PLAUSIBLE was written only in prose.'
            }
        )
        $grade = Invoke-TestGrade 70106 $consistent70106 $missingTierBlock
        $novelIntegrity = $grade.details | Where-Object name -eq 'integrity/novel-findings'
        if ($grade.passed -or $novelIntegrity.passed -or
            $grade.evidence -notlike 'CONTRACT_FAIL:*') {
            throw 'A novel finding without a valid tier did not fail closed.'
        }

        $result69782 = [ordered]@{
            schemaVersion = 1
            pr = 69782
            results = @(
                [ordered]@{
                    id = 'V23'
                    tier = 'VERIFIED'
                    surfaced = $true
                    summary = 'retained'
                }
            )
            novelFindings = @(
                [ordered]@{
                    path = 'src/Components/Endpoints/src/FormMapping/FormDataReader.cs'
                    line = 143
                    tier = 'PLAUSIBLE'
                    summary = 'Dictionary consumer omitted from the projection.'
                }
                [ordered]@{
                    path = 'src/Components/Endpoints/src/FormMapping/Converters/FileConverter.cs'
                    line = 56
                    tier = 'PLAUSIBLE'
                    summary = 'Collection consumer omitted from the projection.'
                }
            )
        }
        $artifact69782 = @'
STATUS: FINDINGS
**NEW_FINDINGS**
1. **V23 — retained known defect**
**UNRESOLVED:**
- FormDataReader.cs:143 partial-projection seam.
- `src/Components/Endpoints/src/FormMapping/Converters/FileConverter.cs:56` partial-projection seam distinct from V23.
**DISCARDED:** None.
'@
        $grade = Invoke-TestGrade 69782 $artifact69782 $result69782
        if (-not $grade.passed -or $grade.metadata.adjudicationPending -or
            @($grade.metadata.adjudicatedNovelFindingIndexes).Count -ne 2) {
            throw 'Adjudicated #69782 partial-projection seams did not pass as allowed PLAUSIBLE observations.'
        }

        $unadjudicatedBlock = $result69782 | ConvertTo-Json -Depth 20 | ConvertFrom-Json -Depth 20
        $unadjudicatedBlock.novelFindings = @(
            [pscustomobject]@{
                path = 'src/Components/Endpoints/src/FormMapping/Other.cs'
                line = 10
                tier = 'PLAUSIBLE'
                summary = 'Unadjudicated seam.'
            }
        )
        $grade = Invoke-TestGrade 69782 $artifact69782 $unadjudicatedBlock
        if ($grade.passed -or -not $grade.metadata.adjudicationPending) {
            throw 'An unadjudicated PLAUSIBLE novel finding did not require adjudication.'
        }
    } finally {
        Remove-Item Env:EVALUATE_WORKSPACE, Env:EVALUATE_GRADER_INPUT
    }

    function New-TestTrialRecord {
        param(
            [string]$Variant,
            [string]$Stimulus,
            [bool]$DeterministicPassed,
            [bool]$V23Passed = $true,
            [double]$PromptScore = 0,
            [bool]$PromptPassed = $true,
            [string]$Status = 'success',
            [bool]$AdjudicationPending = $false
        )

        return [ordered]@{
            type = 'trial-result'
            status = $Status
            variant = $Variant
            stimulus = $Stimulus
            gradeResult = [ordered]@{
                details = @(
                    [ordered]@{
                        name = 'reviewer-regression'
                        graderType = 'program'
                        passed = $DeterministicPassed
                        evidence = if ($DeterministicPassed) { 'pass' } else { 'deterministic failure' }
                        details = @(
                            [ordered]@{
                                name = 'acceptance/V23'
                                passed = $V23Passed
                            }
                        )
                        metadata = [ordered]@{
                            integrity = [ordered]@{
                                failedIds = @()
                                missingIds = @()
                                duplicateIds = @()
                                unexpectedIds = @()
                            }
                            invalidNovelFindingIndexes = @()
                            adjudicationPending = $AdjudicationPending
                        }
                    }
                    [ordered]@{
                        name = 'semantic'
                        graderType = 'prompt'
                        status = 'success'
                        score = $PromptScore
                        passed = $PromptPassed
                        evidence = 'secondary'
                    }
                )
            }
        }
    }

    function New-ComparisonRecords {
        param(
            [int]$BaselineV23Passes = 3,
            [int]$CandidateV23Passes = 3
        )

        return @(
            foreach ($variant in 'current', 'candidate') {
                foreach ($stimulus in 'reviewer-regression-pr-70106', 'reviewer-regression-pr-69782') {
                    for ($run = 1; $run -le 5; $run++) {
                        $v23Passes = if ($variant -eq 'current') {
                            $BaselineV23Passes
                        } else {
                            $CandidateV23Passes
                        }
                        $v23Passed = $stimulus -ne 'reviewer-regression-pr-69782' -or
                            $run -le $v23Passes
                        New-TestTrialRecord -Variant $variant -Stimulus $stimulus `
                            -DeterministicPassed (
                                $stimulus -eq 'reviewer-regression-pr-70106' -or $v23Passed
                            ) -V23Passed $v23Passed
                    }
                }
            }
        )
    }

    $records = New-ComparisonRecords
    $equalPath = Join-Path $temp 'equal.jsonl'
    Set-Content $equalPath @($records | ForEach-Object {
        $_ | ConvertTo-Json -Depth 30 -Compress
    }) -Encoding utf8NoBOM
    $output = & (Join-Path $PSScriptRoot 'assert_reviewer_regression.ps1') `
        -Results $equalPath 3>&1 6>&1 | Out-String
    if ($output -notmatch 'comparison verdict: INDETERMINATE' -or
        $output -notmatch 'Semantic secondary evidence .*: 0/5') {
        throw 'Equal partial V23 retention or score-0 prompt reporting was not handled correctly.'
    }

    $failed = $false
    $regressionPath = Join-Path $temp 'regression.jsonl'
    Set-Content $regressionPath @(
        (New-ComparisonRecords -BaselineV23Passes 3 -CandidateV23Passes 2) |
            ForEach-Object { $_ | ConvertTo-Json -Depth 30 -Compress }
    ) -Encoding utf8NoBOM
    try {
        & (Join-Path $PSScriptRoot 'assert_reviewer_regression.ps1') `
            -Results $regressionPath *> $null
    } catch {
        $failed = $_.Exception.Message -like 'REGRESSION:*acceptance/V23*'
    }
    if (-not $failed) {
        throw 'V23 relative non-inferiority regression did not fail with a REGRESSION verdict.'
    }

    $failed = $false
    $broken = New-ComparisonRecords
    $broken[10].gradeResult.details[0].metadata.adjudicationPending = $true
    $absolutePath = Join-Path $temp 'absolute.jsonl'
    Set-Content $absolutePath @($broken | ForEach-Object {
        $_ | ConvertTo-Json -Depth 30 -Compress
    }) -Encoding utf8NoBOM
    try {
        & (Join-Path $PSScriptRoot 'assert_reviewer_regression.ps1') `
            -Results $absolutePath *> $null
    } catch {
        $failed = $_.Exception.Message -like 'ABSOLUTE_GATE_FAIL:*'
    }
    if (-not $failed) {
        throw 'A candidate #70106 absolute restraint failure did not fail its named gate.'
    }

    $failed = $false
    $missingPath = Join-Path $temp 'missing.jsonl'
    Set-Content $missingPath @($records | Select-Object -SkipLast 1 | ForEach-Object {
        $_ | ConvertTo-Json -Depth 30 -Compress
    }) -Encoding utf8NoBOM
    try {
        & (Join-Path $PSScriptRoot 'assert_reviewer_regression.ps1') `
            -Results $missingPath *> $null
    } catch {
        $failed = $_.Exception.Message -like 'ABSOLUTE_GATE_FAIL:*'
    }
    if (-not $failed) {
        throw 'Assertion self-test expected a missing trial to fail its named absolute gate.'
    }

    $failed = $false
    $errored = New-ComparisonRecords
    $errored[10].status = 'error'
    $errorPath = Join-Path $temp 'error.jsonl'
    Set-Content $errorPath @($errored | ForEach-Object {
        $_ | ConvertTo-Json -Depth 30 -Compress
    }) -Encoding utf8NoBOM
    try {
        & (Join-Path $PSScriptRoot 'assert_reviewer_regression.ps1') `
            -Results $errorPath *> $null
    } catch {
        $failed = $_.Exception.Message -like 'ABSOLUTE_GATE_FAIL:*'
    }
    if (-not $failed) {
        throw 'Assertion self-test expected an execution error to fail its named absolute gate.'
    }
} finally {
    Remove-Item $temp -Recurse -Force
}

Write-Host 'Reviewer regression self-tests passed.'
