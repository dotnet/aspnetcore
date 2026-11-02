#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$stageRun = Join-Path $PSScriptRoot 'stage_run.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-skill-eval-staging-test-' + [guid]::NewGuid().ToString('N')
)
$trustedRoot = Join-Path $testRoot 'trusted'
$candidateRoot = Join-Path $testRoot 'candidate'
$destination = Join-Path $testRoot 'staged'
$trustedMarker = 'TRUSTED_CONTROL_MARKER'
$candidateControlMarker = 'CANDIDATE_CONTROL_CANARY_MUST_NOT_APPEAR'
$candidateDataMarker = 'CANDIDATE_DATA_MARKER'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Throws {
    param(
        [scriptblock]$Action,
        [string]$Expected
    )

    $message = ''
    try {
        & $Action
    } catch {
        $message = $_.Exception.Message
    }
    if ($message -notlike "*$Expected*") {
        throw "Expected an error containing '$Expected'; got '$message'."
    }
}

function Write-ControlPlane {
    param(
        [string]$Root,
        [string]$Marker
    )

    $evalRoot = Join-Path $Root 'eng/skill-evals'
    New-Item -ItemType Directory -Path $evalRoot -Force | Out-Null
    Set-Content (Join-Path $evalRoot 'run.ps1') $Marker
    Set-Content (Join-Path $evalRoot 'assert_results.ps1') $Marker
    Set-Content (Join-Path $evalRoot 'skills-vs-baseline.experiment.yaml') $Marker
    Set-Content (Join-Path $evalRoot 'skills-smoke.experiment.yaml') $Marker
}

New-Item -ItemType Directory -Path $trustedRoot, $candidateRoot | Out-Null
try {
    Write-ControlPlane $trustedRoot $trustedMarker
    Write-ControlPlane $candidateRoot $candidateControlMarker

    $candidateSkill = Join-Path $candidateRoot '.github/skills/widget'
    $candidateEval = Join-Path $candidateRoot 'eng/skill-evals/widget'
    New-Item -ItemType Directory -Path $candidateSkill -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $candidateEval 'fixtures') -Force |
        Out-Null
    Set-Content (Join-Path $candidateSkill 'SKILL.md') $candidateDataMarker
    Set-Content (Join-Path $candidateEval 'eval.vally.yaml') $candidateDataMarker
    Set-Content (Join-Path $candidateEval 'fixtures/input.txt') $candidateDataMarker
    Set-Content (Join-Path $candidateEval 'candidate-runner.ps1') $candidateControlMarker

    # A dot-prefixed directory/file is treated as hidden by Get-ChildItem on every
    # platform PowerShell runs on, even though POSIX "hidden" is only a naming
    # convention; this reproduces a staged fixture shim like ".eval-gh/gh".
    $hiddenFixtureDir = Join-Path $candidateEval 'fixtures/.eval-gh'
    New-Item -ItemType Directory -Path $hiddenFixtureDir -Force | Out-Null
    Set-Content (Join-Path $hiddenFixtureDir 'gh') $candidateDataMarker

    $unrelatedSkill = Join-Path $candidateRoot '.github/skills/unrelated'
    $unrelatedEval = Join-Path $candidateRoot 'eng/skill-evals/unrelated'
    New-Item -ItemType Directory -Path $unrelatedSkill, $unrelatedEval -Force |
        Out-Null
    Set-Content (Join-Path $unrelatedSkill 'SKILL.md') 'UNRELATED_DATA'
    Set-Content (Join-Path $unrelatedEval 'eval.vally.yaml') 'UNRELATED_DATA'

    & $stageRun `
        -TrustedRoot $trustedRoot `
        -CandidateRoot $candidateRoot `
        -EvalName widget `
        -Destination $destination

    $stagedFiles = @(
        Get-ChildItem $destination -Recurse -File -Force |
            ForEach-Object {
                [IO.Path]::GetRelativePath($destination, $_.FullName).Replace('\', '/')
            } |
            Sort-Object
    )
    $expectedFiles = @(
        '.github/skills/widget/SKILL.md'
        'eng/skill-evals/assert_results.ps1'
        'eng/skill-evals/run.ps1'
        'eng/skill-evals/skills-smoke.experiment.yaml'
        'eng/skill-evals/skills-vs-baseline.experiment.yaml'
        'eng/skill-evals/widget/eval.vally.yaml'
        'eng/skill-evals/widget/fixtures/input.txt'
        'eng/skill-evals/widget/fixtures/.eval-gh/gh'
    ) | Sort-Object
    Assert-True (
        @(Compare-Object $expectedFiles $stagedFiles).Count -eq 0
    ) "Unexpected staged files: $($stagedFiles -join ', ')."

    $stagedContent = $stagedFiles |
        ForEach-Object { Get-Content (Join-Path $destination $_) -Raw }
    Assert-True (
        @($stagedContent | Select-String $candidateControlMarker).Count -eq 0
    ) 'Candidate control-plane canary reached the staged tree.'
    Assert-True (
        @($stagedContent | Select-String $trustedMarker).Count -eq 4
    ) 'The trusted runner, assertion, and experiments were not all staged.'
    Assert-True (
        @($stagedContent | Select-String $candidateDataMarker).Count -eq 4
    ) 'The selected candidate skill, eval, and fixtures (including hidden ones) were not all staged.'

    if (-not $IsWindows) {
        $stagedFixture = Join-Path $destination 'eng/skill-evals/widget/fixtures/input.txt'
        $mode = (Get-Item $stagedFixture).UnixFileMode
        Assert-True (
            ($mode -band [IO.UnixFileMode]::UserExecute) -ne 0
        ) 'Staged fixture files were not made executable.'

        $stagedHiddenFixture = Join-Path $destination 'eng/skill-evals/widget/fixtures/.eval-gh/gh'
        $hiddenMode = (Get-Item $stagedHiddenFixture -Force).UnixFileMode
        Assert-True (
            ($hiddenMode -band [IO.UnixFileMode]::UserExecute) -ne 0
        ) 'Staged hidden fixture files were not made executable.'
    }

    Assert-Throws {
        & $stageRun `
            -TrustedRoot $trustedRoot `
            -CandidateRoot $candidateRoot `
            -EvalName '../escape' `
            -Destination (Join-Path $testRoot 'escape')
    } 'Invalid eval name'

    Assert-Throws {
        & $stageRun `
            -TrustedRoot $trustedRoot `
            -CandidateRoot $candidateRoot `
            -EvalName widget `
            -Destination (Join-Path $candidateRoot 'staged')
    } 'must be outside both source trees'

    if (-not $IsWindows) {
        $link = Join-Path $candidateEval 'fixtures/link.txt'
        New-Item -ItemType SymbolicLink `
            -Path $link `
            -Target (Join-Path $candidateEval 'fixtures/input.txt') | Out-Null
        Assert-Throws {
            & $stageRun `
                -TrustedRoot $trustedRoot `
                -CandidateRoot $candidateRoot `
                -EvalName widget `
                -Destination (Join-Path $testRoot 'symlinked')
        } 'is a symlink or reparse point'
    }

    function New-RestrictedCandidate {
        param(
            [string]$Name,
            [string]$EvalSpec
        )

        $skillDir = Join-Path $candidateRoot ".github/skills/$Name"
        $evalDir = Join-Path $candidateRoot "eng/skill-evals/$Name"
        New-Item -ItemType Directory -Path $skillDir, $evalDir -Force | Out-Null
        Set-Content (Join-Path $skillDir 'SKILL.md') $candidateDataMarker
        Set-Content (Join-Path $evalDir 'eval.vally.yaml') $EvalSpec
    }

    New-RestrictedCandidate 'restricted-commands' @'
stimuli:
  - environment:
      commands:
        - chmod +x .eval-gh/gh
'@
    Assert-Throws {
        & $stageRun `
            -TrustedRoot $trustedRoot `
            -CandidateRoot $candidateRoot `
            -EvalName restricted-commands `
            -Destination (Join-Path $testRoot 'restricted-commands')
    } "forbidden 'environment.commands' field"

    New-RestrictedCandidate 'restricted-mcp' @'
stimuli:
  - environment:
      mcpServers:
        example:
          command: node
'@
    Assert-Throws {
        & $stageRun `
            -TrustedRoot $trustedRoot `
            -CandidateRoot $candidateRoot `
            -EvalName restricted-mcp `
            -Destination (Join-Path $testRoot 'restricted-mcp')
    } "forbidden 'environment.mcpServers' field"

    New-RestrictedCandidate 'restricted-agent-environment' @'
stimuli:
  - agent_environment:
      commands:
        - chmod +x .eval-gh/gh
'@
    Assert-Throws {
        & $stageRun `
            -TrustedRoot $trustedRoot `
            -CandidateRoot $candidateRoot `
            -EvalName restricted-agent-environment `
            -Destination (Join-Path $testRoot 'restricted-agent-environment')
    } "forbidden 'agent_environment.commands' field"

    New-RestrictedCandidate 'restricted-flow-mapping' @'
stimuli:
  - environment: { commands: ["chmod +x .eval-gh/gh"] }
'@
    Assert-Throws {
        & $stageRun `
            -TrustedRoot $trustedRoot `
            -CandidateRoot $candidateRoot `
            -EvalName restricted-flow-mapping `
            -Destination (Join-Path $testRoot 'restricted-flow-mapping')
    } "forbidden 'environment.commands' field"

    New-RestrictedCandidate 'restricted-quoted-key' @'
stimuli:
  - environment:
      "commands":
        - chmod +x .eval-gh/gh
'@
    Assert-Throws {
        & $stageRun `
            -TrustedRoot $trustedRoot `
            -CandidateRoot $candidateRoot `
            -EvalName restricted-quoted-key `
            -Destination (Join-Path $testRoot 'restricted-quoted-key')
    } "forbidden 'environment.commands' field"

    New-RestrictedCandidate 'restricted-executor' @'
stimuli:
  - executor:
      name: copilot-sdk
      config:
        provider: https://example.invalid
'@
    Assert-Throws {
        & $stageRun `
            -TrustedRoot $trustedRoot `
            -CandidateRoot $candidateRoot `
            -EvalName restricted-executor `
            -Destination (Join-Path $testRoot 'restricted-executor')
    } "forbidden 'executor.config.provider'"

    New-RestrictedCandidate 'allowed-executor' @'
stimuli:
  - executor: copilot-sdk
    judge_provider: https://example.invalid
'@
    & $stageRun `
        -TrustedRoot $trustedRoot `
        -CandidateRoot $candidateRoot `
        -EvalName allowed-executor `
        -Destination (Join-Path $testRoot 'allowed-executor')

    # A prompt mentioning "commands:"-looking lines is plain text, not YAML
    # mapping keys; staging must not misread it as a forbidden field.
    New-RestrictedCandidate 'allowed-prompt-text' @'
stimuli:
  - name: narrates-forbidden-field-names
    prompt: |
      List the available commands:
        commands:
          - not actually yaml
      Also mention mcpServers: as a word, not a key.
'@
    & $stageRun `
        -TrustedRoot $trustedRoot `
        -CandidateRoot $candidateRoot `
        -EvalName allowed-prompt-text `
        -Destination (Join-Path $testRoot 'allowed-prompt-text')

    Write-Host 'Trusted skill-eval staging self-test passed.'
} finally {
    Remove-Item $testRoot -Recurse -Force
}
