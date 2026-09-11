# Blazor Components guidance

This guidance covers ASP.NET Core Blazor and Razor Components work under `src/Components/**` and
`src/JSInterop/**` — rendering, lifecycle, render modes, JS interop, navigation, forms, and
interactive Server circuits. Consult the relevant sections alongside the requested task and
applicable repository and area instructions.

The [Blazor Components architecture](../src/Components/ARCHITECTURE.md) is the canonical composition
and ownership model for `src/Components`. This guide complements it with task-oriented change and
validation guidance rather than defining a separate architecture.

## Overarching principles

- **Choose representative scenarios:** cover the render modes and lifecycle transitions affected by the change, including mixed-runtime cases where relevant.
- **Treat JS interop and browser state as availability- and lifetime-sensitive:** `IJSRuntime`, `IJSObjectReference`, `ElementReference`, DOM callbacks, and browser resources need render-mode guards and deterministic cleanup.

## Topics

### Scope, layering, and public API shape

- When changing shared abstractions, check consumers in other affected hosts for new environment-specific dependencies.
- Public Components and JS interop APIs must have narrow names that describe the scenario, preserve existing overload compatibility, and expose only genuinely general extension points.
- Use `Microsoft.Extensions.Options` and idempotent DI registration patterns for framework configuration; avoid duplicate registrations or hidden dependencies omitted by slim builders.
- Keep source-generated or framework-only plumbing internal unless public generation contracts require access; expose strongly typed surfaces rather than untyped internal mechanisms.
- For Components APIs, follow the [consumer-facing XML-documentation boundary](../src/Components/AGENTS.md#code-clarity-and-durable-knowledge); generic JSInterop APIs still require XML documentation for consumer-observable behavior, not internal lifecycle narration.

### Render modes and hosting boundaries

- For changes crossing Components renderers, runtimes, or DI scopes, use the [cross-runtime design checkpoint](../src/Components/AGENTS.md#cross-runtime-design-checkpoint) to select relevant render-mode cells and exclusions. Retain explicit static SSR, streaming, and rehydration checks when affected; do not infer correctness from one render mode.
- Root-component and render-mode APIs must carry only serializable parameters and required metadata across process or host boundaries, including parameter definitions needed for unmatched values.
- Cover Auto activation with both cached and uncached WebAssembly resources, including resources becoming available while an existing root remains active.
- If a host cannot understand a known render-mode marker or descriptor, ignore unsupported host-specific markers where safe instead of failing unrelated startup paths.

### Prerendering, static SSR, streaming, and state persistence

- Check browser-dependent initialization with prerendering enabled and disabled, including JS interop, `ElementReference`, and DOM access.
- For prerendered components, check restored parameters, state, and user-visible side effects across activation; cover both persisted-state reuse and initialization when no state is available.
- Cover overlapping enhanced navigations with delayed streaming updates; verify the final DOM belongs to the latest navigation.
- Persisted component state should serialize only required data; protect Server-consumed state with ASP.NET Core data protection, exclude secrets from WebAssembly or Auto client-readable state, and persist metadata and state atomically under consistent size limits.
- Track quiescence through existing renderer and `SetParametersAsync` task flows rather than new public wait hooks unless the extension point is broadly useful.

### Lifecycle, async flow, and renderer synchronization

- Distinguish `SetParametersAsync`, `OnInitialized{Async}`, `OnParametersSet{Async}`, and `OnAfterRender{Async}`; initialization, parameter-change handling, and DOM-dependent work must be in the correct lifecycle stage.
- Validate `ComponentBase` lifecycle changes against synchronous success, asynchronous success, cancellation, and exception paths, including `ErrorBoundary` wrapping and resulting `StateHasChanged` behavior.
- For asynchronous lifecycle changes, cover an incomplete await followed by a new parameter update, event, or disposal before the continuation resumes.
- Do not call `StateHasChanged` redundantly after normal event callbacks when the framework already rerenders; call it explicitly for external updates that bypass parameter binding or event dispatch.
- For fire-and-forget work started by a component callback, check that exceptions and cancellation remain observable when the component is disposed.

### Parameters, cascading values, and binding

- Component parameter names and deserialization must remain case-insensitive, including prerendered, restored, and transport payload paths.
- Treat `ParameterView` as batch-scoped data; do not retain old views after pooled render data can be recycled, and test helper APIs as well as enumeration.
- Framework-supplied parameters, including form-bound values, may overwrite property initializers; defaults that must survive binding belong in lifecycle logic.
- Required, optional, cascading, and two-way bound parameters should expose clear contracts, nullability, and validation without depending on a specific application binding framework.
- Prefer `EventCallback` and `Value`/`ValueChanged`/`ValueExpression` patterns that preserve async callbacks, validation, and parent-child synchronization.

### Rendering, diffing, DOM synchronization, sections, and virtualization

- `RenderTreeBuilder` sequence numbers, regions, stable sorts, and degenerate comparisons must tolerate valid compiler/runtime call patterns without corrupting render batches.
- Use stable `@key` and component identity rules when preserving instances matters; weak or unstable identifiers may recreate components but must not break app correctness.
- For DOM synchronization changes, cover interactive render batches alongside enhanced-navigation document updates so component updates do not overwrite unrelated content.
- Virtualization and scroll-convergence logic should use narrow DOM signals, guard against browser overflow anchoring, and avoid MutationObserver feedback loops triggered by unrelated DOM churn.
- Section, head, title, and attribute updates should preserve user-provided attributes and target the semantically correct DOM node without unnecessary markup or global selectors.

### Events, callbacks, navigation, and enhanced navigation

- Event dispatch must preserve cancellation and exception semantics; real handler failures should flow through the host's explicit error path, not be swallowed or double-reported.
- Custom browser event args require intentional opt-in before untrusted browser data is deserialized, while built-in DOM events must keep their known `EventArgs` mappings for compatibility.
- Location-changing and navigation interception APIs need awaitable handlers, history state, deterministic cancellation, observable outcomes when exposed, and parity between programmatic and JS-initiated navigation.
- Blazor router precedence must reject non-optional parameters after optional parameters, prefer exact matches by specificity (literal, non-optional parameter, optional parameter), and choose the most-specific route over wildcard or optional matches.
- Preserve exclusions for download and non-navigation links, real page loads for external replace-history navigation, and server/client `NavigationManager` synchronization.
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
- For caches or services shared across renderers or circuits, cover concurrent access independently of the component lifecycle tests.

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
