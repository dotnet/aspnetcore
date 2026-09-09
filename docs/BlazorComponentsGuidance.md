# Blazor Components guidance

This guidance covers ASP.NET Core Blazor and Razor Components work under `src/Components/**` and
`src/JSInterop/**` — rendering, lifecycle, render modes, JS interop, navigation, forms, and
interactive Server circuits. Consult the relevant sections alongside the requested task and
applicable repository and area instructions.

## Overarching principles

- **Preserve coherent hosting and lifecycle boundaries:** no single renderer owns every scenario.
- **Keep framework layers separated:** core Components abstractions must not absorb endpoint, hosting, browser, or circuit specifics unless intentionally general.
- **Let the renderer own component state:** lifecycle continuations, events, disposal, and `StateHasChanged` go through the renderer dispatcher or circuit synchronization context.
- **Treat JS interop and browser state as availability- and lifetime-sensitive:** `IJSRuntime`, `IJSObjectReference`, `ElementReference`, DOM callbacks, and browser resources need render-mode guards and deterministic cleanup.

## Topics

### Scope, layering, and public API shape

- Keep render-mode, endpoint, hosting, environment, `HttpContext`, and circuit-specific logic in the assembly that owns that environment; do not move server-only concepts into core Components or Components.Web for one scenario.
- Public Components and JS interop APIs must have narrow names that describe the scenario, preserve existing overload compatibility, and expose only genuinely general extension points.
- Use `Microsoft.Extensions.Options` and idempotent DI registration patterns for framework configuration; avoid duplicate registrations or hidden dependencies omitted by slim builders.
- Keep source-generated or framework-only plumbing internal unless public generation contracts require access; expose strongly typed surfaces rather than untyped internal mechanisms.
- For Components APIs, follow the [consumer-facing XML-documentation boundary](../src/Components/AGENTS.md#code-clarity-and-durable-knowledge); generic JSInterop APIs still require XML documentation for consumer-observable behavior, not internal lifecycle narration.

### Render modes and hosting boundaries

- For changes crossing Components renderers, runtimes, or DI scopes, use the [cross-runtime design checkpoint](../src/Components/AGENTS.md#cross-runtime-design-checkpoint) to select relevant render-mode cells and exclusions. Retain explicit static SSR, streaming, and rehydration checks when affected; do not infer correctness from one render mode.
- Root-component and render-mode APIs must carry only serializable parameters and required metadata across process or host boundaries, including parameter definitions needed for unmatched values.
- Treat Auto as a per-activation renderer choice based on cache and runtime availability; once selected for a component activation, retain that assignment and test both cached and uncached paths.
- If a host cannot understand a known render-mode marker or descriptor, ignore unsupported host-specific markers where safe instead of failing unrelated startup paths.
- Server interactivity options belong with circuit or remote-renderer infrastructure; WebAssembly-only behavior belongs with WebAssembly boot or client infrastructure.

### Prerendering, static SSR, streaming, and state persistence

- Components must distinguish prerender/static SSR from later interactivity; browser-only work, JS interop, `ElementReference` access, and DOM mutation belong after the interactive render point.
- Account for double execution and rehydration: initialization, parameter application, persistent state, antiforgery state, and user-visible side effects must not run twice accidentally.
- Streaming SSR and enhanced navigation responses should converge to the latest desired DOM state; orphaned streaming updates from superseded navigations must be ignored or cancelled deterministically.
- Persisted component state should serialize only required data; protect Server-consumed state with ASP.NET Core data protection, exclude secrets from WebAssembly or Auto client-readable state, and persist metadata and state atomically under consistent size limits.
- Track quiescence through existing renderer and `SetParametersAsync` task flows rather than new public wait hooks unless the extension point is broadly useful.

### Lifecycle, async flow, and renderer synchronization

- Distinguish `SetParametersAsync`, `OnInitialized{Async}`, `OnParametersSet{Async}`, and `OnAfterRender{Async}`; initialization, parameter-change handling, and DOM-dependent work must be in the correct lifecycle stage.
- Validate `ComponentBase` lifecycle changes against synchronous success, asynchronous success, cancellation, and exception paths, including `ErrorBoundary` wrapping and resulting `StateHasChanged` behavior.
- Use `async`/`await` and renderer `Dispatcher.InvokeAsync` for continuations that touch component state; avoid `ContinueWith`, sync-over-async, and background mutations that bypass the renderer context.
- Do not call `StateHasChanged` redundantly after normal event callbacks when the framework already rerenders; call it explicitly for external updates that bypass parameter binding or event dispatch.
- Fire-and-forget work must have an owning lifetime, preserved exceptions or cancellation, and a documented reason it is safe not to await.

### Parameters, cascading values, and binding

- Component parameter names and deserialization must remain case-insensitive, including prerendered, restored, and transport payload paths.
- Treat `ParameterView` as batch-scoped data; do not retain old views after pooled render data can be recycled, and test helper APIs as well as enumeration.
- Framework-supplied parameters, including form-bound values, may overwrite property initializers; defaults that must survive binding belong in lifecycle logic.
- Required, optional, cascading, and two-way bound parameters should expose clear contracts, nullability, and validation without depending on a specific application binding framework.
- Prefer `EventCallback` and `Value`/`ValueChanged`/`ValueExpression` patterns that preserve async callbacks, validation, and parent-child synchronization.

### Rendering, diffing, DOM synchronization, sections, and virtualization

- `RenderTreeBuilder` sequence numbers, regions, stable sorts, and degenerate comparisons must tolerate valid compiler/runtime call patterns without corrupting render batches.
- Use stable `@key` and component identity rules when preserving instances matters; weak or unstable identifiers may recreate components but must not break app correctness.
- Keep render-batch DOM synchronization incremental and scoped to changed regions; enhanced navigation intentionally diffs the whole document, so do not embed component-specific knowledge where shared DOM sync abstractions should own it.
- Virtualization and scroll-convergence logic should use narrow DOM signals, guard against browser overflow anchoring, and avoid MutationObserver feedback loops triggered by unrelated DOM churn.
- Section, head, title, and attribute updates should preserve user-provided attributes and target the semantically correct DOM node without unnecessary markup or global selectors.

### Events, callbacks, navigation, and enhanced navigation

- Event dispatch must preserve cancellation and exception semantics; real handler failures should flow through the host's explicit error path, not be swallowed or double-reported.
- Custom browser event args require intentional opt-in before untrusted browser data is deserialized, while built-in DOM events must keep their known `EventArgs` mappings for compatibility.
- Location-changing and navigation interception APIs need awaitable handlers, history state, deterministic cancellation, observable outcomes when exposed, and parity between programmatic and JS-initiated navigation.
- Blazor router precedence must reject non-optional parameters after optional parameters, prefer exact matches by specificity (literal, non-optional parameter, optional parameter), and choose the most-specific route over wildcard or optional matches.
- Enhanced navigation must preserve browser behavior: exclude download and non-navigation links, use real page loads for external replace-history navigations, and keep server and client `NavigationManager` state synchronized.
- Enhanced form and navigation tests should use explicit promises, per-test storage IDs, and isolated hooks instead of timeouts or shared ambient state.

### JS interop, browser APIs, and serialization

- `IJSRuntime` calls must be guarded by render-mode availability: unavailable during static SSR or prerendering, disconnect-prone on Server, and asynchronous across host boundaries unless a sync contract is explicit.
- Preserve public JS interop surface compatibility, including low-level runtime entry points, nullability, `params` argument behavior, and caller-selected result types.
- Dispose `IJSObjectReference`, `DotNetObjectReference`, event listeners, modules, and browser object URLs deterministically; tolerate expected disconnect failures during cleanup.
- JS interop payloads on hot paths should minimize round trips, payload size, marshaling, and object identity duplication; prefer existing object-reference infrastructure over parallel channels.
- Use source-generated JSON serialization contexts where reflection would break trimming/AOT or add unsupported runtime dependencies; keep browser data normalization precise and platform-faithful.

### Disposal, cancellation, circuits, and resource ownership

- Implement `IDisposable` and `IAsyncDisposable` according to the resource being owned; async-only cleanup should not be hidden behind a synchronous path that blocks or skips required work.
- Components and services must unsubscribe from long-lived events, cancel timers and pending operations, dispose JS references, and prevent callbacks after component or circuit disposal.
- Cancellation should cancel work before disposal invalidates state; call `Cancel` before disposing token sources when consumers may still observe cancellation.
- Open interactive Server circuits before accepting JS interop, but keep long-running initialization in awaitable paths that tests and error handling can observe.
- Shared mutable circuit, renderer, logger, cache, and WebAssembly state needs thread-safe ownership even when today’s host usually runs single-threaded.

### Forms, validation, antiforgery, and file handling

- `EditForm`, `EditContext`, `FieldIdentifier`, validation message components, and form CSS policy should compose through existing forms infrastructure instead of duplicating reflection or field-resolution logic.
- Form identity and field equality must use reference-appropriate semantics so model overrides cannot corrupt edit tracking or validation state.
- Enhanced form posts, streaming form rendering, and traditional submissions need distinct handling that preserves validation, state updates, and error reporting.
- Browser-facing file APIs must expose explicit size and count limits, validate at the component or callback boundary, and account for Blazor Server resource exhaustion.
- Client-side downloads should keep Blob and `createObjectURL` as the compatibility baseline, but large downloads should prefer streaming-capable browser APIs to avoid buffering entire files or exhausting browser memory.
- Antiforgery tokens and form-related persistent state must survive Server, WebAssembly, Auto, and static SSR transitions without leaking or trusting client-modifiable data.

### Security and trust boundaries

- Treat interactive Server circuits as authenticated, stateful connections whose authentication changes, reconnects, JS callbacks, and persisted state require explicit synchronization or reload behavior.
- Do not override protocol-critical authentication settings at runtime; OIDC and remote-auth flows must honor configured security semantics.
- Keep OIDC, antiforgery, and Data Protection primitive changes in their owning security infrastructure while reviewing Components integration, interactive Server circuits, and component-state security here.
- Use `MarkupString` only for trusted content and ensure browser-deserialized event or form data cannot cross into privileged .NET code without intentional validation.
- Server-consumed component-state payloads embedded in HTML or transport data should be encrypted and integrity-protected; WebAssembly or Auto client-readable payloads must contain no secrets, and all modes should avoid exposing full type names or internal structure unless required for correctness.
- Browser features with security headers, such as multithreaded WebAssembly or `SharedArrayBuffer`, must set the required cross-origin policies when enabled.

### WebAssembly boot, static assets, and build packaging

- WebAssembly boot should respect `@microsoft/dotnet-runtime` ownership of resource loading; custom loaders must preserve the runtime integrity contract, and local hashes should remain deterministic Auto cache-readiness heuristics rather than timing-based fallbacks.
- Boot manifests should include only runtime resource kinds the loader understands; unrelated static assets belong in static web asset manifests, not boot metadata.
- Static web asset base paths, publish layouts, and hosted/standalone outputs must handle collisions explicitly and use segment-aware path rewrites.
- Build tasks should avoid version-sensitive runtime dependencies that are unsafe in MSBuild task hosts and regenerate outputs only when meaningful inputs change.
- For publish-time trimming and Native AOT validation, follow [trimming behavior guidance](Trimming.md#validate-trimming-behavior). AOT, trimming, lazy-loaded assemblies, and source-generated serialization changes still need tests or annotations that survive publish, not local suppressions that disappear.

### Tests, diagnostics, and repo fit

- Apply the [repository test requirements](../CONTRIBUTING.md#tests) and [faithful-validation requirements](../.github/copilot-instructions.md#running-tests) when validating changed behavior. Add focused unit, E2E, or browser tests across relevant render modes; include prerendering, interactivity, navigation, forms, serialization, cancellation, disposal, and error paths when affected. For Components E2E work, also follow [Components E2E guidance](../src/Components/AGENTS.md#creating-e2e-tests); generic JSInterop-only work does not inherit that workflow.
- Blazor async tests should be deterministic: use `TaskCompletionSource`, cancellation registration, explicit browser promises, and direct completion hooks rather than delays or timing-sensitive polling.
- Assert non-default observable values, browser console/network behavior, render output, route selection, validation messages, logs, and resource cleanup instead of mirroring helper implementation.
- Keep diagnostics actionable but not noisy: use existing .NET or browser logging channels, include recovery guidance for deployment or startup races, and avoid masking unexpected errors with console-only logging.
