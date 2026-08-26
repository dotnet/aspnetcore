---
name: validate-blazor-feature
description: >-
  Validate a Blazor feature or regression in a real browser using the canonical Components samples before selecting permanent coverage. USE FOR exercising src/Components changes with Playwright; turning a browser failure into a temporary JavaScript diagnostic assertion; repeating it to assess determinism; investigating nondeterministic producer or timing behavior; replacing arbitrary Selenium sleeps with observable waits or explicit gates; choosing between a faithful Jest/jsdom manager or DOM contract and C# Selenium for a real browser producer; and recording the permanent-test handoff. Always use it when a Components task mentions deterministic browser evidence, flaky Selenium or Thread.Sleep, timing gates, or a JS-vs-Selenium boundary. It may author temporary diagnostic probes but does not write permanent E2E/Selenium tests. DO NOT USE FOR non-Components areas or implementing standalone unit tests.
---

# Validate a Blazor feature with the Components samples

Workflow: pick a sample, add a scenario page, set the render mode, build, launch, drive it in a browser, check for errors, and reduce a reproduced failure to a deterministic diagnostic probe before selecting permanent coverage. Remove all temporary diagnostic and sample code once the selected permanent test covers it.

## 1. Pick the sample and where the page goes

Samples live in `src/Components/Samples` (Blazor Web Apps) and `src/Components/WebAssembly/Samples` (standalone). Each is a full checkout against the in-tree framework.

| Sample | Use it to test | Add your page in |
|--------|----------------|------------------|
| **BlazorWebAppPerPage** (+ `.Client`) | Most features: mix static SSR with per-page interactivity; switch one page's mode independently | `.Client/Pages/` for WebAssembly/Auto-capable pages; host `Components/Pages/` for Server-only or static SSR pages |
| **BlazorWebAppGlobal** (+ `.Client`) | Root/global interactivity concerns (whole app one mode) | `.Client/Pages/` (all routable pages live there) |
| **BlazorWebAssemblyStandalone** | Pure client WebAssembly behavior, no server host | `Pages/` |

A WebAssembly or Auto component **must** live in the `.Client` project so it compiles into the client assembly. A page placed in the server host can only run Server or static SSR. Routable pages need `@page "/your-route"`.

## 2. Set the render mode

The two Web App samples express interactivity differently:

- **Per-page** (`BlazorWebAppPerPage`): put the directive at the top of the page/component. Omit it for static SSR.
  ```razor
  @rendermode InteractiveServer       @* or InteractiveWebAssembly, or InteractiveAuto *@
  ```
- **Global** (`BlazorWebAppGlobal`): change the single value on `<Routes>` and `<HeadOutlet>` in host `Components/App.razor`:
  ```razor
  <HeadOutlet @rendermode="InteractiveAuto" />
  ...
  <Routes @rendermode="InteractiveAuto" />
  ```
- **Disable prerendering** (to test the no-prerender path):
  ```razor
  @rendermode @(new InteractiveServerRenderMode(prerender: false))
  ```
- **Static SSR / None**: omit the render mode entirely (per-page sample). Static SSR emits the same markup as interactive, so it is not interactive on its own.

`InteractiveAuto` runs on the **Server** circuit on first load while the WebAssembly assets download, then uses WebAssembly on later visits. To force one platform, name it explicitly.

## 3. Build, then launch

The samples reference the in-tree framework, so the framework and the Blazor JS must be built first:

- Framework assemblies: run `eng\build.cmd` once (look for `artifacts\bin`).
- Blazor JS: `src/Components/Web.JS/dist/Debug/_framework/blazor.web.js` must exist; if not, run `npm run build` in `src/Components/Web.JS`. Without it the page serves no `blazor.web.js` and interactivity never starts.

Then activate the repo SDK and run the sample:

```powershell
. ./activate.ps1
# Per-page Blazor Web App
dotnet run --project src/Components/Samples/BlazorWebAppPerPage/BlazorWebAppPerPage.csproj --no-restore
# Global Blazor Web App
dotnet run --project src/Components/Samples/BlazorWebAppGlobal/BlazorWebAppGlobal.csproj --no-restore
# Standalone WebAssembly
dotnet run --project src/Components/WebAssembly/Samples/BlazorWebAssemblyStandalone/BlazorWebAssemblyStandalone.csproj --no-restore
```

Read the launch URL from stdout: Web App hosts print `Now listening on: http://localhost:<port>`; the standalone (WasmAppHost) prints `App url: http://localhost:<port>/`. Do not assume a port; parse that line.

## 4. Drive it in the browser (Playwright MCP)

Use the `playwright-browser_*` tools. For an interactive behavior, prove it behaviorally; rendered markup alone is a false positive (static SSR emits the same HTML).

1. `playwright-browser_navigate` to `<base>/<route>`.
2. For WebAssembly/Auto/standalone, the runtime boots asynchronously: `playwright-browser_wait_for` the expected text (e.g. `Current count`) before interacting. First paint can take many seconds.
3. `playwright-browser_snapshot` to read current state.
4. `playwright-browser_click` (or `_type`, `_fill_form`) to interact.
5. `playwright-browser_snapshot` again and assert the state changed (e.g. `Current count: 0` → `1`). No change after interaction means the component is static, not interactive: the render mode was not applied or the component is in the wrong project.

Confirm the runtime attached:
- Interactive **Server**: console logs `Information: WebSocket connected to ws://<host>/_blazor?...`.
- Interactive **WebAssembly**/standalone: the page becomes responsive after the runtime boots; no `_blazor` WebSocket is required.

## 5. Check the console and network for errors

After interacting, inspect the console. **Scope it to the current page**: call `playwright-browser_console_messages` with `level: error` and do not set `all: true`. The all-history form returns errors from the whole session, including stale failures from a server you already stopped on another port (see [references/error-checks.md](references/error-checks.md#false-positives-to-ignore) for which to ignore).

- `playwright-browser_console_messages` with `level: error`: expect zero real errors on the page under test.
- `playwright-browser_network_requests`: check `blazor.web.js` (Web App) or `blazor.webassembly.js` (standalone) and `_framework/*` assets return 200, not 404.

See [references/error-checks.md](references/error-checks.md) for the catalog of common failures, the symptom each produces, and the fix.

## 6. Distill the reproduction and establish determinism

After Playwright reproduces the failure, reduce the scenario to the smallest JavaScript probe and assertion that observes the same final browser state. Run it in the real page with Playwright evaluation or from temporary scratch code. This probe is diagnostic: do not add it to a Jest or `.test.ts` suite, do not include it in the production change, and remove it after the permanent scenario supersedes it.

Repeat the unchanged scenario and probe a recorded, bounded number of times. When repository evidence does not suggest another count, 10 runs is a reasonable default confidence sample, not proof that the behavior is deterministic across machines, browsers, or load. Record the count, environment, assertion, and failure signature. Call the reproduction deterministic under the exercised conditions only when every run fails the same assertion for the same reason; never describe the bounded sample as proving determinism.

If the runs do not agree, stop the permanent-test handoff and investigate the owning producer and timing:

- Add targeted logging or browser observations that distinguish whether the producer ran, which precondition differed, and where ordering changed.
- Use a faithful unit or lower-boundary test when that module's contract is the final observable and the test exercises its real inputs.
- When managed timing must be controlled for the browser scenario, introduce an explicit test gate such as a `TaskCompletionSource`, endpoint, or test-only release action and wait for observable browser state before and after releasing it.
- Do not make the race appear stable by inserting an arbitrary fixed delay.

When the browser reproduction is deterministic, record that the permanent C# Selenium scenario must preserve the same setup and final assertion. Prefer existing observable waits such as `Browser.True` and `Browser.Equal` (which poll with `WebDriverWait`) or an explicit test-controlled gate. A bounded polling helper may delay between observable checks when it also enforces a timeout; a fixed `Thread.Sleep` is not the synchronization mechanism for the regression assertion.

## 7. Record the permanent regression boundary

Before handing off to permanent tests, record:

- **Behavior owner**: the subsystem that owns the behavior.
- **Production producer**: the real mechanism that creates the disputed preconditions.
- **Final observable**: the material result the regression test must assert.
- **Selected permanent surface**: the test suite that exercises that producer and observable.
- **Lower-boundary false-pass risk**: how a lower-level test could pass while the shipped behavior still fails.

For browser-owned user-visible behavior (real DOM measurement, layout or geometry, scrolling, browser observers, browser scheduling or event ordering, browser-dependent JS interop, navigation, focus or selection, and rendering or rehydration), hand off to permanent C# Selenium coverage under `src/Components/test/E2ETest`. Extend an existing asset and test class when practical. Require the identical Selenium assertion to be red without the fix and green with it. Include a nearest-opposite control, meaning the closest scenario that must stay green, and, when meaningful, an adjacent control driven by the same production producer. A browser fix remains blocked while Jest is its only regression proof.

Do not recommend adding or retaining Jest or `.test.ts` coverage as proof of the same browser scenario, including as supplemental coverage or in an existing test file. When synthetic geometry, mocked observers or events, or direct state or callback injection stand in for that browser scenario's real producer, they are temporary diagnostic probes outside the production change.

JavaScript or TypeScript unit coverage remains appropriate when the module's deterministic contract is the final observable and its harness faithfully supplies the contract inputs. This includes pure helpers, structural DOM algorithms over ordinary nodes, manager callback or timer-state contracts, and validation-engine contracts. These tests do not prove that real browser scheduling, layout, observers, or a user-visible producer path is reachable; claims about those behaviors remain Selenium scenarios. Managed or service behavior fully owned and observable below the browser remains at that faithful lower boundary.

If WebDriver cannot perform or observe one exact operation, name that limitation and permit only the smallest existing JavaScript helper or `IJavaScriptExecutor` snippet for that step. Keep the permanent scenario orchestration and final user-visible assertion in C# Selenium.

Treat `src/Components/AGENTS.md` section "Permanent regression test boundary" as the normative policy when recording this handoff.

This skill validates the sample interactively and records the handoff. It does not write the permanent Selenium test.

## 8. Finish

- Stop the sample server by its specific PID.
- Remove the Playwright artifacts folder (`.playwright-mcp/`) it drops into the working directory.
- Remove any temporary JavaScript diagnostic probe.
- Per the Components workflow, once the selected permanent test covers the behavior, remove the sample scenario code: `git checkout -- src/Components/Samples src/Components/WebAssembly/Samples` and `git clean -df -- src/Components/Samples src/Components/WebAssembly/Samples`.

## Completion criteria

The validation is done when: the page loaded (200, framework JS served), the behavior was exercised by a real interaction, the resulting state change was observed in a snapshot, and the console shows no real errors for the page under test. For a regression, also record the temporary probe, bounded repeat result, and whether every run failed for the same reason. Only then record the five-field handoff to the selected permanent surface; deterministic browser-owned behavior moves to C# Selenium E2E coverage, while inconsistent behavior returns to producer/timing investigation or an explicit gate.
