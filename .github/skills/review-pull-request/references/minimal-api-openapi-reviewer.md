### Minimal APIs & OpenAPI reviewer

Review `src/Http/**` and `src/OpenApi/**` changes for correctness at the boundary between runtime endpoint behavior and generated OpenAPI contracts. Prefer findings with a concrete endpoint shape, generated document delta, or user-visible compatibility impact.

This file is reference material. The `review-pull-request` skill and `pull-request-review` workflow
give each dimension below an independent, single-dimension pass.

#### Overarching principles

- Generated OpenAPI must describe what Minimal API and MVC endpoints actually bind, return, and expose through endpoint metadata.
- Spec fidelity and compatibility beat clever defaults. Use extension points for ambiguous or custom scenarios instead of throwing, omitting useful data, or changing shipped behavior without a migration path.
- Minimal APIs and MVC can produce different `ApiDescription` and model metadata shapes. Reconcile those shapes without mutating upstream descriptions.
- For MVC controller, `ApiExplorer`, or MVC-owned routing behavior, cross-check mvc-razor-routing-reviewer and keep this review scoped to Minimal API, OpenAPI, or shared generation code.
- Document generation runs both at request time and build time. Preserve DI lifetimes, cancellation, deterministic output, AOT/trimming safety, and source-generator resilience.
- Treat generated documents as a trust boundary: OpenAPI descriptions, XML comments, server URLs, and endpoint metadata can expose sensitive data or reflect user-controlled input.
- Drop style-only, analyzer-enforced, single-sample, and one-off fixture concerns unless they expose a product behavior gap.

#### Dimensions and CHECK items

##### Endpoint metadata and composition

- CHECK `Add*`, `Map*`, and supported `With*` extensions keep their conventional receiver, return value, namespace, and chaining semantics.
- CHECK built-in OpenAPI customization does not rely on obsolete `WithOpenApi` (`ASPDEPR002`); use `AddOpenApiOperationTransformer` or document, operation, and schema transformers for current built-in generation.
- CHECK group and endpoint metadata compose predictably: outer-to-inner ordering, endpoint overrides where intended, and endpoint-specific operation transformers additive rather than last-one-wins.
- CHECK endpoint filters preserve async flow, cancellation, DI/lifetime activation, exception propagation, short-circuiting, metadata visibility, and group-versus-endpoint ordering, including `HttpContext.RequestAborted`.
- CHECK result, `Accepts`, `Produces`, tags, names, and OpenAPI metadata map to request/response docs without losing duplicate status/content-type combinations or defaulting when explicit metadata exists.
- CHECK `TypedResults`, `Results<T1,...>`, and custom `IResult` metadata infer status, content-type, and schema unions that match runtime behavior and stay compatible with explicit `Produces` metadata.
- CHECK document names respect defaults, route/query selection, named options, case-insensitive routing, and multiple-document generation without surprising duplicate-registration behavior.

##### Parameter binding and request bodies

- CHECK parameter source matches runtime binding: path parameters are required, route constraints beat validation attributes, `TryParse`/enum underlying types are honored, and `[AsParameters]` property metadata is preserved.
- CHECK body, form, query, header, route, and custom binding sources map to the correct OpenAPI location or request body; unknown parsable non-route sources should have a safe default plus transformer escape hatch.
- CHECK request body schemas distinguish MVC-exploded form metadata from Minimal API complex form parameters; form payloads should flatten the fields that the binder reads, not wrap them under an artificial parameter name.
- CHECK special binder types are intentional and exact (`IFormFile`, `IFormFileCollection`, `Stream`, `PipeReader`, JSON Patch), with tests for both direct parameters and properties where supported.
- CHECK parameter descriptions and requiredness land on the OpenAPI parameter or request body when that is what callers see; defaults intentionally apply as schema keywords, such as through `ApplyDefaultValue`.

##### Operations, paths, and serialization

- CHECK path conversion handles root paths, route separators, grouped routes, tilde-prefixed routes, shared paths with different methods, and deterministic method/path ordering.
- CHECK HTTP method handling skips only missing or invalid method tokens; valid custom method tokens are converted to `HttpMethod` and emitted as OpenAPI operations. Fixes for missing MVC defaults belong in the ApiExplorer layer.
- CHECK `MapOpenApi` format selection emits the requested JSON/YAML bytes and content type; avoid breaking unsupported extensions, default versions, or browser-visible behavior without a compatibility plan.
- CHECK response generation merges same-status metadata by content type and schema, preserves descriptions intentionally, handles server-sent events item schemas, and supplies the default response only when none exists.
- CHECK server URLs come from request/server features and existing forwarded-headers middleware configuration; do not hand-parse forwarded headers in OpenAPI code.

##### Schemas, references, and nullability

- CHECK schema generation follows `System.Text.Json` type info and the current OpenAPI.NET model; use reflection only where serializer metadata cannot represent handler parameters, and guard `NullabilityInfoContext` feature-switch failures.
- CHECK nullable schemas use current `JsonSchemaType.Null`/OpenAPI.NET semantics. Componentized nullable schemas should use the current wrapper shape, not obsolete `Nullable` properties or older wrapper patterns.
- CHECK validation, default, XML/description, and route-constraint metadata apply to the correct inline or referenced schema; unparsable values should omit the keyword rather than crash document generation.
- CHECK schema reference IDs are stable, friendly, alphanumeric, configurable, collision-tested, and preserve distinct user types even when shapes are structurally identical.
- CHECK nested, collection, dictionary, self, circular, polymorphic, and relative references resolve to the right component without local `#` leaks, duplicate suffix drift, or shared mutable schema state.
- CHECK component registration through `AddComponent(schemaId, schema)` handles reference-ID collisions deterministically and preserves distinct user types instead of relying on structural equality or hash-code matches.
- CHECK `JsonNode` and OpenAPI schema mutation uses cloning where required by one-parent or reference-wrapper semantics, and final component ordering stays deterministic.

##### Transformers, DI, and lifecycle

- CHECK document, operation, and schema transformers run in registration order after framework-populated data they are expected to observe; endpoint-level transformers compose with global transformers.
- CHECK cancellation tokens flow through async generation and transformer APIs with `cancellationToken` last; document unavoidable build-time gaps.
- CHECK activated transformer instances resolve per invocation/scope and dispose transient instances correctly; never cache scoped/transient services in options or singleton wrappers.
- CHECK OpenAPI generation is thread-safe for request-time and build-time callers: mutable schemas, references, transformers, options, caches, and DI scopes must not leak or race across documents.
- CHECK service registrations use `TryAdd*` only for user-overridable services; internal required services can use direct registration. Prefer `IOptionsMonitor<T>` for named OpenAPI options.
- CHECK AOT/trimming annotations, request-delegate generation, and `DynamicallyAccessedMembers` requirements stay on the value that reaches reflection/activation.

##### XML comments and source generation

- CHECK XML comment discovery only includes accessible members the generated support code can reference; generated code suppresses obsolete-member warnings and escapes user text safely.
- CHECK malformed XML, unknown overload shapes, unsupported languages, and C# syntax assumptions degrade to no-op/diagnostic behavior rather than breaking user builds.
- CHECK member keys handle extension methods, async `Task`/`ValueTask`, open and substituted generics, nullable types, type parameters, and conversion operators consistently.
- CHECK comments apply to the right target: operation summary/description, single-response `<returns>`, parameter descriptions/examples, and schema/property descriptions. Edit reference targets, not read-only reference wrappers.
- CHECK OpenAPI text sourced from XML comments, endpoint metadata, server URLs, and descriptions is escaped and reviewed for sensitive or user-controlled disclosure across trust boundaries.
- CHECK build targets that add XML files or interceptors are commented, scoped to the intended inputs, and free of debug leftovers or sample-only shortcuts.

##### Compatibility, tests, and performance

- CHECK pseudo-public build-time interfaces, especially internal `Microsoft.Extensions.ApiDescriptions.IDocumentProvider` located by `dotnet getdocument` using its exact type name, preserve reflection-based namespace/signature compatibility; also check shipped API baselines, OpenAPI.NET quirks, Swagger UI behavior, and third-party generator expectations before changing signatures, defaults, or serialized shape.
- CHECK runtime and build-time generation, transformer failures, and endpoint-metadata problems produce actionable diagnostics or logging without swallowing errors or breaking builds unexpectedly.
- CHECK tests assert behavior, not only snapshots: actual YAML text, content-type dictionaries, transformer order/view, nullability, custom converters, duplicate metadata, reference stability, and case-insensitive document names.
- CHECK cover Minimal API and controller paths when shared code claims parity; keep HTTP-request-based integration tests only where request features are under review.
- CHECK hot paths avoid avoidable enumerators, repeated schema-ID work, and unnecessary buffers, but prefer correct lifetime/reference behavior over premature allocation savings.
- CHECK benchmarks exercise the changed path, include the right endpoint shape, and justify any readability or safety tradeoff in generation hot paths.
