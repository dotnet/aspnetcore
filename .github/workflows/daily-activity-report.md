---
if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}

on:
  permissions: {}
  schedule: daily
  workflow_dispatch:

description: >
  Creates a daily issue summarizing repository activity from the previous
  24-hour UTC window.

permissions:
  contents: read
  discussions: read
  issues: read
  pull-requests: read

tools:
  github:
    mode: gh-proxy
    toolsets: [default]

safe-outputs:
  mentions: false
  allowed-github-references: []
  max-bot-mentions: 1
  report-failure-as-issue: false
  noop:
    report-as-issue: false
  create-issue:
    title-prefix: "Daily Repository Activity:"
    close-older-issues: true
    expires: 7

# ###############################################################
# Select a PAT from the pool and override COPILOT_GITHUB_TOKEN.
# Run agentic jobs in an isolated `copilot-pat-pool` environment.
#
# When org-level billing is available, this will be removed.
# See `shared/pat_pool.README.md` for more information.
# ###############################################################
imports:
  - uses: shared/pat_pool.md
    with:
      environment: copilot-pat-pool

environment: copilot-pat-pool

engine:
  id: copilot
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, 'NO COPILOT PAT AVAILABLE') }}
---

# Daily Repository Activity Report

Create a concise activity report for `${{ github.repository }}` covering the
closed 24-hour window ending at workflow start time, in UTC.

Use the GitHub tools to collect activity whose relevant event timestamp falls
inside the window:

- pull requests opened, merged, or closed
- issues opened or closed
- commits pushed to the default branch
- releases published
- discussions created

Deduplicate each item by its GitHub node or database ID. Group results by the
activity types above, then by outcome where useful. Do not infer missing
metadata; put items with unavailable classification in an `Unclassified`
group.

If the window contains no qualifying activity, call `noop` with:
`No repository activity from <window-start-utc> to <window-end-utc>.`

Otherwise, call `create-issue` once. Use the UTC end date as the stable daily
key and title the issue `Daily Repository Activity: YYYY-MM-DD`.

The issue body must use GitHub-flavored Markdown and:

- begin with `### Summary`
- state the exact UTC window and key counts
- keep important activity visible
- put long per-item lists inside `<details><summary>...</summary>` blocks
- link each item to GitHub and identify its author without using mentions
- use `### Context` for the repository, trigger, and workflow run link
- format the run link as
  `[§${{ github.run_id }}](https://github.com/${{ github.repository }}/actions/runs/${{ github.run_id }})`
- use no `#` or `##` headings, emojis, or footer attribution
