# Interpreting CI Results

## Result Categories

**Known Issues section**: Failures matching existing GitHub issues.

**Build Analysis check status**: Green = *every* failure matched a known issue. Red = at least one unmatched. Verify each failing job is covered before calling it safe to retry.

**Canceled/timed-out jobs**: Jobs canceled due to earlier stage failures or AzDO timeouts. Dependency-canceled jobs don't need investigation. **Timeout-canceled jobs may have all-passing Helix results** — the "failure" is just the AzDO job wrapper timing out, not actual test failures. To verify: get the Helix job pass/fail summary for each job in the timed-out build (include passed work items). If all work items passed, the build effectively passed.

**PR Change Correlation**: Files changed by PR appearing in failures — likely PR-related.

**Build errors**: Compilation failures need code fixes.

**Helix failures**: Test failures on distributed infrastructure.

**Local test failures**: Some repos (e.g., dotnet/sdk) run tests directly on build agents. These can also match known issues — search for the test name with the "Known Build Error" label.

## Per-Failure Details

`failedJobDetails` in JSON: Each failed job includes `errorCategory`, `errorSnippet`, and `helixWorkItems`. Use these for per-job classification instead of applying a single `recommendationHint` to all failures.

Error categories: `test-failure`, `build-error`, `test-timeout`, `crash` (exit codes 139/134/-4), `tests-passed-reporter-failed` (all tests passed but reporter crashed — genuinely infrastructure), `unclassified` (investigate manually).

> ⚠️ **`crash` does NOT always mean tests failed.** Exit code -4 often means the Helix work item wrapper timed out *after* tests completed. Always check `testResults.xml` before concluding a crash is a real failure. See [Recovering Results](#recovering-results-from-crashedcanceled-jobs) below.

> ⚠️ **Be cautious labeling failures as "infrastructure."** Only conclude infrastructure with strong evidence: Build Analysis match, identical failure on target branch, or confirmed outage. Exception: `tests-passed-reporter-failed` is genuinely infrastructure.

> ❌ **Missing packages on flow PRs ≠ infrastructure.** Flow PRs can cause builds to request *different* packages. Check *which* package and *why* before assuming feed delay.

## Recovering Results from Crashed/Canceled Jobs

When an AzDO job is canceled (timeout) or Helix work items show `Crash` (exit code -4), the tests may have actually passed. Follow this procedure:

1. **Find the Helix job IDs** — Read the AzDO "Send to Helix" step log and search for lines containing `Sent Helix Job`. Extract the job GUIDs.

2. **Check Helix job status** — Get pass/fail summary for each job. Look at `failedCount` vs `passedCount`.

3. **For work items marked Crash/Failed** — Check if tests actually passed despite the crash. Try structured test results first (TRX parsing), then search for pass/fail counts in result files without downloading, then download as last resort:
   - Parse the XML: `total`, `passed`, `failed` attributes on the `<assembly>` element
   - If `failed=0` and `passed > 0`, the tests passed — the "crash" is the wrapper timing out after test completion

4. **Verdict**:
   - All work items passed or crash-with-passing-results → **Tests effectively passed.** The failure is infrastructure (wrapper timeout).
   - Some work items have `failed > 0` in testResults.xml → **Real test failures.** Investigate those specific tests.
   - No testResults.xml uploaded → Tests may not have run at all. Check console logs for errors.

> This pattern is common with long-running test suites (e.g., WasmBuildTests) where tests complete but the Helix work item wrapper exceeds its timeout during result upload or cleanup.
