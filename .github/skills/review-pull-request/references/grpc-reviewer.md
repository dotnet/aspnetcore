### gRPC reviewer

Review only the ASP.NET Core gRPC integration area in `src/Grpc/**`: server wire-up, service registration, JSON transcoding, OpenAPI-compatible metadata, interop/perf tests, and templates. The core gRPC implementation belongs in `grpc/grpc-dotnet`.

This file is reference material. The `review-pull-request` skill gives each dimension below an
independent, single-dimension pass.

#### Overarching principles

- Prefer protocol compatibility over local convenience. Match gRPC JSON transcoding, `Google.Protobuf`, and ASP.NET Core routing semantics unless the change documents and tests an intentional divergence.
- Preserve host extensibility. Registrations, options, descriptors, and generated metadata must compose with user services, slim builders, external OpenAPI tooling, interceptors, and existing gRPC server behavior.
- Separate startup work from request-path work. Startup parsing can be straightforward; per-request routing, JSON, buffering, and streaming paths need allocation and cancellation scrutiny.
- Treat diagnostics as part of the contract. User configuration errors should be observable; normal opt-out scenarios should stay quiet.
- Tests should prove the public behavior, not just mirror an implementation helper.

#### Review dimensions

##### Scope, ownership, and API shape

- CHECK: Keep `src/Grpc` focused on ASP.NET Core integration; do not move core channel, client, server, or protocol-library behavior into this mirror.
- CHECK: Public extension methods, options, settings, and metadata types must follow ASP.NET Core naming, XML documentation, chaining, and API-review expectations.
- CHECK: Treat gRPC JSON transcoding as distinct from regular gRPC; do not assume core gRPC constraints, AOT support, performance goals, or error formats automatically apply.
- CHECK: For trimming/AOT-sensitive changes, verify descriptor binding, protobuf JSON converters, JSON transcoding, and templates use source-generated metadata or accurate annotations/suppression justifications for reflection dependencies.
- CHECK: Prefer current repository and platform concepts over legacy local workarounds; retain compatibility notes only when they still affect the current code.

##### Service registration, options, and lifetimes

- CHECK: Use `TryAdd`, `TryAddEnumerable`, or equivalent idempotent registration for framework services and configurators when repeated calls should not duplicate work or override user services.
- CHECK: Ensure feature entry points add every required ASP.NET Core dependency, including routing services for generated constraints; do not rely on defaults omitted by slim builders.
- CHECK: Keep options, lazy serializer state, descriptor registries, and service singletons aligned so late-bound dependencies cannot drift from registered services.
- CHECK: For gRPC interceptors, verify global and per-service registration order, DI activation/lifetime, exception-to-status mapping, async continuations, and streaming-call interactions.
- CHECK: Make `IDisposable`/`IAsyncDisposable` ownership explicit for services, interceptors, converters, buffers, and interop-test processes so DI, host, fixture, and local-code cleanup responsibilities do not leak or double-dispose resources.
- CHECK: Make descriptor and enum caches thread-safe with clear ownership: lock around mutable sets that guard updates, use concurrent maps for read paths, and avoid duplicate-allowing collections for uniqueness.

##### Descriptor, field, and binding semantics

- CHECK: Resolve message, enum, nested, wrapper, well-known, and `Any` descriptors consistently, including payload types supplied only through a user `TypeRegistry`.
- CHECK: Match protobuf field lookup rules for proto names, JSON names, conflict precedence, map-entry descriptors, maps, repeated fields, and case-insensitive settings.
- CHECK: Keep route, query, request-body, and response-body binding mutually consistent: exclude route/body-bound fields from query binding, respect wildcard bodies, and reject unsupported nested body forms with clear errors.
- CHECK: Use dot notation for nested field paths and ensure internal route-value names are rewritten to public field paths before binding.
- CHECK: Nullable and member-not-null annotations must describe real invariants; do not use attributes to hide a possible descriptor or body-binding mismatch.

##### JSON transcoding and status behavior

- CHECK: Converter behavior must match protobuf JSON rules for field masks, `Any`, well-known types, default/presence semantics, enum names and numbers, enum prefix removal, and 64-bit numbers encoded as strings.
- CHECK: When code exposes metadata consumed by external Swagger/OpenAPI tooling, keep runtime JSON converters and that metadata compatible for custom field names, wrappers, maps, enums, response bodies, and well-known types; do not assume an in-repo schema generator exists.
- CHECK: Malformed client JSON or invalid request bodies should map to the appropriate gRPC status with an actionable message while preserving server-side exception details for diagnostics.
- CHECK: Preserve `RpcException` status and translate the supported `grpc-status-details-bin` status-detail trailer when transcoding errors; do not require arbitrary trailers to be emitted or preserved unless the changed code explicitly supports them, and wrap invalid status-detail trailers with an explicit parsing error.
- CHECK: Exception messages should include the invalid value or type when useful and follow .NET style without leaking stack traces unless detailed errors are enabled.

##### HTTP route patterns and metadata

- CHECK: Validate and translate HTTP patterns using gRPC transcoding semantics: leading slash, verb suffixes, empty verbs, multi-segment variables, catch-alls, extra slashes, and discard segments.
- CHECK: Let ASP.NET Core routing perform matching whenever possible; generated templates should preserve DFA routing instead of reimplementing route matching per request.
- CHECK: Escape generated regex constraints, require their services, and verify catch-all plus verb cases cannot match unintended suffixes.
- CHECK: HTTP method metadata should use the representation expected by ASP.NET Core routing and OpenAPI consumers.

##### Performance, buffering, and compatibility

- CHECK: Identify whether code runs at startup or per request before raising allocation findings; optimize request-path closures, substrings, temporary arrays, and converter buffering when material.
- CHECK: Avoid unbounded in-memory buffering for raw `HttpBody` and other non-JSON payloads; use framework buffering thresholds and enforce request/message-size limits where applicable.
- CHECK: Prefer atomic concurrent-cache APIs such as `GetOrAdd` when no separate invariant requires external locking.
- CHECK: Do not add pooling for small bounded temporaries unless size, frequency, and contention justify the complexity.
- CHECK: Keep benchmark validation and shared benchmark configuration active unless a documented repository-compatible exception explains why.

##### Repository build and test infrastructure

- CHECK: Do not isolate gRPC projects from repository `Directory.Build.*`, shared-runtime, project-reference, or test-asset infrastructure unless the exception is necessary and documented.
- CHECK: Avoid redundant default MSBuild properties, direct source-project references from tests, unnecessary framework references, unused restore sources, and ineffective warning suppressions.
- CHECK: Generated XML documentation, shipping flags, platform exclusions, and delayed-build settings need a current consumer or rationale visible in the project.
- CHECK: Test assets that launch client/server processes must work locally and on Helix: paths come from build metadata or payload-relative discovery, logs go under artifacts, processes have timeouts, and temporary outputs are cleaned up.
- CHECK: Prefer existing repo process, publishing, logging, and benchmark helpers over new local infrastructure.

##### Tests, diagnostics, and docs

- CHECK: Add focused tests for changed success and failure behavior: invalid JSON, invalid bodies, status-detail trailers, descriptor misses, field-name conflicts, route verbs, catch-alls, slashes, unannotated methods, and resolver edge cases.
- CHECK: Assert observable fields, OpenAPI metadata, route values, status codes, translated status-detail metadata, and error messages; equivalence to another parser is useful only with direct sanity assertions.
- CHECK: Avoid order-sensitive assertions over external tool output unless the test sorts first or order is part of the contract.
- CHECK: Prefer granular facts over broad theories so failures can be quarantined and diagnosed independently; do not replace quarantines with skips when result data matters.
- CHECK: Do not check in stress-only repeat loops or local repro hooks; keep them as validation-only changes.
- CHECK: Documentation for this area should state commands, directories, external package/runtime relationships, and non-obvious setup without duplicating repository-wide build guidance.
