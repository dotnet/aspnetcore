// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

public class PrefixContainerBenchmark
{
    private PrefixContainer _container = default!;

    [Params(10, 1_000, 100_000)]
    public int KeyCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var keys = new string[KeyCount];
        for (var i = 0; i < keys.Length - 1; i++)
        {
            keys[i] = $"key{i}.value";
        }
        keys[^1] = "target.child";

        _container = new PrefixContainer(keys);
    }

    [Benchmark]
    public IDictionary<string, string> GetKeysFromPrefix() => _container.GetKeysFromPrefix("target");
}
