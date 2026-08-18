# SignalR reconnect producer-drift fixture

## Contract

`IRetryPolicy.NextRetryDelay` returns a nullable delay. A `null` result stops
automatic reconnect and transitions the connection to `Disconnected`.

## Review history and later-head diff

An earlier local review completed before this later commit:

```csharp
var nextDelay = _retryPolicy.NextRetryDelay(retryContext);
return nextDelay ?? TimeSpan.Zero;
```

The edited test covers a policy returning `TimeSpan.Zero` followed by a
successful reconnect.

## Unchanged consumers and tests

The unchanged reconnect suite includes:

- `StopsIfTheReconnectPolicyReturnsNull`: the custom policy returns zero for
  the first retry and `null` after that retry fails. The test awaits `Closed`,
  expects an `OperationCanceledException`, records two retry contexts, and
  expects zero successful reconnections.
- `CanBeInducedByCloseMessageWithAllowReconnectSet`: the custom policy always
  returns zero, and the connection successfully reconnects after a server close
  message allows reconnect.
- `ContinuesIfConnectionLostDuringReconnectHandshake`: the custom policy always
  returns zero while the test fails and retries a reconnect handshake.

All three tests passed before the later commit. The edited later-head test
covers a concrete zero delay followed by a successful reconnect.
