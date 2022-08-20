// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Internal;

internal sealed class CancellationTokenSourcePool
{
    private const int MaxQueueSize = 1024;

    private readonly ConcurrentQueue<PooledCancellationTokenSource> _queue = new();
    private int _count;

    public PooledCancellationTokenSource Rent()
    {
        if (_queue.TryDequeue(out var cts))
        {
            Interlocked.Decrement(ref _count);
            return cts;
        }
        return new PooledCancellationTokenSource(this);
    }

    private bool Return(PooledCancellationTokenSource cts)
    {
        if (Interlocked.Increment(ref _count) > MaxQueueSize || !cts.TryReset())
        {
            Interlocked.Decrement(ref _count);
            return false;
        }

        _queue.Enqueue(cts);
        return true;
    }

    /// <summary>
    /// A <see cref="CancellationTokenSource"/> with a back pointer to the pool it came from.
    /// Dispose will return it to the pool.
    /// </summary>
    public sealed class PooledCancellationTokenSource : CancellationTokenSource
    {
        private readonly CancellationTokenSourcePool _pool;

        public PooledCancellationTokenSource(CancellationTokenSourcePool pool)
        {
            _pool = pool;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // If we failed to return to the pool then dispose
                if (!_pool.Return(this))
                {
                    base.Dispose(disposing);
                }
            }
        }
    }
}
