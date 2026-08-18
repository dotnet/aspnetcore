[CmdletBinding()]
param(
    [ValidateSet('All', 'Reviewer', 'TryFix')]
    [string] $Suite = 'All'
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'ReviewerEvalTools.psm1') -Force

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

        Assert-True (-not (Test-Path -LiteralPath (Join-Path $staged 'fix-challenge/evals'))) `
            'Fix-challenge eval assets leaked into the staged runtime.'
        Assert-True (-not (Test-Path -LiteralPath (Join-Path $staged 'try-fix/evals'))) `
            'Try-fix eval assets leaked into the staged runtime.'
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
