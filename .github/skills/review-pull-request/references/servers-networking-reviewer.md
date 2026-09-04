### Servers networking reviewer

Review only the ASP.NET Core managed servers and networking stack in `src/Servers/**`, `src/Http/**`, `src/Middleware/**`, `src/HttpClientFactory/**`, `src/HealthChecks/**`, and `src/Extensions/**` (HTTP feature infrastructure, notably `src/Extensions/Features`): Kestrel, HttpSys, managed IIS integration, Connections.Abstractions, HTTP abstractions/features/results, middleware, request/response body I/O, Polly HttpClientFactory integration, response/output caching middleware, and health checks. Native IIS C/C++, ANCM, installer, and unmanaged interop concerns belong to native-interop-reviewer. Minimal APIs, results, endpoint filters, and OpenAPI generation belong to minimal-api-openapi-reviewer; this agent owns the underlying HTTP abstractions, features, middleware, body I/O, wire behavior, and server behavior. Generic host, builder, and service-provider lifecycle belong to hosting-di-reviewer; this agent owns server and middleware feature registrations plus required services. Generic `src/Caching/**` Memory/Distributed abstractions belong to cross-cutting-reviewer; keep this agent to response/output caching middleware behavior.

This file is reference material. The `review-pull-request` skill gives each dimension below an
independent, single-dimension pass.

#### Overarching principles

- Preserve wire compatibility and observable lifecycle behavior over local simplification. HTTP version rules, connection reuse, cancellation, shutdown, and feature semantics are user-visible contracts.
- Treat every buffer, pipe, stream, connection, request, response, and callback as an ownership boundary with explicit completion, reset, cancellation, and disposal rules.
- Keep hot paths lean. Request processing, header parsing, middleware dispatch, flow control, and transport loops need allocation, copying, lock, and async-state-machine scrutiny.
- Let backpressure flow through the stack. Producers must not outrun transports, pipes, caches, compression, middleware, or peer flow-control windows.
- Framework components must compose with user middleware, DI, options, features, custom transports, and diagnostics without leaking implementation details.
- Tests should prove observable behavior across protocols and failure paths; diagnostics should explain decisions without noisy hot-path logging or sensitive data.

#### Review dimensions

##### Scope, ownership, and API shape

- CHECK: Keep this agent focused on managed server/networking behavior; route native IIS C/C++, ANCM, installer, unmanaged marshaling, and low-level interop findings to the native interop reviewer.
- CHECK: Cross-check [hosting-di-reviewer](hosting-di-reviewer.md) when server or middleware required services, `IServer` registration, default builders, or service-provider ownership cross the shared boundary: hosting owns host, builder, and provider lifecycle; this agent owns server and middleware feature registration plus required services.
- CHECK: Public APIs, options, features, middleware extensions, and abstractions must preserve source/binary compatibility, clear naming, XML documentation, nullability, and extension-point intent.
- CHECK: Hide transport, parser, cache, pool, generated-code, and state-machine implementation details behind internal types unless the public scenario is deliberate and reviewed.
- CHECK: Use `TryAdd`, `TryAddEnumerable`, named options, and friendly validation for framework services so repeated `Add*`/`Use*` calls compose with user registrations and slim builders.
- CHECK: Avoid dual configuration sources or duplicated options state that can drift between server, middleware, feature, and handler layers.
- CHECK: Control compatibility switches and quirk modes through server or middleware options; avoid checked-in process-wide `AppContext` switches because they leak across concurrent tests and hosts.

##### HTTP protocol compliance across versions

- CHECK: Parse and validate request lines, methods, targets, versions, headers, status codes, content lengths, trailers, and body boundaries according to the active HTTP version.
- CHECK: Keep HTTP/1.1, HTTP/2, and HTTP/3 state machines distinct where frame ordering, stream lifecycle, upgrades, chunking, trailers, GOAWAY, reset, and error-code semantics differ.
- CHECK: Apply peer settings, protocol limits, and negotiated values atomically; guard integer conversions, default values, and invalid enum inputs against overflow or silent fallback.
- CHECK: Preserve keep-alive and connection-close behavior by draining or rejecting bodies deliberately and by preventing leftover buffered bytes from corrupting the next request.
- CHECK: Distinguish TLS ALPN negotiation, cleartext HTTP/2 prior knowledge, QUIC transport selection, and Alt-Svc response-header advertisement; unsupported peers or hosts should fail with protocol-appropriate errors and actionable messages.
- CHECK: `Expect: 100-continue` handling must preserve interim-response timing, gate body reads until appropriate, cover rejection paths, and test timeout plus keep-alive/body-drain interactions.

##### Connection, stream, request, and response lifecycle

- CHECK: Model connection, stream, request, response, and output-producer transitions explicitly from accept/start through completion, abort, reset, drain, reuse, and disposal.
- CHECK: Complete, abort, reset, and dispose each request/stream resource exactly once, including error, cancellation, timeout, shutdown, and early-return paths.
- CHECK: Return pooled HTTP/2 or HTTP/3 streams only after response writing, resets, abort handling, and background write tasks have finished, and reset all mutable state before reuse.
- CHECK: Fire and await `OnStarting`/`OnCompleted` callbacks in the documented order, after internal setup and before `HttpContext` disposal; never fire-and-forget user lifecycle callbacks.
- CHECK: Respect response-start semantics: first write, compression, caching, buffering, trailers, status, and headers must agree about when headers become immutable.
- CHECK: Graceful shutdown should unbind listeners first, then drain, close, or abort in-flight connections through documented timeouts, then dispose transports.

##### Cancellation, timeouts, and graceful drain

- CHECK: Propagate cancellation tokens through async request, body, stream, middleware, cache, health-check, and handler paths; use `HttpContext.RequestAborted` as the request-default token.
- CHECK: Validate timeout and rate-limit options, including infinite and zero values; make absolute timeouts, minimum data rates, keep-alive, request-header, and request-body timers semantically distinct.
- CHECK: Dispose or return `CancellationTokenSource` instances after async work completes, outside locks, and without pooling canceled or linked-token state across requests.
- CHECK: Treat client disconnects, request aborts, application faults, and graceful shutdown as separate outcomes with appropriate error mapping and log levels.
- CHECK: Long-running reads, writes, waits, external process operations, health checks, and test synchronization need deterministic completion, timeout, or cancellation paths.

##### Pipelines, buffers, body I/O, and pooling

- CHECK: Prefer `PipeReader`, `PipeWriter`, `Memory<T>`, `Span<T>`, `ArrayPool<T>`, and `MemoryPool<T>` patterns for high-throughput I/O; avoid stream wrappers that hide required flush, advance, or completion semantics.
- CHECK: Pair `GetMemory`/`GetSpan`, `Advance`, `FlushAsync`, `ReadAsync`, `AdvanceTo`, `CancelPendingRead`, `Complete`, and `CompleteAsync` in protocol-correct order on success and failure paths.
- CHECK: Treat buffering contracts separately: file-buffering read streams spill to disk at `memoryThreshold` and throw at `bufferLimit`, while response/output cache streams disable or bypass buffering when their limits are exceeded.
- CHECK: Return rented buffers, owners, leases, and pooled tokens in `finally`/cleanup paths, and clear references that could root headers, bodies, or request state between pooled uses.
- CHECK: Encode directly into provided buffers when practical, treat `sizeHint` as a non-negative minimum whose writer may return more, enforce component size limits separately, and avoid allocating maximum-size or temporary copy buffers without measured need.
- CHECK: Test body and buffer behavior with repeated reads/writes, zero-length operations, partial receives, completed reads, writer exceptions, and pool reuse.

##### Backpressure, flow control, queues, and hangs

- CHECK: Preserve backpressure from transports, pipes, compression, caching, middleware, and peers; do not introduce producer loops that ignore flush, wait, or flow-control results.
- CHECK: Keep HTTP/2 stream-level and connection-level flow-control windows separately named, updated, reset, and tested; for HTTP/3, rely on System.Net.Quic flow control plus pipe thresholds and enforce fair ordering for streams waiting on shared capacity.
- CHECK: Bound channels, queues, and pending work unless a single-reader/single-writer invariant and lifecycle bound make unbounded growth impossible.
- CHECK: Avoid blocking while holding write locks, flow-control locks, or connection state locks; do not wait synchronously on tasks in server hot paths.
- CHECK: Use non-blocking pipe reads and explicit completion checks where polling is intended, and handle writer-side exceptions without spinning or leaking awaiters.

##### Header parsing, encoding, limits, and text

- CHECK: Parse header tokens with exact word boundaries, ordinal comparisons, correct comma/space/end delimiters, and version-appropriate case sensitivity.
- CHECK: Reject or sanitize invalid control characters, malformed whitespace, invalid request-targets, unsupported versions, and bad content-length values with precise errors.
- CHECK: Keep HPACK/QPACK/header encoding helpers centralized and version-aware; apply header-list-size limits using the actual encoded or computed size, and treat peer QPACK dynamic-table settings as inapplicable to HTTP/3 response paths that use static/literal fields unless dynamic QPACK encoding is introduced.
- CHECK: Date, media type, range, content-disposition, cookie, cache-control, and base64url parsing should accept interoperable valid forms while failing malformed values explicitly.
- CHECK: Avoid allocations from convenience properties on hot header paths when a method, cached value, or direct parser result can express the same contract.

##### Transport, sockets, TLS, ALPN, and certificates

- CHECK: Manage TCP, socket, named-pipe, QUIC, and test transports with explicit handling for partial reads/writes, resets, disconnect tokens, transport exceptions, and endpoint cleanup.
- CHECK: Startup binding failures should unwrap platform-specific and aggregate exceptions into user-facing `IOException`s that include the address and underlying cause.
- CHECK: TLS defaults, client-certificate negotiation, delayed certificates, certificate contexts, SNI, OS capability checks, and disposal rules must be explicit and tested.
- CHECK: ALPN/application-protocol features must reflect negotiated protocol without merging abstractions in a breaking way or exposing implementation-only values.
- CHECK: HTTP/3 and QUIC configuration should report missing host support, unsupported platform/runtime capabilities, and invalid limits with actionable diagnostics.

##### Concurrency, thread safety, and mutable state

- CHECK: Protect shared connection, stream, cache, route, metadata, options, and feature state with locks, `Interlocked`, `Volatile`, concurrent collections, or immutable snapshots that match the invariant.
- CHECK: Minimize critical sections, avoid lock reacquisition across helper boundaries, do not invoke user code under locks, and never hold locks across awaits unless the primitive is designed for it.
- CHECK: Capture and re-check shared state around shutdown, abort, tick, timeout, connection close, and pool-return races; use debug assertions to document lock ownership and impossible states.
- CHECK: Clone user-owned headers, `StringValues`, dictionaries, metadata, and options snapshots before mutation; do not cache mutable provider values that can change after first use.
- CHECK: Initialize synchronization primitives, pooled state, feature collections, and configuration snapshots before any transport or middleware path can observe them.

##### Middleware pipeline, routing, and `RequestDelegate`

- CHECK: Preserve middleware declaration order, short-circuiting, exception propagation, and terminal behavior; do not call `next` after rejection, cancellation, completion, or response start.
- CHECK: Validate required services before `Use*` middleware runs and provide friendly messages that guide users to the matching `Add*` method.
- CHECK: Keep routing, endpoint metadata, filters, generated request delegates, and convention callbacks in the documented precedence order; copy or snapshot mutable endpoint state when builders finalize.
- CHECK: Response-body middleware such as compression, caching, logging, buffering, and diagnostics must respect first-write initialization, `OnStarting`, stream flushing, and header-sent boundaries.
- CHECK: Middleware should take dependencies through DI and stable abstractions, not direct container-specific services or mutable configuration bags that bypass options patterns.

##### `HttpContext`, features, and HTTP abstractions

- CHECK: Feature implementations must advertise only the capabilities valid for the current server and protocol, and nullability annotations must match real availability.
- CHECK: Server-owned request and response feature fields need constrained mutation semantics; user mutations must not desynchronize body streams, body readers/writers, headers, status, or abort tokens.
- CHECK: Validate feature preconditions such as max-request-body-size mutability, HTTP.sys request delegation through `IHttpSysRequestDelegationFeature.CanDelegate`, upgrade support, synchronous I/O, and response start before performing the operation.
- CHECK: Prefer `HttpContext.Items`, endpoint metadata, or narrow feature interfaces for transient state; avoid first-class public properties for low-usage or implementation-specific data.
- CHECK: Body stream shims and feature swaps must avoid significant overhead when inactive and must preserve disposal, `leaveOpen`, async-only, and callback semantics.

##### Polly HttpClientFactory integration, response/output caching, health checks, and package middleware

- CHECK: `src/HttpClientFactory` changes in this repo should focus on Polly handler/policy integration, delegating-handler ordering, diagnostics, and tests; defer core factory pooling, handler lifetime, DNS refresh, named/typed clients, scopes, and disposal behavior to its owning repo, `dotnet/runtime`.
- CHECK: Policy and delegating-handler helpers should make failure categories explicit; avoid broad retry or error policies that treat unrelated 4xx/5xx responses as equivalent without user intent.
- CHECK: Response and output caching must keep lookup, storage-eligibility, and serve phases distinct: method, status, authorization, `Vary` headers including `Vary: Origin`, range requests, compression, content length, and body-size limits affect cache keys and storage, while validators run when serving cache hits.
- CHECK: Cache diagnostics should distinguish request cacheability, response cacheability, body buffering, key selection, and invalid content-length decisions without logging sensitive cache keys or headers.
- CHECK: Health checks must honor cancellation, timeouts, status mapping, result data safety, publisher lifetimes, and non-blocking execution so unhealthy dependencies do not hang the server.
- CHECK: Header propagation, CORS, HTTPS redirection, host filtering, and related middleware must clone user headers, preserve layering through DI, and avoid overstating security guarantees.

##### Diagnostics, observability, and error handling

- CHECK: Map expected failures to precise exception and HTTP error types; unwrap aggregate and platform-specific exceptions where user-facing server startup or transport errors need consistency.
- CHECK: Exception messages should include invalid values, addresses, versions, limits, or option names when useful, while preserving parameter names and avoiding stack traces for known client disconnects.
- CHECK: Use source-generated or typed structured logging on product paths; keep token names PascalCase, message templates stable, values low-cardinality, and hot-path logs quiet.
- CHECK: Emit diagnostics for meaningful state transitions, cache decisions, connection end reasons, certificate warnings, and configuration changes without duplicate logs from multiple layers.
- CHECK: Metrics and counters should use repo naming conventions, low-cardinality tags, cached enable checks where needed, and monotonic values that remain valid when instrumentation toggles.
- CHECK: Use `Debug.Assert` for internal invariants and unreachable states, but throw validated user-facing exceptions for public configuration and input errors.
- CHECK: Kestrel, middleware, Polly HttpClientFactory integration, response/output caching, health checks, and generated logging are NativeAOT-facing; verify trim safety, source-generated metadata, and linker annotations when reflection or dynamic access changes.

##### Tests, benchmarks, and repo integration

- CHECK: Add focused tests for changed behavior across HTTP versions, TLS modes, cancellation, disconnects, keep-alive reuse, pooling, body buffering, middleware ordering, response/output caching, health checks, and diagnostics.
- CHECK: Tests should assert observable responses, headers, trailers, body bytes, callbacks, logs, metrics, connection reuse, resource cleanup, and exception messages rather than mirroring helper internals.
- CHECK: Keep tests deterministic with `TaskCompletionSource`, `TimeProvider`, unique ports/cache keys, explicit cleanup, and standard timeouts; avoid arbitrary timing-based synchronization, allow `Task.Yield` or bounded delays only for intentional async-path or timeout coverage, and prevent global-state leaks.
- CHECK: Cross-process and Helix test assets should discover payload-relative paths, write logs under artifacts, enforce timeouts, validate exit codes, and clean temporary outputs.
- CHECK: Enable file watchers or polling only when the feature or test intentionally requires background monitoring, and measure file-handle, CPU, and background-I/O impact for always-on paths.
- CHECK: Measure hot-path allocation or throughput changes when changing parsers, middleware dispatch, flow control, queues, pools, logging, generated code, or body I/O; distinguish startup cost from per-request cost.
- CHECK: Project files, shared-framework membership, API baselines, build properties, docs, and samples should follow repository conventions; do not raise findings that CI already proves unless domain context changes the risk.
