---
name: learn-from-pr
description: Analyzes completed PRs to extract reusable lessons about codebase conventions, common contributor mistakes, and patterns that should be captured in instruction files. Produces recommendations; the learn-from-pr agent applies them.
---

# Learn from PR

This skill extracts lessons from a single completed PR and **recommends** updates to repo guidance files. It does not apply changes — that's the job of the `learn-from-pr` agent (`.github/agents/learn-from-pr.agent.md`).

**Use case:** every notable PR is a teaching opportunity. Contributors joining a new area repeatedly hit the same conventions; reviewers repeatedly write the same review comments. This skill captures that into reusable guidance so the *next* contributor encounters the convention up front.

## When to Use

- After a notable PR is merged (especially one with significant reviewer iteration)
- After a PR where Copilot/agent involvement failed, succeeded slowly, or succeeded with surprising speed
- When onboarding a cohort of new contributors and want to bootstrap their guidance files
- Manually invoked via the `learn-from-pr` workflow with `apply: false`

## When NOT to Use

- Trivial PRs (typos, version bumps, formatting)
- PRs still open and under active review
- PRs where the lessons would require code changes (use `code-review` instead — that's review feedback, not convention extraction)

## Process

### Step 1: Gather PR context

For the target PR:

1. **PR metadata**: title, body, base branch, merge state, author.
2. **Full diff** — read all changed files in their entirety, not just hunks.
3. **Review comments** — every review comment from every reviewer; note which were addressed vs unaddressed.
4. **General PR comments** — discussion threads, especially questions and clarifications.
5. **Commit history on the PR branch** — what was the iteration path? Big rewrites mid-PR are interesting.
6. **Linked issue(s)** — what was the original goal?

### Step 2: Classify the PR outcome

| Outcome | Signal | What to look for |
|---|---|---|
| **Agent failed** | Many review rounds, large rewrite, PR closed/abandoned | What guidance was missing that caused the agent (or contributor) to start in the wrong direction? |
| **Agent succeeded slowly** | Many review rounds, but eventually merged | What feedback was repeated across rounds? That repetition = missing instruction file. |
| **Agent succeeded quickly** | ≤2 rounds, merged | What pattern worked? Was there existing guidance that helped, or did the author just happen to know? |
| **Convention-heavy** | Many style/convention comments, none about logic | Strong candidate for adding to instruction files. |
| **Area discovery** | Author asked "where does this go?" or "should this be in X or Y?" | Area boundary is unclear; consider adding or extending an `<area>/AGENTS.md` (the canonical cross-agent convention — see [agents.md](https://agents.md)) and, if Copilot needs to auto-load it for that area, a thin `.github/instructions/<area>.instructions.md` shim that delegates to the AGENTS.md. |

### Step 3: Extract candidate lessons

For each piece of repeated feedback, missing context, or unclear convention, draft a **candidate lesson**. Each lesson has:

- **What** — the convention or rule (one sentence)
- **Why** — the reason it matters in this repo (one or two sentences)
- **Where it applies** — file glob or area
- **Evidence** — link(s) to PR comments or commits that demonstrate the lesson
- **Priority** — High / Medium / Low (see below)

### Priority rubric

- **High**: lesson appeared 3+ times in review comments OR the absence of this guidance demonstrably caused rework. Apply if `apply: true`.
- **Medium**: lesson appeared 1-2 times AND has clear forward applicability. Apply if `apply: true`.
- **Low**: lesson is specific to this PR or borderline subjective. Report only; do not apply.

### Step 4: Categorize where each lesson should live

| Lesson type | Destination |
|---|---|
| C# coding convention (nullability, formatting, language version) | `.github/copilot-instructions.md` (general) |
| Per-area convention (e.g., "Components must do X") | `.github/instructions/<area>.instructions.md` |
| Specific reviewer rule for an area | The matching `.github/agents/<area>-expert-reviewer.agent.md` |
| Testing convention | `.github/skills/code-review/SKILL.md` (testing section) or `.github/skills/evaluate-pr-tests/SKILL.md` |
| Workflow / process rule (e.g., "API review needed") | `.github/AGENTIC-WORKFLOWS.md` or `.github/copilot-instructions.md` |
| One-off architectural note close to the code | `<area>/AGENTS.md` (canonical [agents.md](https://agents.md) convention — nearest-wins, cross-agent). If Copilot also needs to auto-load it via `applyTo:` glob, add a thin `.github/instructions/<area>.instructions.md` that delegates to the AGENTS.md. |

If a lesson would belong in a file that doesn't exist yet, recommend creating it. The agent will follow up.

### Step 5: Generate the report

```markdown
## 📚 Lessons from PR #<num>: <title>

**Outcome classification:** [Agent failed | Agent succeeded slowly | Agent succeeded quickly | Convention-heavy | Area discovery]

**Summary:** <2-3 sentences on what this PR teaches.>

### High-priority recommendations

| # | Lesson | Where to apply | Evidence |
|---|---|---|---|
| 1 | <one-sentence rule> | `.github/instructions/forms.instructions.md` | [comment](url), [comment](url) |
| 2 | ... | ... | ... |

### Medium-priority recommendations

(Same table.)

### Low-priority observations

(Bullet list, no applied changes.)

### Not learned

Document what you considered but rejected:

- Author preference vs convention — could not distinguish
- Single occurrence with no forward applicability
- Already documented in <file>
```

### Step 6: Hand off to agent (optional)

If the parent workflow invoked this in `apply: true` mode, the `learn-from-pr` **agent** takes the High and Medium recommendations and applies them as a single draft PR with the matching edits.

## Skill vs Agent

| Aspect | This skill (`learn-from-pr`) | The agent (`learn-from-pr.agent.md`) |
|---|---|---|
| Output | Recommendations + report | Applied edits as a draft PR |
| Side effects | None | Modifies `.github/instructions/`, `.github/copilot-instructions.md`, etc. |
| Use from | Anywhere (CLI, workflow, direct prompt) | The `learn-from-pr` workflow with `apply: true` |
| Idempotent? | Yes | No (creates draft PR; check before re-running) |

## Constraints

- **Never edit code or tests** — this skill is about conventions, not implementation. If you find a code-level lesson, surface it as a Low priority observation pointing at the relevant skill (e.g., `code-review`) for forward enforcement.
- **Don't duplicate existing content** — before recommending a lesson, search `.github/copilot-instructions.md` and `.github/instructions/` for existing similar guidance. Strengthen what's there rather than adding parallel rules.
- **Match existing style** — read the destination file before recommending edits.

## Cross-references

- The `learn-from-pr` agent applies these recommendations: `.github/agents/learn-from-pr.agent.md`
- The pattern is borrowed from [dotnet/maui](https://github.com/dotnet/maui/blob/main/.github/skills/learn-from-pr/SKILL.md) and adapted for aspnetcore.
