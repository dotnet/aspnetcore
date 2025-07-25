// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class PinnedBlockMemoryPoolFactoryTests
{
    [Fact]
    public void CreatePool()
    {
        var factory = new PinnedBlockMemoryPoolFactory(new TestMeterFactory());
        var pool = factory.Create();
        Assert.NotNull(pool);
        Assert.IsType<PinnedBlockMemoryPool>(pool);
    }

    [Fact]
    public void CreateMultiplePools()
    {
        var factory = new PinnedBlockMemoryPoolFactory(new TestMeterFactory());
        var pool1 = factory.Create();
        var pool2 = factory.Create();

        Assert.NotNull(pool1);
        Assert.NotNull(pool2);
        Assert.NotSame(pool1, pool2);
    }

    [Fact]
    public void DisposePoolRemovesFromFactory()
    {
        var factory = new PinnedBlockMemoryPoolFactory(new TestMeterFactory());
        var pool = factory.Create();
        Assert.NotNull(pool);

        var dict = (ConcurrentDictionary<PinnedBlockMemoryPool, nuint>)(typeof(PinnedBlockMemoryPoolFactory)
            .GetField("_pools", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(factory));
        Assert.Single(dict);

        pool.Dispose();
        Assert.Empty(dict);
    }

    [Fact]
    public async Task FactoryHeartbeatWorks()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow.AddDays(1));
        var factory = new PinnedBlockMemoryPoolFactory(new TestMeterFactory(), timeProvider);

        // Use 2 pools to make sure they all get triggered by the heartbeat
        var pool = Assert.IsType<PinnedBlockMemoryPool>(factory.Create());
        var pool2 = Assert.IsType<PinnedBlockMemoryPool>(factory.Create());

        var blocks = new List<IMemoryOwner<byte>>();
        for (var i = 0; i < 10000; i++)
        {
            blocks.Add(pool.Rent());
            blocks.Add(pool2.Rent());
        }

        foreach (var block in blocks)
        {
            block.Dispose();
        }
        blocks.Clear();

        // First eviction pass likely won't do anything since the pool was just very active
        factory.OnHeartbeat();

        var previousCount = pool.BlockCount();
        var previousCount2 = pool2.BlockCount();
        timeProvider.Advance(TimeSpan.FromSeconds(10));
        factory.OnHeartbeat();

        await VerifyPoolEviction(pool, previousCount);
        await VerifyPoolEviction(pool2, previousCount2);

        timeProvider.Advance(TimeSpan.FromSeconds(10));

        previousCount = pool.BlockCount();
        previousCount2 = pool2.BlockCount();
        factory.OnHeartbeat();

        await VerifyPoolEviction(pool, previousCount);
        await VerifyPoolEviction(pool2, previousCount2);

        static async Task VerifyPoolEviction(PinnedBlockMemoryPool pool, int previousCount)
        {
            // Because the eviction happens on a thread pool thread, we need to wait for it to complete
            // and the only way to do that (without adding a test hook in the pool code) is to delay.
            // But we don't want to add an arbitrary delay, so we do a short delay with block count checks
            // to reduce the wait time.
            var maxWait = TimeSpan.FromSeconds(5);
            while (pool.BlockCount() > previousCount - (previousCount / 30) && maxWait > TimeSpan.Zero)
            {
                await Task.Delay(50);
                maxWait -= TimeSpan.FromMilliseconds(50);
            }

            // Assert that the block count has decreased by 3.3-10%.
            // This relies on the current implementation of eviction logic which may change in the future.
            Assert.InRange(pool.BlockCount(), previousCount - (previousCount / 10), previousCount - (previousCount / 30));
        }
    }
}
