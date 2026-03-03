---
name: fix-issue
description: End-to-end issue fixer for dotnet/aspnetcore. Runs 5 phases — Pre-Flight, Test, Gate, Fix, Finalize — creating tests and always generating phase output files. Use when asked to "fix issue #XXXXX", "fix #XXXXX", or "work on issue #XXXXX".
---

# Fix Issue — Streamlined 5-Phase Workflow

End-to-end agent that takes a GitHub issue from investigation through to a completed PR.

**Trigger phrases:** "fix issue #XXXXX", "fix #XXXXX", "work on #XXXXX"

> 🚨 **NEVER** use `gh pr review --approve` or `--request-changes`. Only `--comment` is allowed.

---

## Overview

```
Phase 1: Pre-Flight   → Gather context, classify files, document findings
Phase 2: Test          → Create tests that reproduce the bug
Phase 3: Gate          → Verify tests & check for regressions (only tests added in this PR)
Phase 4: Fix           → ⚠️ MANDATORY multi-model exploration (5 models + cross-pollination)
Phase 5: Finalize      → ⚠️ MANDATORY push, create PR, set AI Summary description — NEVER skip
```

**All phases write output to:** `CustomAgentLogsTmp/PRState/{PRNumber-or-ISSUE-IssueNumber}/PRAgent/{phase}/content.md`

---

## Critical Rules

- ❌ Never run `git checkout`, `git switch`, `git stash`, `git reset` in Phases 1-2
- ❌ Never commit directly to `main` — always create a feature branch in Phase 1
- ❌ Never stop and ask the user — use best judgment to skip blocked phases and continue
- ❌ Never mark a phase complete with pending fields
- ❌ **Never skip Phase 4 multi-model exploration — it is MANDATORY for every fix, no exceptions**
- ❌ **Never skip Phase 5 — PR creation and AI Summary description are MANDATORY**
- ❌ **Never report a test as PASS/FAIL without actually running `dotnet test` — `dotnet build` is NOT a test run**
- ❌ **Never write "Expected to FAIL/PASS" — only write ACTUAL observed results from real test execution**
- ✅ Always activate .NET first: `source activate.sh`
- ✅ Always create `CustomAgentLogsTmp/` output files for every phase
- ✅ Always use `gh repo set-default dotnet/aspnetcore` before `gh pr` commands
- ✅ Always include `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>` in commits
- ✅ Always complete Phase 5 immediately after Phase 4 — do NOT wait for user to ask
- ✅ Always paste raw `dotnet test` output in Gate and try-fix output files — never summarize

### ASP.NET Core Build Commands

```bash
# .NET projects
source activate.sh
./src/{Area}/build.sh -test
dotnet test src/{Area}/test/{TestProject}.csproj --filter "FullyQualifiedName~TestName"

# Java projects (SignalR Java client)
cd src/SignalR/clients/java/signalr && ./gradlew test
```

---

## Phase 1: Pre-Flight (Context Gathering)

> **SCOPE:** Document only. No code analysis. No fix opinions. No running tests.

### Steps

1. **Read the issue** — full body + ALL comments via GitHub MCP tools
2. **Find existing PR** — search for PRs mentioning the issue number
3. **If PR exists:** read description, diff summary (`gh pr diff --stat`), review comments
4. **Create a feature branch** — branch from `main` immediately so all work happens on the branch, not on `main`:
   ```bash
   git checkout -b fix/{short-description}-{issue-number}
   ```
5. **Classify area** — identify `src/{Area}/` from labels or file paths
6. **Identify fix candidates** — from issue comments, PR approach, team guidance
7. **Identify test command** — which build script or test project to use

### Output File

```bash
mkdir -p CustomAgentLogsTmp/PRState/{PRNumber-or-ISSUE-IssueNumber}/PRAgent/pre-flight
```

Write `content.md`:
```markdown
**Issue:** #{IssueNumber} - {Title}
**Area:** {area-label} (`src/{Area}/`)
**PR:** #{PRNumber} (or "None — will create")

### Key Findings
- {Finding 1}
- {Finding 2}

### Test Command
`{test command}`

### Fix Candidates
| # | Source | Approach | Files Changed | Notes |
|---|--------|----------|---------------|-------|
| 1 | {source} | {approach} | `file.ext` | {notes} |
```

---

## Phase 2: Test (Create Tests for the Bug)

> **Goal:** Create tests that reproduce the bug described in the issue.

### Test Strategy — Unit Tests First

**Prefer unit tests over E2E tests.** Unit tests are cheaper, faster, and more reliable.
Only add E2E tests when unit tests cannot fully cover the scenario (e.g., browser-specific
behavior, full rendering pipeline, JavaScript interop, cross-component integration).

| Test Type | Location | When to Use | Run Cost |
|-----------|----------|-------------|----------|
| **Unit tests** (preferred) | `src/{Area}/{Component}/test/` next to `src/` | Logic bugs, parsing, state management, C# code paths | Cheap — seconds |
| **E2E tests** (when needed) | `src/{Area}/test/E2ETest/` | Browser behavior, JS interop, rendering, full pipeline | Expensive — requires browser |

**Decision process:**
1. Can the bug be reproduced with a unit test? → Write a unit test.
2. Does the bug involve browser/JS/rendering behavior that unit tests can't cover? → Write an E2E test.
3. If unsure, try unit tests first. Add an E2E test only if unit tests can't guarantee coverage.

**Finding the right test project:**
```bash
# Unit tests — look next to the source project
ls src/{Area}/{Component}/test/

# E2E tests — look in the area-level test folder
ls src/{Area}/test/E2ETest/
```

### Steps

1. **Determine test type** — unit test (preferred) or E2E test (if unit tests can't cover it)
2. **Find the appropriate test project** using the layout above
3. **Find similar test files** for style reference
4. **Write xUnit tests** that reproduce the bug
5. **Record which tests were created** (file paths, method names, test type)

Alternatively, use the `write-tests` skill via a task agent:

```
Invoke skill: write-tests

Problem: {bug description}
Area: src/{Area}/
Target files: {source files with the bug}
Test project: src/{Area}/{Component}/test/{TestProject}/
```

### Output File

```bash
mkdir -p CustomAgentLogsTmp/PRState/{PRNumber-or-ISSUE-IssueNumber}/PRAgent/test
```

Write `content.md`:
```markdown
### Test Result: {✅ TESTS CREATED / ❌ FAILED / ⚠️ SKIPPED}

**Test Type:** {Unit / E2E / Both}
**Test Type Rationale:** {Why this type was chosen — e.g., "Bug is in JS event handling, requires browser to reproduce"}
**Test Command:** `{command}`
**Tests Created:** {list of test files/methods}

#### Tests Written
- `{TestMethodName1}` — {what it verifies}
- `{TestMethodName2}` — {what it verifies}

#### Conclusion
{How the tests reproduce the bug}
```

---

## Phase 3: Gate (Verify Tests & Check Regressions)

> **Goal:** Verify that tests added in this PR are properly written and check for regressions. Only validate tests we added — do not fix pre-existing failures.
>
> ⚠️ **You MUST actually execute `dotnet test` (not just `dotnet build`).** A build passing
> does NOT count as a test passing. You MUST paste the real test runner output into the
> output file. If you did not run the test, the Gate is NOT complete.
>
> ❌ **NEVER write "Expected to FAIL" or "Expected to PASS" — you MUST write ACTUAL results.**
> ❌ **NEVER summarize test output in your own words — paste the raw output.**

### Steps

1. **Run new tests against buggy code** — they MUST FAIL (proving they catch the bug)
2. **If tests pass against buggy code** — the test is wrong. Rewrite it.
3. **Run only the new tests** — do NOT run the full test suite locally (E2E suites are extremely costly and should only run on CI). Use `--filter` to target only the tests you wrote.
4. **Verify test quality** — tests should be deterministic, well-named, and follow existing style

```bash
source activate.sh
# MANDATORY: Run only our new tests against buggy code — MUST FAIL
dotnet test src/{Area}/test/{TestProject}.csproj --filter "FullyQualifiedName~{NewTestName}"
```

> ⚠️ **Do NOT run the full test suite** (e.g. `dotnet test` without `--filter`).
> Full test runs are extremely costly for E2E tests and should only happen on CI.
> Always use `--filter` to run only the tests you created.

### Output File

```bash
mkdir -p CustomAgentLogsTmp/PRState/{PRNumber-or-ISSUE-IssueNumber}/PRAgent/gate
```

Write `content.md`:
```markdown
### Gate Result: {✅ PASSED / ❌ FAILED / ⚠️ BLOCKED}

**Test Command:** `{exact command executed}`

#### New Tests vs Buggy Code (MUST be actual results, not predictions)
- {test name}: {FAILED as expected / PASSED — test is wrong, must rewrite}

#### Raw Test Output (MANDATORY — paste actual dotnet test output)
```
{Paste the complete stdout/stderr from dotnet test here.
This MUST be real output, not a summary. If this section is missing
or contains a summary instead of raw output, the Gate is INVALID.}
```

#### Conclusion
{Whether tests are properly written and no regressions detected}
```

> **If `dotnet test` fails due to infrastructure issues** (missing browser driver, missing
> dependencies, etc.), set Gate Result to `⚠️ BLOCKED`, paste the actual error output, and
> continue to Phase 4. Do NOT write a fake passing result.

---

## Phase 4: Fix (MANDATORY Multi-Model Exploration)

> **⚠️ THIS PHASE IS MANDATORY. YOU MUST NEVER SKIP IT. NO EXCEPTIONS.**
>
> Even if you already have a working fix, you MUST still run all 5 models to explore
> alternative approaches. The purpose is to find the BEST fix, not just A fix.
>
> **Goal:** Explore 5+ alternative fixes using different AI models, cross-pollinate, select the best.

### Models (run SEQUENTIALLY — they modify the same files)

| Order | Model | Type |
|-------|-------|------|
| 1 | claude-sonnet-4.6 | Standard |
| 2 | claude-opus-4.6 | Premium |
| 3 | gpt-5.2 | Standard |
| 4 | gpt-5.3-codex | Standard |
| 5 | gemini-3-pro-preview | Standard |

### Checklist (you MUST complete ALL of these)

- [ ] Attempt 1 launched with claude-sonnet-4.6
- [ ] Attempt 2 launched with claude-opus-4.6
- [ ] Attempt 3 launched with gpt-5.2
- [ ] Attempt 4 launched with gpt-5.3-codex
- [ ] Attempt 5 launched with gemini-3-pro-preview
- [ ] Cross-pollination round completed (at least 2 models)
- [ ] Best fix selected with comparison table

### Round 1: Independent Exploration

For each model, launch a `general-purpose` task agent with that model:

```
prompt: |
  You are exploring an ALTERNATIVE fix for {bug description}.

  ## Problem
  {detailed bug description from Pre-Flight}

  ## Prior Attempts (DO NOT repeat these)
  {list all prior attempts and their approaches}

  ## Your Task
  Find a DIFFERENT approach.

  ## Instructions
  1. Revert to original: git checkout HEAD~1 -- {fix files} (or HEAD if no fix committed yet)
  2. Implement your fix
  3. Run tests: {test command from Test phase}
     ⚠️ You MUST run `dotnet test`, not just `dotnet build`.
     `dotnet build` succeeding does NOT count as PASS.
  4. Save approach to CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/try-fix/attempt-{N}/approach.md
  5. Save FULL `dotnet test` output to CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/try-fix/attempt-{N}/test-output.txt
  6. Save result to CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/try-fix/attempt-{N}/result.txt
     - Write PASS only if `dotnet test` actually ran and tests passed
     - Write FAIL if tests ran and failed
     - Write BLOCKED if tests could not run (infrastructure issue) — include error in test-output.txt
     - NEVER write PASS based on build success alone
  7. Save diff: git diff > CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/try-fix/attempt-{N}/fix.diff
  8. After done, revert: git checkout HEAD -- . && git clean -fd
```

### Round 2: Cross-Pollination

After all 5 models complete, ask at least 2 models (via task agent):
```
Given these {N} prior attempts and their results, do you have any NEW fix ideas?
Respond with "NO NEW IDEAS" or describe a new approach.
```

Run any new ideas as additional attempts. Repeat until all models say "NO NEW IDEAS" (max 3 rounds).

### Selecting the Best Fix

Compare all passing candidates on:
1. **Correctness** — Does it address root cause?
2. **Simplicity** — Fewer changes = better
3. **Robustness** — Handles edge cases?
4. **Team guidance** — Does it follow maintainer recommendations?
5. **Backward compatibility** — Breaking changes?

### Applying the Selected Fix

After selecting the best fix:
1. Apply the fix (or keep the PR fix if it's best)
2. Commit with descriptive message + `Co-authored-by` trailer

### Output File

```bash
mkdir -p CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/try-fix
```

Write `content.md`:
```markdown
### Fix Exploration Summary

**Total Attempts:** {N}
**Passing Candidates:** {N}
**Selected Fix:** {description}

#### Attempt Results
| # | Model | Approach | Result | Key Insight |
|---|-------|----------|--------|-------------|
| 1 | claude-sonnet-4.6 | {approach} | ✅/❌ | {insight} |
| ... | ... | ... | ... | ... |

#### Cross-Pollination
| Model | Round | New Ideas? | Details |
|-------|-------|------------|---------|
| ... | 2 | Yes/No | {idea or "NO NEW IDEAS"} |

**Exhausted:** {Yes/No}

#### Comparison
| Criterion | Fix A | Fix B | ... |
|-----------|-------|-------|-----|
| Correctness | ... | ... | ... |
| Simplicity | ... | ... | ... |

#### Recommendation
{Why selected fix is best}
```

---

## Phase 5: Finalize (MANDATORY — Push, PR, Comment)

> **⚠️ THIS PHASE IS MANDATORY. EXECUTE IT IMMEDIATELY AFTER PHASE 4.**
>
> Do NOT wait for the user to ask. Do NOT stop after committing the fix.
> The task is NOT complete until all 3 steps below are done.

### Checklist (ALL required — do not skip any)

- [ ] **Step 1: Push** — Create branch and push to fork
- [ ] **Step 2: Create PR** — Open PR against dotnet/aspnetcore
- [ ] **Step 3: Set PR Description** — Set AI Summary as PR body

### Step 1: Push to Fork

Push the feature branch created in Phase 1:
```bash
git push {fork-remote} fix/{short-description}-{issue-number}
```

### Step 2: Create PR

```bash
gh repo set-default dotnet/aspnetcore

gh pr create --repo dotnet/aspnetcore \
  --head {user}:{branch} --base main \
  --title "{title}" --body "PR created — description will be set by AI Summary."
```

### Step 3: Set PR Description (AI Summary)

```bash
bash .github/skills/ai-summary-comment/scripts/post-ai-summary-comment.sh {IssueNumber} {PRNumber}
```

This sets the PR body to the AI Summary content (nested expandable sections with Pre-Flight, Test, Gate, and Fix phases). The issue reference is embedded in the Pre-Flight section — no separate `Fixes #` line is needed.

### Output File

```bash
mkdir -p CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/finalize
```

Write `content.md`:
```markdown
### Finalize Result: ✅ COMPLETE

**Branch:** `fix/{description}-{issue-number}`
**PR:** #{PRNumber} — {url}
**AI Summary Comment:** Posted ✅

#### PR Details
- Title: {title}
- Files changed: {count}
- Tests added: {count}
- Selected fix: {approach description}
```

---

## Output Directory Structure (MANDATORY)

```
CustomAgentLogsTmp/PRState/{PRNumber-or-ISSUE-IssueNumber}/PRAgent/
├── pre-flight/
│   └── content.md              # Phase 1 output
├── test/
│   └── content.md              # Phase 2 output
├── gate/
│   └── content.md              # Phase 3 output (MUST contain raw dotnet test output)
├── try-fix/
│   ├── content.md              # Phase 4 summary
│   └── attempt-{N}/            # Per-model attempt
│       ├── approach.md         # What was tried
│       ├── result.txt          # PASS / FAIL / BLOCKED (based on actual test run, not build)
│       ├── test-output.txt     # Raw dotnet test stdout/stderr (MANDATORY)
│       ├── fix.diff            # git diff of changes
│       └── analysis.md         # Why it worked/failed (if failed)
├── finalize/
│   └── content.md              # Phase 5 output
```

---

## Quick Reference

| Phase | Key Action | If Blocked |
|-------|------------|------------|
| 1. Pre-Flight | Read issue + PR | Skip missing info, continue |
| 2. Test | Create tests for the bug | Create tests via write-tests skill |
| 3. Gate | Verify tests & check regressions | Document failures, continue |
| 4. Fix | **5-model try-fix (MANDATORY, sequential)** | Skip failing models, continue with remaining |
| 5. Finalize | **Push + PR + Comment (MANDATORY)** | Never skip — task is incomplete without this |

---

## ⚠️ Multi-Model Try-Fix Enforcement

**The multi-model exploration in Phase 4 is NEVER optional.** Even if:
- You already have a working fix from Phase 2
- The fix seems obvious or trivial
- Only one file needs changing
- Time is a concern

You MUST still launch all 5 models to explore alternatives. The value is in discovering
approaches you didn't think of. A simpler, more correct, or more robust fix often emerges
from multi-model exploration.

**If a model fails or times out:** Document it as a failed attempt and continue with the
remaining models. The minimum requirement is that all 5 models were ATTEMPTED.

**After all models complete:** You MUST run cross-pollination with at least 2 models before
selecting the best fix.

---

## ⚠️ Finalize Enforcement

**Phase 5 is NEVER optional.** The task is NOT complete until:
1. The branch is pushed to the fork
2. A PR is created on dotnet/aspnetcore
3. The AI Summary is set as the PR description

**Do NOT stop after Phase 4.** Do NOT wait for the user to ask "did you create a PR?"
Execute Phase 5 immediately and automatically after Phase 4 completes.
