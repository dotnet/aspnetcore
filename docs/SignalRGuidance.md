# SignalR contributor guidance

This guidance covers implementation, design, testing, and review work under `src/SignalR/**`.
Use it with the repository-wide contributor instructions and the documentation already maintained
inside the SignalR tree.

## Source ownership

Keep changes in the layer that owns the behavior:

| Area | Responsibility |
| --- | --- |
| [`server/Core`](../src/SignalR/server/Core) | Hub abstractions, dispatch, connection lifetime, groups, users, filters, and the in-process `HubLifetimeManager`. |
| [`server/SignalR`](../src/SignalR/server/SignalR) | ASP.NET Core DI and endpoint integration, plus server integration tests. |
| [`server/StackExchangeRedis`](../src/SignalR/server/StackExchangeRedis) | Redis scaleout and its cross-server routing tests. |
| [`server/Specification.Tests`](../src/SignalR/server/Specification.Tests) | Reusable conformance tests for `HubLifetimeManager` implementations. |
| [`common/Http.Connections`](../src/SignalR/common/Http.Connections) | Negotiation and the server implementations of WebSockets, Server-Sent Events, Long Polling, and HTTP sends. |
| [`common/SignalR.Common`](../src/SignalR/common/SignalR.Common) | Shared hub messages, handshake support, and related test infrastructure. |
| [`common/Shared`](../src/SignalR/common/Shared) | Shared text and binary framing plus stateful reconnect buffering compiled into multiple projects. |
| [`common/Protocols.*`](../src/SignalR/common) | JSON, MessagePack, and Newtonsoft.Json hub protocol implementations. |
| [`clients`](../src/SignalR/clients) | Independent .NET, TypeScript, and Java clients with different platform and feature capabilities. |
| [`docs/specs`](../src/SignalR/docs/specs) | The Hub Protocol and transport wire specifications. |

Do not move HTTP transport behavior into hub dispatch, provider-specific scaleout behavior into the
core lifetime manager, or client-specific constraints into shared protocol contracts. Changes that
cross these boundaries should preserve the abstractions between them and include tests at each
affected boundary.

## Wire contracts and compatibility

Treat the [Hub Protocol](../src/SignalR/docs/specs/HubProtocol.md) and
[transport protocols](../src/SignalR/docs/specs/TransportProtocols.md) as compatibility contracts.

- The Hub Protocol assumes reliable, ordered message delivery and does not provide general
  retransmission or reordering.
- The client handshake is the first hub-protocol message. It is JSON regardless of the selected hub
  protocol and uses the record-separator text framing implemented by `HandshakeProtocol`.
- JSON and MessagePack represent the same logical message families, but their wire encodings are not
  interchangeable. Preserve invocation, streaming, completion, cancellation, ping, close, ack, and
  sequence semantics in every affected protocol.
- Preserve forward-compatible parser behavior. In particular, do not reject currently tolerated
  unknown JSON properties, unknown message types, or trailing MessagePack data without a deliberate
  protocol compatibility plan.
- Text framing uses the record separator. Binary framing uses a length prefix and must continue to
  handle segmented input, incomplete messages, multiple buffered messages, and invalid lengths.
- Defaults are observable contracts. Changes to serializers, protocol versions, transfer formats,
  timeouts, keep-alive behavior, reconnect, headers, or error details require compatibility analysis
  across the server and every supported client.

Protocol-only changes belong with focused tests under `common/SignalR.Common/test`. Transport
behavior belongs with `common/Http.Connections/test`; do not use a hub integration test as the only
proof of framing or transport behavior.

## Connection and hub lifetime

`HubConnectionHandler<THub>` coordinates the server lifetime around the hub dispatcher and
`HubLifetimeManager<THub>`. Preserve the ordering and cleanup relationships among connection
initialization, `OnConnectedAsync`, hub dispatch, connection cleanup, and `OnDisconnectedAsync`.

- Normal close, abort, timeout, handshake failure, and application exceptions are distinct paths.
  Validate cleanup and observable errors for every path affected by a change.
- Connection-aborted tokens, active invocations, upload streams, message buffers, groups, users, and
  provider state must not outlive the connection that owns them.
- Keep per-connection state on `HubConnectionContext` or its features instead of passing parallel
  values that can drift between the HTTP connection, hub dispatcher, and lifetime manager.
- Hub invocations are serialized per connection by default.
  `HubOptions.MaximumParallelInvocationsPerClient` changes the non-streaming invocation limit;
  streaming invocations are intentionally not counted by that limit.
- Propagate cancellation through dispatch, stream reads and writes, and user handlers. Complete
  stream trackers, linked cancellation sources, scopes, activated hubs, and owned filters on success,
  cancellation, and failure.
- Do not replace awaitable lifetime work with blocking waits or unobserved tasks. Any intentionally
  detached operation needs an owner, cancellation, and an observable failure path.

Use `HubConnectionHandlerTests`, `HubFilterTests`, and the dispatcher tests under
`server/SignalR/test/Microsoft.AspNetCore.SignalR.Tests` for server lifetime and dispatch behavior.

## Hubs, options, DI, and extensibility

- Register SignalR and map hubs through the integration layer in `server/SignalR`; keep reusable hub
  and lifetime abstractions in `server/Core`.
- Global `HubOptions` are copied into per-hub `HubOptions<THub>` before per-hub configuration is
  applied. Preserve inherited values, validation, and user configuration order.
- Hub filters may be supplied as instances or activated through DI. Preserve the distinction between
  container-owned and framework-created instances and dispose only instances the framework owns.
- Keep typed hub clients and strongly typed proxies aligned with dynamic hub scenarios. Do not expose
  internal dispatch or reflection plumbing solely to avoid maintaining the intended public contract.
- Public or protected API changes must follow the
  [API review process](APIReviewProcess.md) and update the applicable
  [API baseline](APIBaselines.md). Public API baselines record compatibility; they do not replace API
  design review.
- Hub discovery, method binding, typed proxies, and serializers are trimming- and AOT-sensitive.
  Preserve existing annotations and validate publish-time behavior using the
  [trimming guidance](Trimming.md) when reflection or generated metadata changes.

## Transports, negotiation, and reconnect

The transports share a connection abstraction but do not have identical HTTP or framing behavior:

- WebSockets is full duplex and can carry text or binary frames. It is the only transport for which
  clients may skip negotiation.
- Server-Sent Events is server-to-client and text-only; client-to-server data uses HTTP POST. Preserve
  SignalR's supported newline normalization rather than assuming arbitrary event-stream framing.
- Long Polling is server-to-client and pairs with HTTP POST. Its `200`, `204`, and error responses
  distinguish poll timeout, connection completion, and failure.
- Negotiation selects from server-advertised transports and transfer formats. Preserve
  `negotiateVersion`, the distinction between public connection ID and secret connection token,
  redirect/error payloads, and explicit client transport selection.
- Stateful reconnect is opt-in at both ends and uses the ack/sequence protocol and message buffer.
  Do not treat it as ordinary automatic reconnect or assume that every client or transport supports
  it. Preserve buffer limits, cancellation under backpressure, duplicate suppression, and resend
  ordering.
- Browser WebSockets and Server-Sent Events cannot set arbitrary request headers. Keep documented
  access-token alternatives and do not generalize that restriction to Long Polling or non-browser
  clients.

Exercise transport selection and negotiation in `HttpConnectionDispatcherTests` and
`NegotiateProtocolTests`. Exercise WebSocket, Server-Sent Events, and Long Polling details in their
transport-specific tests.

## Client compatibility

The clients implement the same Hub Protocol but do not have feature parity. Check the affected
client explicitly instead of inferring behavior from another implementation.

| Capability | .NET client | TypeScript client | Java client |
| --- | --- | --- | --- |
| Transports | WebSockets, Server-Sent Events, Long Polling | WebSockets, Server-Sent Events, Long Polling | WebSockets and Long Polling |
| Skip negotiation | WebSockets only | WebSockets only | WebSockets only |
| Automatic reconnect | Opt-in with `WithAutomaticReconnect` | Opt-in with `withAutomaticReconnect` | Not implemented |
| Stateful reconnect | Separate opt-in; WebSockets only | Separate stateful reconnect support | Not implemented |
| Streaming surface | `IAsyncEnumerable<T>` and `ChannelReader<T>` with cancellation | `IStreamResult<T>` and disposable subscriptions | RxJava `Observable<T>` and `Disposable.dispose()` for stream cancellation |

Header, cookie, proxy, certificate, credential, and WebSocket configuration support also varies by
platform. Preserve the guards and alternatives on browser-sensitive .NET APIs and the
transport-specific behavior in the TypeScript client.

Client callbacks and subscriptions need deterministic removal. Preserve pending invocation
completion, cancellation, and terminal errors across stop and reconnect paths using the conventions
of that client rather than introducing a new cross-client abstraction. In the Java client,
`Subscription.unsubscribe()` removes hub-method handlers, while RxJava `Disposable.dispose()`
cancels a stream subscription.

## Scaleout, groups, and users

`DefaultHubLifetimeManager<THub>` owns in-process routing. `RedisHubLifetimeManager<THub>` adds
cross-server channels for hub, group, user, and connection messages.

- Preserve local-delivery short-circuiting and the routing identity required by each message type.
- Remote group add/remove operations use a management channel and acknowledgements; keep connection
  cleanup and membership changes consistent across servers.
- Do not assume that scaleout gives stronger replay or ordering guarantees than the provider and
  tests establish.
- Add single-server lifetime-manager contracts to `HubLifetimeManagerTestsBase<T>`, reusable
  cross-server contracts to `ScaleoutHubLifetimeManagerTests<TBackplane>`, and Redis-specific
  behavior to the StackExchange.Redis tests.
- Isolate group names, user names, connections, and provider state in parallel tests.

## Testing and validation

Choose the smallest test boundary that owns the changed behavior:

| Change | Primary test boundary |
| --- | --- |
| Framing, handshake, or hub-message parsing | `common/SignalR.Common/test` |
| Negotiation or a server transport | `common/Http.Connections/test` |
| Hub dispatch, lifetime, options, filters, or reconnect integration | `server/SignalR/test` |
| Lifetime-manager contract | `server/Specification.Tests` |
| Provider-independent cross-server behavior | `server/Specification.Tests` |
| Redis-specific routing or integration | `server/StackExchangeRedis/test` |
| .NET client behavior | `clients/csharp/**/test` |
| TypeScript client behavior | `clients/ts/**/tests`; use `FunctionalTests` for hosted browser/client-server behavior |
| Java client behavior | `clients/java/signalr/test` |
| Performance-sensitive protocol or dispatch work | `perf/Microbenchmarks` or the existing Crankier load application |

Follow the [repository test requirements](../CONTRIBUTING.md#tests) and
[faithful-validation requirements](../.github/copilot-instructions.md#running-tests). The
[SignalR README](../src/SignalR/README.md#test) describes the area build and test entry points, and
the TypeScript-specific workflows are documented in
[JS unit tests](../src/SignalR/docs/JSUnitTests.md) and
[JS functional tests](../src/SignalR/docs/JSFunctionalTests.md).

Tests should assert observable message shape, transport status or close behavior, lifetime
transitions, completion, cancellation, ordering, diagnostics, and cleanup. Avoid timing-only waits;
use bounded coordination that makes hangs and shutdown races fail deterministically.
