---
name: evaluate-pr-tests
description: Evaluates the quality, coverage, and appropriateness of tests added or modified in a dotnet/aspnetcore PR. Use when reviewing a PR's test suite, when contributors ask "are my tests good enough?", or when invoked from the evaluate-pr-tests workflow.
---

# Evaluate PR Tests

This skill assesses whether the tests in a PR are **adequate** for the behavior change. The goal is not "do tests exist?" — it's "do these tests prove the change works and would catch a regression?"

**Reviewer mindset:** A passing test does not prove correctness. A test that passes both with and without the fix is worse than no test — it gives false confidence and locks in incorrect behavior.

## When to Use

- Reviewing a PR that adds or modifies test files
- A contributor asks "did I write enough tests?"
- Invoked by the `evaluate-pr-tests` workflow (slash command or dispatch)

## What to Evaluate

For each test added or modified in the PR, assess against the rubric below. Score each dimension; produce an overall verdict.

### Rubric

> **Lead with substantive value, not rubric compliance.** A test that scores ✅ on every dimension below but duplicates pre-existing coverage or asserts a non-existent invariant adds *negative* value to the codebase. Always answer "what does this test exercise that wasn't covered before, and does its assertion match what the production code actually does?" before scoring style/convention dimensions.

| Dimension | ❌ Insufficient | ⚠️ Needs improvement | ✅ Adequate |
|---|---|---|---|
| **D0: Substantive value (apply first, weighted highest)** | Test duplicates pre-existing coverage; or asserts behavior the production code doesn't actually implement; or asserts a tautology (literal constant, type identity) | Test covers a real path but the marginal coverage over existing tests is minimal | Test covers a previously-uncovered branch, parameter combination, or interaction; assertion matches what the source actually does |
| **D1: Does the test exercise the change?** (bug-fix PRs only) | Test passes identically with the change reverted | Test fails without the change, but only on one obvious path | Test fails without the change, exercises the changed condition specifically |
| **D2: Is the right test framework used?** | xUnit v2 / NUnit / MSTest used where v3 is the convention; component unit logic using a browser-based framework | E2E test used for what's clearly a unit-level scenario | Component unit logic → **TestRenderer pattern** (shared infra from `src/Components/Shared/test/`); E2E → **Selenium** under existing `src/Components/test/E2ETest/`, or **Playwright** for new E2E projects (preferred for greenfield); general unit → xUnit v3 |
| **D3: Are edge cases covered?** | Only the happy path | Happy path + one trivial error case | Boundaries (null, empty, max), error paths, concurrent access where relevant |
| **D4: Are assertions specific?** | `Assert.NotNull(result)` only; tautological | Some specific assertions, mostly type-checks | Semantically meaningful — values, behavior, side-effects on collaborators |
| **D5: Is test isolation correct?** | Shared mutable state between tests; ordering-dependent | Some shared fixtures used inappropriately | Each test sets up its own state; uses `IClassFixture<T>` / `ICollectionFixture<T>` correctly |
| **D6: Does the test reflect aspnetcore conventions?** | "Arrange/Act/Assert" comments present; uses `== null`; explicit Moq.Setup chains that should use newer APIs | Mixed | Follows `.github/copilot-instructions.md` and nearby file style |
| **D7: For Blazor PRs — render mode coverage** | Component supports multiple render modes but no E2E coverage exercises both | Only one render mode covered when the change is render-mode-sensitive | E2E tests under `src/Components/test/E2ETest/` cover the relevant render modes (TestRenderer unit tests do not model render modes — render-mode parity must be proven at the E2E level) |
| **D8: For Blazor PRs — pre-rendering** | Pre-render path not tested when component fetches data | Pre-render tested but `PersistentComponentState` not verified | Pre-render → interactive transition covered with no double-fetch |
| **D9: Consolidation opportunities** | 5+ near-identical `[Fact]` tests differing only in input value | 2-4 `[Fact]` tests that could be one `[Theory]` | Appropriate use of `[Theory]`/`[InlineData]` for parameter sweeps; distinct `[Fact]` only for distinct scenarios |

### Severity rules

- **Any ❌ on D0** (substantive value) → overall verdict is `❌ Tests are insufficient`. A duplicate or wrong-invariant test makes the PR worse, not better.
- **Any ❌ on D1** (test doesn't exercise the change, for bug-fix PRs) → `❌ Tests are insufficient`.
- **Multiple ⚠️** without ❌ → `⚠️ Tests need improvement`.
- **All dimensions ✅ or N/A** → `✅ Tests are adequate`.

### Assertion fabrication — the most important check

Before scoring any test ✅ on D4, **read the production code the test claims to exercise** and verify the assertion describes what the source actually does. Common failure modes:

1. **Cargo-cult invariant**: assertion locks in a behavior from a different framework or system (e.g., "MVC behavior") that the Blazor/aspnetcore code doesn't implement. The test passes only because of how the test enumerates state, not because of the framework's behavior.
2. **Convenient enumeration**: the assertion iterates "the first N items" or "until the next sentinel" and stops, which makes the assertion trivially true regardless of the broader behavior.
3. **Tautology**: asserts a literal constant the source code writes verbatim (e.g., `Assert.Equal("radio", typeAttribute)` for a line that reads `builder.AddAttribute(3, "type", "radio")`). Passes when the source compiles; provides no regression coverage.
4. **Self-referential setup**: test arranges state that makes its own assertion trivially true, with no observable behavior under test.

When you suspect any of these, **trace the assertion back to the specific source line(s)** it's supposed to cover and verify the relationship. Flag suspected cases at severity 🚩 (above ⚠️) with explicit reasoning — these are the highest-priority feedback for the author.

### Coverage-burst pattern recognition

When evaluating a PR that's part of a multi-author "improve test coverage" wave (common pattern: 3+ similar PRs from new contributors over a short window targeting adjacent components), apply extra scrutiny to D0 (substantive value):

- **Compare new tests against pre-existing tests in the same file** — duplicate detection is the most common issue.
- **Look for "ISSUE #N FIX" comments in test bodies** — signal of iterative AI-assisted authoring that may have over-generated tests.
- **Check whether the headline test count overstates the real coverage gain** — "15 new tests" is meaningless if 6 are duplicates and 1 is wrong.
- **Don't soften feedback to be encouraging** — a PR that adds 8 valuable tests and 4 duplicates is more useful than one that adds 12 mixed-quality tests; the contributor's *next* PR will be better if this one's feedback is direct.

## Process

### Step 1: Gather context

1. Fetch the PR diff and the list of changed test files.
2. For each test file: read the **full file** (not just the diff). You need the surrounding test class context.
3. **Fetch the production source the tests claim to exercise** — typically the `src/` peer of the changed `test/` file. This is what D0 (substantive value) and the assertion-fabrication check require.
4. **Fetch the pre-existing version of each changed test file** (`git show <merge-base>:<path>`) and enumerate what tests already covered. Any new test that overlaps an existing one is a duplicate-detection candidate.
5. For each non-test source file changed: identify what behavior changed (this is what the tests must exercise).
6. Note the test project type: TestRenderer-based unit tests (pulling in `$(ComponentsSharedSourceRoot)test\**\*.cs`), plain xUnit, Selenium-based E2E (`src/Components/test/E2ETest/`), Playwright (`src/ProjectTemplates/test/Templates.Blazor.Tests/` today; preferred for new E2E projects), etc.
7. If the PR title or labels suggest it's part of a coverage burst (multi-author test-coverage wave), apply the extra-scrutiny rules above to D0.

### Step 2: Verify tests fail without the fix

If the PR is a bug fix, the strongest signal is that the new tests **fail without the fix**. If the PR runner permits, suggest invoking the `verify-tests-fail-without-fix` skill to confirm this automatically:

```bash
test -f .github/skills/verify-tests-fail-without-fix/SKILL.md && \
  echo "Recommend: pwsh .github/skills/verify-tests-fail-without-fix/scripts/verify.ps1 -PrNumber <N>"
```

If automated verification isn't possible (CI-side limits), at minimum **read the test alongside the change and reason about whether the test exercises the changed code path**.

### Step 3: Apply the rubric

For each test, score every dimension. Note files and line numbers for findings.

### Step 4: Score the PR overall

Apply the severity rules above to produce the verdict.

### Step 5: Output

Use the format the parent workflow expects. Default template:

```markdown
## 🧪 PR Test Evaluation

**Overall Verdict:** [✅ / ⚠️ / ❌]

[1-2 sentence summary: what the tests cover well, what they miss.]

### Test files reviewed

- `test/MyArea.Tests/FooTests.cs` (added, 3 tests)
- `test/MyArea.Tests/BarTests.cs` (modified, 1 test changed)

### Findings

| # | Dimension | Severity | File:Line | Issue | Suggested fix |
|---|---|---|---|---|---|
| 1 | D1: Exercises change? | ❌ | FooTests.cs:42 | Test asserts `result is not null` — passes even when the bug is present | Assert on the specific value: `Assert.Equal(expected, result.X)` |
| 2 | D7: Render mode | ⚠️ | BarTests.cs:18 | Only Server mode tested; component supports Auto | Add `[Theory]` with `[InlineData(RenderMode.Server)]`, `[InlineData(RenderMode.WebAssembly)]` |

### Coverage gaps

- (Bullet list of what isn't tested but should be.)

### What's done well

- (Brief — recognize good test patterns to reinforce them.)
```

## aspnetcore Test Conventions Cheat Sheet

- **xUnit SDK v3** for all new tests (xUnit v2 is being migrated).
- **No** "Arrange / Act / Assert" comments — the structure should be self-evident.
- **Moq** for mocking, not NSubstitute or others.
- Test method names use the existing style in the file (`MethodName_Scenario_ExpectedBehavior` is common but not universal).
- Each `src/<Area>/` typically has a paired `src/<Area>/test/` or `src/<Area>/tests/` project. Tests live there.
- **TestRenderer pattern** for Razor component unit logic — projects pull in the shared infra via `<Compile Include="$(ComponentsSharedSourceRoot)test\**\*.cs" LinkBase="Helpers" />` and use `CreateTestRenderer()`, `AssertFrame`, `CapturedBatch` from `src/Components/Shared/test/`. **bUnit is not used in aspnetcore source** — it's for external apps consuming Blazor as a library.
- **Selenium** is the incumbent E2E framework — `src/Components/test/E2ETest/Microsoft.AspNetCore.Components.E2ETests.csproj` imports `$(SharedSourceRoot)E2ETesting\E2ETesting.props` for shared Selenium infrastructure. For **new** E2E projects/surfaces, **prefer Playwright** (the reference pattern lives in `src/ProjectTemplates/test/Templates.Blazor.Tests/`). Don't mix frameworks within a single existing Selenium-based project — add a new project rather than retrofitting.

## When NOT to Use This Skill

- Reviewing non-test changes (use `code-review` skill)
- Auditing test infrastructure changes (these need human review)
- Performance test changes (different rubric — defer to perf reviewers)
