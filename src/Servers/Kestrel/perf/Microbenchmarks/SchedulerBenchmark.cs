// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class SchedulerBenchmark
{
    private const int InnerLoopCount = 1024;
    private const int OuterLoopCount = 64;
    private const int OperationsPerInvoke = InnerLoopCount * OuterLoopCount;

    private static readonly int IOQueueCount = Math.Min(Environment.ProcessorCount, 16);

    private PipeScheduler[] _ioQueueSchedulers;
    private PipeScheduler[] _threadPoolSchedulers;
    private PipeScheduler[] _inlineSchedulers;

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
    private int _totalToReport;
    private readonly PaddedInteger[] _counters = new PaddedInteger[OuterLoopCount];

    private Func<int, ParallelLoopState, PipeScheduler[], PipeScheduler[]> _parallelAction;
    private Action<object> _action;

    [GlobalSetup]
    public void Setup()
    {
        _parallelAction = ParallelBody;
        _action = new Action<object>(ScheduledAction);

        _inlineSchedulers = new PipeScheduler[IOQueueCount];
        for (var i = 0; i < _inlineSchedulers.Length; i++)
        {
            _inlineSchedulers[i] = PipeScheduler.Inline;
        }

        _threadPoolSchedulers = new PipeScheduler[IOQueueCount];
        for (var i = 0; i < _threadPoolSchedulers.Length; i++)
        {
            _threadPoolSchedulers[i] = PipeScheduler.ThreadPool;
        }

        _ioQueueSchedulers = new PipeScheduler[IOQueueCount];
        for (var i = 0; i < _ioQueueSchedulers.Length; i++)
        {
            _ioQueueSchedulers[i] = new IOQueue();
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _totalToReport = OuterLoopCount;

        for (var i = 0; i < _counters.Length; i++)
        {
            _counters[i].Remaining = InnerLoopCount;
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke, Baseline = true)]
    public void ThreadPoolScheduler() => Schedule(_threadPoolSchedulers);

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void IOQueueScheduler() => Schedule(_ioQueueSchedulers);

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InlineScheduler() => Schedule(_inlineSchedulers);

    private void Schedule(PipeScheduler[] schedulers)
    {
        Parallel.For(0, OuterLoopCount, () => schedulers, _parallelAction, (s) => { });

        while (_totalToReport > 0)
        {
            _semaphore.Wait();
            _totalToReport--;
        }
    }

    private void ScheduledAction(object o)
    {
        var counter = (int)o;
        var result = Interlocked.Decrement(ref _counters[counter].Remaining);
        if (result == 0)
        {
            _semaphore.Release();
        }
    }

    private PipeScheduler[] ParallelBody(int i, ParallelLoopState state, PipeScheduler[] schedulers)
    {
        PipeScheduler pipeScheduler = schedulers[i % schedulers.Length];
        object counter = i;
        for (var t = 0; t < InnerLoopCount; t++)
        {
            pipeScheduler.Schedule(_action, counter);
        }

        return schedulers;
    }

    [StructLayout(LayoutKind.Explicit, Size = 128)]
    private struct PaddedInteger
    {
        // Padded to avoid false sharing
        [FieldOffset(64)]
        public int Remaining;
    }
}
