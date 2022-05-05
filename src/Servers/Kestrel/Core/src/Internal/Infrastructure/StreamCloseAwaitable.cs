// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class StreamCloseAwaitable : ICriticalNotifyCompletion
{
    private static readonly Action _callbackCompleted = () => { };

    // Initialize to completed so UpdateCompletedStreams runs at least once during connection teardown
    // if there are still active streams.
    private Action? _callback = _callbackCompleted;

    public StreamCloseAwaitable GetAwaiter() => this;
    public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

    public void GetResult()
    {
        Debug.Assert(ReferenceEquals(_callback, _callbackCompleted));

        _callback = null;
    }

    public void OnCompleted(Action continuation)
    {
        if (ReferenceEquals(_callback, _callbackCompleted) ||
            ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
        {
            Task.Run(continuation);
        }
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        OnCompleted(continuation);
    }

    public void Complete()
    {
        Interlocked.Exchange(ref _callback, _callbackCompleted)?.Invoke();
    }
}
