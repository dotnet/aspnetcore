// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Caching.Benchmarks;

#if DEBUG
// validation
using (var hc = new HybridCacheBenchmarks())
{
    for (int i = 0; i < 10; i++)
    {
        Console.WriteLine((await hc.HitDistributedCache()).Name);
        Console.WriteLine((await hc.HitHybridCache()).Name);
        Console.WriteLine((await hc.HitHybridCacheImmutable()).Name);
    }
}

/*
using (var obj = new DistributedCacheBenchmarks { PayloadSize = 11512, Sliding = true })
{
    Console.WriteLine($"Expected: {obj.PayloadSize}*{DistributedCacheBenchmarks.OperationsPerInvoke} = {obj.PayloadSize * DistributedCacheBenchmarks.OperationsPerInvoke}");
    Console.WriteLine();

    obj.Backend = DistributedCacheBenchmarks.BackendType.Redis;
    obj.GlobalSetup();
    Console.WriteLine(obj.GetSingleRandom());
    Console.WriteLine(obj.GetSingleFixed());
    Console.WriteLine(obj.GetSingleRandomBuffer());
    Console.WriteLine(obj.GetSingleFixedBuffer());
    Console.WriteLine(obj.GetConcurrentRandom());
    Console.WriteLine(obj.GetConcurrentFixed());
    Console.WriteLine(await obj.GetSingleRandomAsync());
    Console.WriteLine(await obj.GetSingleFixedAsync());
    Console.WriteLine(await obj.GetSingleRandomBufferAsync());
    Console.WriteLine(await obj.GetSingleFixedBufferAsync());
    Console.WriteLine(await obj.GetConcurrentRandomAsync());
    Console.WriteLine(await obj.GetConcurrentFixedAsync());
    Console.WriteLine();

    obj.Backend = DistributedCacheBenchmarks.BackendType.SqlServer;
    obj.GlobalSetup();
    Console.WriteLine(obj.GetSingleRandom());
    Console.WriteLine(obj.GetSingleFixed());
    Console.WriteLine(obj.GetSingleRandomBuffer());
    Console.WriteLine(obj.GetSingleFixedBuffer());
    Console.WriteLine(obj.GetConcurrentRandom());
    Console.WriteLine(obj.GetConcurrentFixed());
    Console.WriteLine(await obj.GetSingleRandomAsync());
    Console.WriteLine(await obj.GetSingleFixedAsync());
    Console.WriteLine(await obj.GetSingleRandomBufferAsync());
    Console.WriteLine(await obj.GetSingleFixedBufferAsync());
    Console.WriteLine(await obj.GetConcurrentRandomAsync());
    Console.WriteLine(await obj.GetConcurrentFixedAsync());
    Console.WriteLine();
}
*/
#else
BenchmarkSwitcher.FromAssembly(typeof(DistributedCacheBenchmarks).Assembly).Run(args: args);
#endif
