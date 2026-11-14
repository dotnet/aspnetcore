---
name: issue-triage
description: >-
  Triage a dotnet/aspnetcore GitHub issue or PR into structured JSON with
  classification (type, area, feature, platform, severity), suggested response,
  and automatable actions.
  Triggers: "triage #123", "triage issue", "classify issue", "analyze issue",
  "what's this issue about", "label this issue". Also triggered when an issue
  number is given after the issue-triage skill is already mentioned.
---

# Triage Issue

**Bug pipeline: Step 1 of 2 (Triage).** See [`docs/repro.md`](../../docs/repro.md).

Analyze a dotnet/aspnetcore GitHub issue and produce a structured, schema-validated triage JSON.

## ⛔ MANDATORY FIRST STEPS (do not skip)

1. Read THIS entire SKILL.md before any investigation
2. Read [references/schema-cheatsheet.md](references/schema-cheatsheet.md) for required fields and enums
3. Read [references/anti-patterns.md](references/anti-patterns.md) for critical rules

These 3 reads are REQUIRED. Do not proceed to Phase 1 until all three are loaded.

> **Quick flow:**
> 1. Load issue data via `gh` CLI
> 2. Read references: [schema-cheatsheet](references/schema-cheatsheet.md), [labels](references/labels.md), [triage-examples](references/triage-examples.md), [anti-patterns](references/anti-patterns.md)
> 3. Create brief plan (5-10 lines)
> 4. Investigate code — **READ-ONLY, never edit source files**
> 5. Generate JSON → validate → write to `artifacts/ai/triage/{number}.json`

```
Phase 1 (Fetch) → Phase 2 (Preprocess + Investigate) → Phase 3 (Analyze) → Phase 3.5 (Workaround Search) → Phase 3.7 (Validate Code) → Phase 4 (Validate) → Phase 5 (Persist)
```

---

## Phase 1 — Fetch Issue

Fetch the issue using the GitHub CLI. No local cache — use `gh` directly:

```bash
gh issue view {number} --repo dotnet/aspnetcore \
  --json number,title,body,labels,state,createdAt,closedAt,author,comments,url
```

Save output to a temp file for convenience:

```bash
mkdir -p /tmp/aspnetcore/triage
gh issue view {number} --repo dotnet/aspnetcore \
  --json number,title,body,labels,state,createdAt,closedAt,author,comments,url \
  > /tmp/aspnetcore/triage/{number}-raw.json
cat /tmp/aspnetcore/triage/{number}-raw.json
```

**Duplicate search** — search for related issues:

```bash
gh search issues --repo dotnet/aspnetcore --state all --limit 10 \
  "KEYWORD1 KEYWORD2" --json number,title,labels,state
```

---

## Phase 2 — Preprocess

### 1. Read and annotate the issue

From the fetched JSON, extract:
- **Title and body**: exact text as reported
- **Labels**: current GitHub labels (goes into `meta.currentLabels`)
- **State**: open or closed
- **Comments**: maintainer responses, workaround mentions, confirmations
- **Environment info**: .NET version, ASP.NET Core version, OS, IDE
- **Code snippets**: anything in ` ``` ` blocks
- **Stack traces**: exception text and call stack

### 2. Code Investigation (MANDATORY)

> **Scope: READ code, don't WRITE code.** Grep, read files, trace call chains. Never create files, compile, or execute.

**Before ANY classification**, search the source code for the types, methods, APIs, or behaviors mentioned in the issue. Read the relevant files. Record every finding in `analysis.codeInvestigation` as `{file, finding, relevance}` (with optional `lines`).

**Do NOT classify until you have examined source code.** For bugs, include at least one `codeInvestigation` entry.

**Source layout guide:**

| Area | Source location |
|------|----------------|
| Blazor | `src/Components/` |
| MVC / Razor Pages | `src/Mvc/` |
| Minimal APIs | `src/Http/Routing/`, `src/Http/Http/` |
| Kestrel | `src/Servers/Kestrel/` |
| IIS / ANCM | `src/Servers/IIS/` |
| SignalR | `src/SignalR/` |
| Auth / Identity | `src/Security/`, `src/Identity/` |
| Data Protection | `src/DataProtection/` |
| gRPC | `src/Grpc/` |
| Health Checks | `src/HealthChecks/` |
| Middleware (general) | `src/Middleware/` |
| Hosting | `src/Hosting/` |
| OpenAPI | `src/OpenApi/` |

**Search steps:**
1. Grep for the types/methods/APIs mentioned in the issue
2. Read the relevant source files and trace call chains
3. Check if the reported behavior matches current code
4. For bugs: trace the code path — does the issue still exist?
5. For feature requests: has it been implemented since filing?
6. For questions: does the source confirm or contradict the assumption?

**Example searches:**

```bash
# Find where a type/method is implemented
grep -rn "TypeName\|MethodName" src/ --include="*.cs" -l

# Find middleware registration
grep -rn "UseXxx\|AddXxx" src/Http/ --include="*.cs" -l

# Find DI extension methods
grep -rn "services.Add" src/ --include="*.cs" -l

# Check recent changes (regressions)
git log --oneline -20 -- src/Blazor/
git log --oneline --after="2024-01-01" -- src/Mvc/ | head -20
```

### 3. Additional Research

| Signal in issue | Source to consult |
|----------------|-------------------|
| Regression claim | `git log --oneline -- {path}` to find recent changes |
| Already fixed? | `gh search issues "KEYWORD" --repo dotnet/aspnetcore --state closed` |
| Docs gap | `https://learn.microsoft.com/aspnet/core/` |
| Cross-repo dependency | Check `src/` for references to `Microsoft.Extensions.*` packages |

---

> **Pre-flight — confirm before analyzing:**
>
> - [ ] Issue data fetched with `gh`
> - [ ] Read [references/schema-cheatsheet.md](references/schema-cheatsheet.md)
> - [ ] Read [references/labels.md](references/labels.md)
> - [ ] Read [references/triage-examples.md](references/triage-examples.md)
> - [ ] Read [references/anti-patterns.md](references/anti-patterns.md)
> - [ ] Created a brief plan (5-10 lines)
>
> **Reminder:** Triage is READ-ONLY. Do NOT edit any source files (.cs, .csproj, .json).

---

## Phase 3 — Analyze

### First triage in session

Read [references/labels.md](references/labels.md) for valid label values and cardinality, [references/triage-examples.md](references/triage-examples.md) for calibration, and [references/schema-cheatsheet.md](references/schema-cheatsheet.md) for required fields.

### Classify and generate JSON

Write brief internal analysis (3–5 sentences), classify the type, then read [references/research-by-type.md](references/research-by-type.md) for type-specific research. Conduct the research, then generate the JSON.

Write to `/tmp/aspnetcore/triage/{number}.json` — use this exact literal path.

> **⚠️ Schema Compliance:**
>
> 1. Read [references/schema-cheatsheet.md](references/schema-cheatsheet.md) — authoritative source
> 2. Review [references/labels.md](references/labels.md) — use only valid label values
> 3. Critical constraints:
>    - `meta.schemaVersion` must be `"1.0"`, `meta.repo` must be `"dotnet/aspnetcore"`
>    - **Optional fields:** OMIT entirely if not applicable. Do NOT set to `null`.
>    - `analysis.codeInvestigation` is MANDATORY for bugs.
>    - No extra properties allowed (`additionalProperties: false`).

#### JSON Groups Overview

| Group | Content |
|-------|---------|
| `summary` | One-sentence description of the issue (top-level string, required). |
| `meta` | Version `"1.0"`, issue number, repo, `analyzedAt` (ISO 8601). |
| `classification` | `type` and `area` (required objects with confidence). `feature`, `platforms`, `tenets` optional. |
| `evidence` | `bugSignals` (for bugs), `reproEvidence` (attachments/links), `regression`, `fixStatus`. |
| `analysis` | `summary` (required), `codeInvestigation` (findings), `keySignals`, `rationale`, `resolution`. |
| `output` | `actionability` (suggested action) and `actions` (automatable tasks). |

---

## Phase 3.5 — Workaround Search

For bugs, questions, and feature requests: **actively search for workarounds** the reporter can use now. Follow [references/workaround-search.md](references/workaround-search.md).

- Set proposal `category` to `"workaround"` / `"fix"` / `"alternative"` / `"investigation"`
- Include `codeSnippet` on any proposal with copy-paste-ready code
- Set `validated` to `"untested"` initially

Skip for duplicates and abandoned issues (omit `analysis.resolution`).

---

## Phase 3.7 — Workaround Validation (conditional)

If any proposal contains fenced code blocks or ASP.NET Core API calls: validate with parallel agents.

**Gate:** No code blocks → skip (set `validated: "untested"`). ~60% of triages skip this step.

**Agents** (parallel `explore` type, Haiku model):
1. **API correctness** — do the types/methods exist with correct signatures in this .NET version?
2. **Behavioral correctness** — DI registration, middleware ordering, disposal, does it solve the problem?
3. **Platform safety** — will it work on the reporter's OS/runtime?

**Synthesis:** All pass → `validated: "yes"`. Any warn → add caveats, reduce confidence. Any fail → fix or strip code, set `validated: "no"`.

---

## Phase 4 — Validate

> **🛑 PHASE GATE: You CANNOT proceed to Phase 5 without passing validation.**
> **Skipping validation = INVALID triage. The task is incomplete.**

```bash
# Try pwsh first, fall back to python3
pwsh .github/skills/issue-triage/scripts/validate-triage.ps1 /tmp/aspnetcore/triage/{number}.json \
  || python3 .github/skills/issue-triage/scripts/validate-triage.py /tmp/aspnetcore/triage/{number}.json
```

- **Exit 0** = ✅ valid → proceed to Phase 5
- **Exit 1** = ❌ fix the errors listed, then re-run. Repeat up to 3 times.
- **Exit 2** = fatal error, stop and report

> **⚠️ NEVER hand-roll your own validation. NEVER assume it passes. RUN THE SCRIPT.**

---

## Phase 5 — Persist & Present

> **🛑 PHASE GATE: Phase 4 validator MUST have printed ✅ before you reach this step.**

### 1. Persist to artifacts

```bash
pwsh .github/skills/issue-triage/scripts/persist-triage.ps1 /tmp/aspnetcore/triage/{number}.json
```

This copies the JSON to `artifacts/ai/triage/{number}.json`. No git push — the file is left for human review.

### 2. Present summary

```
✅ Triage: artifacts/ai/triage/{number}.json

Type:     bug (0.95)       Area: area-blazor
Feature:  feature-blazor-component-model
Severity: high             Action: needs-investigation

Actions:
  labels-1   update-labels   [low]  Add bug, area-blazor, feature-blazor-component-model
  comment-1  add-comment     [high] ⚠️ requires human edit
```

**Pipeline hint:**
- If `classification.type.value == "bug"` and `output.actionability.suggestedAction == "needs-investigation"`: next step is **issue-repro** (`artifacts/ai/repro/{number}.json`).
- If repro already exists and reproduces: escalate to team for fix.

If `add-comment` exists, show `comment` in a copy-paste block. **⚠️ NEVER post via GitHub API.**

---

## Anti-Patterns

See [references/anti-patterns.md](references/anti-patterns.md) — **read this file on first triage in session**.

**#0 (CRITICAL):** Triage is READ-ONLY. If you edit a source file during triage, you have FAILED.

**#1 (CRITICAL):** NEVER use `store_memory` during triage. Triage produces JSON artifacts, not memories.

**#2 (CRITICAL):** NEVER skip the validation script. You MUST run `validate-triage.ps1` (or `.py` fallback) and see ✅ before persisting.

**#3 (CRITICAL):** NEVER push to any git remote. Output goes to `artifacts/ai/triage/` only.

---

## Scripts

- **`scripts/validate-triage.ps1 <triage.json>`** — Validate against schema + rationale coverage + action integrity
- **`scripts/persist-triage.ps1 <triage.json>`** — Copy to `artifacts/ai/triage/` (no git operations)
