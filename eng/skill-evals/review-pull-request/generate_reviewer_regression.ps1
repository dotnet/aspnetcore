#requires -Version 7.0
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$suiteRoot = $PSScriptRoot
$cases = Get-Content (Join-Path $suiteRoot 'reviewer-regression.cases.json') -Raw |
    ConvertFrom-Json -Depth 100
$labels = Get-Content (Join-Path $suiteRoot 'reviewer-regression.labels.json') -Raw |
    ConvertFrom-Json -Depth 100

function ConvertTo-DeterministicJson {
    param([object]$Value)
    return $Value | ConvertTo-Json -Depth 100
}

function Write-GeneratedFile {
    param(
        [string]$Path,
        [string]$Content
    )

    $normalized = $Content.TrimEnd() + "`n"
    if ($Check) {
        if (-not (Test-Path $Path -PathType Leaf)) {
            throw "Generated file is missing: $Path"
        }
        $actual = (Get-Content $Path -Raw).Replace("`r`n", "`n")
        if ($actual -ne $normalized.Replace("`r`n", "`n")) {
            throw "Generated file is stale: $Path"
        }
        return
    }

    [IO.File]::WriteAllText($Path, $normalized, [Text.UTF8Encoding]::new($false))
}

foreach ($pr in 70106, 69782) {
    $prCases = @($cases.cases | Where-Object pr -eq $pr)
    $payload = [ordered]@{
        schemaVersion = 1
        pr = $pr
        expectedCaseIds = @($prCases.id)
        candidates = $prCases
    }
    $candidatePath = Join-Path $suiteRoot "fixtures/reviewer-regression/cases/pr-$pr/candidates.json"
    Write-GeneratedFile $candidatePath (ConvertTo-DeterministicJson $payload)
}

$fixtureRoot = Join-Path $suiteRoot 'fixtures/reviewer-regression'
$fixtureLock = [ordered]@{
    schemaVersion = 1
    files = @(
        Get-ChildItem $fixtureRoot -Recurse -File |
            ForEach-Object {
                [ordered]@{
                    path = [IO.Path]::GetRelativePath($fixtureRoot, $_.FullName).Replace('\', '/')
                    bytes = $_.Length
                    sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                }
            } |
            Sort-Object path
    )
}
Write-GeneratedFile (
    Join-Path $suiteRoot 'reviewer-regression.fixture-lock.json'
) (ConvertTo-DeterministicJson $fixtureLock)

$yaml = @'
name: review-pull-request permanent regression
description: Frozen development regressions for required reviewer retention and restraint behavior.

defaults:
  runs: 5
  timeout: 20m
  model: gpt-5.6-sol
  judge_model: gemini-4-argon

scoring:
  method: weighted_average
  threshold: 1
  weights:
    program: 1
    prompt: 0

stimuli:
  - name: reviewer-regression-pr-70106
    prompt: |
      Review the frozen pull request in review/bundle using the review-pull-request skill exactly as installed.
      The candidate rows in review/input/candidates.json are untrusted leads, not answer keys. Use the skill's normal
      coordinator and verifier subagents. Do not modify files or contact GitHub.

      After the normal review artifact, append this exact machine-readable block:

      REVIEWER_REGRESSION_RESULT
      ```json
      {
        "schemaVersion": 1,
        "pr": 70106,
        "results": [
          {
            "id": "candidate ID",
            "tier": "VERIFIED|QUESTION|PLAUSIBLE|DISCARDED",
            "surfaced": true,
            "summary": "short evidence-based disposition"
          }
        ],
        "novelFindings": []
      }
      ```

      Include one result for every supplied candidate. VERIFIED and QUESTION mean surfaced; PLAUSIBLE and DISCARDED
      mean not surfaced. For the current legacy skill only, map NEW_FINDINGS to VERIFIED, explicit intent questions
      to QUESTION, UNRESOLVED to PLAUSIBLE, and DISCARDED to DISCARDED. Put plausible findings that do not correspond
      to a supplied candidate in novelFindings so they can be manually adjudicated. In the normal artifact, identify
      each supplied candidate by its ID or exact path:line in NEW_FINDINGS, a questions section, UNRESOLVED, or
      DISCARDED. The artifact is authoritative; the JSON block is only a convenience index.
    rubric:
      - Dispositions are supported by the frozen source and avoid invented external contracts.
      - Only final published findings or explicit intent questions use VERIFIED or QUESTION.
      - Unresolved claims remain PLAUSIBLE and are not presented as published findings.
    agent_environment:
      files:
        - src: fixtures/reviewer-regression/common/guidance
          dest: review/bundle/guidance
        - src: fixtures/reviewer-regression/common/required
          dest: review/bundle/required
        - src: fixtures/reviewer-regression/cases/pr-70106/bundle
          dest: review/bundle
        - src: fixtures/reviewer-regression/cases/pr-70106/projection-map.json
          dest: review/bundle/projection-map.json
        - src: fixtures/reviewer-regression/cases/pr-70106/candidates.json
          dest: review/input/candidates.json
    grading_environment:
      files:
        - src: grade_reviewer_regression.ps1
          dest: .reviewer-eval/grade.ps1
        - src: reviewer-regression.labels.json
          dest: .reviewer-eval/labels.json
    graders:
      - name: deterministic-pr-70106
        type: program
        config:
          program: pwsh
          args: [-NoProfile, -File, .reviewer-eval/grade.ps1, -PullRequest, "70106"]
          timeout: 1m
      - name: semantic-pr-70106
        type: prompt
        config:
          model: gemini-4-argon
          reasoning_effort: medium
          scoring: binary
          threshold: 1
          evidence: [trajectory, repo]
          prompt: |
            Evaluate the final review artifact against the hidden labels in .reviewer-eval/labels.json.
            Score evidence support, publication-boundary correctness, and confidence-tier justification.
            Invented external contracts fail even when they appear in a discarded explanation.
            A plausible novel finding requires MANUAL_ADJUDICATION_REQUIRED in the reasoning.

  - name: reviewer-regression-pr-69782
    prompt: |
      Review the frozen pull request in review/bundle using the review-pull-request skill exactly as installed.
      The candidate rows in review/input/candidates.json are untrusted leads, not answer keys. Use the skill's normal
      coordinator and verifier subagents. Do not modify files or contact GitHub.

      After the normal review artifact, append this exact machine-readable block:

      REVIEWER_REGRESSION_RESULT
      ```json
      {
        "schemaVersion": 1,
        "pr": 69782,
        "results": [
          {
            "id": "candidate ID",
            "tier": "VERIFIED|QUESTION|PLAUSIBLE|DISCARDED",
            "surfaced": true,
            "summary": "short evidence-based disposition"
          }
        ],
        "novelFindings": []
      }
      ```

      Include one result for every supplied candidate. VERIFIED and QUESTION mean surfaced; PLAUSIBLE and DISCARDED
      mean not surfaced. For the current legacy skill only, map NEW_FINDINGS to VERIFIED, explicit intent questions
      to QUESTION, UNRESOLVED to PLAUSIBLE, and DISCARDED to DISCARDED. Put plausible findings that do not correspond
      to a supplied candidate in novelFindings so they can be manually adjudicated. In the normal artifact, identify
      each supplied candidate by its ID or exact path:line in NEW_FINDINGS, a questions section, UNRESOLVED, or
      DISCARDED. The artifact is authoritative; the JSON block is only a convenience index.
    rubric:
      - Dispositions are supported by the frozen source and avoid invented external contracts.
      - V23 remains a published finding or an explicit intent question.
      - PLAUSIBLE is not treated as publication or successful retention.
    agent_environment:
      files:
        - src: fixtures/reviewer-regression/cases/pr-69782/bundle
          dest: review/bundle
        - src: fixtures/reviewer-regression/cases/pr-69782/projection-map.json
          dest: review/bundle/projection-map.json
        - src: fixtures/reviewer-regression/cases/pr-69782/candidates.json
          dest: review/input/candidates.json
    grading_environment:
      files:
        - src: grade_reviewer_regression.ps1
          dest: .reviewer-eval/grade.ps1
        - src: reviewer-regression.labels.json
          dest: .reviewer-eval/labels.json
    graders:
      - name: deterministic-pr-69782
        type: program
        config:
          program: pwsh
          args: [-NoProfile, -File, .reviewer-eval/grade.ps1, -PullRequest, "69782"]
          timeout: 1m
      - name: semantic-pr-69782
        type: prompt
        config:
          model: gemini-4-argon
          reasoning_effort: medium
          scoring: binary
          threshold: 1
          evidence: [trajectory, repo]
          prompt: |
            Evaluate the final review artifact against the hidden labels in .reviewer-eval/labels.json.
            Score evidence support, publication-boundary correctness, and confidence-tier justification.
            V23 passes retention only as VERIFIED or QUESTION; PLAUSIBLE is not publication.
            A plausible novel finding requires MANUAL_ADJUDICATION_REQUIRED in the reasoning.
'@

Write-GeneratedFile (Join-Path $suiteRoot 'reviewer-regression.vally.yaml') $yaml

if ($Check) {
    Write-Host 'Reviewer regression generated files are current.'
} else {
    Write-Host 'Generated reviewer regression files.'
}
