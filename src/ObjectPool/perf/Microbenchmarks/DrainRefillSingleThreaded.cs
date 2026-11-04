// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.ObjectPool.Microbenchmarks;

#pragma warning disable R9A038, S109

[MemoryDiagnoser]
public class DrainRefillSingleThreaded
{
    private DefaultObjectPool<Foo> _pool = null!;
    private Foo[] _store = null!;

    [Params(8, 16, 64, 256, 1024, 2048)]
    public int Count { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _pool = new DefaultObjectPool<Foo>(new DefaultPooledObjectPolicy<Foo>(), Count);
        for (int i = 0; i < Count; i++)
        {
            _pool.Return(new Foo());
        }

        _store = new Foo[Count];
    }

    [Benchmark]
    public void DrainRefillSingle()
    {
        for (int i = 0; i < Count; i++)
        {
            _store[i] = _pool.Get();
        }

        for (int i = 0; i < Count; i++)
        {
            _pool.Return(_store[i]);
        }
    }
}
