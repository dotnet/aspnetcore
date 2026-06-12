# Agentic Workflows in aspnetcore

One-page rubric for anyone adding or reviewing a `.github/workflows/*.md` (gh-aw) workflow in this repo.

## Decision tree: Skill vs Agent vs Workflow

| Form | Lives in | Purpose | Use when |
|---|---|---|---|
| **Skill** | `.github/skills/<name>/SKILL.md` | Codified procedure invokable by humans-with-Copilot AND by workflows | Logic that is reusable, advisory, or composable. **Default choice — start here.** |
| **Agent** | `.github/agents/<name>.agent.md` | Specialized persona auto-loaded by Copilot when its `description:` matches | Domain-specific reviewer/router (e.g., `blazor-expert-reviewer`); autonomous behavior that *applies* changes (e.g., `learn-from-pr`) |
| **Workflow** | `.github/workflows/<name>.md` | Schedule, trigger, or composition | When something must run on a cron, a PR event, or a slash command. Should be **thin** (60–200 lines). All real logic in skills/agents. |

**Rule of thumb**: write a skill first. A workflow is just the dispatch mechanism. An agent is a skill with a persona and the ability to apply changes.

## Tenets (paraphrased from .NET org internal practice — primary source dotnet/vitals, an internal-only reference repo)

The original Field Guide lives in the internal `dotnet/vitals` repository and is not publicly accessible. The tenets below are paraphrased from those internal patterns; they stand on their own as the rubric for new agentic workflows in this repo. Internal Microsoft contributors can consult the source Field Guide directly; external contributors should treat this document as authoritative.

1. **Verify against current gh-aw docs** — gh-aw releases multiple times a day. Patterns learned from assistants, memories, or this file must be cross-checked at <https://gh.io/gh-aw>.
2. **Least privilege on every dimension** — `permissions:`, `safe-outputs:`, `network.allowed:`, `tools:`, secrets. Default to minimum; opt in to more only with justification.
3. **Human-in-the-loop for consequential actions** — writes that affect contributors, security-sensitive areas, or release artifacts require a human gate. Bots draft; humans send.
4. **Deterministic by default** — pre-collect data in bash, gate cheaply, hand structured JSON to the agent. Use agents only when input is unstructured or the decision space is open.
5. **Untrusted input goes inside the agent sandbox** — never execute fork-author code in pre- or post-agent steps where secrets are reachable. The agent sandbox is the security boundary.
6. **Eliminate alert fatigue** — no daily "nothing happened" issues, no comments on every PR push. Use `noop: report-as-issue: false`, `hide-older-comments: true`, `max:` caps everywhere.
7. **Pinned dashboard issues over per-event noise** — for periodic monitoring, update one pinned issue; never spawn one issue per finding per day.
8. **Never present bot actions as human actions** — bot is the visible actor; the responsible human is captured separately (co-author trailer, comment metadata).
9. **Resist building a workflow-management platform** — every layer of meta-orchestration on top of gh-aw becomes a product you maintain. Prefer simple compositions.
10. **Document the trigger's executed-to-filtered ratio** — convenience triggers (slash commands, `pull_request: types: [labeled]`) compile to broad subscriptions. Estimate worst-case invocation rate; keep activation cheap.
11. **Filter outside the agent job** — keep skip/branch logic in steps that run before the agent. Refactor agent jobs toward more deterministic pre-steps over time.

## Required structure

Every new agentic workflow MUST have:

- ✅ `imports: - shared/pat_pool.md` (don't duplicate the PAT pool `case()` expression per workflow)
- ✅ `safe-outputs:` with `max:` caps on every output type
- ✅ `noop: report-as-issue: false` (no junk issues for empty runs)
- ✅ `if: !(github.event_name == 'schedule' && github.event.repository.fork)` (fork protection)
- ✅ `permissions:` set to read-only by default; writes only via `safe-outputs`
- ✅ Frontmatter `description:` that explains exactly when the workflow should fire (this is what `disable-model-invocation: false` agents auto-load on)

## Required tests before merge

For any new agentic workflow PR:

1. **Compile**: run `gh aw compile <workflow-id>` and commit the `.lock.yml` + `.github/aw/actions-lock.json` delta.
2. **Dry-run dispatch**: trigger via `workflow_dispatch` with `dry-run: true` (every workflow must support this) and verify the prompt logs but no side-effects fire.
3. **PAT pool**: ensure `.github/workflows/validate-pat-pool.yml` is present and green in the last 24h.
4. **Tenet review**: re-read the tenets above. Specifically check tenet 2 (least privilege) and tenet 3 (human-in-the-loop).

## See also

- [agents.md](https://agents.md) — the canonical AGENTS.md cross-agent convention (Linux Foundation, 60k+ projects). aspnetcore already uses it for `src/Components/AGENTS.md` and `eng/common/AGENTS.md`.
- [.github/workflows/shared/pat_pool.README.md](workflows/shared/pat_pool.README.md) — PAT pool implementation details (local to this repo)
- [GitHub Agentic Workflows reference](https://gh.io/gh-aw) — primary public reference for gh-aw syntax, security architecture, triggers, and engine configuration.
- The `dotnet/vitals` Field Guide is the internal-only source for many of the patterns referenced in this document. Microsoft contributors with access can consult it directly for design rationale; external contributors should rely on the tenets and required-structure sections above plus the gh-aw public reference.
- Reference implementations to copy (all public):
  - **Thin orchestrator → skill** pattern: [dotnet/runtime/.github/workflows/code-review.md](https://github.com/dotnet/runtime/blob/main/.github/workflows/code-review.md)
  - **Per-area reviewer agent**: [dotnet/runtime/.github/agents/extensions-reviewer.agent.md](https://github.com/dotnet/runtime/blob/main/.github/agents/extensions-reviewer.agent.md)
  - **Skill-as-shim + 4-wave review**: [dotnet/msbuild/.github/agents/expert-reviewer.agent.md](https://github.com/dotnet/msbuild/blob/main/.github/agents/expert-reviewer.agent.md) and [dotnet/msbuild/.github/skills/reviewing-msbuild-code/SKILL.md](https://github.com/dotnet/msbuild/blob/main/.github/skills/reviewing-msbuild-code/SKILL.md)
  - **Self-improving instructions**: [dotnet/maui/.github/agents/learn-from-pr.agent.md](https://github.com/dotnet/maui/blob/main/.github/agents/learn-from-pr.agent.md)
  - **CI failure scanner with KBE pattern**: [dotnet/runtime/.github/workflows/ci-failure-scan.md](https://github.com/dotnet/runtime/blob/main/.github/workflows/ci-failure-scan.md)
  - **Comprehensive root AGENTS.md**: [dotnet/msbuild/AGENTS.md](https://github.com/dotnet/msbuild/blob/main/AGENTS.md)
