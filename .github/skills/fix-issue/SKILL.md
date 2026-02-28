---
name: fix-issue
description: End-to-end issue fixer for dotnet/aspnetcore. Runs 4 phases — Pre-Flight, Gate, Multi-Model Fix, Report — creating tests if needed and always generating phase output files. Use when asked to "fix issue #XXXXX", "fix #XXXXX", or "work on issue #XXXXX".
---

# Fix Issue — Streamlined 4-Phase Workflow

End-to-end agent that takes a GitHub issue from investigation through to a completed PR.

**Trigger phrases:** "fix issue #XXXXX", "fix #XXXXX", "work on #XXXXX"

> 🚨 **NEVER** use `gh pr review --approve` or `--request-changes`. Only `--comment` is allowed.

---

## Overview

```
Phase 1: Pre-Flight   → Gather context, classify files, document findings
Phase 2: Gate          → Verify tests exist (create if missing), validate they catch the bug
Phase 3: Fix           → Multi-model exploration (5 models + cross-pollination)
Phase 4: Report        → Create/update PR with full results
```

**All phases write output to:** `CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/{phase}/content.md`

---

## Critical Rules

- ❌ Never run `git checkout`, `git switch`, `git stash`, `git reset` in Phases 1-2
- ❌ Never stop and ask the user — use best judgment to skip blocked phases and continue
- ❌ Never mark a phase complete with pending fields
- ✅ Always activate .NET first: `source activate.sh`
- ✅ Always create `CustomAgentLogsTmp/` output files for every phase
- ✅ Always use `gh repo set-default dotnet/aspnetcore` before `gh pr` commands
- ✅ Always include `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>` in commits

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
4. **Classify area** — identify `src/{Area}/` from labels or file paths
5. **Identify fix candidates** — from issue comments, PR approach, team guidance
6. **Identify test command** — which build script or test project to use

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

## Phase 2: Gate (Test Verification)

> **Goal:** Ensure tests exist that catch the bug. Create them if missing.

### Decision Tree

```
Tests exist in PR/codebase?
├── YES → Run them → Do they fail without fix? → Gate ✅
└── NO  → Create tests first (see below) → Then gate
```

### Creating Tests (when none exist)

Use the `write-tests` skill via a task agent:

```
Invoke skill: write-tests

Problem: {bug description}
Area: src/{Area}/
Target files: {source files with the bug}
Test project: src/{Area}/test/{TestProject}/
```

Alternatively, create tests directly:
1. Find the appropriate test project: `find src/{Area} -name "*Test*.csproj" -o -name "*Tests*.csproj"`
2. Find similar test files for style reference
3. Write xUnit tests that reproduce the bug
4. Verify tests FAIL against current (buggy) code

### Gate Verification

1. **Run tests WITH current code** (should FAIL if bug exists, PASS if fix already applied)
2. **Record results**

### Output File

```bash
mkdir -p CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/gate
```

Write `content.md`:
```markdown
### Gate Result: {✅ PASSED / ❌ FAILED / ⚠️ SKIPPED}

**Test Command:** `{command}`
**Tests Created:** {Yes (list) / No (already existed)}

#### Test Results
- Exit code: {code}
- Tests passed: {count}
- Key output: {relevant info}

#### Conclusion
{Why gate passed/failed/was skipped}
```

**If Gate FAILS** (tests don't catch the bug): Fix the tests, re-run, document.
**If Gate PASSES**: Proceed to Phase 3.

---

## Phase 3: Fix (Multi-Model Exploration)

> **Goal:** Explore 5+ alternative fixes using different AI models, cross-pollinate, select the best.

### Models (run SEQUENTIALLY — they modify the same files)

| Order | Model | Type |
|-------|-------|------|
| 1 | claude-sonnet-4.6 | Standard |
| 2 | claude-opus-4.6 | Premium |
| 3 | gpt-5.2 | Standard |
| 4 | gpt-5.3-codex | Standard |
| 5 | gemini-3-pro-preview | Standard |

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
  3. Run tests: {test command from Gate}
  4. Save approach to CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/try-fix/attempt-{N}/approach.md
  5. Save result to CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/try-fix/attempt-{N}/result.txt
  6. Save diff: git diff > CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/try-fix/attempt-{N}/fix.diff
  7. After done, revert: git checkout HEAD -- . && git clean -fd
```

### Round 2: Cross-Pollination

After all 5 models complete, ask each (via task agent):
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
3. Push to fork branch

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

## Phase 4: Report (PR Creation & Summary)

> **Goal:** Create or update the PR with full results.

### Steps

1. **Create branch:** `fix/{area}-{short-description}`
2. **Stage only relevant files** (not `.github/` or `CustomAgentLogsTmp/`)
3. **Commit** with descriptive message + `Fixes #{IssueNumber}` + `Co-authored-by` trailer
4. **Push to fork:** `gh repo fork --remote=false` if needed, then `git push fork {branch}`
5. **Create PR:**
   ```bash
   gh repo set-default dotnet/aspnetcore
   gh pr create --repo dotnet/aspnetcore --head {user}:{branch} --base main \
     --title "{title}" --body "{body with multi-model results}"
   ```
6. **PR body must include:**
   - Summary and root cause
   - Changes made (per file)
   - Multi-model exploration table
   - Cross-pollination results
   - Selected fix rationale
   - Breaking changes (if any)
   - Test results

### Output File

```bash
mkdir -p CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/report
```

Write `content.md`:
```markdown
### Final Report

**PR:** #{PRNumber}
**Recommendation:** ✅ APPROVE

#### Root Cause
{description}

#### Fix Assessment
{why the selected fix is appropriate}

#### Multi-Model Results
{summary table}

#### PR Link
{URL}
```

---

## Output Directory Structure (MANDATORY)

```
CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/
├── pre-flight/
│   └── content.md              # Phase 1 output
├── gate/
│   └── content.md              # Phase 2 output
├── try-fix/
│   ├── content.md              # Phase 3 summary
│   └── attempt-{N}/            # Per-model attempt
│       ├── approach.md         # What was tried
│       ├── result.txt          # Pass / Fail / Blocked
│       ├── fix.diff            # git diff of changes
│       └── analysis.md         # Why it worked/failed (if failed)
└── report/
    └── content.md              # Phase 4 output
```

---

## Quick Reference

| Phase | Key Action | If Blocked |
|-------|------------|------------|
| 1. Pre-Flight | Read issue + PR | Skip missing info, continue |
| 2. Gate | Verify/create tests | Create tests via write-tests skill |
| 3. Fix | 5-model try-fix (sequential) | Skip failing models, continue |
| 4. Report | Create PR with full results | Document what completed |
