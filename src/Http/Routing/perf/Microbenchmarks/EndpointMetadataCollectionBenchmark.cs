// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

public class EndpointMetadataCollectionBenchmark
{
    private object[] _items;
    private EndpointMetadataCollection _collection;

    [Params(3, 10, 25)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var seeds = new Type[]
        {
                typeof(Metadata1),
                typeof(Metadata2),
                typeof(Metadata3),
                typeof(Metadata4),
                typeof(Metadata5),
                typeof(Metadata6),
                typeof(Metadata7),
                typeof(Metadata8),
                typeof(Metadata9),
        };

        _items = new object[Count];
        for (var i = 0; i < _items.Length; i++)
        {
            _items[i] = seeds[i % seeds.Length];
        }

        _collection = new EndpointMetadataCollection(_items);
    }

    // This is a synthetic baseline that visits each item and does an as-cast.
    [Benchmark(Baseline = true, OperationsPerInvoke = 5)]
    public void Baseline()
    {
        var items = _items;
        for (var i = items.Length - 1; i >= 0; i--)
        {
            GC.KeepAlive(_items[i] as IMetadata1);
        }

        for (var i = items.Length - 1; i >= 0; i--)
        {
            GC.KeepAlive(_items[i] as IMetadata2);
        }

        for (var i = items.Length - 1; i >= 0; i--)
        {
            GC.KeepAlive(_items[i] as IMetadata3);
        }

        for (var i = items.Length - 1; i >= 0; i--)
        {
            GC.KeepAlive(_items[i] as IMetadata4);
        }

        for (var i = items.Length - 1; i >= 0; i--)
        {
            GC.KeepAlive(_items[i] as IMetadata5);
        }
    }

    [Benchmark(OperationsPerInvoke = 5)]
    public void GetMetadata()
    {
        GC.KeepAlive(_collection.GetMetadata<IMetadata1>());
        GC.KeepAlive(_collection.GetMetadata<IMetadata2>());
        GC.KeepAlive(_collection.GetMetadata<IMetadata3>());
        GC.KeepAlive(_collection.GetMetadata<IMetadata4>());
        GC.KeepAlive(_collection.GetMetadata<IMetadata5>());
    }

    [Benchmark(OperationsPerInvoke = 5)]
    public void GetOrderedMetadata()
    {
        foreach (var item in _collection.GetOrderedMetadata<IMetadata1>())
        {
            GC.KeepAlive(item);
        }

        foreach (var item in _collection.GetOrderedMetadata<IMetadata2>())
        {
            GC.KeepAlive(item);
        }

        foreach (var item in _collection.GetOrderedMetadata<IMetadata3>())
        {
            GC.KeepAlive(item);
        }

        foreach (var item in _collection.GetOrderedMetadata<IMetadata4>())
        {
            GC.KeepAlive(item);
        }

        foreach (var item in _collection.GetOrderedMetadata<IMetadata5>())
        {
            GC.KeepAlive(item);
        }
    }

    private interface IMetadata1 { }
    private interface IMetadata2 { }
    private interface IMetadata3 { }
    private interface IMetadata4 { }
    private interface IMetadata5 { }
    private sealed class Metadata1 : IMetadata1 { }
    private sealed class Metadata2 : IMetadata2 { }
    private sealed class Metadata3 : IMetadata3 { }
    private sealed class Metadata4 : IMetadata4 { }
    private sealed class Metadata5 : IMetadata5 { }
    private sealed class Metadata6 : IMetadata1, IMetadata2 { }
    private sealed class Metadata7 : IMetadata2, IMetadata3 { }
    private sealed class Metadata8 : IMetadata4, IMetadata5 { }
    private sealed class Metadata9 : IMetadata1, IMetadata2 { }
}
