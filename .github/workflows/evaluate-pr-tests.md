---
description: Evaluates test quality, coverage, and appropriateness on PRs that add or modify tests in dotnet/aspnetcore
on:
  slash_command:
    name: evaluate-tests
    events: [pull_request_comment]
  workflow_dispatch:
    inputs:
      pr_number:
        description: 'PR number to evaluate (required for workflow_dispatch; slash-command callers supply via the triggering PR context)'
        required: false
        type: number
      suppress_output:
        description: 'Dry-run — evaluate but do not post output on the PR'
        required: false
        type: boolean
        default: false
  roles: [admin, maintain, write]

labels: ["pr-review", "testing"]

# Trigger filtering: slash_command compiles to issue_comment (platform handles
# command matching). workflow_dispatch is always allowed.
if: >-
  github.event_name == 'issue_comment' ||
  github.event_name == 'workflow_dispatch'

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
  model: claude-sonnet-4.6
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}

safe-outputs:
  add-comment:
    max: 1
    target: "*"
    hide-older-comments: true
  noop:
    report-as-issue: false
  messages:
    footer: "> 🧪 *Test evaluation by [{workflow_name}]({run_url})*"
    run-started: "🔬 Evaluating tests on this PR… [{workflow_name}]({run_url})"
    run-success: "✅ Test evaluation complete! [{workflow_name}]({run_url})"
    run-failure: "❌ Test evaluation failed. [{workflow_name}]({run_url}) {status}"

tools:
  github:
    toolsets: [default]

network: defaults

concurrency:
  group: "evaluate-pr-tests-${{ github.event.issue.number || inputs.pr_number || github.run_id }}"
  cancel-in-progress: false

timeout-minutes: 20

steps:
  - name: Gate — skip if no test source files in diff
    env:
      GH_TOKEN: ${{ github.token }}
      PR_NUMBER: ${{ github.event.issue.number || inputs.pr_number }}
    run: |
      # Verify this is an open PR
      if ! STATE=$(gh pr view "$PR_NUMBER" --repo "$GITHUB_REPOSITORY" --json state --jq .state 2>&1); then
        echo "::error::Failed to fetch PR #$PR_NUMBER state: $STATE"
        exit 1
      fi
      if [ "$STATE" != "OPEN" ]; then
        echo "::notice::PR #$PR_NUMBER is $STATE — skipping evaluation. The downstream agent will noop."
        # Exit 0 so this clean skip doesn't show as a CI failure. The agent
        # will still activate and call `noop` after seeing no work to do.
        exit 0
      fi
      # Try gh pr diff first; fall back to REST API on command failure
      if DIFF_OUTPUT=$(gh pr diff "$PR_NUMBER" --repo "$GITHUB_REPOSITORY" --name-only 2>/dev/null); then
        TEST_FILES=$(echo "$DIFF_OUTPUT" \
          | grep -E '\.(cs|razor)$' \
          | grep -iE '(test/|tests/|TestCases|UnitTests|E2ETest)' \
          || true)
      else
        # gh pr diff fails with HTTP 406 for PRs with 300+ files; use paginated files API
        if ! API_FILES=$(gh api "repos/$GITHUB_REPOSITORY/pulls/$PR_NUMBER/files" --paginate --jq '.[].filename' 2>&1); then
          echo "::error::gh pr diff failed and REST API fallback also failed: $API_FILES"
          exit 1
        fi
        TEST_FILES=$(echo "$API_FILES" \
          | grep -E '\.(cs|razor)$' \
          | grep -iE '(test/|tests/|TestCases|UnitTests|E2ETest)' \
          || true)
      fi
      if [ -z "$TEST_FILES" ]; then
        echo "::notice::No test source files (.cs/.razor) found in PR diff for PR #$PR_NUMBER. Nothing to evaluate; agent will noop."
        # Exit 0 — a "no work to do" gate is a clean skip, not a failure.
        exit 0
      fi
      echo "✅ Found test files to evaluate:"
      echo "$TEST_FILES" | head -20
---

# Evaluate PR Tests

Invoke the **evaluate-pr-tests** skill: read and follow `.github/skills/evaluate-pr-tests/SKILL.md`.

## Context

- **Repository**: ${{ github.repository }}
- **PR Number**: ${{ github.event.issue.number || inputs.pr_number }}

The PR is available via MCP tools. Use `gh pr view` and `gh pr diff` for PR data.

## Pre-flight check

Before starting, verify the skill file exists:

```bash
test -f .github/skills/evaluate-pr-tests/SKILL.md
```

If the file is **missing**, the fork PR branch is likely not rebased on latest `main`. Post a comment via `add_comment`:

```markdown
## 🧪 PR Test Evaluation

❌ **Cannot evaluate**: this PR's branch does not include the evaluate-pr-tests skill (`.github/skills/evaluate-pr-tests/SKILL.md` is missing).

**Fix**: rebase your fork on the latest `main` branch and push again. The evaluation will trigger automatically once the skill file is available.
```

Then stop — do not proceed.

## Dry-run mode

When triggered via `workflow_dispatch`, `suppress_output` controls behavior.
- `true` → run the full evaluation but **do not** post output on the PR (log only).
- `false` (default) → post the output as normal.

## When no action is needed

If there is nothing to evaluate (PR has no test files, docs-only change, etc.), **always** call `noop` with a message — never silently exit:

```json
{"noop": {"message": "No action needed: [brief explanation]"}}
```

## Running the skill

1. Use `gh pr view <number>` to fetch PR metadata.
2. Read the SKILL.md and apply its criteria to the changed test files.
3. Post the result via `add_comment` with `item_number` set to the PR number.

## Posting Results

If dry-run mode is active, log to stdout only — do **not** call `add_comment`.

Otherwise, post a single collapsible comment:

```markdown
## 🧪 PR Test Evaluation

**Overall Verdict:** [✅ Tests are adequate | ⚠️ Tests need improvement | ❌ Tests are insufficient]

[1-2 sentence summary]

> 👍 / 👎 — Was this evaluation helpful? React to let us know!

<details>
<summary>📊 Expand Full Evaluation</summary>

[Full report per SKILL.md]

</details>
```
