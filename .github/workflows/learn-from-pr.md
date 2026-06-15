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
# This workflow is manually dispatched only. The default
# COPILOT_GITHUB_TOKEN is fine here — the PAT pool is for high-volume
# auto-triggered workflows (code-review on every PR push, etc.), not
# for operator-invoked tasks. Mirrors the pattern used by
# cswin32-update.md and other workflow_dispatch-only workflows.
# ###############################################################

engine:
  id: copilot
  model: claude-opus-4.6

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
