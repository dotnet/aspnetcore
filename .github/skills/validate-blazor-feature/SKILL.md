---
name: validate-blazor-feature
description: >-
  Validate a Blazor feature or behavior interactively in a browser using the canonical Components samples in this repo, before writing E2E tests. USE FOR exercising a Blazor change in src/Components (a render-mode behavior, an interactive component, enhanced navigation, forms, streaming, prerendering), deciding which sample to use, where to add a test page, how to set the render mode (Server/WebAssembly/Auto/static SSR), how to confirm the app is actually interactive (not just static SSR), and how to inspect the browser console and network for errors. Covers BlazorWebAppGlobal, BlazorWebAppPerPage, and BlazorWebAssemblyStandalone, launching them against the in-tree framework, and driving them with the Playwright MCP browser tools. DO NOT USE FOR writing the permanent E2E/Selenium tests themselves, non-Components areas, or unit tests.
---

# Validate a Blazor feature with the Components samples

Workflow: pick a sample, add a scenario page, set the render mode, build, launch, drive it in a browser, and check for errors. Validate behavior before writing E2E tests; remove the sample code afterward (`git checkout`/`git clean`) once the E2E test covers it.

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

## 6. Finish

- Stop the sample server by its specific PID.
- Remove the Playwright artifacts folder (`.playwright-mcp/`) it drops into the working directory.
- Per the Components workflow, once an E2E test covers the behavior, remove the sample scenario code: `git checkout -- src/Components/Samples src/Components/WebAssembly/Samples` and `git clean -df -- src/Components/Samples src/Components/WebAssembly/Samples`.

## Completion criteria

The validation is done when: the page loaded (200, framework JS served), the behavior was exercised by a real interaction, the resulting state change was observed in a snapshot, and the console shows no real errors for the page under test. Only then move on to writing the E2E test.
