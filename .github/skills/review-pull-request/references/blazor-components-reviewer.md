### Blazor Components reviewer

Review only ASP.NET Core Blazor and Razor Components runtime work under `src/Components/**` and `src/JSInterop/**` — rendering, lifecycle, render modes, JS interop, navigation, forms, and interactive Server circuits. The Scope wave lists the full change taxonomy.

This file is reference material. The `review-pull-request` skill and `pull-request-review` workflow
give each dimension below an independent, single-dimension pass.

#### Overarching principles

- **Preserve behavior across hosting models:** Server, WebAssembly, Auto, static SSR, prerendered, streaming, and rehydrated flows stay coherent; no single renderer owns every scenario.
- **Keep framework layers separated:** core Components abstractions must not absorb endpoint, hosting, browser, or circuit specifics unless intentionally general.
- **Let the renderer own component state:** lifecycle continuations, events, disposal, and `StateHasChanged` go through the renderer dispatcher or circuit synchronization context.
- **Treat JS interop and browser state as availability- and lifetime-sensitive:** `IJSRuntime`, `IJSObjectReference`, `ElementReference`, DOM callbacks, and browser resources need render-mode guards and deterministic cleanup.
- **Prove observable behavior in tests** — browser, renderer, routing, validation, serialization — not helper equivalence.

#### Review dimensions

##### Scope, layering, and public API shape

- CHECK: Keep render-mode, endpoint, hosting, environment, `HttpContext`, and circuit-specific logic in the assembly that owns that environment; do not move server-only concepts into core Components or Components.Web for one scenario.
- CHECK: Public Components and JS interop APIs must have narrow names that describe the scenario, preserve existing overload compatibility, and expose only genuinely general extension points.
- CHECK: Use `Microsoft.Extensions.Options` and idempotent DI registration patterns for framework configuration; avoid duplicate registrations or hidden dependencies omitted by slim builders.
- CHECK: Keep source-generated or framework-only plumbing internal unless public generation contracts require access; expose strongly typed surfaces rather than untyped internal mechanisms.
- CHECK: New public APIs require XML documentation for consumer-observable behavior, not internal lifecycle narration.

##### Render modes and hosting boundaries

- CHECK: Review behavior separately for Server, WebAssembly, Auto, static SSR, prerendered, and non-prerendered flows; do not infer correctness from one render mode.
- CHECK: Root-component and render-mode APIs must carry only serializable parameters and required metadata across process or host boundaries, including parameter definitions needed for unmatched values.
- CHECK: Treat Auto as a per-activation renderer choice based on cache and runtime availability; once selected for a component activation, retain that assignment and test both cached and uncached paths.
- CHECK: If a host cannot understand a known render-mode marker or descriptor, ignore unsupported host-specific markers where safe instead of failing unrelated startup paths.
- CHECK: Server interactivity options belong with circuit or remote-renderer infrastructure; WebAssembly-only behavior belongs with WebAssembly boot or client infrastructure.

##### Prerendering, static SSR, streaming, and state persistence

- CHECK: Components must distinguish prerender/static SSR from later interactivity; browser-only work, JS interop, `ElementReference` access, and DOM mutation belong after the interactive render point.
- CHECK: Account for double execution and rehydration: initialization, parameter application, persistent state, antiforgery state, and user-visible side effects must not run twice accidentally.
- CHECK: Streaming SSR and enhanced navigation responses should converge to the latest desired DOM state; orphaned streaming updates from superseded navigations must be ignored or cancelled deterministically.
- CHECK: Persisted component state should serialize only required data; protect Server-consumed state with ASP.NET Core data protection, exclude secrets from WebAssembly or Auto client-readable state, and persist metadata and state atomically under consistent size limits.
- CHECK: Track quiescence through existing renderer and `SetParametersAsync` task flows rather than new public wait hooks unless the extension point is broadly useful.

##### Lifecycle, async flow, and renderer synchronization

- CHECK: Distinguish `SetParametersAsync`, `OnInitialized{Async}`, `OnParametersSet{Async}`, and `OnAfterRender{Async}`; initialization, parameter-change handling, and DOM-dependent work must be in the correct lifecycle stage.
- CHECK: Validate `ComponentBase` lifecycle changes against synchronous success, asynchronous success, cancellation, and exception paths, including `ErrorBoundary` wrapping and resulting `StateHasChanged` behavior.
- CHECK: Use `async`/`await` and renderer `Dispatcher.InvokeAsync` for continuations that touch component state; avoid `ContinueWith`, sync-over-async, and background mutations that bypass the renderer context.
- CHECK: Do not call `StateHasChanged` redundantly after normal event callbacks when the framework already rerenders; call it explicitly for external updates that bypass parameter binding or event dispatch.
- CHECK: Fire-and-forget work must have an owning lifetime, preserved exceptions or cancellation, and a documented reason it is safe not to await.

##### Parameters, cascading values, and binding

- CHECK: Component parameter names and deserialization must remain case-insensitive, including prerendered, restored, and transport payload paths.
- CHECK: Treat `ParameterView` as batch-scoped data; do not retain old views after pooled render data can be recycled, and test helper APIs as well as enumeration.
- CHECK: Framework-supplied parameters, including form-bound values, may overwrite property initializers; defaults that must survive binding belong in lifecycle logic.
- CHECK: Required, optional, cascading, and two-way bound parameters should expose clear contracts, nullability, and validation without depending on a specific application binding framework.
- CHECK: Prefer `EventCallback` and `Value`/`ValueChanged`/`ValueExpression` patterns that preserve async callbacks, validation, and parent-child synchronization.

##### Rendering, diffing, DOM synchronization, sections, and virtualization

- CHECK: `RenderTreeBuilder` sequence numbers, regions, stable sorts, and degenerate comparisons must tolerate valid compiler/runtime call patterns without corrupting render batches.
- CHECK: Use stable `@key` and component identity rules when preserving instances matters; weak or unstable identifiers may recreate components but must not break app correctness.
- CHECK: Keep render-batch DOM synchronization incremental and scoped to changed regions; enhanced navigation intentionally diffs the whole document, so do not embed component-specific knowledge where shared DOM sync abstractions should own it.
- CHECK: Virtualization and scroll-convergence logic should use narrow DOM signals, guard against browser overflow anchoring, and avoid MutationObserver feedback loops triggered by unrelated DOM churn.
- CHECK: Section, head, title, and attribute updates should preserve user-provided attributes and target the semantically correct DOM node without unnecessary markup or global selectors.

##### Events, callbacks, navigation, and enhanced navigation

- CHECK: Event dispatch must preserve cancellation and exception semantics; real handler failures should flow through the host's explicit error path, not be swallowed or double-reported.
- CHECK: Custom browser event args require intentional opt-in before untrusted browser data is deserialized, while built-in DOM events must keep their known `EventArgs` mappings for compatibility.
- CHECK: Location-changing and navigation interception APIs need awaitable handlers, history state, deterministic cancellation, observable outcomes when exposed, and parity between programmatic and JS-initiated navigation.
- CHECK: Blazor router precedence must reject non-optional parameters after optional parameters, prefer exact matches by specificity (literal, non-optional parameter, optional parameter), and choose the most-specific route over wildcard or optional matches.
- CHECK: Enhanced navigation must preserve browser behavior: exclude download and non-navigation links, use real page loads for external replace-history navigations, and keep server and client `NavigationManager` state synchronized.
- CHECK: Enhanced form and navigation tests should use explicit promises, per-test storage IDs, and isolated hooks instead of timeouts or shared ambient state.

##### JS interop, browser APIs, and serialization

- CHECK: `IJSRuntime` calls must be guarded by render-mode availability: unavailable during static SSR or prerendering, disconnect-prone on Server, and asynchronous across host boundaries unless a sync contract is explicit.
- CHECK: Preserve public JS interop surface compatibility, including low-level runtime entry points, nullability, `params` argument behavior, and caller-selected result types.
- CHECK: Dispose `IJSObjectReference`, `DotNetObjectReference`, event listeners, modules, and browser object URLs deterministically; tolerate expected disconnect failures during cleanup.
- CHECK: JS interop payloads on hot paths should minimize round trips, payload size, marshaling, and object identity duplication; prefer existing object-reference infrastructure over parallel channels.
- CHECK: Use source-generated JSON serialization contexts where reflection would break trimming/AOT or add unsupported runtime dependencies; keep browser data normalization precise and platform-faithful.

##### Disposal, cancellation, circuits, and resource ownership

- CHECK: Implement `IDisposable` and `IAsyncDisposable` according to the resource being owned; async-only cleanup should not be hidden behind a synchronous path that blocks or skips required work.
- CHECK: Components and services must unsubscribe from long-lived events, cancel timers and pending operations, dispose JS references, and prevent callbacks after component or circuit disposal.
- CHECK: Cancellation should cancel work before disposal invalidates state; call `Cancel` before disposing token sources when consumers may still observe cancellation.
- CHECK: Open interactive Server circuits before accepting JS interop, but keep long-running initialization in awaitable paths that tests and error handling can observe.
- CHECK: Shared mutable circuit, renderer, logger, cache, and WebAssembly state needs thread-safe ownership even when today’s host usually runs single-threaded.

##### Forms, validation, antiforgery, and file handling

- CHECK: `EditForm`, `EditContext`, `FieldIdentifier`, validation message components, and form CSS policy should compose through existing forms infrastructure instead of duplicating reflection or field-resolution logic.
- CHECK: Form identity and field equality must use reference-appropriate semantics so model overrides cannot corrupt edit tracking or validation state.
- CHECK: Enhanced form posts, streaming form rendering, and traditional submissions need distinct handling that preserves validation, state updates, and error reporting.
- CHECK: Browser-facing file APIs must expose explicit size and count limits, validate at the component or callback boundary, and account for Blazor Server resource exhaustion.
- CHECK: Client-side downloads should keep Blob and `createObjectURL` as the compatibility baseline, but large downloads should prefer streaming-capable browser APIs to avoid buffering entire files or exhausting browser memory.
- CHECK: Antiforgery tokens and form-related persistent state must survive Server, WebAssembly, Auto, and static SSR transitions without leaking or trusting client-modifiable data.

##### Security and trust boundaries

- CHECK: Treat interactive Server circuits as authenticated, stateful connections whose authentication changes, reconnects, JS callbacks, and persisted state require explicit synchronization or reload behavior.
- CHECK: Do not override protocol-critical authentication settings at runtime; OIDC and remote-auth flows must honor configured security semantics.
- CHECK: Defer OIDC, antiforgery, and Data Protection primitive changes to [auth-security-reviewer.md](auth-security-reviewer.md); keep interactive Server circuit and component-state security review here.
- CHECK: Use `MarkupString` only for trusted content and ensure browser-deserialized event or form data cannot cross into privileged .NET code without intentional validation.
- CHECK: Server-consumed component-state payloads embedded in HTML or transport data should be encrypted and integrity-protected; WebAssembly or Auto client-readable payloads must contain no secrets, and all modes should avoid exposing full type names or internal structure unless required for correctness.
- CHECK: Browser features with security headers, such as multithreaded WebAssembly or `SharedArrayBuffer`, must set the required cross-origin policies when enabled.

##### WebAssembly boot, static assets, and build packaging

- CHECK: WebAssembly boot should respect `@microsoft/dotnet-runtime` ownership of resource loading; custom loaders must preserve the runtime integrity contract, and local hashes should remain deterministic Auto cache-readiness heuristics rather than timing-based fallbacks.
- CHECK: Boot manifests should include only runtime resource kinds the loader understands; unrelated static assets belong in static web asset manifests, not boot metadata.
- CHECK: Static web asset base paths, publish layouts, and hosted/standalone outputs must handle collisions explicitly and use segment-aware path rewrites.
- CHECK: Build tasks should avoid version-sensitive runtime dependencies that are unsafe in MSBuild task hosts and regenerate outputs only when meaningful inputs change.
- CHECK: AOT, trimming, lazy-loaded assemblies, and source-generated serialization changes need tests or annotations that survive publish, not local suppressions that disappear.

##### Tests, diagnostics, and repo fit

- CHECK: Add focused unit, E2E, or browser tests for changed observable behavior across relevant render modes; include prerendering, interactivity, navigation, forms, serialization, cancellation, disposal, and error paths when affected.
- CHECK: Blazor async tests should be deterministic: use `TaskCompletionSource`, cancellation registration, explicit browser promises, and direct completion hooks rather than delays or timing-sensitive polling.
- CHECK: Assert non-default observable values, browser console/network behavior, render output, route selection, validation messages, logs, and resource cleanup instead of mirroring helper implementation.
- CHECK: Keep diagnostics actionable but not noisy: use existing .NET or browser logging channels, include recovery guidance for deployment or startup races, and avoid masking unexpected errors with console-only logging.
- CHECK: Follow Components sample and E2E workflow guidance when implementing changes; for review-only findings, require enough targeted validation to cover the changed contract without asking for full-suite runs.
