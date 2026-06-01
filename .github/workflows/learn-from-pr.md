---
description: "Analyze a completed PR for lessons learned about codebase conventions, agent failures, and reusable patterns. Use to keep .github/instructions/ and agentic guidance current as the codebase and contributor pool evolves."
on:
  workflow_dispatch:
    inputs:
      pr_number:
        description: 'PR number to learn from (must be merged or closed)'
        required: true
        type: number
      apply:
        description: 'Apply recommendations directly (otherwise: report only)'
        required: false
        type: boolean
        default: false
  roles: [admin, maintain, write]

permissions:
  contents: read
  issues: read
  pull-requests: read

# ###############################################################
# Override COPILOT_GITHUB_TOKEN with a random PAT from the pool.
# See: .github/workflows/shared/pat_pool.README.md
# ###############################################################
imports:
  - shared/pat_pool.md

engine:
  id: copilot
  model: claude-opus-4.6
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}

tools:
  github:
    toolsets: [default, search]
  edit:
  bash: ["cat", "grep", "head", "tail", "find", "ls", "wc", "jq", "git", "date", "sort", "uniq"]

safe-outputs:
  create-pull-request:
    title-prefix: "[learn-from-pr] "
    draft: true
    max: 1
    protected-files: blocked
    allowed-files:
      - ".github/copilot-instructions.md"
      - ".github/instructions/**"
      - ".github/agents/**"
      - ".github/skills/**"
      - ".github/AGENTIC-WORKFLOWS.md"
    labels: [agentic-workflows]
    allowed-labels: [agentic-workflows]
  add-comment:
    max: 1
    target: "*"
  noop:
    report-as-issue: false

network:
  allowed:
    - defaults

timeout-minutes: 30
---

# Learn from PR

Invoke the **learn-from-pr** skill: read and follow `.github/skills/learn-from-pr/SKILL.md`.

## Context

- **PR number:** ${{ inputs.pr_number }}
- **Mode:** ${{ inputs.apply && 'Apply recommendations as draft PR' || 'Report only (no changes)' }}

## Behavior by mode

- **Report only** (`apply: false`, default): perform the analysis, post results as a comment on the source PR. No edits to repo files.
- **Apply** (`apply: true`): perform the analysis, open a draft PR with the recommended edits to instruction/skill/agent files. Use the `learn-from-pr` agent (`.github/agents/learn-from-pr.agent.md`) for the apply-step rules.

## Constraints

- Only edit files in `.github/copilot-instructions.md`, `.github/instructions/**`, `.github/agents/**`, `.github/skills/**`, or `.github/AGENTIC-WORKFLOWS.md` (enforced via `allowed-files`).
- Never modify source code or tests as part of this workflow.
- Open a single draft PR; do not push directly to `main`.
