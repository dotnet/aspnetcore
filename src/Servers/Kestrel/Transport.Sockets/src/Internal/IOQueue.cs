// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.IO.Pipelines;

#nullable enable

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;

internal sealed class IOQueue : PipeScheduler, IThreadPoolWorkItem
{
    public static readonly int DefaultCount = DetermineDefaultCount();

    private readonly ConcurrentQueue<Work> _workItems = new ConcurrentQueue<Work>();
    private int _doingWork;

    public override void Schedule(Action<object?> action, object? state)
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

    private readonly struct Work
    {
        public readonly Action<object?> Callback;
        public readonly object? State;

        public Work(Action<object?> callback, object? state)
        {
            Callback = callback;
            State = state;
        }
    }

    private static int DetermineDefaultCount()
    {
        // Since each IOQueue schedules one work item to process its work, the number of IOQueues determines the maximum
        // parallelism of processing work queued to IOQueues. The default number below is based on the processor count and tries
        // to use a high-enough number for that to not be a significant limiting factor for throughput.
        //
        // On Windows, the default number is limited due to some other perf issues. Once those are fixed, the same heuristic
        // could apply there as well.

        int processorCount = Environment.ProcessorCount;
        if (OperatingSystem.IsWindows() || processorCount <= 32)
        {
            return Math.Min(processorCount, 16);
        }

        return processorCount / 2;
    }
}
