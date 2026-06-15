---
name: code-review
description: Review code changes in dotnet/aspnetcore. Routes to area-specific reviewer agents in .github/agents/ when the touched files match an area; falls back to the general aspnetcore-expert-reviewer agent for unmatched areas. Use when reviewing PRs, checking code before submission, or asked to critique a change.
---

# dotnet/aspnetcore Code Review

This skill is a **dispatcher**. The actual review logic lives in the agents under `.github/agents/`. Each agent's frontmatter `description:` declares which areas it covers.

## Routing

For each file touched by the PR, find the best-matching agent:

| Area pattern | Agent |
|---|---|
| `src/Components/**`, `src/Razor/**`, `src/JSInterop/**` | `blazor-expert-reviewer` |
| _(other areas — Kestrel, SignalR, Identity, MVC, etc.)_ | `aspnetcore-expert-reviewer` (general aspnetcore conventions) |

When multiple agents apply (e.g., a PR touches both Components and a non-Blazor area), **launch them in parallel as sub-tasks** and aggregate their findings into a single PR comment.

If no specific agent matches and `aspnetcore-expert-reviewer` is unavailable, fall back to the general conventions in this file plus `.github/copilot-instructions.md`.

## What this skill provides directly

If you're invoking this skill from outside the `code-review` workflow (e.g., a developer asking "review this diff" from the CLI), apply these cross-cutting rules even when no area agent matches:

### Mindset

Be polite but very skeptical. Treat the PR description and linked issues as **claims to verify**, not facts to accept. Form your own assessment from the code first, then reconcile with the author's narrative.

### Process

1. **Gather context** — full diff, full source of every changed file (not just hunks), consumers/callers for any public-API change, sibling types, recent git history per file.
2. **Form an independent assessment** without reading the PR description.
3. **Reconcile with the PR narrative** — where they disagree, investigate; don't defer.
4. **Detect new public API surface** — changes to `*.PublicAPI.Shipped.txt` / `Unshipped.txt`, new `public` members under `Microsoft.AspNetCore.*` → API review board approval required. See the `assessing-breaking-changes` skill.
5. **Apply area agent + general conventions** — invoke matched agents in parallel; integrate findings.
6. **Post one consolidated comment** with the verdict.

### Output

Severity ladder:
- 🔴 **BLOCKING** — must fix before merge (bugs, API contract violations, missing required tests, public API in violation of conventions)
- ⚠️ **MAJOR** — should fix (perf regressions, missing validation, established-pattern violations)
- 💡 **MODERATE** — consider (style improvements, minor readability)
- 💭 **MINOR/NIT** — drop unless quick to address

Verdict: any BLOCKING → request changes; otherwise → comment. **Never approve** — the bot must not count as a PR approval.

Don't post LGTM-only comments. If you have nothing actionable to say, call `noop` instead.

## General aspnetcore conventions (apply when no per-area agent overrides)

These are extracted from `.github/copilot-instructions.md` and apply to every aspnetcore PR unless a per-area agent's rules override them.

### Language & style
- Latest C# (currently C# 13).
- File-scoped namespaces; single-line `using` directives.
- Newline before `{` of code blocks.
- `nameof` instead of string literals.
- XML doc comments for public APIs with `<example>` / `<code>` when applicable.

### Nullable reference types
- Variables non-nullable by default; check at entry points.
- `is null` / `is not null`, never `== null` / `!= null`.
- Trust C# null annotations — don't add redundant null checks.

### Testing
- **xUnit SDK v3** for all new tests.
- **No** "Arrange / Act / Assert" comments.
- **Moq** for mocking.
- Copy existing style in nearby files for test method names and capitalization.

### Build & test
- Activate locally-installed .NET first: `source activate.sh` (Linux/Mac) or `. ./activate.ps1` (Windows).
- Per-area build scripts: e.g., `./src/Http/build.sh -test`.

### Forbidden file changes (unless explicitly requested)
- `global.json`
- `package.json` / `package-lock.json`
- `NuGet.config`

### Per-area conventions
Check both `<area>/AGENTS.md` (the [canonical cross-agent convention](https://agents.md), nearest-wins) and `.github/instructions/*.instructions.md` (Copilot-specific shims auto-loaded via `applyTo:`). The aspnetcore pattern is a two-layer redirect — the `instructions.md` file delegates to the `AGENTS.md` for cross-agent reach. When proposing per-area guidance, prefer adding to the AGENTS.md.

## Pattern

This shim follows the pattern proven in `dotnet/msbuild` (`reviewing-msbuild-code` skill, ~8 lines, points at `expert-reviewer` agent). The skill is for **discoverability** (Copilot auto-loads it by `description:` match); the agent is the **source of truth** for review content.
