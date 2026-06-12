# Proposal: Agentic Workflow Onboarding for aspnetcore

This branch adds infrastructure to support ~10 external contributors (initially Blazor toolkit) and codifies the conventions reviewers would otherwise enforce by hand on later review rounds.

**This is a proposal — nothing is pushed.** Review locally; iterate; recompile lock files before push.

## What's added

### Cross-cutting infrastructure

| File | Source | Purpose |
|---|---|---|
| `.github/workflows/shared/pat_pool.md` | Verbatim from dotnet/runtime | Shared PAT-pool import. Used by all new agentic workflows so the `case()` expression isn't duplicated per file. |
| `.github/workflows/shared/pat_pool.README.md` | Verbatim from dotnet/runtime | Operator docs for the PAT pool. |
| `.github/workflows/validate-pat-pool.yml` | Verbatim from dotnet/runtime/vitals/sdk | Daily PAT health check. Fills the gap that aspnetcore/msbuild/roslyn share today (silent degradation when a PAT expires). |
| `.github/AGENTIC-WORKFLOWS.md` | New | One-page rubric for anyone adding a new gh-aw workflow. References dotnet/vitals tenets. |

### Code-review surface (Blazor-aware) — refined to msbuild's skill-as-shim pattern

| File | Source | Purpose |
|---|---|---|
| `.github/workflows/code-review.md` | Modeled on runtime's `code-review.md` (thin orchestrator) | Triggers on every PR open/sync; dispatches to area-specific reviewer agents in parallel; aggregates findings into a single review submission. |
| `.github/skills/code-review/SKILL.md` | Refined per msbuild's `reviewing-msbuild-code` pattern (skill-as-shim) | Thin dispatcher (~95 lines): routing table from PR area → reviewer agent + fallback general aspnetcore conventions when no agent matches. Source of truth for area-specific rules is the agent, not this skill. |
| `.github/agents/blazor-expert-reviewer.agent.md` | New, modeled on runtime's `extensions-reviewer.agent.md` + msbuild's `expert-reviewer.agent.md` | **The centerpiece.** 12 principles + 12 review dimensions + 4-wave workflow (Discover → Validate → Post → Summary). Validates findings before posting (msbuild pattern). Posts inline review comments via `create_pull_request_review_comment` for diff-line findings, `add_comment` for design-level. Severity ladder: BLOCKING / MAJOR / MODERATE / MINOR. Never uses APPROVE. |

### Test-contract enforcement

| File | Source | Purpose |
|---|---|---|
| `.github/workflows/evaluate-pr-tests.md` | Modeled on MAUI's `copilot-evaluate-tests.md` | Slash-command + workflow_dispatch trigger. Pre-gates on "no test files in diff = exit early". |
| `.github/skills/evaluate-pr-tests/SKILL.md` | New, MAUI-pattern | Rubric (8 dimensions) for assessing test quality. Includes Blazor-specific dimensions for render-mode coverage and pre-rendering. |
| `.github/skills/verify-tests-fail-without-fix/SKILL.md` | New, MAUI-pattern | The TDD-discipline enforcement skill: revert production change, rerun tests, verify they fail. Strongest signal that a regression test is real. |

### Compatibility & breaking-change discipline (NEW — from msbuild deep-dive)

| File | Source | Purpose |
|---|---|---|
| `.github/skills/assessing-breaking-changes/SKILL.md` | Adapted from msbuild's same-named skill | Blast-radius checklist, deprecation protocol, render-mode parity compat dimension, wire-compat considerations (SignalR/DataProtection/Antiforgery/Auth/PersistentComponentState), WASM trim-safety as compat dimension.

### Skill validation (NEW — from msbuild deep-dive, copied verbatim)

| File | Source | Purpose |
|---|---|---|
| `.github/workflows/skill-validation.yml` | Verbatim from dotnet/msbuild | Validates skills + agents on every PR touching `.github/skills/**` or `.github/agents/**`. Downloads `skill-validator` binary from dotnet/skills releases, caches daily. Smart-filters to only validate changed skills. |
| `.github/workflows/skill-validation-comment.yml` | Verbatim from dotnet/msbuild | Paired `workflow_run`-triggered companion that posts results as a PR comment. Safe for fork PRs because it never checks out PR code. |

### Self-improving guidance

| File | Source | Purpose |
|---|---|---|
| `.github/workflows/learn-from-pr.md` | Modeled on MAUI | Manual-dispatch (`workflow_dispatch`); analyzes a merged PR. `apply: false` = report only; `apply: true` = open draft PR with guidance updates. |
| `.github/skills/learn-from-pr/SKILL.md` | New, MAUI-pattern | Analysis-only: extracts lessons, classifies by destination, prioritizes High/Medium/Low. |
| `.github/agents/learn-from-pr.agent.md` | New, MAUI-pattern | Apply-mode: takes the skill's High/Medium recommendations and produces a draft PR with edits to `.github/instructions/`, `.github/copilot-instructions.md`, etc. **Highest-compounding-leverage workflow for onboarding** — every Blazor PR landed makes the next contributor's experience better. |

## What's NOT changed (intentional)

- `.github/copilot/settings.json` — kept as-is (still imports dotnet-arcade-skills + dotnet-skills + dotnet-test plugins)
- `.github/copilot-instructions.md` — kept as-is (the `learn-from-pr` agent will extend it over time)
- Existing agentic workflows (`community-pr-issue-check.md`, `issue-triage-agent.md`, `cswin32-update.md`, `browsertesting-deps-update.md`, `test-quarantine.md`) — kept as-is. Augmentation to `community-pr-issue-check.md` (e.g., suggest area-blazor labels, check API review) is a separate follow-up.
- `.lock.yml` files — NOT included. Must be regenerated with `gh aw compile` before push.


## Required before pushing

1. **Compile lock files**:
   ```bash
   gh aw compile code-review evaluate-pr-tests learn-from-pr
   git add .github/workflows/*.lock.yml .github/aw/actions-lock.json
   ```
2. **Confirm PAT pool secrets exist in the repo** (`COPILOT_PAT_0` through `COPILOT_PAT_9`, or at least one). The `validate-pat-pool.yml` workflow will report any that are invalid/empty.
3. **Decide on auto-enable for `blazor-expert-reviewer`**: the agent has `description:` set so Copilot will auto-load it when relevant. Confirm desired behavior; if the agent should be opt-in only, add `disable-model-invocation: true` to its frontmatter.
4. **Choose Copilot vs Claude per workflow**: the code-review workflow defaults to `claude-opus-4.6` (matching runtime). Adjust if aspnetcore prefers a different model.
5. **Test in fork first**: dispatch each new workflow manually with `dry_run: true` (or equivalent) in a fork before merging.

## Reference implementations consulted

- **dotnet/runtime**: thin-orchestrator + per-area reviewer pattern (`code-review.md`, `extensions-reviewer.agent.md`, `system-net-review.agent.md`); shared PAT-pool import (`shared/pat_pool.md`); CI failure scanner with KBE pattern.
- **dotnet/maui**: full PR-lifecycle skills (`learn-from-pr`, `verify-tests-fail-without-fix`, `evaluate-pr-tests`, `write-tests-agent` router); `.github/instructions/*.instructions.md` per-area guidance pattern.
- **dotnet/vitals**: tenets (`docs/chapters/tenets.md`), validate-pat-pool.yml; safe-outputs discipline.

## Two-track summary (from the cross-org review)

This branch addresses **Track 2** (aspnetcore contributor onboarding) of the larger two-track review. **Track 1** (VMR QB load reduction) is addressed in dotnet/dotnet PR #6192 + my proposed follow-ups (`failure-router`, `find-mergeable-codeflow-prs`).

## Open questions for the maintainer

1. Should `code-review.md` post on **every** PR push (current) or only on PR open + opt-in slash command for re-review?
2. Should `blazor-expert-reviewer` be auto-loaded (current — has `description:`) or opt-in via `disable-model-invocation: true`?
3. The `learn-from-pr` workflow currently restricts edits to `.github/` only. Should it also allow edits to per-area `AGENTS.md` files alongside source code?
4. Is the `claude-opus-4.6` model choice acceptable, or does aspnetcore prefer `copilot` engine default?

## Corrections applied (after cross-check against cerebro + aspnetcore source)

The first commit on this branch contained three test-infrastructure inaccuracies that have been corrected in a follow-up commit (cross-checked against the actual `dotnet/aspnetcore` source tree and the `Blazor-Playground/cerebro` Blazor skills library):

| Original claim (wrong) | Corrected claim (verified) |
|---|---|
| "bUnit for Razor component logic" | **TestRenderer pattern** from `src/Components/Shared/test/`, brought in via `<Compile Include="$(ComponentsSharedSourceRoot)test\**\*.cs" LinkBase="Helpers" />`. bUnit is not used anywhere in aspnetcore source. |
| "Playwright (preferred) or Selenium for E2E" | **Selenium** is the incumbent (`src/Components/test/E2ETest/` imports `$(SharedSourceRoot)E2ETesting\E2ETesting.props`); for **new** E2E projects/surfaces, **prefer Playwright** (reference pattern in `src/ProjectTemplates/test/Templates.Blazor.Tests/`). Don't mix frameworks within an existing Selenium project. |
| "Tests run under both render modes (use bUnit's render-mode parameterization)" | TestRenderer is in-process and does **not** model render modes. Render-mode parity is proven at the E2E level (`ServerExecutionTests/`, `ServerRenderingTests/`). |
| "PRs touching `src/Components/**` must follow `src/Components/AGENTS.md`" | `src/Components/AGENTS.md` does exist — I was wrong in commit 2. (My local fork was behind upstream.) AGENTS.md is the [canonical cross-agent convention](https://agents.md) used by 60k+ projects, including aspnetcore (2 files: `src/Components/AGENTS.md`, `eng/common/AGENTS.md`). The Copilot-specific `instructions.md` is a thin shim that delegates to the AGENTS.md. Commit 4 restores the AGENTS.md guidance and explains the two-layer pattern. |
