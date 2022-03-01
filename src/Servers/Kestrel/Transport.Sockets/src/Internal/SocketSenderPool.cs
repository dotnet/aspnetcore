// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;

internal class SocketSenderPool : IDisposable
{
    private const int MaxQueueSize = 1024; // REVIEW: Is this good enough?

    private readonly ConcurrentQueue<SocketSender>[] _queues;
    private int _count;
    private readonly PipeScheduler _scheduler;
    private bool _disposed;

    public SocketSenderPool(PipeScheduler scheduler)
    {
        _scheduler = scheduler;

        _queues = new ConcurrentQueue<SocketSender>[Environment.ProcessorCount];

        for (var i = 0; i < _queues.Length; i++)
        {
            _queues[i] = new ConcurrentQueue<SocketSender>();
        }
    }

    public SocketSender Rent()
    {
        var partition = Thread.GetCurrentProcessorId() % _queues.Length;

        if (_queues[partition].TryDequeue(out var sender))
        {
            Interlocked.Decrement(ref _count);
            return sender;
        }
        return new SocketSender(_scheduler);
    }

    public void Return(SocketSender sender)
    {
        // This counting isn't accurate, but it's good enough for what we need to avoid using _queue.Count which could be expensive
        if (_disposed || Interlocked.Increment(ref _count) > MaxQueueSize)
        {
            Interlocked.Decrement(ref _count);
            sender.Dispose();
            return;
        }

        var partition = Thread.GetCurrentProcessorId() % _queues.Length;

        sender.Reset();
        _queues[partition].Enqueue(sender);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            foreach (var queue in _queues)
            {
                while (queue.TryDequeue(out var sender))
                {
                    sender.Dispose();
                }
            }
        }
    }
}
