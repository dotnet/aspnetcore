---
name: verify-tests
description: Verifies unit tests catch the bug in dotnet/aspnetcore. Two modes - verify failure only (test creation) or full verification (test + fix validation). Use when asked to "verify tests", "check if tests catch the bug", or after writing tests.
---

# Verify Tests Catch the Bug

Verifies that unit tests correctly reproduce a bug and that a fix resolves it.

## Two Modes

### Mode 1: Verify Failure Only (after test creation)

Confirms tests FAIL against current buggy code.

```bash
source activate.sh
dotnet test {test-project} --filter "{filter}"
# Expected: FAIL (tests reproduce the bug)
```

### Mode 2: Full Verification (test + fix)

Confirms tests FAIL without fix AND PASS with fix.

```bash
# Step 1: Revert fix, keep tests
git stash push -- {fix-files}

# Step 2: Run tests — should FAIL
dotnet test {test-project} --filter "{filter}"

# Step 3: Restore fix
git stash pop

# Step 4: Run tests — should PASS
dotnet test {test-project} --filter "{filter}"
```

## Gate Result

- ✅ **PASS**: Tests fail without fix AND pass with fix
- ❌ **FAIL**: Tests pass without fix (don't catch bug) OR fail with fix
- ⚠️ **SKIP**: Build environment not available

## For Java Projects

```bash
cd src/SignalR/clients/java/signalr && ./gradlew test
```
