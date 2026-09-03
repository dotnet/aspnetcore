### MVC, Razor, and routing reviewer

Review MVC, Razor, and MVC-owned routing behavior in `src/Mvc/**`, `src/Razor/**`, and `src/Html.Abstractions/**`. Minimal API runtime and OpenAPI generation belong to the minimal-api-openapi reviewer; coordinate only when shared metadata or routing infrastructure is intentionally affected.

This file is reference material. The `review-pull-request` skill gives each dimension below an
independent, single-dimension pass.

#### Overarching principles

- Prefer compatibility and documented extensibility over local simplification; these layers expose public, protected, analyzer, metadata, and tooling contracts.
- Keep pipeline semantics aligned: action selection, model binding, validation, filters, formatters, endpoint routing, link generation, and Razor rendering must agree on route values, metadata, nullability, culture, and error behavior.
- Treat generated output and metadata as contracts: Razor generated code, application models, descriptors, API descriptions, validation metadata, and route patterns stay deterministic and tooling-friendly.
- Separate startup, build, design-time, and request-path work; cache reflection, route, descriptor, compiled Razor, and formatter artifacts only with clear invalidation and thread-safety rules.
- Tests should prove observable behavior through responses, model state, generated links, rendered HTML, diagnostics, and build artifacts, not mirror helper implementation details.

#### Review dimensions

##### Scope, ownership, and API compatibility

- CHECK: Keep MVC/Razor behavior in the MVC, Razor, or HTML abstraction layers; don't move Minimal API, OpenAPI, Kestrel, or application-specific policy into this area.
- CHECK: Preserve public, protected, abstract, virtual, constructor, property, analyzer, and XML-doc contracts unless the change has a deliberate compatibility plan with alternatives, obsoletion, and tests.
- CHECK: Keep implementation plumbing internal by default; expose interfaces, abstract classes, virtual members, metadata types, or options only for a concrete extension scenario.
- CHECK: Follow current ASP.NET Core naming, nullability, options, DI, provider, convention, and extension-method patterns; avoid duplicate abstractions or layering shortcuts when an existing contract can carry the metadata.
- CHECK: Make compatibility switches, feature gates, and migration paths explicit; don't rely on current default behavior when the API lets callers omit values or metadata.

##### Controllers, actions, descriptors, and action selection

- CHECK: Controller and action discovery stays deterministic across naming conventions, visibility, attributes, generic types, inherited members, and application-model conventions.
- CHECK: Action selection applies stable disambiguation across HTTP methods, action names, selector metadata, route values, defaults, overloads, and ambiguous candidates; preserve endpoint declaration or registration order only where routing/action-selection contracts use it.
- CHECK: Action descriptors, controller models, page descriptors, and parameter descriptors stay plain data containers with deterministic metadata ordering and convention application.
- CHECK: Action invocation supports sync and async actions, cancellation tokens, exceptions, nullable/default parameters, generic return types, and result-type mapping without hiding binding or execution failures.
- CHECK: Runtime result mapping, API descriptions, ApiExplorer metadata, and analyzers agree about controller/action behavior so tooling doesn't report a different contract than the runtime uses; coordinate with [minimal-api-openapi-reviewer.md](minimal-api-openapi-reviewer.md) when shared metadata affects Minimal API/OpenAPI parity.

##### Model binding, value providers, and metadata

- CHECK: Merge binding info from attributes, parameter/property metadata, model metadata, and conventions through the standard helpers and provider ordering; don't manually assign duplicated binding state that can drift.
- CHECK: Binding sources (body, route, query, header, form, files, services) are explicit when ambiguity changes behavior, and every source reports success, failure, or no-result through the standard model-binding pipeline.
- CHECK: Custom binders and binder providers preserve provider ordering, avoid repeated factory work, distinguish missing data from conversion failure, and populate `ModelState` with property-path-aware errors.
- CHECK: Complex-type, collection, dictionary, record, constructor, optional-parameter, nullable, empty-string, and value-type binding validate supported shapes before activation and handle defaults deliberately.
- CHECK: Form and file handling stays in value providers, model binders, and form options rather than input/output formatter options; preserve buffering, threshold, and temporary-file behavior through the model-binding path.
- CHECK: Model metadata caches and property collections use deterministic ordering, immutable or defensive data, correct invalidation keys, and thread-safe lazy initialization.

##### Model validation, ApiController behavior, and ProblemDetails

- CHECK: Validation timing is explicit for top-level nodes, properties, constructor parameters, records, `[Required]`, nullable-reference validation metadata, non-nullable value types, and custom validators; don't treat C# `required` or `RequiredMemberAttribute` alone as MVC validation metadata unless the change intentionally adds and tests that contract.
- CHECK: Binding failures, validation failures, empty bodies, missing required values, and formatter exceptions map to the intended `ModelState`, `ApiController` automatic responses, and `ProblemDetails` payloads.
- CHECK: Validation and API-convention metadata stays discoverable and extensible through providers, attributes, conventions, and static API convention types rather than hard-coded response assumptions.
- CHECK: Runtime behavior, generated API descriptions, analyzer diagnostics, and tests stay in sync for status codes, result types, error keys, and validation metadata.
- CHECK: Validation and model-binding messages use resource or message-provider paths so localization, culture-sensitive formatting, and customization keep working.

##### Filters, results, CORS, and cross-cutting metadata

- CHECK: Authorization, resource, action, exception, and result filters execute in documented order across global, controller, and action scopes; filter metadata projected onto endpoints must preserve routing metadata semantics without inventing an endpoint `FilterScope`.
- CHECK: Short-circuit paths preserve context state, still run the filters the contract requires, and avoid double execution when a filter sets results, handles exceptions, or skips `next()`.
- CHECK: Sync and async filters compose without sync-over-async, lost exceptions, or stale context reuse; filter factories cache only immutable or safely reusable instances.
- CHECK: Action results validate required services and route/link data before execution, preserve filter context, return semantically correct status codes, and avoid null-success ambiguity.
- CHECK: CORS, authorization, antiforgery, and other cross-cutting metadata flow through MVC filters and endpoint routing without assuming attributes own behavior they only describe.

##### Input and output formatters, content negotiation, and serialization

- CHECK: Input and output formatters honor media types, charsets, encodings, `Accept`/`Content-Type` negotiation, formatter ordering, empty/null body rules, and complete request/response body handling.
- CHECK: Formatter errors produce actionable model-state or exception information without losing the original parse or serialization context.
- CHECK: JSON, XML, and custom formatter options come from MVC options, registered services, or documented providers rather than ad hoc serializer instances.
- CHECK: Buffering, thresholds, temporary files, streams, and readers/writers are disposed or reused safely and don't introduce unbounded per-request memory use.
- CHECK: Formatter extensibility composes with endpoint metadata, `ApiController` behavior, result types, and content negotiation instead of bypassing the standard MVC pipeline.

##### Endpoint routing, route templates, constraints, and link generation

- CHECK: Route templates are parsed and validated early for braces, separators, optional parameters, catch-alls, complex segments, defaults, route names, and invalid parameter text with clear errors.
- CHECK: Attribute routes default to order 0 unless explicitly ordered; routing, endpoint metadata, and action selection preserve route order, precedence, comparer policy semantics, conventional/dynamic route registration order, and ambiguity reporting without treating source declaration position as an attribute-route tie-breaker.
- CHECK: Route constraints evaluate before model binding where routing owns the decision, handle expected failures without throwing, and use copied route data so failed matches don't mutate parent state.
- CHECK: Link generation through endpoint routing, `IUrlHelper`, HTML helpers, tag helpers, and route-name APIs round-trips with matching constraints, encodes values correctly, and treats ambient values and nullable route names deliberately.
- CHECK: Route and link-generation caches account for endpoint data-source changes, route names, templates, defaults, constraints, comparer semantics, and thread-safe invalidation.

##### Razor compilation, code generation, and build integration

- CHECK: Razor parsing, directives, imports, generated C#, line pragmas, nullable annotations, tag helper binding, and design-time output preserve runtime semantics and debugging accuracy.
- CHECK: Generated Razor output, baselines, manifests, scoped assets, and compiler arguments stay deterministic and incremental, updated only when the changed behavior requires it.
- CHECK: Razor parsing, diagnostics, line pragmas, and source mapping handle CRLF, LF, CR, and mixed line endings consistently across developer, CI, and deployed environments.
- CHECK: Scoped CSS selector rewriting preserves valid CSS across pseudo-classes, deep combinators, imports, IDs, globals, and package styles; verify browser-visible behavior plus exact build/publish artifacts such as compressed assets, cache-control headers, manifests, and scoped-CSS bundles.
- CHECK: Razor build and runtime features are gated by supported target frameworks, language versions, and compatibility settings rather than silently changing behavior for existing apps.
- CHECK: Razor compilation and page-descriptor caches are thread-safe, invalidated by the correct file or endpoint changes, and cache shared async compilation work as reusable `Task<CompiledViewDescriptor>` values rather than non-reusable `ValueTask` instances.
- CHECK: Roslyn, response-file, MSBuild target, and SDK integration follow repository build conventions and avoid rewriting unchanged outputs that trigger downstream work.

##### Razor Pages, views, discovery, and rendering

- CHECK: Razor Pages routing respects explicit `@page` routes and configured conventions; transform folder/file-name-derived routes only when that is the documented behavior.
- CHECK: View, page, partial, layout, component, `_ViewImports`, and area discovery use accurate paths for lookup, caching, diagnostics, line mapping, and debugger experience.
- CHECK: View lookup caches key on view name, page path, controller, area, expander values, culture-sensitive inputs, and change tokens so stale views aren't reused.
- CHECK: `ViewData`, `ViewBag`, model values, template metadata, and formatted model values handle nulls, inheritance, and scope changes without mutating the wrong rendering context.
- CHECK: `TempData` cookie and session providers preserve DataProtection serialization, `Keep`/`Peek` lifecycle, redirect round-trips, cookie consent and attributes, and failure handling.
- CHECK: View buffers, sections, response-start callbacks, and component rendering release pooled resources, avoid retaining large arrays, and preserve output order for sync and async rendering.

##### Tag helpers, HTML helpers, view components, and HTML content

- CHECK: Tag helpers bind attributes, names, case-insensitive matches, dictionaries, child content, and `ViewContext` per Razor/MVC conventions without accidentally broadening their target element scope.
- CHECK: HTML helpers, tag helpers, and view components use `IHtmlContent`, encoders, `HtmlContentBuilder`, and trusted `Html.Raw` paths deliberately so output is encoded exactly once.
- CHECK: Helper APIs that mutate dictionaries, route values, attributes, or `ViewData` document that mutation and make defensive copies when internal normalization could surprise callers.
- CHECK: Sync and async helper/component overloads stay paired and share semantics without blocking asynchronous child content or requiring services before they're needed.
- CHECK: HTML generator/helper parameter ordering, generic names, overload shape, and extension methods stay consistent across related APIs to avoid breaking implementers.

##### Performance, caching, concurrency, and resource lifetime

- CHECK: Identify request-path work before raising allocation findings; avoid repeated reflection, descriptor construction, route parsing, substring creation, array allocation, and closure capture on per-request paths.
- CHECK: Cache reflection results, model metadata, filters, binder decisions, formatters, route matches, link-generation candidates, compiled views, and page descriptors only when ownership, invalidation, and comparer semantics are clear.
- CHECK: Cached metadata, collections, dictionaries, and descriptors are immutable, defensively copied, or synchronized so callers can't mutate shared state across requests or scopes.
- CHECK: Async MVC/Razor pipeline code avoids blocking waits, unsafe sync bridges, re-awaiting non-reusable values, or dropping cancellation and exception context.
- CHECK: MVC/Razor model metadata, action discovery, tag helpers, generated Razor code, and reflection caches are trimming/AOT-sensitive; require annotations, source-generated alternatives, or compatibility tests when changes affect reflected shapes.
- CHECK: Dispose streams, readers, writers, pooled buffers, temporary files, and compiler resources on success, failure, cancellation, and early-return paths; clear large buffers before pooling when they can retain sensitive data.

##### Localization, diagnostics, documentation, and tests

- CHECK: Error messages, resource strings, XML docs, comments, and samples describe actual behavior, constraints, defaults, and applicability; stale or misleading documentation is a product bug.
- CHECK: Culture-sensitive parsing, formatting, casing, route values, validation messages, and localization options use invariant or current culture deliberately and are tested where behavior is observable.
- CHECK: Logs and diagnostics include useful route, action, type, field, path, formatter, constraint, and exception context while avoiding noisy request hot-path logging.
- CHECK: Add focused unit, functional, integration, or build tests at the lowest layer that proves the behavior; use server-hosted tests only when server integration is the contract being tested.
- CHECK: Tests cover success, failure, ambiguity, edge values, culture, cache invalidation, route/link round-trips, generated HTML, generated Razor/build output, and ApiDescription/ApiExplorer controller parity shared with Minimal API/OpenAPI without relying on implementation-only assertions; see [minimal-api-openapi-reviewer.md](minimal-api-openapi-reviewer.md) for the adjacent reviewer.
