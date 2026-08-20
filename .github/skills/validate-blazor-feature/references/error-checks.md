# Common Blazor sample validation failures

Symptom → likely cause → fix. Grouped by where the failure surfaces. "Console" means the browser console via `playwright-browser_console_messages`.

## Table of contents
- [Build / launch failures](#build--launch-failures)
- [Interactivity not working](#interactivity-not-working)
- [Console / network errors](#console--network-errors)
- [WebAssembly-specific](#webassembly-specific)
- [False positives to ignore](#false-positives-to-ignore)
- [Quick checklist](#quick-checklist)

## Build / launch failures

| Symptom | Cause | Fix |
|---------|-------|-----|
| Build error: missing `Microsoft.AspNetCore.*` / type not found | Framework not built; `<Reference>` resolves to `artifacts\bin` which is empty | Run `eng\build.cmd` once; confirm `artifacts\bin` populated |
| `dotnet run` fails: SDK not found / wrong version | Repo SDK not activated | Run `. ./activate.ps1` (or `source activate.sh`) first |
| A component placed in the server host won't compile for WebAssembly/Auto | WebAssembly code must be in the `.Client` project | Move the page/component into the `.Client` project |
| New `.razor` page 404s | Missing `@page "/route"`, or page added to the wrong project for its render mode | Add the `@page` directive; place WebAssembly/Auto pages in `.Client` |

## Interactivity not working

| Symptom | Cause | Fix |
|---------|-------|-----|
| Button click does nothing; state never changes | Render mode not applied (static SSR) | Add `@rendermode` to the page (per-page sample) or set it on `<Routes>`/`<HeadOutlet>` (global sample) |
| Page interactive in one sample but not after moving the component | Component in server host but marked WebAssembly/Auto | Move it into the `.Client` project |
| `@rendermode InteractiveAuto` behaves like Server on first load | Expected: Auto uses Server while WebAssembly downloads, then WebAssembly on later visits | Reload after assets cache, or name the platform explicitly to force it |
| Interactivity starts late / flickers | Prerendering renders static HTML first, then the component goes interactive | Expected; to test without it use `@rendermode @(new InteractiveServerRenderMode(prerender: false))` |
| `<Routes>` interactive but a page inside isn't | In per-page mode, interactivity is per page; the page lacks `@rendermode` | Add the directive to that page/component |

## Console / network errors

| Console / network symptom | Cause | Fix |
|---------|-------|-----|
| No `blazor.web.js` request, or it 404s | Blazor JS not built | `npm run build` in `src/Components/Web.JS`; confirm `dist/Debug/_framework/blazor.web.js` |
| `Failed to start the circuit` / no `_blazor` WebSocket connects | Server-interactive component failed to start, or antiforgery/middleware misconfigured | Check server stdout for the exception; confirm `app.MapRazorComponents` registers the render mode |
| `blazor.web.js` loads but no WebSocket and no WASM | No interactive render mode is actually applied anywhere | Apply a render mode (see Interactivity section) |
| 500 on `/` or a page | Server-side exception during SSR | Read the server stdout/log; reproduce by requesting the URL directly |
| Antiforgery token errors on a form POST | Static SSR form posted without antiforgery wiring | Ensure the form uses the framework form handling / `app.UseAntiforgery()` is present (it is in the samples) |

## WebAssembly-specific

| Symptom | Cause | Fix |
|---------|-------|-----|
| Page stuck on "Loading..." | WebAssembly runtime still downloading/booting | `playwright-browser_wait_for` the expected text; allow several seconds, then re-snapshot |
| Console: failed to load `dotnet.js` / `_framework/*.wasm` 404 | Static web assets not produced for the client project | Rebuild the client/standalone project; confirm `_framework` assets serve 200 |
| `blazor.boot.json` 404 on a current .NET preview | The boot manifest format/name changed across versions | Not necessarily a defect: verify the app actually boots and `blazor.webassembly.js` serves; judge by behavior, not this single file |
| Standalone app: blank page, no errors | Wrong URL (used the host "Now listening" pattern) | The standalone WasmAppHost prints `App url: http://localhost:<port>/`; navigate there |

## False positives to ignore

- **Errors pointing at a port you already stopped** (`ERR_CONNECTION_REFUSED`, `Failed to complete negotiation`, `WebSocket closed 1006`): these are a previously-running sample's reconnect attempts captured by `--all`. Scope error checks to the current page and ignore other-port URLs.
- **WebSocket disconnect on navigation/shutdown**: closing the tab or stopping the server logs a disconnect; it is not a failure of the behavior under test.
- **`blazor.boot.json` 404** on current previews (see WebAssembly table): confirm boot via behavior instead.

## Quick checklist

Run through this for any interactive-behavior validation:

1. Framework built (`artifacts\bin`) and `blazor.web.js` present.
2. Sample launched; URL parsed from `Now listening on` / `App url`.
3. Page returns 200; `blazor.web.js` / `blazor.webassembly.js` and `_framework/*` return 200.
4. For Server interactivity: console shows `_blazor` WebSocket connected.
5. For WebAssembly/standalone: waited for the runtime to boot before interacting.
6. Interacted (click/type) and observed the state change in a fresh snapshot.
7. Console (scoped to the current page) shows zero real errors; other-port noise ignored.
8. Server stopped by PID; `.playwright-mcp/` removed; sample scenario code reverted once E2E covers it.
