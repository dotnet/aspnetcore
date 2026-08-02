// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Channels;

namespace Microsoft.AspNetCore.SignalR.Internal;

// Use a Channel instead of a SemaphoreSlim so that we can potentially save task allocations (ValueTask!)
// Additionally initial perf results show faster RPS when using Channel instead of SemaphoreSlim
internal sealed class ChannelBasedSemaphore
{
    private readonly Channel<int> _channel;

    public ChannelBasedSemaphore(int maxCapacity)
    {
        _channel = Channel.CreateBounded<int>(maxCapacity);
        for (var i = 0; i < maxCapacity; i++)
        {
            _channel.Writer.TryWrite(1);
        }
    }

    public bool TryAcquire()
    {
        return _channel.Reader.TryRead(out _);
    }

    // The int result isn't important, only reason it's exposed is because ValueTask<T> doesn't implement ValueTask so we can't cast like we could with Task<T> to Task
    public ValueTask<int> WaitAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }

    public void Release()
    {
        if (!_channel.Writer.TryWrite(1))
        {
            throw new SemaphoreFullException();
        }
    }

    public ValueTask RunAsync<TState>(Func<TState, Task<bool>> callback, TState state)
    {
        if (TryAcquire())
        {
            _ = RunTask(callback, state);
            return ValueTask.CompletedTask;
        }

        return RunSlowAsync(callback, state);
    }

    private async ValueTask RunSlowAsync<TState>(Func<TState, Task<bool>> callback, TState state)
    {
        _ = await WaitAsync();
        _ = RunTask(callback, state);
    }

    private async Task RunTask<TState>(Func<TState, Task<bool>> callback, TState state)
    {
        try
        {
            var shouldRelease = await callback(state);
            if (shouldRelease)
            {
                Release();
            }
        }
        catch
        {
            // DefaultHubDispatcher catches and handles exceptions
            // It does write to the connection in exception cases which also can't throw because we catch and log in HubConnectionContext
            Debug.Assert(false);
        }
    }
}
