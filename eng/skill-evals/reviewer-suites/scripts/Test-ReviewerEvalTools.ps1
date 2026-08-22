[CmdletBinding()]
param(
    [ValidateSet('All', 'Reviewer', 'TryFix')]
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

$configuration = Get-ReviewerEvalConfiguration

Invoke-Test 'Eval assets stay outside runtime skill trees' {
    foreach ($skill in @('fix-challenge', 'try-fix'))
    {
        $skillRoot = Join-Path $repoRoot ".github/skills/$skill"
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
    Assert-True (Test-Path -LiteralPath (Join-Path $repoRoot 'eng/skill-evals/try-fix/eval-policy.md') -PathType Leaf) `
        'Try-fix eval policy is missing from eng/skill-evals.'

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

if ($Suite -in @('All', 'Reviewer'))
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
}

if ($Suite -in @('All', 'TryFix'))
{
    Invoke-Test 'Try-fix Vally spec validates independently' {
        $result = Test-EvalSuites -Paths $configuration.TryFixEvals
        Assert-Equal 0 $result.Errors.Count "Try-fix validation failed: $($result.Errors -join '; ')"
        Assert-True ($result.Records.Count -gt 0) 'Try-fix suite had no records.'
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
