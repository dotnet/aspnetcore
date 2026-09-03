### SignalR reviewer

Review only the ASP.NET Core SignalR area in `src/SignalR/**`: hubs, hub protocols, transports, Redis scaleout, streaming, reconnect, connection lifetime, server/client proxy APIs, tests, samples, and the TypeScript, Java, and .NET clients.

This file is reference material. The `review-pull-request` skill and `pull-request-review` workflow
give each dimension below an independent, single-dimension pass.

#### Overarching principles

- Preserve the SignalR wire contract: hub protocol framing, serialization defaults, handshake, and negotiated capabilities stay compatible across JSON, MessagePack, and supported clients unless the change carries a deliberate compatibility plan.
- Treat connection lifetime as shared distributed state: connection IDs, groups, users, reconnect, backplane messages, and transport cleanup stay consistent through completion, cancellation, errors, and shutdown.
- Keep async paths observable and non-blocking: hub methods, transports, dispatch loops, and client callbacks use `Task`/`ValueTask`, respect cancellation, and avoid untracked fire-and-forget work.
- Bound long-lived work: streaming, channels, keep-alives, reconnect loops, and transport buffers need backpressure, timeouts, and deterministic completion or cleanup.
- Require client compatibility review: server changes must account for TypeScript, Java, and .NET client capabilities, browser constraints, and platform-specific APIs.
- Tests prove observable protocol, transport, and lifecycle behavior, not helper implementation details.

#### Review dimensions

##### Scope, API shape, and compatibility

- CHECK: Keep changes within server, client, protocol, transport, scaleout, sample, and test boundaries; do not solve unrelated hosting, routing, or auth concerns here.
- CHECK: Public APIs, extension methods, options, client builders, and hub contracts must preserve naming, chaining, nullability, XML documentation, and compatibility for existing consumers.
- CHECK: Treat defaults as contract — serialization options, transfer formats, reconnect behavior, keep-alive intervals, timeout values, and header precedence need a migration path before changing.
- CHECK: Prefer typed hub clients and strongly typed client proxies where they improve refactoring safety without breaking dynamic hub scenarios.
- CHECK: New platform or version negotiation must expose clear deprecation, fallback, and error behavior rather than silently changing existing message shapes.

##### Hub and connection lifetime

- CHECK: Initialize connection features, caller context, user identity, headers, connection metadata, and group services before application hub code can observe them.
- CHECK: Coordinate connect, reconnect, disconnect, abort, graceful close, and dispose so connection state, callbacks, groups, users, and backplane notifications are released or emitted exactly once.
- CHECK: Treat connection identifiers according to the connection mode: automatic reconnect receives a new ID, stateful reconnect preserves state only when negotiated, and skipped negotiation can intentionally omit IDs.
- CHECK: Use connection-scoped context or features for per-connection state instead of loose parameters that can drift between hub, transport, and protocol layers.
- CHECK: Do not invoke hub code, client callbacks, or user delegates while holding locks over shared connection state.

##### Transports, negotiation, fallback, and reconnect

- CHECK: WebSockets, server-sent events, and long polling must share transport-agnostic semantics while preserving each transport's framing, close, cancellation, buffering, and request/response constraints.
- CHECK: Negotiate the best supported transport and transfer format from client and server capabilities, and respect explicit transport selection when fallback is disabled or unavailable.
- CHECK: Reconnect loops provide sensible bounded defaults, honor custom policy termination and cancellation, propagate terminal failures to pending invocations, and avoid duplicate server state after successful reconnect.
- CHECK: Keep WebSocket close frames and reason codes, secure/non-secure schemes, server-sent-events stream closure, long-poll completion, and HTTP version constraints observable and testable.
- CHECK: Browser transports must not rely on unsupported features such as custom WebSocket headers; use documented alternatives such as access-token query flow with appropriate guards or warnings.
- CHECK: Handshake timeout changes validate options, enforce timeouts before hub dispatch, handle malformed or slow handshakes, clean up each transport correctly, and emit useful diagnostics.

##### Hub protocols, serialization, and framing

- CHECK: JSON and MessagePack hub protocols must implement equivalent semantics for invocation, stream item, completion, cancellation, ping, error, headers, and forward-compatible handling of skipped unknown JSON properties or trailing MessagePack items unless a compatibility reason is documented.
- CHECK: Binary framing must follow the SignalR framing specification exactly, including length prefixes, partial-frame handling, and backward-compatible field omission.
- CHECK: Validate message structure before dispatch and translate malformed, unsupported, or impossible states into observable invocation or connection errors.
- CHECK: Preserve wire compatibility for optional data, null handling, collection binding, transfer format, encoding, protocol version, and serialization-default changes.
- CHECK: Protocol tests must cover every affected protocol variant, not just one serializer or transport.

##### Streaming, channels, and backpressure

- CHECK: Server-to-client and client-to-server streaming use async streams or channels with bounded buffering where the contract requires backpressure, explicit completion, and cancellation propagation.
- CHECK: Pass `CancellationToken` through pending reads, writes, dispatch, and user handlers so client abort and server shutdown interrupt long-lived work.
- CHECK: Distinguish normal completion, cancellation, backpressure, and protocol errors in channel and pipeline results; report `IsCompleted` and `IsCanceled` accurately.
- CHECK: Preserve message ordering within a connection and document any scaleout or multi-transport limits where ordering cannot be global.
- CHECK: Complete channel readers and writers, cancel pending operations, and dispose only genuinely disposable resources such as registrations, protocol resources, and transport resources on every stream success, failure, cancellation, and exception path.

##### Async execution, concurrency, and dispatch ordering

- CHECK: Hub methods may be synchronous when all work is synchronous; methods that perform async work and internal async operations should return awaitable types and be awaited; avoid blocking waits, `async void`, unobserved tasks, and unnecessary `ContinueWith`.
- CHECK: Protect shared mutable connection, subscription, handler, and group state with a primitive that preserves the required invariant; use concurrent collections only when they express the full invariant.
- CHECK: Schedule continuations asynchronously for externally completed tasks, and queue explicitly when connection work must not flow `ExecutionContext`.
- CHECK: Dispatch multiple handlers in a documented order and surface each failure deterministically; do not parallelize where sequential callback behavior is part of the contract.
- CHECK: Hub method concurrency honors per-connection invocation serialization by default and `MaximumParallelInvocationsPerClient` when configured; streaming overlap, cancellation, and ordering remain explicit.
- CHECK: Use bounded waits at test and production coordination points so hangs, unexpected cancellation, and shutdown races fail deterministically.

##### Backplane, scaleout, groups, and user routing

- CHECK: Backplane messages must carry or derive the routing identities each message type needs, such as target hub, groups, users, connections, and source server when the protocol requires it, precisely enough to prevent duplicate delivery, loops, and cross-hub leakage.
- CHECK: Scaleout delivery is idempotent where retries are possible and documents ordering guarantees that differ from single-server connections.
- CHECK: Group add/remove, user routing, and automatic disconnect cleanup stay consistent across reconnect, abort, shutdown, and backplane-reconnect paths.
- CHECK: Backplane state transitions coordinate asynchronously without blocking hub dispatch; best-effort providers such as Redis Pub/Sub have no durable replay, so changes must avoid additional loss during reconnect.
- CHECK: Prefer provider abstractions that decouple hub invocation from transport-specific scaleout details while keeping diagnostics specific enough for operators.

##### Options, DI, hub filters, and extensibility

- CHECK: Register services, hubs, protocols, transports, authorization hooks, and options through DI with lifetimes that match state ownership and user-override expectations.
- CHECK: Per-hub options compose with global `HubOptions` instead of replacing inherited configuration users expect to observe or mutate.
- CHECK: Hub filters and other extensibility points use strongly typed contracts, efficient DI activation, cached factories when appropriate, and deterministic disposal for created instances.
- CHECK: Clone connection and transport options without silently dropping supported user configuration; copy defaults before applying caller-supplied overrides such as headers.
- CHECK: Keep transport-specific validation in the transport layer and shared hub/protocol validation in shared abstractions so error behavior stays consistent.
- CHECK: Trimming and AOT-sensitive paths such as hub method binding, typed clients/proxies, and JSON or MessagePack serialization include the required metadata annotations, source generation, or compatibility guidance where reflection is used.

##### Clients, proxies, and platform compatibility

- CHECK: Client compatibility changes use an explicit TypeScript, Java, and .NET capability matrix for capabilities, errors, cancellation, reconnect, headers, and streaming instead of assuming parity across clients.
- CHECK: Client proxy and subscription APIs provide deterministic unsubscribe/dispose, argument validation, parameter type binding, and async callback dispatch.
- CHECK: Do not expand generic HTTP or platform abstractions with SignalR-specific state when connection options can carry the user contract.
- CHECK: Platform-specific APIs need explicit guards, annotations, or alternatives rather than runtime failures, especially browser, mobile, and TLS-sensitive paths.
- CHECK: Samples demonstrate new advanced patterns without breaking established scenarios or hiding required production configuration.

##### Security, headers, diagnostics, and errors

- CHECK: Treat negotiate and redirect payloads, URLs, access tokens, and user headers as security-sensitive; preserve caller override rules without sharing mutable header state across requests.
- CHECK: Authentication and authorization wiring belongs in DI and hub configuration, not ad hoc transport-specific checks.
- CHECK: Use structured logging for connection, transport, protocol, reconnect, scaleout, and error transitions at levels that aid diagnosis without logging duplicate exceptions in retry loops.
- CHECK: Exceptions crossing protocol or client boundaries include actionable context without leaking server internals unless detailed errors are explicitly enabled.
- CHECK: Reserve runtime assertions for true invariants; user input, protocol violations, platform limits, and network failures need observable error handling.

##### Performance, resource management, and validation

- CHECK: Identify hot paths before optimizing, then avoid avoidable allocations through spans, pooled buffers, copy-on-write state, direct buffer writes, closure-free callbacks, and cached metadata where lifetime is clear.
- CHECK: Array pools, channels, buffers, readers, writers, WebSockets, HTTP responses, cancellation registrations, and client subscriptions need deterministic cleanup on success and failure paths.
- CHECK: Benchmarks cover the changed protocol, transport, or dispatch path and justify tradeoffs that reduce readability, diagnostics, or compatibility.
- CHECK: Tests assert semantic message shape, lifecycle transitions, headers, errors, completion, ordering, and cleanup before secondary details such as payload length or timing.
- CHECK: Parallel tests sharing keys, ports, backplanes, files, clients, or servers must isolate state and clean up outputs so local and Helix runs stay deterministic.
