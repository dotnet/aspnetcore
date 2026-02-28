---
name: ai-summary-comment
description: Sets the PR description to the AI Summary content with expandable sections. Use after completing any fix-issue phase (pre-flight, test, gate, fix). Triggers on 'post comment to PR', 'update PR progress', 'post summary comment'.
metadata:
  author: dotnet-aspnetcore
  version: "4.0"
compatibility: Requires GitHub CLI (gh) authenticated with access to dotnet/aspnetcore repository.
---

# AI Summary Comment Skill

This skill sets the PR description to the AI Summary content during the fix-issue workflow. The description uses **nested expandable `<details>` sections**, providing rich context to maintainers and contributors.

**⚠️ Self-Contained Rule**: All content in PR descriptions must be self-contained. Never reference local files like `CustomAgentLogsTmp/` — GitHub users cannot access your local filesystem.

**✨ Key Features**:
- **PR Description**: AI Summary is set as the PR body (not a comment)
- **Nested Expandable Sections**: Top-level "Automated Fix Report" with nested phase sections
- **DryRun Support**: Use `DRY_RUN=1` to preview changes locally before posting

## PR Description Format

The AI Summary is set as the PR body using nested expandable sections:

```
🤖 AI Summary

▼ 🔍 Automated Fix Report
  ────────────────────────────────────
  ► 🔍 Pre-Flight — Context & Validation
  ────────────────────────────────────
  ► 🧪 Test — Bug Reproduction
  ────────────────────────────────────
  ► 🚦 Gate — Test Verification & Regression
  ────────────────────────────────────
  ► 🔧 Fix — Analysis & Comparison
```

The issue reference (e.g., `**Issue:** #12345`) is embedded in the Pre-Flight section — no separate `Fixes #` line is needed.

## Usage

```bash
# Auto-loads all phases from CustomAgentLogsTmp/PRState/ISSUE-{number}/PRAgent/
bash .github/skills/ai-summary-comment/scripts/post-ai-summary-comment.sh 21384 65567

# Dry run
DRY_RUN=1 bash .github/skills/ai-summary-comment/scripts/post-ai-summary-comment.sh 21384 65567
```

## Expected Directory Structure

Scripts auto-load from the fix-issue skill's output directory:

```
CustomAgentLogsTmp/PRState/ISSUE-{IssueNumber}/PRAgent/
├── pre-flight/
│   └── content.md
├── test/
│   └── content.md
├── gate/
│   └── content.md
├── try-fix/
│   ├── content.md              # Summary with comparison table
│   └── attempt-{N}/
│       ├── approach.md         # What was tried
│       ├── result.txt          # PASS / FAIL
│       ├── fix.diff            # git diff of changes
│       └── analysis.md         # Why it worked/failed (optional)
```

## Integration with fix-issue Skill

After the fix phase completes in the fix-issue workflow, invoke this skill:

```
Invoke skill: ai-summary-comment

Post the AI summary comment on PR #YYYYY for issue #XXXXX.
```

The skill will:
1. Read all phase directories from `CustomAgentLogsTmp/PRState/ISSUE-{N}/PRAgent/`
2. Build nested expandable `<details>` sections for each phase
3. Set the PR description to the AI Summary content (replaces any existing body)

## Technical Details

- PR body identified by HTML marker `<!-- AI Summary -->`
- Uses `gh api PATCH /repos/.../pulls/{n}` to update PR description
- Repository: `dotnet/aspnetcore`
