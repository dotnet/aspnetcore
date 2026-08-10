# Deferred connection abort/dispose fixture

## Accepted issue behavior

Queued connection-lifetime work must tolerate state changes between scheduling
and execution. The issue demonstrates `Abort()` immediately followed by
`DisposeAsync()` terminating the process from deferred work.

## Frozen implementation

`Abort()` queues a static callback whose state is the connection's
`CancellationTokenSource`. `DisposeAsync()` disposes that source without
waiting for the queued callback. The patch catches only
`ObjectDisposedException` around the deferred `Cancel()` call.

An analogous Kestrel connection-closing path catches the same disposed-source
race. BCL behavior distinguishes a disposed source from exceptions raised by
cancellation callbacks.

## Regression evidence

The regression runs in `RemoteExecutor`, constrains the worker pool, blocks the
only worker, calls `Abort()`, disposes the connection, then releases the worker.
Untouched patched head and current CI pass. A first-chance exception listener
can observe the disposed-source exception without changing the product
assertion.

