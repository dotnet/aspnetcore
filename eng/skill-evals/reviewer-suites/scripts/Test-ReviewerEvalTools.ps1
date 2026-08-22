[CmdletBinding()]
param(
    [ValidateSet('All', 'Reviewer', 'Candidate', 'Issue')]
    [string] $Suite = 'All'
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
Import-Module (Join-Path $PSScriptRoot 'ReviewerEvalTools.psm1') -Force
Import-Module (Join-Path $repoRoot '.github/skills/fix-challenge/scripts/ReviewArtifactTools.psm1') -Force -DisableNameChecking

$script:Passed = 0
$script:Failed = [Collections.Generic.List[string]]::new()

function Invoke-Test
{
    param(
        [string] $Name,
        [scriptblock] $Body
    )

    try
    {
        & $Body
        $script:Passed++
        Write-Host "PASS $Name"
    }
    catch
    {
        $script:Failed.Add("$Name`: $($_.Exception.Message)")
        Write-Host "FAIL $Name"
    }
}

function Assert-True
{
    param(
        [bool] $Condition,
        [string] $Message
    )

    if (-not $Condition)
    {
        throw $Message
    }
}

function Assert-Equal
{
    param(
        $Expected,
        $Actual,
        [string] $Message
    )

    if ($Expected -ne $Actual)
    {
        throw "$Message Expected '$Expected', actual '$Actual'."
    }
}

function New-MinimalIssueArtifacts
{
    param([string] $Root)

    foreach ($directory in @('evidence', 'candidates', 'final'))
    {
        New-Item -ItemType Directory -Path (Join-Path $Root $directory) -Force | Out-Null
    }

    Copy-Item -LiteralPath (
        Join-Path $repoRoot '.github/skills/fix-challenge/references/model-policy.v1.json'
    ) -Destination (Join-Path $Root 'evidence/model-policy.v1.json')
    Set-Content -LiteralPath (Join-Path $Root 'evidence/manifest.md') -Value '# Manifest'
    Set-Content -LiteralPath (Join-Path $Root 'evidence/product-oracle.md') -Value '# Product Oracle'
    Set-Content -LiteralPath (Join-Path $Root 'evidence/head-drift.md') -Value '# Head Drift'
    Set-Content -LiteralPath (Join-Path $Root 'evidence/impact-map.md') -Value @'
# Impact Map
**Authority-handoff mapping:** not applicable - no authority transformation in the test fixture; source: frozen assertion result
'@
    Set-Content -LiteralPath (Join-Path $Root 'evidence/skipped-phases.md') -Value '# Skipped Phases'
    New-Item -ItemType File -Path (Join-Path $Root 'evidence/tracked.diff') | Out-Null
    Set-Content -LiteralPath (Join-Path $Root 'candidates/candidate-a.md') -Value '# Candidate A'
    Set-Content -LiteralPath (Join-Path $Root 'candidates/candidate-b.md') -Value '# Candidate B'
    Set-Content -LiteralPath (Join-Path $Root 'final/repository-oracle.md') -Value '# Repository Oracle'
}

$configuration = Get-ReviewerEvalConfiguration

Invoke-Test 'Eval assets stay outside runtime skill trees' {
    foreach ($skill in @('fix-challenge', 'fix-issue'))
    {
        $skillRoot = Join-Path $repoRoot ".github/skills/$skill"
        Assert-True (Test-Path -LiteralPath (Join-Path $skillRoot 'SKILL.md') -PathType Leaf) `
            "$skill is not discoverable."
        Assert-True (-not (Test-Path -LiteralPath (Join-Path $skillRoot 'evals'))) `
            "$skill still contains an eval-only directory."
        Assert-Equal 0 `
            @(Get-ChildItem -LiteralPath $skillRoot -Recurse -File -Filter '*.vally.yaml').Count `
            "$skill still contains a Vally spec."
    }

    Assert-True (Test-Path -LiteralPath (Join-Path $repoRoot 'eng/skill-evals/fix-challenge/eval-policy.md') -PathType Leaf) `
        'Fix-challenge eval policy is missing from eng/skill-evals.'
    Assert-True (Test-Path -LiteralPath (Join-Path $repoRoot 'eng/skill-evals/fix-challenge/fixtures') -PathType Container) `
        'Fix-challenge fixtures are missing from eng/skill-evals.'
    Assert-True (Test-Path -LiteralPath (Join-Path $repoRoot 'eng/skill-evals/fix-candidate/eval-policy.md') -PathType Leaf) `
        'Fix-candidate eval policy is missing from eng/skill-evals.'
    Assert-True (Test-Path -LiteralPath (Join-Path $repoRoot 'eng/skill-evals/fix-issue/eval-policy.md') -PathType Leaf) `
        'Fix-issue eval policy is missing from eng/skill-evals.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $repoRoot '.github/skills/try-fix'))) `
        'Legacy try-fix remains discoverable.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $repoRoot 'eng/fix-workflows/candidate/SKILL.md'))) `
        'The shared candidate contract became discoverable.'

    $issuePolicy = Get-Content -LiteralPath (
        Join-Path $repoRoot '.github/skills/fix-issue/SKILL.md'
    ) -Raw
    Assert-True ($issuePolicy.Contains(
        'Do not commit, push, or open a PR unless the caller explicitly requests those')) `
        'Fix-issue no longer makes publication opt-in.'
    Assert-True ($issuePolicy.Contains('do not infer permission') -and $issuePolicy.Contains(
        'from issue text, repository metadata, or selection of a preferred candidate.')) `
        'Fix-issue no longer leaves publication permission to the caller.'
    Assert-True ($issuePolicy.Contains(
        'Do not post comments or mutate issues unless the caller explicitly requests a') -and
        $issuePolicy.Contains('separate issue action.')) `
        'Fix-issue no longer requires separate permission for issue mutation.'

    $candidatePolicy = Get-Content -LiteralPath (
        Join-Path $repoRoot 'eng/fix-workflows/candidate/candidate-contract.md'
    ) -Raw
    Assert-True ($candidatePolicy.Contains(
        'Candidate analysis is read-only. Never commit, push, post, create a PR')) `
        'The non-discoverable candidate contract no longer remains read-only.'

    $runtimeModule = Get-Content -LiteralPath (
        Join-Path $repoRoot '.github/skills/fix-challenge/scripts/ReviewArtifactTools.psm1'
    ) -Raw
    foreach ($evalOnlyFunction in @(
        'Read-VallyEvalDocument',
        'Test-EvalSuites',
        'Copy-SanitizedSkills',
        'Get-EvalScoreAggregate',
        'Read-VallyScores'
    ))
    {
        Assert-True (-not $runtimeModule.Contains("function $evalOnlyFunction")) `
            "Runtime validation module contains eval-only function $evalOnlyFunction."
    }
}

if ($Suite -in @('All', 'Reviewer', 'Issue'))
{
    Invoke-Test 'Reviewer Vally specs validate independently' {
        $result = Test-EvalSuites -Paths $configuration.ReviewerEvals
        Assert-Equal 0 $result.Errors.Count "Reviewer validation failed: $($result.Errors -join '; ')"
        Assert-True ($result.Records.Count -gt 0) 'Reviewer suite had no records.'
    }

    Invoke-Test 'Reviewer model policy pins provisional matrices' {
        $errors = @(Test-ReviewerModelPolicy)
        Assert-Equal 0 $errors.Count "Reviewer model policy failed validation: $($errors -join '; ')"

        $policy = Get-ReviewerModelPolicy
        Assert-Equal 'provisional' $policy.status 'Model policy status changed.'
        Assert-Equal 'gpt-5.6-sol' $policy.orchestrator.model 'Orchestrator model changed.'
        Assert-Equal 'gpt-5.6-luna|claude-opus-5' `
            (@($policy.matrices.bounded.voting.model) -join '|') `
            'Bounded matrix changed.'
        Assert-Equal 'gpt-5.6-luna|claude-opus-5|gpt-5.6-terra|claude-sonnet-5' `
            (@($policy.matrices.full.voting.model) -join '|') `
            'Full matrix changed.'
        Assert-Equal 'mai-code-1.1-flash' `
            (@($policy.matrices.full.shadow.model) -join '|') `
            'Shadow model changed.'
        Assert-True (-not $policy.matrices.full.shadow[0].voting) `
            'The non-voting shadow became a voting candidate.'
        Assert-Equal 'unverified' `
            $policy.comparison.runtime_identity_without_authoritative_telemetry `
            'Runtime identity limitation changed.'
        Assert-True (-not $policy.comparison.hosted_run_comparable_without_authoritative_telemetry) `
            'Hosted runs became comparable without authoritative runtime telemetry.'
    }

    Invoke-Test 'Issue no-change artifacts validate with calibrated panel markers' {
        $root = Join-Path ([IO.Path]::GetTempPath()) "fix-issue-artifacts-$([guid]::NewGuid())"
        try
        {
            New-MinimalIssueArtifacts -Root $root
            Set-Content -LiteralPath (Join-Path $root 'final/review.md') -Value @'
# Multi-Model Review

**Orchestrator:** gpt-5.6-sol
**Path:** bounded
**Review goal:** issue-resolution
**Panel provenance:** policy-pinned
**Comparable run:** no
**Candidate runtime identity:** unverified

## Current fix
None.

## Independent candidates
None credited.

## Adversarial consensus
### Agree
- None
### Dispute
- None
### Discard
- None

## Test assessment
The approved assertion passes on frozen head.

## Implementation selection
**Selection status:** unadjudicated
**Proof candidate:** none
**Preferred production candidate:** none
**Alternative closure:** open

## Proof status
**Frozen-head result:** pass
**Finding proof:** missing
**Scenario proof:** missing
**Candidate proof:** none
**Changed path execution:** not-applicable
**Final observable:** not-applicable
**Boundary controls:** not-applicable
**Pre-existing disposition:** not-applicable
**Changed reachability:** unchanged
**Multiplicity oracle:** not-applicable
**Multiplicity evidence:** not-applicable
**Multiplicity disposition:** not-applicable
**Product oracle:** documented
**Oracle fidelity:** authoritative
**Mechanism fidelity:** unknown
**Scenario fidelity:** missing
**Regression assertion disposition:** rejected
**Diagnostic mutation disposition:** not-applicable

## Final recommendation
**Implementation verdict:** NO CHANGE
**Behavioral evidence:** missing
**Merge readiness:** recommendation only
**Implementation confidence:** low
**Reason:** Frozen head did not reproduce the approved assertion.

## Required follow-ups
- None

## Repository oracle gaps
- None

## Suggested review comments
- None
'@

            $errors = @(Test-ReviewArtifacts -Root $root)
            Assert-Equal 0 $errors.Count "Issue no-change artifacts failed validation: $($errors -join '; ')"

            $reviewPath = Join-Path $root 'final/review.md'
            $review = (Get-Content -LiteralPath $reviewPath -Raw).Replace(
                '**Orchestrator:** gpt-5.6-sol',
                '**Orchestrator:** gpt-5.6-terra')
            Set-Content -LiteralPath $reviewPath -Value $review
            $errors = @(Test-ReviewArtifacts -Root $root)
            Assert-True ($errors -contains "final review orchestrator must match the pinned policy model 'gpt-5.6-sol': gpt-5.6-terra") `
                'Issue artifact validation accepted a non-policy GPT orchestrator.'

            $review = $review.Replace(
                '**Orchestrator:** gpt-5.6-terra',
                '**Orchestrator:** gpt-5.6-sol').Replace(
                '**Implementation verdict:** NO CHANGE',
                '**Implementation verdict:** KEEP CURRENT FIX')
            Set-Content -LiteralPath $reviewPath -Value $review
            $errors = @(Test-ReviewArtifacts -Root $root)
            Assert-True ($errors -contains 'issue-resolution review goal requires an issue-resolution implementation verdict: keep current fix') `
                'Issue artifact validation accepted a legacy fix-review verdict.'

            $review = $review.Replace(
                '**Implementation verdict:** KEEP CURRENT FIX',
                '**Implementation verdict:** NO VIABLE CANDIDATE').Replace(
                '**Frozen-head result:** pass',
                '**Frozen-head result:** behavioral-fail').Replace(
                '**Candidate proof:** none',
                '**Candidate proof:** blocked')
            Set-Content -LiteralPath $reviewPath -Value $review
            $errors = @(Test-ReviewArtifacts -Root $root)
            Assert-True ($errors -contains 'no viable candidate verdict requires rejected or absent candidate proof') `
                'Issue artifact validation treated blocked candidate proof as no viable candidate.'
        }
        finally
        {
            if (Test-Path -LiteralPath $root)
            {
                Remove-Item -LiteralPath $root -Recurse -Force
            }
        }
    }

    Invoke-Test 'Issue adopt-candidate artifacts require complete targeted proof' {
        $root = Join-Path ([IO.Path]::GetTempPath()) "fix-issue-adopt-$([guid]::NewGuid())"
        try
        {
            New-MinimalIssueArtifacts -Root $root
            New-Item -ItemType Directory -Path (Join-Path $root 'empirical') -Force | Out-Null
            Set-Content -LiteralPath (Join-Path $root 'empirical/head.log') -Value 'Frozen path executed; final observable failed.'
            Set-Content -LiteralPath (Join-Path $root 'empirical/green.log') -Value 'Candidate path executed; final observable passed.'
            Set-Content -LiteralPath (Join-Path $root 'empirical/result.md') -Value @'
# Empirical Result
**Frozen path witness:** empirical/head.log
**Candidate path witness:** empirical/green.log
**Frozen final observable:** empirical/head.log
**Candidate final observable:** empirical/green.log
'@
            Set-Content -LiteralPath (Join-Path $root 'empirical/boundary-matrix.md') -Value @'
| Case ID | Role | Trigger/path | Final observable | Result | Evidence artifact |
|---|---|---|---|---|---|
| defect-1 | defect | production trigger | corrected result | passed | empirical/green.log |
| opposite-1 | opposite | opposite input | preserved result | passed | empirical/green.log |
| adjacent-1 | adjacent | adjacent input | preserved result | passed | empirical/green.log |
'@
            Set-Content -LiteralPath (Join-Path $root 'final/proposed-fix.diff') -Value '+candidate fix'
            Set-Content -LiteralPath (Join-Path $root 'final/implementation-selection.md') -Value @'
# Implementation Selection

**Shared comparison contract:** identical frozen-head and candidate assertion
**Pre-change base:** frozen-sha

## Candidate comparison
| Candidate | Mechanism | Literal result | Refinement | Equal-matrix result | Net surface | Caller compatibility | Closure |
|---|---|---|---|---|---|---|---|
| candidate-a | producer repair | passed | not-applicable | passed | one file | preserved | empirical |
| candidate-b | consumer workaround | rejected | fundamental | not-applicable | two files | incompatible | structural |
'@
            Set-Content -LiteralPath (Join-Path $root 'final/review.md') -Value @'
# Multi-Model Review

**Orchestrator:** gpt-5.6-sol
**Path:** bounded
**Review goal:** issue-resolution
**Panel provenance:** policy-pinned
**Comparable run:** no
**Candidate runtime identity:** unverified

## Current fix
None.

## Independent candidates
Candidate A and Candidate B.

## Adversarial consensus
### Agree
- The defect is reproduced.
### Dispute
- None
### Discard
- Candidate B

## Test assessment
The identical assertion fails on frozen head and passes with Candidate A.

## Implementation selection
**Selection status:** preferred
**Proof candidate:** candidate-a
**Preferred production candidate:** candidate-a
**Alternative closure:** structural

## Proof status
**Frozen-head result:** behavioral-fail
**Finding proof:** empirical
**Scenario proof:** empirical
**Candidate proof:** targeted-proven
**Changed path execution:** demonstrated
**Final observable:** inspected
**Boundary controls:** passed
**Pre-existing disposition:** not-pre-existing
**Changed reachability:** unchanged
**Multiplicity oracle:** not-applicable
**Multiplicity evidence:** not-applicable
**Multiplicity disposition:** not-applicable
**Product oracle:** documented
**Oracle fidelity:** authoritative
**Mechanism fidelity:** reproduced
**Scenario fidelity:** exact
**Regression assertion disposition:** required-regression
**Diagnostic mutation disposition:** not-applicable

## Final recommendation
**Implementation verdict:** ADOPT CANDIDATE
**Behavioral evidence:** empirical
**Merge readiness:** ready
**Implementation confidence:** medium
**Reason:** Candidate A passes the common matrix and the alternative is structurally incompatible.

## Required follow-ups
- Run broader CI.

## Repository oracle gaps
- None

## Suggested review comments
- None
'@

            $errors = @(Test-ReviewArtifacts -Root $root)
            Assert-Equal 0 $errors.Count "Issue adopt-candidate artifacts failed validation: $($errors -join '; ')"
        }
        finally
        {
            if (Test-Path -LiteralPath $root)
            {
                Remove-Item -LiteralPath $root -Recurse -Force
            }
        }
    }
}

if ($Suite -in @('All', 'Candidate'))
{
    Invoke-Test 'Fix-candidate Vally spec validates independently' {
        $result = Test-EvalSuites -Paths $configuration.CandidateEvals
        Assert-Equal 0 $result.Errors.Count "Fix-candidate validation failed: $($result.Errors -join '; ')"
        Assert-True ($result.Records.Count -gt 0) 'Fix-candidate suite had no records.'
    }
}

if ($Suite -in @('All', 'Issue'))
{
    Invoke-Test 'Fix-issue Vally spec validates independently' {
        $result = Test-EvalSuites -Paths $configuration.IssueEvals
        Assert-Equal 0 $result.Errors.Count "Fix-issue validation failed: $($result.Errors -join '; ')"
        Assert-True ($result.Records.Count -gt 0) 'Fix-issue suite had no records.'
    }
}

Invoke-Test 'Runtime staging contains only required skill files' {
    $root = Join-Path ([IO.Path]::GetTempPath()) "review-skills-$([guid]::NewGuid())"
    try
    {
        New-Item -ItemType Directory -Path $root | Out-Null
        $staged = Copy-SanitizedSkills -Destination (Join-Path $root 'staged')
        foreach ($skill in $configuration.StagedSkillFiles.Keys)
        {
            foreach ($relativePath in $configuration.StagedSkillFiles[$skill])
            {
                $path = Join-Path (Join-Path $staged $skill) $relativePath
                Assert-True (Test-Path -LiteralPath $path -PathType Leaf) `
                    "Missing staged $skill runtime file $relativePath."
            }
        }

        $expectedFiles = [Collections.Generic.HashSet[string]]::new(
            [StringComparer]::OrdinalIgnoreCase)
        foreach ($skill in $configuration.StagedSkillFiles.Keys)
        {
            foreach ($relativePath in $configuration.StagedSkillFiles[$skill])
            {
                $expectedFiles.Add("$skill/$relativePath") | Out-Null
            }
        }
        $stagedFiles = @(Get-ChildItem -LiteralPath $staged -Recurse -File)
        Assert-Equal $expectedFiles.Count $stagedFiles.Count `
            'Runtime staging copied an unexpected number of files.'
        foreach ($file in $stagedFiles)
        {
            $relativePath = [IO.Path]::GetRelativePath($staged, $file.FullName).Replace('\', '/')
            Assert-True ($expectedFiles.Contains($relativePath)) `
                "Runtime staging copied eval-only or unexpected file $relativePath."
        }
        Assert-Equal 0 @($stagedFiles | Where-Object {
            $_.Name -eq 'eval-policy.md' -or
            $_.Name.EndsWith('.vally.yaml', [StringComparison]::OrdinalIgnoreCase) -or
            $_.FullName -match '[\\/]fixtures[\\/]'
        }).Count 'Runtime staging included eval policy, fixture, or canonical spec material.'
    }
    finally
    {
        if (Test-Path -LiteralPath $root)
        {
            Remove-Item -LiteralPath $root -Recurse -Force
        }
    }
}

Invoke-Test 'Score aggregation preserves family macro weighting' {
    $document = [pscustomobject]@{
        evals = @(
            [pscustomobject]@{ id = 1; eval_metadata = [pscustomobject]@{ tier = 'train'; score_family = 'a'; provenance = [pscustomobject]@{ kind = 'synthetic'; source = 'x' } } }
            [pscustomobject]@{ id = 2; eval_metadata = [pscustomobject]@{ tier = 'train'; score_family = 'a'; provenance = [pscustomobject]@{ kind = 'synthetic'; source = 'x' } } }
            [pscustomobject]@{ id = 3; eval_metadata = [pscustomobject]@{ tier = 'train'; score_family = 'b'; provenance = [pscustomobject]@{ kind = 'synthetic'; source = 'y' } } }
        )
    }
    $aggregate = Get-EvalScoreAggregate -Document $document -Scores @{
        '1' = 1.0
        '2' = 1.0
        '3' = 0.0
    }
    Assert-Equal 0 $aggregate.Errors.Count 'Aggregation failed.'
    Assert-Equal 0.5 $aggregate.Result.tiers.train.family_macro `
        'Duplicate family cases changed macro weight.'
}

if ($script:Failed.Count -gt 0)
{
    $script:Failed | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "$script:Passed portable deterministic reviewer tests passed."
