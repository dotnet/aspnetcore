# Server connection documentation-placement fixture

## Local cleanup path

The connection state has one close branch, and `PendingReadState.Abort` has no
other callers:

```csharp
private void OnConnectionClosed()
{
    _pendingReadState.Abort();
}

private sealed class PendingReadState
{
    public ReadPhase Phase { get; private set; }

    public void Abort()
    {
        if (Phase == ReadPhase.Pending)
        {
            Phase = ReadPhase.Completed;
        }
    }
}
```

A proposed comment above `PendingReadState` says, "While pending, a connection
close releases the read state to completion." The focused connection-close
test already verifies that the pending state completes.

## Deferred callback boundary

A transport handoff temporarily detaches its completion callback, captures the
current callback generation, transfers the transport, and then reattaches the
callback. The first completion after reattachment can belong to the detached
generation and can arrive before the ordinary completion notification. Without
the generation check, that stale completion is delivered to the new owner.

A proposed comment beside the generation capture says, "Capture the generation
before handoff because a completion from the detached registration can arrive
first after reattachment." Paired tests verify that a stale completion is
ignored and a current-generation completion is delivered exactly once.

## Public API proposal

The public `CloseAsync` documentation already states when the returned task
completes and which cancellation token applies. A proposed XML remarks section
also describes the internal callback-generation field, detach/reattach order,
and stale-completion filter.
