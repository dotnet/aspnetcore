// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.ObjectPool.Microbenchmarks;

[MemoryDiagnoser]
public class DrainRefillMultiTheaded
{
    private DefaultObjectPool<Foo> _pool = null!;
    private Foo[][] _stores = null!;
    private ManualResetEventSlim _terminate = null!;
    private Task[] _tasks = null!;

    [Params(8, 16, 64, 256, 1024, 2048)]
    public int Count { get; set; }

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

        _stores = new Foo[ThreadCount][];
        for (int i = 0; i < ThreadCount; i++)
        {
            _stores[i] = new Foo[Count];
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
                    BenchmarkLoop(_stores[threadIndex]);
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
    public void DrainRefillMulti()
    {
        BenchmarkLoop(_stores[ThreadCount - 1]);  // take the last slot
    }

    private void BenchmarkLoop(Foo[] store)
    {
        int num = (Count / ThreadCount) - 1;

        for (int i = 0; i < num; i++)
        {
            store[i] = _pool.Get();
            store[i].SimulateWork();
        }

        for (int i = 0; i < num; i++)
        {
            store[i].SimulateWork();
            _pool.Return(store[i]);
        }
    }
}
