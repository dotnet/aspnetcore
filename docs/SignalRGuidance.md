# SignalR guidance

This guidance covers SignalR server, common, .NET client, TypeScript client, and Java client
changes under `src/SignalR/**`. Apply the relevant topics alongside cross-cutting guidance;
an unaffected mechanism is not a demand for feature parity or additional tests.

The [SignalR architecture](../src/SignalR/ARCHITECTURE.md) supplies ownership and composition
context, not a mandatory checklist. This reviewer-owned snapshot does not establish behavior
or a binding contract on an older target branch. Verify defect claims against frozen `head`,
`mergeBase`, and `baseTip` source and applicable primary contracts. Missing architecture context
is disclosed, not replaced with live-main or PR-head guidance; a missing or invalid required
guide blocks review.

## Overarching principles

- Trace an applicable changed edge from its actual producer through the owning layer to a
  concrete observable effect. Architecture descriptions and neighboring implementations are
  orientation, not evidence that the affected path executes or has the same contract.
- Preserve established wire and consumer behavior across supported versions and implementations.
  Distinguish an intentional capability boundary from an incomplete fix; require a binding
  target or primary contract before reporting an omission.

## Topics

### Transport, encoding, dispatch, and routing ownership

- Keep HTTP negotiation and transport selection distinct from the hub handshake and protocol
  selection. Check transfer-format compatibility and startup failure paths when affected;
  an open transport is not proof of a successfully established hub connection.
- Trace transport data, framing and encoding, hub dispatch, and lifetime-manager addressing
  through their respective owners. A completed HTTP send or poll is not necessarily a
  connection shutdown, and a routing operation does not own application method execution.

### Connection, invocation, and provider lifetime

- Check connection-owned state separately from short-lived hub activations, invocation scopes,
  filters, streams, and provider subscriptions. Preserve the established disposal owner,
  including caller-supplied or DI-owned resources, rather than extending every resource to
  connection lifetime.
- Follow cleanup on the affected success, failed-startup, invocation-error, cancellation,
  disconnect, and replacement paths. Observe pending invocation outcomes, stream completion,
  callback ordering, membership removal, and resource release at the relevant boundary;
  do not assume every startup failure runs normal disconnection callbacks.

### Wire compatibility and parsing

- Compare changed negotiation, handshake, framing, message fields, versions, and serialization
  against the applicable Transport Protocols or Hub Protocol clauses in the frozen target.
  Resolve relevant primary contracts explicitly; architecture links do not delegate every
  protocol clause or make today's description a release-branch requirement.
- Check segmented and coalesced input, text versus binary framing, invocation IDs, completions,
  and errors when affected. Distinguish normative wire requirements from an implementation's
  existing tolerance for unknown fields or message types; tightening tolerated input can
  change consumer behavior without granting the same tolerance to all clients or encodings.

### Independent client capabilities

- Verify affected .NET, TypeScript, and Java implementations independently where a shared wire
  change reaches them. Do not infer identical transports, reconnect support, serializers,
  streaming APIs, or platform behavior merely from protocol interoperability.
- Trace the affected client's connection state, pending calls, handler registrations, and
  stream subscriptions through its actual runtime and transport adapter. Handler removal,
  stream cancellation, connection stop, and transport loss need not share a lifetime or
  observable outcome.

### Ordinary and stateful reconnect

- Distinguish ordinary reconnect, which establishes a new connection, from stateful reconnect,
  which resumes retained connection state. Check the actual negotiated capability, client
  opt-in, transport, startup failure, retry, stop, and buffer-disposal paths when affected;
  do not assume skipped negotiation or every client supports resumption.
- For stateful reconnect changes, trace acknowledgement and sequence tracking, duplicate
  suppression, buffered resend ordering, and new writes through their owners. Verify the
  effect on pending operations and retained resources; reconnect alone is not durable
  delivery, unrestricted replay, or application acknowledgement.

### Scaleout and addressing

- Check connection, group, and user addressing against the affected lifetime-manager contract,
  distinguishing local fast paths from cross-server routing and membership acknowledgements.
  Keep backplane serialization separate from client hub encoding and reconnect acknowledgements.
- Verify provider-specific behavior and cross-server consumers when changed, including applicable
  Azure SignalR Service contracts when available. Scaleout does not migrate hub instances,
  invocation scopes, or live reconnect buffers, nor establish total ordering or durable replay.
  Record unavailable provider-contract evidence rather than inferring parity from local delivery.

### Concurrency, cancellation, and backpressure

- Follow per-connection invocation limits, concurrent writes, active streams, cancellation, and
  shutdown through the actual synchronization path. Returning to the message loop does not
  finish a stream or release its scope; an invocation failure need not end unrelated operations.
- For buffering and flow-control changes, trace who advances or releases buffers and what
  unblocks waiting producers during acknowledgement, completion, cancellation, and disposal.
  Establish concrete ordering, stalled-progress, or retained-resource effects rather than
  treating a concurrent collection or a passing send as proof of the whole invariant.

### Faithful verification boundaries

- Assess changed coverage at the owner of the disputed precondition and observable result:
  protocol parsing, real transport input, dispatch, client state, or provider routing as
  appropriate. Injected messages or disconnect callbacks establish downstream response,
  not that a transport, browser, or provider can produce the scenario.
- Use frozen test source as supporting evidence, not execution proof. Do not demand browser or
  cross-client tests for an unrelated edge, or report missing tests alone as a defect. Disclose
  prohibited execution and unresolved runtime or external-contract evidence under the existing
  source-only review contract.
