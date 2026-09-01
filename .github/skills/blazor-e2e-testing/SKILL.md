---
name: blazor-e2e-testing
description: >-
  Build, run, and debug Blazor end-to-end tests in dotnet/aspnetcore. USE FOR validating Components changes with targeted Selenium E2E tests, preparing test assets after C# or JavaScript changes, reproducing a failing Blazor E2E test interactively with Components.TestServer and Playwright, and distinguishing quarantined tests from new failures. DO NOT USE FOR running the full Components E2E suite locally, validating a feature in a temporary sample before permanent test coverage (use validate-blazor-feature), unit tests, or non-Components areas.
---

# Test Blazor end to end

Run the smallest targeted Selenium E2E set that exercises the changed behavior. Never run the full Components E2E suite locally; full coverage belongs in CI.

## 1. Define the test scope

Identify the existing test class and method nearest to the behavior. Tests live in `src/Components/test/E2ETest`; reusable scenarios and hosts live in `src/Components/test/testassets`.

For a behavior spanning renderers or runtimes, state the required cells before running tests: Server/WebAssembly/Auto, global/per-page interactivity, initial activation/enhanced navigation, prerendered/non-prerendered, and interactive `Router` present/absent. Run only the cells required by the change and report broader compatibility separately.

For a new feature or behavioral fix, use the `validate-blazor-feature` skill first to exercise the scenario in a canonical sample. Add permanent E2E coverage only after the sample behavior works in a browser.

## 2. Prepare the repository and assets

Activate the repository SDK before any `dotnet` command:

```powershell
. ./activate.ps1
```

In a fresh worktree, initialize submodules before building:

```powershell
git submodule update --init --recursive
```

If JavaScript or TypeScript under `src/Components/Web.JS` changed, rebuild the Debug bundle from that directory. Do not use `build:production` on its own because the E2E build consumes the Debug bundle.

```powershell
Push-Location src/Components/Web.JS
npm run build
Pop-Location
```

Build the E2E project and all referenced test apps. Do not use `--no-dependencies` for this preparation step because stale test-app outputs can make a test pass or fail for the wrong reason.

```powershell
dotnet build src/Components/test/E2ETest/Microsoft.AspNetCore.Components.E2ETests.csproj `
    --no-restore -v:q -p:UseIisNativeAssets=false
```

If restore inputs changed, run `.\restore.cmd` first. If the build reports unresolved MessagePack types, confirm the submodules were initialized rather than retrying with different build flags.

## 3. Run only the targeted tests

After the dependency-aware build succeeds, use `--no-build` for the focused loop:

```powershell
dotnet test src/Components/test/E2ETest/Microsoft.AspNetCore.Components.E2ETests.csproj `
    --no-build `
    --filter "FullyQualifiedName~Namespace.TestClass.TestMethod" `
    -l "console;verbosity=minimal"
```

Use a class filter when several methods jointly cover the behavior.

For a behavioral fix, establish the red/green result at the faithful boundary: confirm the targeted test fails for the expected reason without the fix and passes with it. A passing test that bypasses the browser or runtime mechanism producing the disputed precondition is not evidence for the behavior.

## 4. Debug a failure interactively

Do not repeatedly rerun a failing E2E test. After two failures at the same boundary, isolate it with the real test server and browser:

1. Read the failing test and its fixture to identify the host, route, render mode, and interaction.
2. Start `Components.TestServer` manually:

   ```powershell
   dotnet run --project src/Components/test/testassets/Components.TestServer/Components.TestServer.csproj `
       --no-build -p:UseIisNativeAssets=false
   ```

3. Open `http://127.0.0.1:5019/subdir` and select the scenario used by the test. If the fixture launches a different host, use the URL and route from that fixture instead.
4. Drive the same interaction with the `playwright-browser_*` tools:
   - Navigate to the scenario and wait for its expected content.
   - Take a snapshot before and after the interaction; rendered markup alone does not prove interactivity.
   - Inspect current-page console errors and network requests for failed `_framework` assets.
   - Confirm the material effect at the user-visible boundary instead of checking only an internal callback or flag.
5. Stop only the server process you started, using its exact PID. Do not terminate all `dotnet`, browser, `testhost`, or WebDriver processes by name.

After changing C# or test assets, rebuild the E2E project dependency-aware. After changing JavaScript, rebuild `src/Components/Web.JS` first. Restart the server, reproduce the scenario in Playwright, then rerun the exact test with `--no-build`.

## 5. Handle quarantined tests correctly

A failure is a known quarantine only when the test has `[QuarantinedTest("issue-url")]`. Read the linked issue and record that the quarantined test failed. Do not classify `WebDriverTimeoutException`, `StaleElementReferenceException`, or another Selenium exception as a flake based only on exception type; those exceptions can expose real regressions.

An unquarantined failure blocks the targeted validation until it is explained or fixed. If faithful reproduction is impractical, state the exercised boundary and limitation instead of calling the behavior verified.

## Completion criteria

The required render-mode and lifecycle cells are explicit, the dependency-aware E2E build succeeds, and every targeted test passes. For a new test or an interactively investigated failure, the browser reproduction must also show the expected user-visible behavior with no relevant console or network errors. Stop every process started manually.
