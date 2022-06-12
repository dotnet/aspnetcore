// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;

internal sealed class AckHandler : IDisposable
{
    private readonly ConcurrentDictionary<int, AckInfo> _acks = new ConcurrentDictionary<int, AckInfo>();
    private readonly Timer _timer;
    private readonly long _ackThreshold = (long)TimeSpan.FromSeconds(30).TotalMilliseconds;
    private readonly TimeSpan _ackInterval = TimeSpan.FromSeconds(5);
    private readonly object _lock = new object();
    private bool _disposed;

    public AckHandler()
    {
        _timer = NonCapturingTimer.Create(state => ((AckHandler)state!).CheckAcks(), state: this, dueTime: _ackInterval, period: _ackInterval);
    }

    public Task CreateAck(int id)
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            return _acks.GetOrAdd(id, _ => new AckInfo()).Tcs.Task;
        }
    }

    public void TriggerAck(int id)
    {
        if (_acks.TryRemove(id, out var ack))
        {
            ack.Tcs.TrySetResult();
        }
    }

    private void CheckAcks()
    {
        if (_disposed)
        {
            return;
        }

        var currentTick = Environment.TickCount64;

        foreach (var pair in _acks)
        {
            var elapsed = currentTick - pair.Value.CreatedTick;
            if (elapsed > _ackThreshold)
            {
                if (_acks.TryRemove(pair.Key, out var ack))
                {
                    ack.Tcs.TrySetCanceled();
                }
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;

            _timer.Dispose();

            foreach (var pair in _acks)
            {
                if (_acks.TryRemove(pair.Key, out var ack))
                {
                    ack.Tcs.TrySetCanceled();
                }
            }
        }
    }

    private sealed class AckInfo
    {
        public TaskCompletionSource Tcs { get; private set; }
        public long CreatedTick { get; private set; }

        public AckInfo()
        {
            CreatedTick = Environment.TickCount64;
            Tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
