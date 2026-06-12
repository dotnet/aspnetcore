---
description: "Review pull request changes for correctness, performance, and consistency with aspnetcore conventions. Dispatches to area-specific reviewer agents (e.g., blazor-expert-reviewer) when relevant."

permissions:
  contents: read
  issues: read
  pull-requests: read

network:
  allowed:
    - defaults

tools:
  github:
    mode: remote
    toolsets: [default, search]
  web-fetch:

checkout:
  fetch-depth: 50

safe-outputs:
  add-comment:
    max: 1
    target: "triggering"
    hide-older-comments: true
    discussions: false
    issues: false
  create-pull-request-review-comment:
    max: 30
  submit-pull-request-review:
    max: 1
  noop:
    report-as-issue: false

on:
  pull_request:
    types: [opened, synchronize]

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
---

# Code Review (dispatcher)

You are reviewing pull request #${{ github.event.pull_request.number }} in dotnet/aspnetcore. This workflow is a **thin dispatcher** — all review logic lives in skills and agents under `.github/`.

## Step 1: Load the dispatcher skill

Read `.github/skills/code-review/SKILL.md`. It contains the routing rules from PR-touched areas to reviewer agents.

## Step 2: Route and invoke

For each area touched by the PR, identify the matching reviewer agent (e.g., `blazor-expert-reviewer` for `src/Components/**`, `src/Razor/**`, `src/JSInterop/**`). Launch matched agents in parallel as sub-tasks.

Each agent will perform its own four-wave review process (Discover → Validate → Post → Summary) per its `.agent.md` definition. Wave 3 of each agent posts inline review comments via `create_pull_request_review_comment`; you do not need to forward those.

## Step 3: Aggregate

After all agents complete:
- Consolidate their Wave 4 summaries into a single PR comment via `add_comment`, listing each agent's dimension-summary and findings checklist.
- Submit a single combined review via `submit_pull_request_review`:
  - Any BLOCKING from any agent → `event: REQUEST_CHANGES`
  - Otherwise → `event: COMMENT`
  - **Never `APPROVE`** — the bot must not count as a PR approval.

If no agents match (e.g., a PR touches only `docs/**` or only unmatched areas and `aspnetcore-expert-reviewer` does not exist), fall back to applying the general conventions in the dispatcher skill, but err on the side of `noop` with a message rather than posting a vacuous "looks good" comment.

If your review yields zero actionable findings, call `noop` with a brief message — do **not** post LGTM-only comments.
