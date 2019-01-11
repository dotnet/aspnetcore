// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    public class IOQueue : PipeScheduler, IThreadPoolWorkItem
    {
        private readonly ConcurrentQueue<Work> _workItems = new ConcurrentQueue<Work>();
        private volatile int _doingWork;

        public override void Schedule(Action<object> action, object state)
        {
            var work = new Work
            {
                Callback = action,
                State = state
            };

            // Enqueue prior to checking _doingWork.
            _workItems.Enqueue(work);

            // Fast check if already doing work.
            if (_doingWork == 0)
            {
                // Not working, set as working, and check if it was already working (via atomic Interlocked).
                if (Interlocked.Exchange(ref _doingWork, 1) == 0)
                {
                    // Wasn't working, schedule.
                    System.Threading.ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
                }
            }
        }

        void IThreadPoolWorkItem.Execute()
        {
            while (true)
            {
                while (_workItems.TryDequeue(out Work item))
                {
                    item.Callback(item.State);
                }

                // All work done.
                // Set _doingWork (0 == false) prior to checking IsEmpty to catch any missed work in interim.
                _doingWork = 0;

                // Check if there is work to do
                if (_workItems.IsEmpty)
                {
                    // Nothing to do, exit.
                    break;
                }

                // Is work, can we set it as active again (via atomic Interlocked), prior to scheduling?
                if (Interlocked.Exchange(ref _doingWork, 1) == 1)
                {
                    // Execute has been rescheduled already, exit.
                    break;
                }

                // Is work, wasn't already scheduled so continue loop.
            }
        }

        private struct Work
        {
            public Action<object> Callback;
            public object State;
        }
    }
}
