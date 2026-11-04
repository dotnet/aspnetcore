// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.ObjectPool.Microbenchmarks;

[MemoryDiagnoser]
public class GetReturnMultiThreaded
{
    private const int Count = 8;

    private DefaultObjectPool<Foo> _pool = null!;
    private ManualResetEventSlim _terminate = null!;
    private Task[] _tasks = null!;

    [Params(1, 2, 4, 8)]
    public int ThreadCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _pool = new DefaultObjectPool<Foo>(new DefaultPooledObjectPolicy<Foo>(), Count);
        for (int i = 0; i < Count; i++)
        {
            _pool.Return(new Foo());
        }

        _terminate = new ManualResetEventSlim();

        _tasks = new Task[ThreadCount - 1];
        for (int i = 0; i < ThreadCount - 1; i++)
        {
            int threadIndex = i;
            _tasks[i] = Task.Run(() =>
            {
                while (!_terminate.IsSet)
                {
                    BenchmarkLoop();
                }
            });
        }

        // give ample time to the contention tasks to start running
        Thread.Sleep(250);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _terminate.Set();
        Task.WaitAll(_tasks);
        _terminate.Dispose();
    }

    [Benchmark]
    public void GetReturnMulti()
    {
        BenchmarkLoop();
    }

    private void BenchmarkLoop()
    {
        var o = _pool.Get();

        o.SimulateWork();

        _pool.Return(o);
    }
}
