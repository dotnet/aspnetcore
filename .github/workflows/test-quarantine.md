---
on:
  schedule:
    - cron: "0 10 * * *"
  workflow_dispatch:

description: "Daily quarantine/unquarantine flaky tests based on Azure DevOps pipeline analytics"

permissions:
  contents: read
  issues: read
  pull-requests: read
  actions: read

safe-outputs:
  create-pull-request:
    title-prefix: "[test-quarantine] "
    labels: [test-failure]
    draft: false
    max: 10
    base-branch: main
  create-issue:
    title-prefix: "Quarantine "
    labels: [test-failure]
    max: 10
  close-issue:
    target: "*"
    max: 10
  add-comment:
    target: "*"
    max: 10

plugins:
  - lewing-public/dotnet-dnceng

tools:
  edit:
  bash: [":*"]
  github:
    toolsets: [repos, issues, pull_requests, search]
  web-fetch:

network:
  allowed:
    - defaults
    - "dev.azure.com"
    - "vstmr.dev.azure.com"
    - "helix.dot.net"
    - "*.blob.core.windows.net"

timeout-minutes: 45
---

# Daily Test Quarantine Management

You are an automated workflow that manages flaky test quarantine in the dotnet/aspnetcore repository. You perform two tasks each day:

1. **Unquarantine** tests that have been reliably passing for 30+ days
2. **Quarantine** tests that are flaky and causing CI failures

Before creating any PRs or issues, check for existing open PRs in dotnet/aspnetcore with the `[test-quarantine]` prefix in their title that already address the same tests. Skip any test that is already covered by an existing open PR.

---

## Part 1: Unquarantine Reliable Tests

### Step 1.1 — Gather passing test data from the quarantined pipeline

Query the **aspnetcore-quarantined-tests** pipeline (definition ID **84**) in the `dnceng-public` Azure DevOps organization, `public` project.

1. Get all completed builds from the last 30 days:
   ```
   GET https://dev.azure.com/dnceng-public/public/_apis/build/builds?definitions=84&statusFilter=completed&$top=100&minTime={30_DAYS_AGO}&api-version=7.1
   ```

2. For each build, get all test results:
   ```
   GET https://vstmr.dev.azure.com/dnceng-public/public/_apis/testresults/resultsbyBuild?buildId={BUILD_ID}&$top=10000&api-version=7.1-preview.1
   ```

3. Aggregate per test name across all builds: total pass count, total fail count, total "other" count, number of builds the test appeared in.

### Step 1.2 — Identify unquarantine candidates

A test is a candidate for unquarantining if ALL of the following are true:
- It has a **100% pass rate** (zero failures) across the past 30 days
- It does **not** have a suspiciously low total count (i.e. it appeared in at least 20 of the 30 builds)
- It is **not** `AlwaysTestTests.SuccessfulTests.GuaranteedQuarantinedTest` (this test must always stay quarantined)
- It is an **individual test case**, not a work item (exclude names ending in `.WorkItemExecution`)

For IIS tests compiled into multiple assemblies (Common.LongTests, Common.FunctionalTests), the same test method appears with different namespace prefixes (e.g., `FunctionalTests.StartupTests.X`, `IISExpress.FunctionalTests.StartupTests.X`, `NewHandler.FunctionalTests.StartupTests.X`, `NewShim.FunctionalTests.StartupTests.X`). ALL variants must have 100% pass rates. Variants with 0 pass / 0 fail (all "other" outcomes) represent tests skipped by `[ConditionalFact]` and should be excluded from the pass-rate check — they are neither passing nor failing.

### Step 1.3 — Match candidates to source code

Search the repository for `[QuarantinedTest(` attributes. The `[QuarantinedTest]` attribute can be applied at three levels:

1. **Method level** — on an individual test method (most common). Example:
   ```csharp
   [Fact]
   [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/12345")]
   public async Task MyTest() { ... }
   ```

2. **Class level** — on an entire test class, which quarantines all tests within it. Example:
   ```csharp
   [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/49126")]
   public class RoutePatternCompletionProviderTests { ... }
   ```

3. **Assembly level** — applied via `[assembly: QuarantinedTest(...)]`, which quarantines all tests in the assembly.

For each unquarantine candidate from Step 1.2, find the corresponding `[QuarantinedTest]` attribute in source:

- If the attribute is on an **individual method**, unquarantine that method by removing the attribute.
- If the attribute is on a **class**, only remove it if **every test method in that class** appears in the quarantine pipeline data with a 100% pass rate over the past 30 days. Verify by counting the distinct test methods for that class in the AzDO data and confirming all have zero failures.
- If the attribute is at the **assembly level**, only remove it if every test in that assembly has 100% pass rate. This is rare and should be handled conservatively.

Extract the **issue URL** from the `QuarantinedTest` attribute argument (e.g., `[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/12345")]`).

### Step 1.4 — Group by issue and create PRs

Group the candidates by their associated GitHub issue number. For each group:

1. **Create a PR** that removes the `[QuarantinedTest(...)]` attribute(s) from the test method(s) or class. Do NOT remove the `using Microsoft.AspNetCore.InternalTesting;` statement — it may be used by other attributes.

2. In the PR body, explain that the test(s) have been passing 100% for 30+ days in the quarantined pipeline and are being unquarantined.

3. For each issue referenced:
   - Search the entire repository for any **remaining** `[QuarantinedTest]` attributes that reference that issue URL.
   - If **no other** quarantined tests reference that issue, **close the issue** with a comment explaining all associated tests have been unquarantined.
   - If other tests still reference the issue, do **not** close it.

---

## Part 2: Quarantine Flaky Tests

### Step 2.1 — Gather failure data from the main CI pipeline

Query the **aspnetcore-ci** pipeline (definition ID **83**) for two types of failures:

#### Source A: Main branch failures
Get all completed builds on `refs/heads/main` from the last 30 days. For each build with `result` = `failed` or `partiallySucceeded`, get the failed test results:
```
GET https://vstmr.dev.azure.com/dnceng-public/public/_apis/testresults/resultsbyBuild?buildId={BUILD_ID}&outcomes=Failed&$top=1000&api-version=7.1-preview.1
```

#### Source B: PR retry failures
Get all PR builds (`reasonFilter=pullRequest`) from the last 30 days. Group by PR number and `pr.sourceSha` (from `triggerInfo`). Find groups where:
- Multiple builds exist for the same PR + source SHA
- At least one build failed and a subsequent one succeeded
- This was the **final commit** for the PR (the last `pr.sourceSha` seen for that PR)
- The PR was **merged** (check via `gh pr view`)

For qualifying builds, get the failed test results.

#### Source C: Work item crash investigation
For work items (names ending in `.WorkItemExecution`) that failed 2+ times, investigate the Helix console logs to find the individual test(s) that caused the crash:

1. Get the Helix job ID from the build timeline:
   ```
   GET https://dev.azure.com/dnceng-public/public/_apis/build/builds/{BUILD_ID}/timeline?api-version=7.1
   ```
   Look for `issues` in records containing the work item name and extract the job ID from `"job {JOB_ID}"`.

2. Get the console log file from Helix:
   ```
   GET https://helix.dot.net/api/2019-06-17/jobs/{JOB_ID}/workitems/{WI_NAME}/files
   ```
   Find the file starting with `console.` and download it.

3. Search the console log (which can be 10MB+) for `[FAIL]` markers to find the specific test that caused the crash. Use `curl | grep` for efficiency.

### Step 2.2 — Combine and identify quarantine candidates

Combine failure counts from all three sources. A test is a candidate for quarantining if:
- It is an **individual test case** (not a `.WorkItemExecution`)
- It has failed **2 or more times** total across all sources
- It is **not already quarantined** (check the source code for existing `[QuarantinedTest]` attributes)
- The failures are **not** from a PR that modified the test itself (check if the PR's changed files include the test file)

Additionally, if a **test class** has failed more than 3 times total with **multiple failing test methods** that share the **same error message or similar stack traces**, you may quarantine the entire class.

### Step 2.3 — Group related failures and file issues

Before creating issues and PRs, group related failures together:

- If **multiple test methods within the same test class** are failing with the **same error message or similar stack traces** (e.g., the same exception type and call chain), they should be treated as a single group caused by the same underlying problem.
- File **one issue** for the entire group, listing all affected test names under `## Failing Test(s)`.
- In the quarantine PR, all tests in the group should reference the **same issue URL** in their `[QuarantinedTest]` attribute.
- If the entire class qualifies for class-level quarantine (>3 failures, multiple methods, similar errors), apply the `[QuarantinedTest]` attribute to the class instead of individual methods.

### Step 2.4 — File issues and create PRs

For each test or group of tests to quarantine:

1. **Create a test-failure issue** with this exact structure:
   - **Title**: `Quarantine {FULLY_QUALIFIED_TEST_NAME}`
   - **Body**: Use the `50_test_failure.md` template format:
     - `## Failing Test(s)` — fully qualified test name(s)
     - `## Error Message` — from the most recent failure's console log, in a ` ```text ``` ` block
     - `## Stacktrace` — in a `<details>` block with ` ```text ``` `
     - `## Logs` — the full console log content from the most recent failure, in a `<details>` block with ` ```text ``` `. Get this from the Helix work item files API: find the file named `{TestClassName}_{TestMethodName}.log` for the specific test.
     - `## Build` — link to the most recent failing build: `https://dev.azure.com/dnceng-public/public/_build/results?buildId={BUILD_ID}`

2. **Post an investigation comment** on the new issue. Examine all available failure logs for the test. Be concise but thorough:
   - If you can identify a root cause, explain it and suggest a fix if one is obvious.
   - If you cannot determine the root cause, say so.
   - You may reference Microsoft official docs or issues in other repos within the `dotnet` GitHub org if relevant, but do not include any other external links.
   - Do not include potentially sensitive information such as access tokens.

3. **Create a PR** that:
   - Adds `[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/{ISSUE_NUMBER}")]` to the test method (or class)
   - Adds `using Microsoft.AspNetCore.InternalTesting;` if not already present in the file
   - References the issue in the PR body with `Fixes #{ISSUE_NUMBER}` only if this is the sole test for that issue; otherwise just mention the issue without `Fixes`

---

## Important Rules

- **Always exclude** `AlwaysTestTests.SuccessfulTests.GuaranteedQuarantinedTest` from all analysis. This test must never be unquarantined.
- **Check for existing PRs** before creating new ones. Search for open PRs with `[test-quarantine]` in the title that mention the same test names. Skip tests already covered.
- **One PR per issue** for unquarantining. Group tests by their quarantine issue.
- **One issue + one PR per test** (or per related group) for quarantining.
- When modifying IIS tests in `Common.LongTests` or `Common.FunctionalTests`, be aware these are compiled into multiple test assemblies (IIS.FunctionalTests, IISExpress.FunctionalTests, IIS.NewHandler.FunctionalTests, IIS.NewShim.FunctionalTests). A single source change affects all variants.

## Output Budget and Prioritization

This workflow has a limit of **10** for each safe output type (PRs, issues, issue closures, comments) per run. You must plan your output usage carefully to avoid orphaned state.

### Budget planning

Before creating any outputs, build a complete plan of all actions you intend to take. Count the totals for each output type:

- **Unquarantine actions** each consume: 1 PR + 0-1 issue closures (only if no remaining tests reference the issue).
- **Quarantine actions** each consume: 1 issue + 1 PR + 1 comment. These three outputs are **atomic** — never create a quarantine PR without its corresponding issue, and never create an issue without its corresponding PR. If you don't have budget remaining for all three, skip that test entirely and let the next day's run handle it.

If the total planned actions exceed any output limit, **trim from the bottom of the priority list** until all limits are satisfied. It is always safe to defer work to the next day's run.

### Priority order

Process items in this order:

1. **Quarantine** tests first, sorted by total failure count (most failures first). Flaky tests actively harm CI, so fixing them is higher priority than unquarantining stable tests.
2. **Unquarantine** tests second, sorted by total pass count (most runs first). These tests are already stable and just need cleanup.

### Atomicity rules

- **Never create a quarantine PR without a corresponding issue.** If you've hit the issue limit, stop creating quarantine PRs too.
- **Never create a quarantine issue without a corresponding PR.** If you've hit the PR limit, stop creating quarantine issues too.
- **Never create a quarantine issue without its investigation comment.** If you've hit the comment limit, stop creating quarantine issues and PRs too.
- **Unquarantine PRs do not require issues or comments**, so they can fill remaining PR budget after quarantine actions are complete.
- **Issue closures are best-effort.** If you run out of close-issue budget, the issue simply stays open until the next run — this is harmless.

## API Reference (Azure DevOps & Helix)

These are the key API endpoints. All are public and require no authentication:

| Purpose | Endpoint |
|---------|----------|
| List builds | `GET https://dev.azure.com/dnceng-public/public/_apis/build/builds?definitions={DEF_ID}&...&api-version=7.1` |
| Test results per build | `GET https://vstmr.dev.azure.com/dnceng-public/public/_apis/testresults/resultsbyBuild?buildId={ID}&api-version=7.1-preview.1` |
| Build timeline | `GET https://dev.azure.com/dnceng-public/public/_apis/build/builds/{ID}/timeline?api-version=7.1` |
| Helix work item files | `GET https://helix.dot.net/api/2019-06-17/jobs/{JOB_ID}/workitems/{WI_NAME}/files` |
| Helix console log | Download the `Link` URL from the files response for the file starting with `console.` |
| Per-test log | Download the `Link` URL for the file named `{TestClass}_{TestMethod}.log` |
