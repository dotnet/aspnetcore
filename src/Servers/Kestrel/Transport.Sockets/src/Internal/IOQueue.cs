// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal class IOQueue : PipeScheduler, IThreadPoolWorkItem
    {
        private readonly ConcurrentQueue<Work> _workItems = new ConcurrentQueue<Work>();
        private int _processingRequested;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ScheduleToProcessWork()
        {
            // Schedule a thread pool work item to process Work.
            // Only one work item is scheduled at any given time to avoid over-parallelization.
            // When the work item begins running, this field is reset to 0.
            if (Interlocked.CompareExchange(ref _processingRequested, 1, 0) == 0)
            {
                System.Threading.ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
            }
        }

        public override void Schedule(Action<object> action, object state)
        {
            _workItems.Enqueue(new Work(action, state));

            ScheduleToProcessWork();
        }

        void IThreadPoolWorkItem.Execute()
        {
            // Ensure processing is requested when new work is queued.
            Interlocked.Exchange(ref _processingRequested, 0);

            ConcurrentQueue<Work> workItems = _workItems;
            if (!workItems.TryDequeue(out Work work))
            {
                return;
            }

            int startTimeMs = Environment.TickCount;

            // Schedule a work item to parallelize processing of work.
            ScheduleToProcessWork();

            while (true)
            {
                work.Callback(work.State);

                // Avoid this work item for delaying other items on the ThreadPool queue.
                if (Environment.TickCount - startTimeMs >= 15)
                {
                    break;
                }

                if (!workItems.TryDequeue(out work))
                {
                    return;
                }
            }
        }

        private readonly struct Work
        {
            public readonly Action<object> Callback;
            public readonly object State;

            public Work(Action<object> callback, object state)
            {
                Callback = callback;
                State = state;
            }
        }
    }
}
