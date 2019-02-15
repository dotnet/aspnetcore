// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class SchedulerBenchmark
    {
        private const int InnerLoopCount = 1024;
        private const int OuterLoopCount = 64;
        private const int OperationsPerInvoke = InnerLoopCount * OuterLoopCount;

        private PipeScheduler _lockFreeQueue;
        private PipeScheduler _lockBasedQueue;

        private static Action<object> _action = (o) => { };

        private static Action<int> _lockFreeAction;
        private static Action<int> _lockBasedAction;

        [GlobalSetup]
        public void Setup()
        {
            _lockFreeQueue = new IOQueueLockFree();
            _lockBasedQueue = new IOQueueLockBased();

            _lockFreeAction =
                (n) =>
                {
                    for (var i = 0; i < InnerLoopCount; i++)
                    {
                        _lockFreeQueue.Schedule(_action, null);
                    }
                };


            _lockBasedAction =
                (n) =>
                {
                    for (var i = 0; i < InnerLoopCount; i++)
                    {
                        _lockBasedQueue.Schedule(_action, null);
                    }
                };
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke, Baseline = true)]
        public void LockBased() => Schedule(_lockBasedAction);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void LockFree() => Schedule(_lockFreeAction);

        private void Schedule(Action<int> scheduleAction) => Parallel.For(0, OuterLoopCount, scheduleAction);

        public class IOQueueLockFree : PipeScheduler, IThreadPoolWorkItem
        {
            private readonly ConcurrentQueue<Work> _workItems = new ConcurrentQueue<Work>();
            private int _doingWork;

            public override void Schedule(Action<object> action, object state)
            {
                _workItems.Enqueue(new Work(action, state));

                // Set working if it wasn't (via atomic Interlocked).
                if (Interlocked.CompareExchange(ref _doingWork, 1, 0) == 0)
                {
                    // Wasn't working, schedule.
                    System.Threading.ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
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
                    // This doesn't need to be volatile due to the following barrier (i.e. it is volatile).
                    _doingWork = 0;

                    // Ensure _doingWork is written before IsEmpty is read.
                    // As they are two different memory locations, we insert a barrier to guarantee ordering.
                    Thread.MemoryBarrier();

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
        }

        public class IOQueueLockBased : PipeScheduler, IThreadPoolWorkItem
        {
            private readonly object _workSync = new object();
            private readonly ConcurrentQueue<Work> _workItems = new ConcurrentQueue<Work>();
            private bool _doingWork;

            public override void Schedule(Action<object> action, object state)
            {
                var work = new Work(action, state);

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
