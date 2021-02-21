// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal
{
    internal class AckHandler : IDisposable
    {
        private readonly ConcurrentDictionary<int, AckInfo> _acks = new ConcurrentDictionary<int, AckInfo>();
        private readonly Timer _timer;
        private readonly TimeSpan _ackThreshold = TimeSpan.FromSeconds(30);
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

            var utcNow = DateTime.UtcNow;

            foreach (var pair in _acks)
            {
                var elapsed = utcNow - pair.Value.Created;
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

        private class AckInfo
        {
            public TaskCompletionSource Tcs { get; private set; }
            public DateTime Created { get; private set; }

            public AckInfo()
            {
                Created = DateTime.UtcNow;
                Tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }
}
