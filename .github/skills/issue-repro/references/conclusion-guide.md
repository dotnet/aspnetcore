# Conclusion Guide

How to choose the correct `conclusion` value for a bug reproduction attempt.

## ⚠️ Critical Principle: Factual vs Editorial

**Reproduction is FACTUAL, not editorial.**

| Question | Type | Who Decides |
|----------|------|-------------|
| "Did the reported behavior occur?" | **Factual** | You (the reproducer) |
| "Is this behavior a defect?" | **Editorial** | Maintainers |
| "Should this be fixed?" | **Editorial** | Maintainers |

**Your job is ONLY to answer the factual question.** If the reporter says "I get 500 error" and you get 500 error, that's `reproduced` — even if the 500 is intentional or by-design.

---

## Decision Flowchart

```
Did the REPORTED BEHAVIOR occur?
├─ Yes, observed what reporter described → reproduced
│  (500 error, exception, wrong response, missing output — all are "reproduced")
├─ No, behavior differs from report → not-reproduced
├─ Couldn't run at all
│  ├─ Missing platform/OS? → needs-platform
│  ├─ Missing hardware? → needs-hardware
│  └─ Missing info/assets? → partial or inconclusive
└─ Some aspects matched, some didn't → partial
```

---

## Conclusion Values

### `reproduced`

**The reported behavior was observed.** Covers: HTTP errors, exceptions, wrong JSON response, missing headers, startup failure, incorrect routing, rendering issues.

- **Required evidence:** ≥1 reproduction step with `result: "failure"` or `result: "wrong-output"`
- **Use when:** the behavior the reporter described actually occurred

**⚠️ "By Design" is still `reproduced`.**
If reporter says "UseAuthorization returns 401 when called before UseAuthentication" and you verify that — `reproduced`. Put editorial assessment in `notes`.

### `not-reproduced`

**The reported behavior was NOT observed.** All steps ran differently than described.

- **Required evidence:** ≥1 step with `result: "success"`, AND ALL steps must be `success` or `skip`
- **Common reasons:** already fixed in current version, environment difference, missing repro details

**Important:** Always document what WAS observed so humans can evaluate the attempt.

### `needs-platform`

Cannot reproduce because the required platform is not available.

- **Before concluding:** Try Docker for Linux scenarios. Only use `needs-platform` if Docker can't help (e.g., Windows GUI, iOS).
- **Examples:**
  - `"Bug reported on Windows with IIS — Docker cannot run IIS"`
  - `"Requires macOS for Safari-specific WASM behavior"`

### `needs-hardware`

Cannot reproduce because specific hardware is required.

- **Examples:**
  - `"Requires a physical Android device — emulator behavior differs"`

### `partial`

Some aspects reproduced, others could not be verified.

- **Required:** populate `blockers[]` with specific reasons
- **Example:** `"HTTP 500 reproduced but exact stack trace differs — missing reporter's custom middleware"`

### `inconclusive`

Cannot determine whether the bug exists. Results are ambiguous.

- **Last resort.** Prefer any other conclusion when possible.
- **Common reasons:** issue description too vague, behavior intermittent and inconsistent across runs

---

## Supporting Fields

### `assessment` (optional)

An **editorial** classification — what you think is happening, without corrupting the factual `conclusion`.

| Value | When |
|-------|------|
| `unknown` | Cannot determine |
| `likely-bug` | Behavior seems unintentional |
| `working-as-designed` | Behavior is intentional |
| `breaking-change` | Behavior changed intentionally between versions |
| `docs-gap` | Code works but docs don't explain it well |
| `user-error` | Incorrect usage, not a framework defect |

### `notes`

Write 1–3 sentences:
1. What you did (the approach)
2. What you observed (the result)
3. Any caveats or editorial observations

### `blockers[]`

Specific reasons reproduction was incomplete. Each entry should tell a human exactly what's missing.

**Good:** `"Requires Windows — cannot test IIS integration from macOS"`
**Bad:** `"Couldn't reproduce"` (too vague)

---

## Scope Derivation

After cross-platform verification, set the `scope` field:

| Results | `scope` |
|---------|---------|
| Reproduced on ≥2 platforms | `"universal"` |
| Reproduced on primary only | `"platform-specific/{platform}"` |
| Only tested one platform | `"unknown"` |
| Phase 3D skipped | `"unknown"` |

**Platform names for scope:** `windows`, `linux`, `macos`, `wasm`, `ios`, `android`
