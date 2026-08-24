#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$runner = Join-Path $PSScriptRoot 'run.ps1'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-skill-eval-runner-test-' + [guid]::NewGuid().ToString('N')
)
$fakeVally = Join-Path $testRoot 'fake-vally.ps1'
$record = Join-Path $testRoot 'invocation.txt'
$specialized = Join-Path $testRoot 'specialized.vally.yaml'
$output = Join-Path $testRoot 'results'
$relativeOutput = 'artifacts/skill-eval-runner-selftest'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Invocation {
    $lines = @(Get-Content $record)
    Assert-True ($lines.Count -gt 1) 'Fake Vally did not capture an invocation.'
    Assert-True ($lines[0] -ne $repoRoot) 'Run used the repository as its working directory.'
    Assert-True (-not (Test-Path $lines[0])) 'The isolated working directory was not removed.'
    return $lines
}

New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    @'
if ($args -contains '--version') {
    Write-Output 'fake-vally 1.0'
    return
}
if ($env:SKILL_EVAL_FAKE_FAILURE) {
    throw 'fake Vally failure'
}
[IO.File]::WriteAllLines(
    $env:SKILL_EVAL_RUNNER_RECORD,
    @((Get-Location).Path) + [string[]]$args
)
'@ | Set-Content $fakeVally
    Set-Content $specialized "name: specialized`n"
    $env:SKILL_EVAL_RUNNER_RECORD = $record

    Push-Location $testRoot
    try {
        & $runner -Vally $fakeVally -VallyPrefix @()
    } finally {
        Pop-Location
    }
    $validateInvocation = Read-Invocation
    Assert-True ($validateInvocation[1] -eq 'experiment') (
        'Default validation did not resolve the experiment.'
    )
    Assert-True ($validateInvocation[2] -eq 'run') (
        'Default validation did not use Vally experiment run.'
    )
    Assert-True ($validateInvocation -contains '--dry-run') (
        'Default validation could invoke models.'
    )
    Assert-True ($validateInvocation -contains '--compare') (
        'Default validation did not resolve comparison mode.'
    )

    & $runner Run `
        -Eval eng/skill-evals/review-public-api/eval.vally.yaml `
        -Vally $fakeVally `
        -VallyPrefix @() `
        -OutputDirectory $relativeOutput `
        '--workers' '2'
    $standardInvocation = Read-Invocation
    Assert-True ($standardInvocation[1] -eq 'experiment') 'Standard run did not use Vally experiment.'
    Assert-True ($standardInvocation[2] -eq 'run') 'Standard run did not use Vally experiment run.'
    Assert-True ($standardInvocation -contains '--compare') 'Standard run omitted A/B comparison.'
    Assert-True ($standardInvocation -contains '--eval-filter') 'Standard run omitted --eval-filter.'
    Assert-True ($standardInvocation -contains 'review-public-api/eval.vally.yaml') (
        'Standard run did not resolve the repository-relative eval path.'
    )
    Assert-True ($standardInvocation -contains '--workers') 'Run did not forward additional arguments.'
    Assert-True ($standardInvocation -contains '2') 'Run did not forward the workers value.'
    Assert-True ($standardInvocation -contains (Join-Path $repoRoot $relativeOutput)) (
        'Run did not resolve the repository-relative output path.'
    )

    & $runner Run `
        -Eval $specialized `
        -Vally $fakeVally `
        -VallyPrefix @() `
        -OutputDirectory $output
    $specializedInvocation = Read-Invocation
    Assert-True ($specializedInvocation[1] -eq 'eval') 'Specialized run did not use Vally eval.'
    Assert-True ($specializedInvocation -contains '--eval-spec') 'Specialized run omitted --eval-spec.'

    & $runner RunAgent `
        -Vally $fakeVally `
        -VallyPrefix @() `
        -OutputDirectory $output `
        '--runs' '1' '--tag' 'eval_id=1'
    $agentInvocation = Read-Invocation
    Assert-True ($agentInvocation[1] -eq 'eval') 'Agent run did not use Vally eval.'
    Assert-True ($agentInvocation -contains '--executor-plugin') (
        'Agent run omitted its repository-local executor plugin.'
    )
    Assert-True ($agentInvocation -contains '--executor') 'Agent run omitted --executor.'
    Assert-True ($agentInvocation -contains 'blazor-component-readiness-agent') (
        'Agent run did not select the component-readiness executor.'
    )
    Assert-True ($agentInvocation -contains '--workspace') (
        'Agent run omitted its isolated workspace.'
    )
    $workspaceIndex = [Array]::IndexOf($agentInvocation, '--workspace')
    Assert-True ($workspaceIndex -ge 0) 'Agent run workspace index was not found.'
    Assert-True (-not $agentInvocation[$workspaceIndex + 1].StartsWith($repoRoot)) (
        'Agent run workspace was placed inside the developer checkout.'
    )
    Assert-True (-not (Test-Path $agentInvocation[$workspaceIndex + 1])) (
        'Agent run workspace was not removed.'
    )
    Assert-True ($agentInvocation -contains '--workers') (
        'Agent run did not bound default concurrency.'
    )
    Assert-True ($agentInvocation -contains '--runs') 'Agent run did not forward --runs.'
    Assert-True ($agentInvocation -contains 'eval_id=1') 'Agent run did not forward its tag.'

    $unsafeAgentRunRejected = $false
    try {
        & $runner RunAgent `
            -Vally $fakeVally `
            -VallyPrefix @() `
            '--workspace' $repoRoot
    } catch {
        $unsafeAgentRunRejected = $_.Exception.Message -like (
            '*owns --workspace to preserve exact agent binding and workspace isolation*'
        )
    }
    Assert-True $unsafeAgentRunRejected 'Agent run accepted a workspace override.'

    $shortEvalOverrideRejected = $false
    try {
        & $runner RunAgent `
            -Vally $fakeVally `
            -VallyPrefix @() `
            '-e' $specialized
    } catch {
        $shortEvalOverrideRejected = $_.Exception.Message -like (
            '*owns -e to preserve exact agent binding and workspace isolation*'
        )
    }
    Assert-True $shortEvalOverrideRejected 'Agent run accepted the short eval-spec override.'

    $wrongAgentSuiteRejected = $false
    try {
        & $runner RunAgent `
            -Eval $specialized `
            -Vally $fakeVally `
            -VallyPrefix @()
    } catch {
        $wrongAgentSuiteRejected = $_.Exception.Message -like (
            '*only supports the component-readiness representative and regression suites*'
        )
    }
    Assert-True $wrongAgentSuiteRejected 'Agent run accepted an unrelated suite.'

    $env:SKILL_EVAL_FAKE_FAILURE = 'true'
    $failurePropagated = $false
    try {
        & $runner Run -Vally $fakeVally -VallyPrefix @() -OutputDirectory $output
    } catch {
        $failurePropagated = $_.Exception.Message -like '*fake Vally failure*'
    }
    Assert-True $failurePropagated 'Vally invocation failure was not propagated.'

    Write-Host 'Skill-eval runner self-test passed.'
} finally {
    Remove-Item Env:SKILL_EVAL_RUNNER_RECORD -ErrorAction SilentlyContinue
    Remove-Item Env:SKILL_EVAL_FAKE_FAILURE -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $testRoot
}
