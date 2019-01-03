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
        private readonly object _workSync = new object();
        private readonly ConcurrentQueue<Work> _workItems = new ConcurrentQueue<Work>();
        private bool _doingWork;

        public override void Schedule(Action<object> action, object state)
        {
            var work = new Work
            {
                Callback = action,
                State = state
            };

            _workItems.Enqueue(work);

            lock (_workSync)
            {
                if (!_doingWork)
                {
                    System.Threading.ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
                    _doingWork = true;
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

                lock (_workSync)
                {
                    if (_workItems.IsEmpty)
                    {
                        _doingWork = false;
                        return;
                    }
                }
            }
        }

        private struct Work
        {
            public Action<object> Callback;
            public object State;
        }
    }
}
