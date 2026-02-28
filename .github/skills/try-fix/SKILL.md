---
name: try-fix
description: Attempts ONE alternative fix for a bug in dotnet/aspnetcore, tests it empirically, and reports results. ALWAYS explores a DIFFERENT approach from existing PR fixes. Use when CI or an agent needs to try independent fix alternatives.
---

# Try Fix — Single Alternative Approach

Attempts ONE alternative fix for a bug, tests it, and reports the result.

> 🚨 ALWAYS explores a DIFFERENT approach from existing fixes and prior attempts.

## When to Use

- Called by the `fix-issue` skill during Phase 3 (multi-model exploration)
- Each invocation tries a single, unique approach
- Run SEQUENTIALLY — multiple try-fix invocations modify the same files

## Input Required

The caller must provide:
- **Problem description**: What the bug is
- **Test command**: How to verify the fix
- **Target files**: Which files contain the bug
- **Prior attempts**: List of approaches already tried (to avoid repeating)

## Workflow

### 1. Revert to Baseline

```bash
# If a fix is already applied, revert to original buggy code
git checkout HEAD~1 -- {fix-files}
# OR if working from clean main:
# Files are already at baseline
```

### 2. Implement Alternative Fix

- Read the target source files
- Understand the bug
- Design a DIFFERENT approach from all prior attempts
- Implement the fix

### 3. Run Tests

```bash
# .NET
source activate.sh
dotnet test {test-project} --filter "{filter}"

# Java
cd src/SignalR/clients/java/signalr && ./gradlew test
```

### 4. Save Results

```bash
mkdir -p CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/try-fix/attempt-{N}
```

Save these files:
- `approach.md` — Description of what was tried
- `result.txt` — "Pass" or "Fail" + test command output summary
- `fix.diff` — `git diff > fix.diff`
- `analysis.md` — Why it worked or failed (especially for failures)

### 5. Revert Changes

```bash
git checkout HEAD -- .
git clean -fd
```

## Output Format

```markdown
## Attempt {N} — {Model Name}

**Approach:** {one-line summary}
**Result:** ✅ Pass / ❌ Fail

### Details
{What was changed and why}

### Key Insight
{What this attempt revealed about the bug}
```

## Area Reference

| Area | Build Command | Test Discovery |
|------|--------------|----------------|
| Http | `./src/Http/build.sh -test` | `find src/Http -name "*Tests.csproj"` |
| Servers | `./src/Servers/build.sh -test` | `find src/Servers -name "*Tests.csproj"` |
| SignalR (.NET) | `./src/SignalR/build.sh -test` | `find src/SignalR -name "*Tests.csproj"` |
| SignalR (Java) | `cd src/SignalR/clients/java/signalr && ./gradlew test` | Test classes in `test/src/` |
| Components | `./src/Components/build.sh -test` | `find src/Components -name "*Tests.csproj"` |
| Mvc | `./src/Mvc/build.sh -test` | `find src/Mvc -name "*Tests.csproj"` |
| Security | `./src/Security/build.sh -test` | `find src/Security -name "*Tests.csproj"` |
