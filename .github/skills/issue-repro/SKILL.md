---
name: issue-repro
description: >-
  Reproduce a dotnet/aspnetcore bug systematically and capture structured
  reproduction results. Produces schema-validated JSON with step-by-step
  commands, outputs, environment details, and conclusion.
  Triggers: "repro #123", "reproduce #123", "reproduce issue", "try to reproduce",
  "can you reproduce", "repro this bug", "create reproduction".
---

# Bug Reproduction

**Bug pipeline: Step 2 of 2 (Repro).** See [`docs/repro.md`](../../docs/repro.md).

Systematically reproduce a dotnet/aspnetcore bug and produce structured, schema-validated reproduction JSON.

## ⛔ MANDATORY FIRST STEPS (do not skip)

1. Read THIS entire SKILL.md before any investigation
2. Read [references/schema-cheatsheet.md](references/schema-cheatsheet.md) for required fields and enums
3. Read [references/anti-patterns.md](references/anti-patterns.md) for critical rules

These 3 reads are REQUIRED. Do not proceed to Phase 1 until all three are loaded.

> **Quick flow:**
> 1. Load issue + any prior triage JSON
> 2. Read references: [schema-cheatsheet](references/schema-cheatsheet.md), [anti-patterns](references/anti-patterns.md)
> 3. Create brief plan (5-10 lines: strategy, platform, expected outcome)
> 4. Check environment: `dotnet --version`, `dotnet --info`
> 5. Build repro project and attempt reproduction
> 6. Test multiple ASP.NET Core versions (3A reporter's → 3B latest → 3C main)
> 7. Generate JSON → validate → write to `artifacts/ai/repro/{number}.json`

```
Phase 1 (Fetch) → Phase 2 (Assess) → Phase 3 (Reproduce) → Phase 4 (JSON + Output) → Phase 5 (Validate) → Phase 6 (Persist & Present)
```

---

## Phase 1 — Fetch Issue

```bash
mkdir -p /tmp/aspnetcore/repro
gh issue view {number} --repo dotnet/aspnetcore \
  --json number,title,body,labels,state,createdAt,author,comments,url \
  > /tmp/aspnetcore/repro/{number}-raw.json
cat /tmp/aspnetcore/repro/{number}-raw.json
```

**Triage boost** — if `artifacts/ai/triage/{number}.json` exists, load it:

```bash
cat artifacts/ai/triage/{number}.json 2>/dev/null
```

Extract from triage: `classification.area`, `evidence.bugSignals`, `analysis.nextQuestions` as **hints** (verify independently).

---

## Phase 2 — Assess & Plan

### 1. Classify the bug

Read [references/bug-categories.md](references/bug-categories.md) to classify the bug type.

### 2. Extract reporter's version & TFM

- `{reporter_dotnet}`: exact .NET version (e.g., `9.0.2`)
- `{reporter_tfm}`: target framework (e.g., `net9.0`)
- `{reporter_code}`: reproduction code from the issue
- Reporter's OS / environment

If not stated, use the latest stable release.

### 3. Environment check

**⚠️ Run `dotnet --info` in `/tmp/aspnetcore/repro/{number}/`** (NOT the aspnetcore repo, which has `global.json` pinning the SDK). Record SDK version and runtime version.

```bash
mkdir -p /tmp/aspnetcore/repro/{number}
cd /tmp/aspnetcore/repro/{number}
dotnet --info
```

Check: Docker (`docker --version`), available SDKs.

### 4. Determine reproduction approach

| Priority | Signals | Platform file |
|----------|---------|---------------|
| 1 | Blazor, WASM, browser error, WebAssembly | [platform-blazor-wasm.md](references/platform-blazor-wasm.md) |
| 2 | Minimal API, controller, routing, middleware | [platform-webapi.md](references/platform-webapi.md) |
| 3 | Docker, container, Linux-specific | [platform-docker-linux.md](references/platform-docker-linux.md) |
| 4 | (none / pure C# API) | [platform-console.md](references/platform-console.md) |

**Read the selected platform file.** Follow its Create → Build → Run → Verify steps.

### 5. Plan

Output a brief plan before executing (5-10 lines: what platform, what version, what approach).

---

## Key Rules (read before Phase 3)

Read [references/anti-patterns.md](references/anti-patterns.md) for the full list. Critical rules:

1. **Stop at "did it reproduce."** Root cause is the fix team's job.
2. **Editorial judgment in conclusion.** If the reported behavior occurred, it's `reproduced` — even if by-design.
3. **Never stop at build success.** Many bugs manifest at RUNTIME or during HTTP requests.
4. **Stale build artifacts.** Fresh project dirs or `rm -rf bin/ obj/` between versions.
5. **Honesty over completion.** `not-reproduced` and `needs-platform` are VALID SUCCESS conclusions.
6. **NEVER modify product source.** Do not edit files in `src/`. Repro creates NEW test projects in `/tmp/aspnetcore/repro/` only.
7. **NEVER use `store_memory`.** Reproduction produces JSON artifacts, not memories.
8. **NEVER skip validation.** You MUST run `validate-repro.ps1` (or `.py` fallback) and see ✅ before persisting.
9. **NEVER push.** No `git push` operations.

**Intermittent bugs:** If results are inconsistent, run 3–5 times. Reproduced ≥1 time → `reproduced` with note "Intermittent: X/Y runs".

**Effort budget:** Phases 1–2: ~5 min. Phase 3A: ~15–20 min. Phases 3B–3C: ~10–15 min. Total: ~30–50 min.

---

> **Pre-flight — confirm before reproducing:**
>
> - [ ] Issue data loaded via `gh`
> - [ ] Prior triage JSON loaded (if exists in `artifacts/ai/triage/`)
> - [ ] Read [references/schema-cheatsheet.md](references/schema-cheatsheet.md)
> - [ ] Read [references/anti-patterns.md](references/anti-patterns.md)
> - [ ] Environment checked: `dotnet --info` in temp dir
> - [ ] Created a brief plan (5-10 lines)
> - [ ] Never use `sudo`

---

## Phase 3 — Reproduce

> **Overview — you will test up to 3 configurations:**
> - **3A:** Reporter's version on primary platform *(always)*
> - **3B:** Latest stable release *(always)*
> - **3C:** Main branch source *(MANDATORY if 3B still reproduced)*
>
> **🛑 MINIMUM 2 VERSIONS REQUIRED.** You must test at least the reporter's version (3A) AND latest stable (3B).

### 3A. Reproduce with reporter's version

Follow the platform file from Phase 2.4. For each step, capture:

| Field | Limit |
|-------|-------|
| `command` | Exact command (redact paths) |
| `exitCode` | 0=success, non-zero=failure |
| `output` | 2KB success, 4KB failure |
| `filesCreated` | Filename + source code content for repro files |
| `layer` | `setup` / `csharp` / `hosting` / `middleware` / `http` / `deployment` |
| `result` | `success` / `failure` / `wrong-output` / `skip` |

**Step `result` = what actually happened** (technical outcome), not whether it was expected.

**Making HTTP requests:** Use `curl`, `dotnet-httprepl`, or a simple `HttpClient` test project. For ASP.NET Core apps, start the server in background and make requests:

```bash
dotnet run &
APP_PID=$!
sleep 2  # wait for startup
curl -s http://localhost:5000/endpoint
kill $APP_PID
```

**Push hard.** Don't bail early. Only conclude `not-reproduced` after exhausting approaches.

### 3B. Test on latest stable release

> **⚠️ Clean build required:** Create a fresh project directory per version (`/tmp/aspnetcore/repro/{number}-latest/`) or `rm -rf bin/ obj/` between versions.

Use the same platform strategy from 3A with the latest stable .NET / ASP.NET Core. Record in `versionResults`.

### 3C. Test on main branch (if reproduced on latest)

> **🛑 Do NOT skip when reproduced on latest.** If the bug reproduces on the latest stable release, testing main is MANDATORY — it tells us whether a fix exists but hasn't been released.

To test against main branch source, build and run the aspnetcore repo (if needed):

```bash
# Activate the local SDK from repo root first
REPO_ROOT=$(git rev-parse --show-toplevel)
source "$REPO_ROOT/activate.sh"

# Or point to daily builds from dev feed
dotnet new webapi -n Repro{number}-main
# Add Microsoft.AspNetCore.App from daily build NuGet feed if available
```

If main branch source builds are unavailable, test the latest preview package if one exists, and document the limitation.

### 3D. Cross-platform verification (conditional)

| Primary result | Run 3D? |
|----------------|---------|
| `reproduced` (platform-specific) | **Yes** — test alternative platform |
| `not-reproduced` + reporter on different platform | **Yes** |
| `reproduced` (pure API bug, no platform signals) | **Skip** — note why |
| `not-reproduced` + same platform | No |

**Time cap:** 5 minutes. **Default alternative:** Docker Linux x64.

---

## Phase 4 — Generate JSON

### 1. Choose conclusion

Read [references/conclusion-guide.md](references/conclusion-guide.md). Key question: did the reported behavior occur?

| Conclusion | When |
|------------|------|
| `reproduced` | Reported behavior occurred |
| `not-reproduced` | Reported behavior did not occur |
| `needs-platform` / `needs-hardware` | Requires unavailable platform/hardware |
| `partial` / `inconclusive` | Partial or ambiguous results |

### 2. Generate JSON

Write to `/tmp/aspnetcore/repro/{number}.json`. Run `mkdir -p /tmp/aspnetcore/repro` first.

Schema: [references/repro-schema.json](references/repro-schema.json). See [references/repro-examples.md](references/repro-examples.md) for full examples.

**Key rules:**
- **Optional fields: OMIT entirely** — do NOT set to `null`
- `environment.aspnetcoreVersion`: exact version tested
- `versionResults`: include `platform` field (e.g., `"host-macos-arm64"`, `"docker-linux-x64"`)
- Redact paths (`/Users/{name}/` → `$HOME/`), tokens, credentials.

### 3. Generate output (required for definitive conclusions)

When conclusion is `reproduced` or `not-reproduced`, generate the `output` object with actionability, actions, and a proposed response. Skip for blocked conclusions.

#### Choosing suggestedAction

| Scenario | suggestedAction | Confidence |
|----------|----------------|------------|
| Reproduced on all versions including latest/main | `needs-investigation` | 0.90+ |
| Reproduced on reporter's version, fixed on latest | `close-as-fixed` | 0.85+ |
| Reproduced on reporter's version, fixed on main | `keep-open` | 0.80+ |
| Not reproduced — likely environment/config issue | `request-info` | 0.70+ |
| Not reproduced — works on all tested versions | `close-as-fixed` | 0.75+ |

See [references/response-guidelines.md](references/response-guidelines.md) for proposed response templates.

---

## Phase 5 — Validate

> **🛑 PHASE GATE: You CANNOT persist without passing validation.**

```bash
pwsh .github/skills/issue-repro/scripts/validate-repro.ps1 /tmp/aspnetcore/repro/{number}.json \
  || python3 .github/skills/issue-repro/scripts/validate-repro.py /tmp/aspnetcore/repro/{number}.json
```

- **Exit 0** = ✅ valid → proceed to Phase 6
- **Exit 1** = ❌ fix errors, re-run. Repeat up to 3 times.
- **Exit 2** = fatal error, stop and report

> **⚠️ NEVER hand-roll your own validation. NEVER assume it passes. RUN THE SCRIPT.**

---

## Phase 6 — Persist & Present

### 1. Persist (only after validator prints ✅)

```bash
pwsh .github/skills/issue-repro/scripts/persist-repro.ps1 /tmp/aspnetcore/repro/{number}.json
```

This copies the JSON to `artifacts/ai/repro/{number}.json`. No git push.

### 2. Present summary

```
✅ Reproduction: artifacts/ai/repro/{number}.json

Conclusion:  reproduced
Steps:       4 (2 success, 1 failure, 1 skip)
Environment: macOS arm64, SDK 9.0.102

Version results:
  .NET 8.0.10 (reporter): ❌ REPRODUCED
  .NET 9.0.2 (latest):    ❌ REPRODUCED
  main (source):          ✅ not-reproduced
```
