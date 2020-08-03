// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl
{
    internal abstract class AwaitableProvider
    {
        public abstract ManualResetValueTaskSource<object> GetAwaitable();
        public abstract void CompleteCurrent();
        public abstract int ActiveCount { get; }
    }

    /// <summary>
    /// Provider returns multiple awaitables. Awaitables are completed FIFO.
    /// </summary>
    internal class MultipleAwaitableProvider : AwaitableProvider
    {
        private Queue<ManualResetValueTaskSource<object>> _awaitableQueue;
        private Queue<ManualResetValueTaskSource<object>> _awaitableCache;

        public override void CompleteCurrent()
        {
            var awaitable = _awaitableQueue.Dequeue();
            awaitable.TrySetResult(null);

            // Add completed awaitable to the cache for reuse
            _awaitableCache.Enqueue(awaitable);
        }

        public override ManualResetValueTaskSource<object> GetAwaitable()
        {
            if (_awaitableQueue == null)
            {
                _awaitableQueue = new Queue<ManualResetValueTaskSource<object>>();
                _awaitableCache = new Queue<ManualResetValueTaskSource<object>>();
            }

            // First attempt to reuse an existing awaitable in the queue
            // to save allocating a new instance.
            if (_awaitableCache.TryDequeue(out var awaitable))
            {
                // Reset previously used awaitable
                Debug.Assert(awaitable.GetStatus() == ValueTaskSourceStatus.Succeeded, "Previous awaitable should have been completed.");
                awaitable.Reset();
            }
            else
            {
                awaitable = new ManualResetValueTaskSource<object>();
            }

            _awaitableQueue.Enqueue(awaitable);

            return awaitable;
        }

        public override int ActiveCount => _awaitableQueue?.Count ?? 0;
    }

    /// <summary>
    /// Provider has a single awaitable.
    /// </summary>
    internal class SingleAwaitableProvider : AwaitableProvider
    {
        private ManualResetValueTaskSource<object> _awaitable;

        public override void CompleteCurrent()
        {
            _awaitable.TrySetResult(null);
        }

        public override ManualResetValueTaskSource<object> GetAwaitable()
        {
            if (_awaitable == null)
            {
                _awaitable = new ManualResetValueTaskSource<object>();
            }
            else
            {
                Debug.Assert(_awaitable.GetStatus() == ValueTaskSourceStatus.Succeeded, "Previous awaitable should have been completed.");
                _awaitable.Reset();
            }

            return _awaitable;
        }

        public override int ActiveCount => _awaitable != null && _awaitable.GetStatus() != ValueTaskSourceStatus.Succeeded ? 1 : 0;
    }
}
