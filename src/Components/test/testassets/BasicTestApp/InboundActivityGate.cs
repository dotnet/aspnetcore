// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BasicTestApp;

// Interactive Server only: holds back the next inbound circuit activity until the test releases it,
// so later messages from JS reach the dispatcher first.
public sealed class InboundActivityGate
{
    private TaskCompletionSource _next;
    private TaskCompletionSource _held;

    public void HoldNext() => Volatile.Write(ref _next, new(TaskCreationOptions.RunContinuationsAsynchronously));

    public Task TakeHold()
    {
        var hold = Interlocked.Exchange(ref _next, null);
        if (hold is not null)
        {
            Volatile.Write(ref _held, hold);
        }

        return hold?.Task;
    }

    public void ReleaseHeld() => Interlocked.Exchange(ref _held, null)?.TrySetResult();
}
