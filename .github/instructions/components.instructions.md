---
description: Instructions for folder 'src\Components'
applyTo: "src/Components/**"
---

When working on issues in the Components area, follow the instructions in
[src/Components/AGENTS.md](../../src/Components/AGENTS.md) and consult the relevant sections of the
[Blazor and Components guidance](../../docs/BlazorComponentsGuidance.md).

For Components Server behavior, including shared code used by the server, also follow
[Components Server instructions](components.server.instructions.md).

## Review checks

* Check new `[Parameter]` names against other declared and inherited parameters, ignoring case. Blazor's `ComponentProperties` enumerates inherited properties and uses case-insensitive parameter lookup; distinct parameter members with the same name, including casing variants, collide and throw during parameter binding. Adding a base-class parameter can therefore break existing derived components.
* Select permanent tests by the behavior owner, real production producer, and final observable, not by whether a `.razor` or TypeScript file changed. Managed RenderTree markup/attribute output and service state fully observable below the browser belong in .NET unit tests, using `TestRenderer` where appropriate. Deterministic JavaScript/TypeScript module contracts belong in `Web.JS` `.test.ts` tests when the harness faithfully supplies their inputs. Do not also require Selenium for a contract fully covered at that faithful lower boundary.
* Browser-owned behavior requires the owning real-browser suite: C# Selenium under `src/Components/test/E2ETest` for Components, or Playwright under `src/ProjectTemplates/test/Templates.Blazor.Tests` when generated template content owns the behavior. This includes DOM measurement, layout, observers, browser scheduling/event ordering, browser-dependent interop, navigation, focus/selection, and browser-observed rendering or rehydration, not managed RenderTree output alone. Keep Platform browser-API coverage in its existing `src/Components/Platform/testassets/PlatformTestApp.E2E.Tests` suite.
* Do not treat synthetic geometry, mocked browser observers/events, or direct callback/state injection as proof of the corresponding browser producer path, or retain Jest/`.test.ts` tests as supplemental proof of that same browser scenario. Reject vacuous tests: exercise the intended branch and assert the selected contract's final observable, not an intermediate helper result or unchanged setup state that can pass while the intended behavior fails. For regression fixes, require the same assertion to fail for the expected reason without the fix and pass with it at the selected permanent boundary.
