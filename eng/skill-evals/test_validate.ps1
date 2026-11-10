#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$runner = Join-Path $PSScriptRoot 'run.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-skill-eval-layout-test-' + [guid]::NewGuid().ToString('N')
)

function Write-TestRepository {
    param([string]$Root)

    $eval = Join-Path $Root 'eng/skill-evals/widget'
    $skill = Join-Path $Root '.github/skills/widget'
    New-Item -ItemType Directory -Path "$eval/fixtures/sample" -Force | Out-Null
    New-Item -ItemType Directory -Path $skill -Force | Out-Null
    Set-Content "$skill/SKILL.md" "---`nname: widget`ndescription: Widget.`n---`n"
    Set-Content "$eval/fixtures/sample/input.txt" "input"
    Set-Content "$eval/eval.vally.yaml" @'
name: widget
type: capability
defaults:
  runs: 5
  model: test-model
  judge_model: test-judge
stimuli:
  - name: widget
    prompt: Explain the widget.
    rubric:
      - Explains the widget
'@
    Set-Content (Join-Path $Root 'eng/skill-evals/skills-vs-baseline.experiment.yaml') @'
name: skills-vs-baseline
evals:
  - "*/eval.vally.yaml"
vary:
  - /environment/skills
baseline: baseline
variants:
  baseline:
    environment:
      skills: []
  skilled:
    environment:
      skills:
        - "../../.github/skills/${eval.parent}"
'@
    Set-Content (Join-Path $Root 'eng/skill-evals/skills-smoke.experiment.yaml') @'
name: skills-smoke
evals:
  - "*/eval.vally.yaml"
defaults:
  runs: 1
'@
    & git -C $Root init -q
    & git -C $Root config user.email skill-evals@example.invalid
    & git -C $Root config user.name 'Skill eval self-test'
    & git -C $Root add -A
    & git -C $Root commit -qm baseline
}

function Invoke-Case {
    param(
        [string]$Name,
        [scriptblock]$Mutation,
        [string]$Expected,
        [bool]$ShouldFail = $true
    )

    $root = Join-Path $testRoot ([guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $root | Out-Null
    Write-TestRepository $root
    & $Mutation $root
    $failed = $false
    $message = ''
    try {
        & $runner Validate -Root $root -Vally $fakeVally -VallyPrefix @()
    } catch {
        $failed = $true
        $message = $_.Exception.Message
    }
    $passed = $failed -eq $ShouldFail -and (
        -not $Expected -or $message -like "*$Expected*"
    )
    if (-not $passed) {
        throw "$Name failed. Expected failure=$ShouldFail containing '$Expected'; got '$message'."
    }
    Write-Host "  [OK] $Name"
}

New-Item -ItemType Directory -Path $testRoot | Out-Null
$fakeVally = Join-Path $testRoot 'fake-vally.ps1'
@'
if ($args -contains '--version') {
    Write-Output 'fake-vally 1.0'
}
'@ | Set-Content $fakeVally

try {
    Invoke-Case 'clean repository' {} '' $false
    Invoke-Case 'missing experiment' {
        param($root)
        Remove-Item "$root/eng/skill-evals/skills-vs-baseline.experiment.yaml"
    } 'Missing standard experiment'
    Invoke-Case 'missing smoke experiment' {
        param($root)
        Remove-Item "$root/eng/skill-evals/skills-smoke.experiment.yaml"
    } 'Missing smoke experiment'
    Invoke-Case 'missing runtime skill' {
        param($root)
        Remove-Item -Recurse "$root/.github/skills/widget"
    } 'no matching runtime skill'
    Invoke-Case 'eval spec in runtime skill' {
        param($root)
        Set-Content "$root/.github/skills/widget/eval.vally.yaml" 'name: wrong'
    } 'is eval-only'
    Invoke-Case 'evals directory in runtime skill' {
        param($root)
        New-Item -ItemType Directory "$root/.github/skills/widget/evals" | Out-Null
    } 'is eval-only'
    Invoke-Case 'runtime fixture directory is allowed' {
        param($root)
        New-Item -ItemType Directory "$root/.github/skills/widget/fixtures" | Out-Null
        Set-Content "$root/.github/skills/widget/fixtures/runtime.txt" 'runtime asset'
    } '' $false
    Invoke-Case 'untracked eval spec' {
        param($root)
        Set-Content "$root/eng/skill-evals/widget/specialized.vally.yaml" 'name: specialized'
    } 'is not tracked by git'
    Invoke-Case 'untracked fixture' {
        param($root)
        Set-Content "$root/eng/skill-evals/widget/fixtures/sample/untracked.txt" 'input'
    } 'is not tracked by git'
    Invoke-Case 'empty fixture directory' {
        param($root)
        Remove-Item "$root/eng/skill-evals/widget/fixtures/sample/input.txt"
    } 'is empty'

    if (-not $IsWindows) {
        Invoke-Case 'symlinked fixture' {
            param($root)
            New-Item -ItemType SymbolicLink `
                -Path "$root/eng/skill-evals/widget/fixtures/sample/link.txt" `
                -Target "$root/eng/skill-evals/widget/fixtures/sample/input.txt" | Out-Null
        } 'is a symlink'
    }

    Write-Host 'Skill-eval layout self-tests passed.'
} finally {
    Remove-Item -Recurse -Force $testRoot
}
