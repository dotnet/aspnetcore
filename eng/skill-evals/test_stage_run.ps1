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

function Assert-Rejected {
    param(
        [scriptblock]$Action,
        [string]$Scenario
    )

    $rejected = $false
    try {
        & $Action
    } catch {
        $rejected = $true
    }
    if (-not $rejected) {
        throw "$Scenario was accepted."
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

    if (-not $env:SKILL_EVAL_STAGE_RESTRICTION) {
        New-RestrictedCandidate 'restricted-grading-commands' @'
grading_environment:
  commands:
    - node candidate-grader.mjs
stimuli:
  - name: case
'@
        Assert-Throws {
            & $stageRun `
                -TrustedRoot $trustedRoot `
                -CandidateRoot $candidateRoot `
                -EvalName restricted-grading-commands `
                -Destination (Join-Path $testRoot 'restricted-grading-commands')
        } "forbidden 'grading_environment.commands' field"

        New-RestrictedCandidate 'restricted-grading-mcp' @'
stimuli:
  - name: case
    grading_environment:
      mcpServers:
        candidate:
          command: node
'@
        Assert-Throws {
            & $stageRun `
                -TrustedRoot $trustedRoot `
                -CandidateRoot $candidateRoot `
                -EvalName restricted-grading-mcp `
                -Destination (Join-Path $testRoot 'restricted-grading-mcp')
        } "forbidden 'grading_environment.mcpServers' field"
    }

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

    New-RestrictedCandidate 'allowed-generic-graders' @'
grading_environment:
  files:
    - src: candidate-grader.py
      dest: custom/grader.py
stimuli:
  - name: generic-case
    graders:
      - type: program
        name: generic-program
        config:
          program: python
          args:
            - custom/grader.py
          env:
            GENERIC_MODE: flexible
'@
    & $stageRun `
        -TrustedRoot $trustedRoot `
        -CandidateRoot $candidateRoot `
        -EvalName allowed-generic-graders `
        -Destination (Join-Path $testRoot 'allowed-generic-graders')

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

    $canonicalReviewSpec = @'
grading_environment:
  files:
    - src: ../grade_disposition.mjs
      dest: grade_disposition.mjs
stimuli:
  - name: review-case
    prompt: Review the staged case.
    graders:
      - type: program
        name: disposition-review-case
        config:
          program: node
          args:
            - grade_disposition.mjs
          env:
            DISPOSITION_MODE: review
'@
    New-RestrictedCandidate 'review-pull-request' $canonicalReviewSpec
    $agentsDir = Join-Path $candidateRoot '.github/agents'
    $docsDir = Join-Path $candidateRoot 'docs'
    New-Item -ItemType Directory -Path $agentsDir, $docsDir -Force | Out-Null
    Set-Content (Join-Path $agentsDir 'review-guide-worker.agent.md') $candidateDataMarker
    Set-Content (Join-Path $agentsDir 'review-candidate-verifier.agent.md') $candidateDataMarker
    Set-Content (Join-Path $agentsDir 'unrelated.agent.md') 'UNRELATED_AGENT'
    Set-Content (Join-Path $docsDir 'CrossCuttingGuidance.md') $candidateDataMarker
    Set-Content (Join-Path $docsDir 'BlazorComponentsGuidance.md') $candidateDataMarker
    Set-Content (Join-Path $docsDir 'UnrelatedDoc.md') 'UNRELATED_DOC'
    foreach ($controlFile in @(
        'grade_disposition.mjs',
        'review-vs-baseline.experiment.yaml',
        'review-smoke.experiment.yaml'
    )) {
        Set-Content (Join-Path $trustedRoot "eng/skill-evals/$controlFile") $trustedMarker
    }

    $reviewDestination = Join-Path $testRoot 'staged-review-pull-request'
    & $stageRun `
        -TrustedRoot $trustedRoot `
        -CandidateRoot $candidateRoot `
        -EvalName review-pull-request `
        -Destination $reviewDestination

    $stagedReviewFiles = @(
        Get-ChildItem $reviewDestination -Recurse -File -Force |
            ForEach-Object {
                [IO.Path]::GetRelativePath($reviewDestination, $_.FullName).Replace('\', '/')
            } |
            Sort-Object
    )
    $expectedReviewFiles = @(
        '.github/agents/review-candidate-verifier.agent.md'
        '.github/agents/review-guide-worker.agent.md'
        '.github/skills/review-pull-request/SKILL.md'
        'docs/BlazorComponentsGuidance.md'
        'docs/CrossCuttingGuidance.md'
        'eng/skill-evals/assert_results.ps1'
        'eng/skill-evals/grade_disposition.mjs'
        'eng/skill-evals/review-vs-baseline.experiment.yaml'
        'eng/skill-evals/review-smoke.experiment.yaml'
        'eng/skill-evals/review-pull-request/eval.vally.yaml'
        'eng/skill-evals/run.ps1'
        'eng/skill-evals/skills-smoke.experiment.yaml'
        'eng/skill-evals/skills-vs-baseline.experiment.yaml'
    ) | Sort-Object
    Assert-True (
        @(Compare-Object $expectedReviewFiles $stagedReviewFiles).Count -eq 0
    ) "Unexpected staged review-pull-request files: $($stagedReviewFiles -join ', ')."

    foreach ($relativePath in @(
        '.github/agents/review-candidate-verifier.agent.md'
        '.github/agents/review-guide-worker.agent.md'
        'docs/CrossCuttingGuidance.md'
        'docs/BlazorComponentsGuidance.md'
    )) {
        $sourcePath = Join-Path $candidateRoot $relativePath
        $missingDestination = Join-Path $testRoot ([guid]::NewGuid().ToString('N'))
        Remove-Item $sourcePath
        try {
            Assert-Throws {
                & $stageRun `
                    -TrustedRoot $trustedRoot `
                    -CandidateRoot $candidateRoot `
                    -EvalName review-pull-request `
                    -Destination $missingDestination
            } "Missing candidate support file '$relativePath'"
            Assert-True (-not (Test-Path $missingDestination)) 'Failed staging left a partial tree.'
        } finally {
            Set-Content $sourcePath $candidateDataMarker
        }
    }

    $canonicalComponentSpec = @'
grading_environment:
  files:
    - src: ../grade_disposition.mjs
      dest: grade_disposition.mjs
stimuli:
  - name: component-case
    graders:
      - type: program
        name: disposition-component-case
        config:
          program: node
          args:
            - grade_disposition.mjs
          env:
            DISPOSITION_MODE: component
'@
    $componentEval = Join-Path $candidateRoot 'eng/skill-evals/review-candidate-verifier'
    New-Item -ItemType Directory -Path $componentEval -Force | Out-Null
    Set-Content (Join-Path $componentEval 'eval.vally.yaml') $canonicalComponentSpec
    $componentDestination = Join-Path $testRoot 'staged-verifier'
    & $stageRun -TrustedRoot $trustedRoot -CandidateRoot $candidateRoot `
        -EvalName review-candidate-verifier -Destination $componentDestination
    Assert-True (
        -not (Test-Path (Join-Path $componentDestination '.github/skills/review-candidate-verifier'))
    ) 'A component suite must not invent a runtime skill.'
    Assert-True (
        (Get-Content (Join-Path $componentDestination 'eng/skill-evals/grade_disposition.mjs') -Raw).Trim() -eq $trustedMarker
    ) 'The component grader does not come from the trusted control plane.'
    Assert-True (
        Test-Path (Join-Path $componentDestination '.github/agents/review-candidate-verifier.agent.md')
    ) 'The component verifier profile is missing.'

    $extendedCanonicalComponentSpec = $canonicalComponentSpec.Replace(
        '  - name: component-case',
        "  - name: component-case`n    grading_environment:`n      files:`n        - src: ../grade_disposition.mjs`n          dest: grade_disposition.mjs"
    ) + @'

      - type: program
        name: secondary-component-case
        config:
          program: node
          args:
            - grade_disposition.mjs
          env:
            DISPOSITION_MODE: component
'@
    Set-Content (Join-Path $componentEval 'eval.vally.yaml') $extendedCanonicalComponentSpec
    & $stageRun -TrustedRoot $trustedRoot -CandidateRoot $candidateRoot `
        -EvalName review-candidate-verifier `
        -Destination (Join-Path $testRoot 'staged-verifier-extended-canonical')

    function Assert-RestrictedDispositionSpecRejected {
        param(
            [string]$EvalName,
            [string]$Scenario,
            [string]$EvalSpec
        )

        $evalPath = Join-Path $candidateRoot "eng/skill-evals/$EvalName/eval.vally.yaml"
        Set-Content $evalPath $EvalSpec
        $rejectedDestination = Join-Path $testRoot "rejected-$Scenario"
        Assert-Rejected {
            & $stageRun -TrustedRoot $trustedRoot -CandidateRoot $candidateRoot `
                -EvalName $EvalName -Destination $rejectedDestination
        } "Restricted grader scenario '$Scenario'"
        Assert-True (-not (Test-Path $rejectedDestination)) (
            "Restricted grader scenario '$Scenario' left a partial staged tree."
        )
    }

    $reviewRestrictions = @(
        @{
            Name = 'ReviewMissingRootGradingFiles'
            Spec = $canonicalReviewSpec -replace '(?ms)^grading_environment:.*?(?=^stimuli:)', ''
        },
        @{
            Name = 'ReviewSubstitutedGraderSource'
            Spec = $canonicalReviewSpec.Replace('../grade_disposition.mjs', 'candidate-grader.mjs')
        },
        @{
            Name = 'ReviewSubstitutedGraderDestination'
            Spec = $canonicalReviewSpec.Replace('dest: grade_disposition.mjs', 'dest: candidate-grader.mjs')
        },
        @{
            Name = 'ReviewExtraGradingFile'
            Spec = $canonicalReviewSpec.Replace(
                '      dest: grade_disposition.mjs',
                "      dest: grade_disposition.mjs`n    - src: ../candidate-grader.mjs`n      dest: candidate-grader.mjs"
            )
        },
        @{
            Name = 'ReviewNestedSubstitutedGradingFile'
            Spec = $canonicalReviewSpec.Replace(
                '  - name: review-case',
                "  - name: review-case`n    grading_environment:`n      files:`n        - src: candidate-grader.mjs`n          dest: grade_disposition.mjs"
            )
        },
        @{
            Name = 'ReviewMissingPrimaryDispositionGrader'
            Spec = $canonicalReviewSpec -replace '(?ms)^    graders:.*\z', ''
        },
        @{
            Name = 'ReviewPrimaryDispositionReplacedByPrompt'
            Spec = $canonicalReviewSpec.Replace('      - type: program', '      - type: prompt')
        },
        @{
            Name = 'ReviewWrongPrimaryDispositionName'
            Spec = $canonicalReviewSpec.Replace('name: disposition-review-case', 'name: candidate-review-case')
        },
        @{
            Name = 'ReviewDuplicatePrimaryDispositionGrader'
            Spec = $canonicalReviewSpec + @'

      - type: program
        name: disposition-review-case
        config:
          program: node
          args:
            - grade_disposition.mjs
          env:
            DISPOSITION_MODE: review
'@
        }
    )
    foreach ($case in $reviewRestrictions) {
        if ($env:SKILL_EVAL_STAGE_RESTRICTION -and
            $env:SKILL_EVAL_STAGE_RESTRICTION -ne $case.Name) {
            continue
        }
        Assert-RestrictedDispositionSpecRejected -EvalName review-pull-request `
            -Scenario $case.Name -EvalSpec $case.Spec
    }

    $componentRestrictions = @(
        @{
            Name = 'ComponentSubstitutedProgram'
            Spec = $canonicalComponentSpec.Replace('program: node', 'program: pwsh')
        },
        @{
            Name = 'ComponentSubstitutedArguments'
            Spec = $canonicalComponentSpec.Replace(
                '            - grade_disposition.mjs',
                "            - grade_disposition.mjs`n            - candidate-grader.mjs"
            )
        },
        @{
            Name = 'ComponentWrongDispositionMode'
            Spec = $canonicalComponentSpec.Replace('DISPOSITION_MODE: component', 'DISPOSITION_MODE: review')
        },
        @{
            Name = 'ComponentExtraGraderEnvironment'
            Spec = $canonicalComponentSpec.Replace(
                '            DISPOSITION_MODE: component',
                "            DISPOSITION_MODE: component`n            CANDIDATE_OVERRIDE: enabled"
            )
        },
        @{
            Name = 'ComponentExtraProgramConfig'
            Spec = $canonicalComponentSpec.Replace(
                '          program: node',
                "          program: node`n          timeout: 1"
            )
        },
        @{
            Name = 'ComponentInvalidAdditionalProgramGrader'
            Spec = $canonicalComponentSpec + @'

      - type: program
        name: candidate-secondary
        config:
          program: python
          args:
            - candidate-grader.py
          env:
            DISPOSITION_MODE: component
'@
        }
    )
    foreach ($case in $componentRestrictions) {
        if ($env:SKILL_EVAL_STAGE_RESTRICTION -and
            $env:SKILL_EVAL_STAGE_RESTRICTION -ne $case.Name) {
            continue
        }
        Assert-RestrictedDispositionSpecRejected -EvalName review-candidate-verifier `
            -Scenario $case.Name -EvalSpec $case.Spec
    }

    Write-Host 'Trusted skill-eval staging self-test passed.'
} finally {
    Remove-Item $testRoot -Recurse -Force
}
