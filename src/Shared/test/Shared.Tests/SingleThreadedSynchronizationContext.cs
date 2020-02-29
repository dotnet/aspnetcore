// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Microsoft.Extensions.Internal
{
    internal class SingleThreadedSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object State)> _queue = new BlockingCollection<(SendOrPostCallback Callback, object State)>();

        public override void Send(SendOrPostCallback d, object state) // Sync operations
        {
            throw new NotSupportedException($"{nameof(SingleThreadedSynchronizationContext)} does not support synchronous operations.");
        }

        public override void Post(SendOrPostCallback d, object state) // Async operations
        {
            _queue.Add((d, state));
        }

        public static void Run(Action action)
        {
            var previous = Current;
            var context = new SingleThreadedSynchronizationContext();
            SetSynchronizationContext(context);
            try
            {
                action();

                while (context._queue.TryTake(out var item))
                {
                    item.Callback(item.State);
                }
            }
            finally
            {
                context._queue.CompleteAdding();
                SetSynchronizationContext(previous);
            }
        }
    }
}
