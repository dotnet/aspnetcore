// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    public class IOQueue : PipeScheduler
#if NETCOREAPP3_0
        , IThreadPoolWorkItem
#endif
    {
#if !NETCOREAPP3_0
        private static readonly WaitCallback _doWorkCallback = s => ((IOQueue)s).Execute();
#endif
        private readonly ConcurrentQueue<Work> _workItems = new ConcurrentQueue<Work>();
        private int _doingWork;

        public override void Schedule(Action<object> action, object state)
        {
            var work = new Work
            {
                Callback = action,
                State = state
            };

            // Order is important here with Execute.

            // 1. Enqueue prior to checking _doingWork.
            _workItems.Enqueue(work);

            // 2. MemoryBarrier to ensure ordering of Write (workItems.Enqueue) -> Read (_doingWork) is preserved,
            // as order is reversed between Schedule and Execute, and they are two different memory locations.
            Thread.MemoryBarrier();

            // 3. Fast check if already doing work, don't need Volatile here due to explicit MemoryBarrier above
            if (_doingWork == 0)
            {
                // 4. Not working, set as working, and check if it was already working (via atomic Interlocked).
                var submitWork = Interlocked.Exchange(ref _doingWork, 1) == 0;

                if (submitWork)
                {
                    // 5. Wasn't working, schedule.
#if NETCOREAPP3_0
                    System.Threading.ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
#else
                    System.Threading.ThreadPool.UnsafeQueueUserWorkItem(_doWorkCallback, this);
#endif
                }
            }
        }

#if NETCOREAPP3_0
        void IThreadPoolWorkItem.Execute()
#else
        private void Execute()
#endif
        {
            while (true)
            {
                var workItems = _workItems;
                while (workItems.TryDequeue(out Work item))
                {
                    item.Callback(item.State);
                }

                // Order is important here with Schedule.

                // 1. Set _doingWork (0 == false) prior to checking .IsEmpty
                // Don't need Volatile here due to explicit MemoryBarrier below
                _doingWork = 0;

                // 2. MemoryBarrier to ensure ordering of Write (_doingWork) -> Read (workItems.IsEmpty) is preserved, 
                // as order is reversed between Schedule and Execute, and they are two different memory locations.
                Thread.MemoryBarrier();

                // 3. Check if there is work to do
                if (workItems.IsEmpty)
                {
                    // Nothing to do, exit.
                    break;
                }

                // 4. Is work, can we set it as active again (via atomic Interlocked), prior to scheduling?
                var alreadyScheduled = Interlocked.Exchange(ref _doingWork, 1) == 1;

                if (alreadyScheduled)
                {
                    // Execute has been rescheduled already, exit.
                    break;
                }

                // 5. Is work, wasn't already scheduled so continue loop
            }
        }

        private struct Work
        {
            public Action<object> Callback;
            public object State;
        }
    }
}
