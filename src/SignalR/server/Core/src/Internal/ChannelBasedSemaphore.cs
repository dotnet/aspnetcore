// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;

namespace Microsoft.AspNetCore.SignalR.Internal;

// Use a Channel instead of a SemaphoreSlim so that we can potentially save task allocations (ValueTask!)
// Additionally initial perf results show faster RPS when using Channel instead of SemaphoreSlim
internal sealed class ChannelBasedSemaphore
{
    internal readonly Channel<int> _channel;

    public ChannelBasedSemaphore(int maxCapacity)
    {
        _channel = Channel.CreateBounded<int>(maxCapacity);
        for (var i = 0; i < maxCapacity; i++)
        {
            _channel.Writer.TryWrite(1);
        }
    }

    public bool AttemptWait()
    {
        return _channel.Reader.TryRead(out _);
    }

    public ValueTask<int> WaitAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }

    public ValueTask ReleaseAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(1, cancellationToken);
    }
}
