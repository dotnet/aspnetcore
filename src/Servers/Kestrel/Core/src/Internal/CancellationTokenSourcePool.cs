// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class CancellationTokenSourcePool
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
            // This counting isn't accurate, but it's good enough for what we need to avoid using _queue.Count which could be expensive
            if (!cts.TryReset() || Interlocked.Increment(ref _count) > MaxQueueSize)
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
        public class PooledCancellationTokenSource : CancellationTokenSource
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
}
