// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.ObjectPool.Microbenchmarks;

[MemoryDiagnoser]
public class GetReturnSingleThreaded
{
    private const int Count = 8;
    private DefaultObjectPool<Foo> _pool = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _pool = new DefaultObjectPool<Foo>(new DefaultPooledObjectPolicy<Foo>(), Count);
        for (int i = 0; i < Count; i++)
        {
            _pool.Return(new Foo());
        }
    }

    [Benchmark]
    public void GetReturnSingle()
    {
        _pool.Return(_pool.Get());
    }
}
