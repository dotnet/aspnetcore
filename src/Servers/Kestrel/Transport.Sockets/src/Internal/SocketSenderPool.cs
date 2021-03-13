// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal class SocketSenderPool : IDisposable
    {
        private const int MaxQueueSize = 1024; // REVIEW: Is this good enough?

        private readonly ConcurrentQueue<SocketSender> _queue = new();
        private readonly PipeScheduler _scheduler;
        private bool _disposed;

        public SocketSenderPool(PipeScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public SocketSender Rent()
        {
            if (_queue.TryDequeue(out var sender))
            {
                return sender;
            }
            return new SocketSender(_scheduler);
        }

        public void Return(SocketSender sender)
        {
            if (_disposed || _queue.Count > MaxQueueSize)
            {
                sender.Dispose();
                return;
            }

            sender.Reset();

            _queue.Enqueue(sender);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                while (_queue.TryDequeue(out var sender))
                {
                    sender.Dispose();
                }
            }
        }
    }
}
