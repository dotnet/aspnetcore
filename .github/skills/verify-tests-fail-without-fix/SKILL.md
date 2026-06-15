---
name: verify-tests-fail-without-fix
description: Verifies that tests added in a PR actually fail without the accompanying fix — proving the test exercises the bug. Use when evaluating bug-fix PRs, when writing new regression tests, or when the evaluate-pr-tests skill needs concrete verification.
---

# Verify Tests Fail Without Fix

This skill confirms that tests added in a bug-fix PR **fail when the production change is reverted**. A test that passes both with and without the fix is not a regression test — it's a false-positive that locks in whatever behavior happens to be present.

**TDD discipline reminder:** the canonical sequence is

1. Write the test.
2. Run it. **It must fail.** (Confirms the test exercises the bug.)
3. Apply the fix.
4. Run the test again. **It must pass.** (Confirms the fix works.)

This skill automates step 2 retrospectively for PRs that include both the test and the fix together.

## When to Use

- Evaluating a bug-fix PR that includes new tests
- After writing a regression test, before submitting the PR
- Invoked by `evaluate-pr-tests` skill as the strongest signal of test adequacy

## When NOT to Use

- New-feature PRs where tests assert positive behavior (no prior state to "fail against")
- API-shape PRs that add public surface without behavior change
- Refactoring PRs where tests should pass before and after
- PRs that only modify documentation

## Process

### Step 1: Identify the boundary

The skill needs two checkouts:
1. **Tip of PR branch** — contains both fix and tests; tests should **pass**.
2. **Tip of PR branch with the fix reverted** — contains tests only; tests should **fail**.

Run from the PR branch:

```bash
# Identify the test files added in the PR
PR_BRANCH=$(git rev-parse HEAD)
TARGET=$(git merge-base HEAD origin/main)
ADDED_TEST_FILES=$(git diff --name-only --diff-filter=A "$TARGET..$PR_BRANCH" | grep -E '(test/|tests/|TestCases|UnitTests|E2ETest)' | grep -E '\.(cs|razor)$')

# Identify the production files modified in the PR
MODIFIED_PROD_FILES=$(git diff --name-only --diff-filter=M "$TARGET..$PR_BRANCH" \
  | grep -E '\.(cs|razor|json|csproj|targets|props)$' \
  | grep -v -E '(test/|tests/|TestCases|UnitTests|E2ETest)')
```

If `ADDED_TEST_FILES` is empty, this skill is not applicable — exit with a clear message.

### Step 2: Stash the test files

Keep the test files from the PR but revert the production changes:

```bash
# Save test files
mkdir -p /tmp/pr-tests
for f in $ADDED_TEST_FILES; do
  mkdir -p "/tmp/pr-tests/$(dirname $f)"
  cp "$f" "/tmp/pr-tests/$f"
done

# Revert production files to the merge-base
for f in $MODIFIED_PROD_FILES; do
  git checkout "$TARGET" -- "$f"
done

# Restore test files
for f in $ADDED_TEST_FILES; do
  cp "/tmp/pr-tests/$f" "$f"
done
```

### Step 3: Build & run

Activate the local .NET environment first (per aspnetcore convention):

```bash
source activate.sh
```

Then run only the new tests, scoped to the relevant project:

```bash
# Identify the test project(s) containing the added test files
TEST_PROJECTS=$(for f in $ADDED_TEST_FILES; do
  dirname "$f" | xargs -I{} find {} -maxdepth 5 -name "*.csproj" | head -1
done | sort -u)

# Run tests, filtered to the new test methods if possible
for proj in $TEST_PROJECTS; do
  proj_dir=$(dirname "$proj")
  # Use the per-area build script if available
  if [ -f "${proj_dir%test*}build.sh" ]; then
    "${proj_dir%test*}build.sh" -test --filter "FullyQualifiedName~$(...derived from added test names...)"
  else
    dotnet test "$proj" --filter "FullyQualifiedName~..."
  fi
done
```

### Step 4: Interpret results

| Outcome | Meaning | Action |
|---|---|---|
| **All new tests FAIL** | Tests exercise the change correctly. | ✅ Verified — report success. |
| **Any new test PASSES** | That test does not exercise the fix — it would have passed against the buggy code. | ❌ Report which test(s) and ask the author to either tighten the assertion or remove the test. |
| **Build fails** | The production revert broke compilation; tests can't run. | ⚠️ Report — manual review needed. May indicate the test depends on new API surface, which itself is a valid signal. |
| **Test errors (not failures)** | Setup code throws; test never runs. | ⚠️ Report — usually means the test depends on the new API; treat as ✅ if the dependency is the new behavior. |

### Step 5: Clean up

Restore the working tree to the PR tip:

```bash
git checkout HEAD -- $MODIFIED_PROD_FILES
```

(Or: `git stash pop` if you stashed instead of `cp`.)

### Step 6: Report

```markdown
## 🔬 Tests-Fail-Without-Fix Verification

**Verdict:** ✅ All N new tests fail without the production change / ❌ M of N new tests pass without the fix.

### Details

| Test | Without fix | With fix | Verified? |
|---|---|---|---|
| `MyClass.Foo_WhenX_ShouldY` | ❌ Failed (expected) | ✅ Passed | ✅ Yes — exercises the change |
| `MyClass.Foo_TrivialCheck` | ✅ Passed | ✅ Passed | ❌ No — assertion is too loose, suggest tightening |

### Suggested actions

- Tighten the assertion in `MyClass.Foo_TrivialCheck` to fail against the original buggy behavior (e.g., assert on the specific value instead of "not null").
- ...
```

## Limits & caveats

- **Doesn't work for API-shape changes** — reverting the production change leaves test references to types/members that don't exist. The build failure is itself a signal but isn't the failure mode this skill is checking for.
- **Doesn't work for asynchronous/flaky tests** — a test that "sometimes" passes without the fix is still a defect of the test, but a single run may not reveal it. Run flaky tests N times if suspect.
- **Doesn't work for tests that depend on data setup that requires the production change** — e.g., a new migration that creates a column the test reads. Treat build failures from missing types/members as a third valid outcome ("test depends on new code, behavior verification not applicable").
- **Doesn't scale to PRs with >10 new tests** — the round-trip cost of building twice is real. Consider sampling.

## Required environment

- Git checkout with both PR and main accessible
- Activated .NET environment (per `source activate.sh`)
- The build script for the test's area (e.g., `src/Components/build.sh -test`)

## Integration

This skill is invoked by:
- `evaluate-pr-tests` (cited as the strongest signal in its rubric)
- `write-tests-agent` style workflows (to verify tests authored by Copilot fail correctly before reporting success)
- Direct human invocation from the CLI when writing regression tests
