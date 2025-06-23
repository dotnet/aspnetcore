// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.Extensions.Internal.Test;

public class PinnedBlockMemoryPoolTests : MemoryPoolTests
{
    protected override MemoryPool<byte> CreatePool() => new PinnedBlockMemoryPool();

    [Fact]
    public void DoubleDisposeWorks()
    {
        var memoryPool = CreatePool();
        memoryPool.Dispose();
        memoryPool.Dispose();
    }

    [Fact]
    public void DisposeWithActiveBlocksWorks()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        memoryPool.Dispose();
    }

    [Fact]
    public void CanEvictBlocks()
    {
        using var memoryPool = new PinnedBlockMemoryPool();

        var block = memoryPool.Rent();
        block.Dispose();
        Assert.Equal(1, memoryPool.BlockCount());

        // First eviction does nothing because we double counted the initial rent due to it needing to allocate
        memoryPool.PerformEviction();
        Assert.Equal(1, memoryPool.BlockCount());

        memoryPool.PerformEviction();
        Assert.Equal(0, memoryPool.BlockCount());
    }

    [Fact]
    public void EvictsSmallAmountOfBlocksWhenTrafficIsTheSame()
    {
        using var memoryPool = new PinnedBlockMemoryPool();

        var blocks = new List<IMemoryOwner<byte>>();
        for (var i = 0; i < 10000; i++)
        {
            blocks.Add(memoryPool.Rent());
        }
        Assert.Equal(0, memoryPool.BlockCount());
        memoryPool.PerformEviction();

        foreach (var block in blocks)
        {
            block.Dispose();
        }
        blocks.Clear();
        Assert.Equal(10000, memoryPool.BlockCount());
        memoryPool.PerformEviction();

        var originalCount = memoryPool.BlockCount();
        for (var j = 0; j < 100; j++)
        {
            var previousCount = memoryPool.BlockCount();
            // Rent and return at the same rate
            for (var i = 0; i < 100; i++)
            {
                blocks.Add(memoryPool.Rent());
            }
            foreach (var block in blocks)
            {
                block.Dispose();
            }
            blocks.Clear();

            Assert.Equal(previousCount, memoryPool.BlockCount());

            // Eviction while rent+return is the same
            memoryPool.PerformEviction();
            Assert.InRange(memoryPool.BlockCount(), previousCount - (previousCount / 100), previousCount - 1);
        }

        Assert.True(memoryPool.BlockCount() <= originalCount - 100, "Evictions should have removed some blocks");
    }

    [Fact]
    public void DoesNotEvictBlocksWhenActive()
    {
        using var memoryPool = new PinnedBlockMemoryPool();

        var blocks = new List<IMemoryOwner<byte>>();
        for (var i = 0; i < 10000; i++)
        {
            blocks.Add(memoryPool.Rent());
        }
        Assert.Equal(0, memoryPool.BlockCount());
        memoryPool.PerformEviction();

        foreach (var block in blocks)
        {
            block.Dispose();
        }
        blocks.Clear();
        Assert.Equal(10000, memoryPool.BlockCount());
        memoryPool.PerformEviction();
        var previousCount = memoryPool.BlockCount();

        // Simulate active usage, rent without returning
        for (var i = 0; i < 100; i++)
        {
            blocks.Add(memoryPool.Rent());
        }
        previousCount -= 100;

        // Eviction while pool is actively used should not remove blocks
        memoryPool.PerformEviction();
        Assert.Equal(previousCount, memoryPool.BlockCount());
    }

    [Fact]
    public void EvictsBlocksGraduallyWhenIdle()
    {
        using var memoryPool = new PinnedBlockMemoryPool();

        var blocks = new List<IMemoryOwner<byte>>();
        for (var i = 0; i < 10000; i++)
        {
            blocks.Add(memoryPool.Rent());
        }
        Assert.Equal(0, memoryPool.BlockCount());
        memoryPool.PerformEviction();

        foreach (var block in blocks)
        {
            block.Dispose();
        }
        blocks.Clear();
        Assert.Equal(10000, memoryPool.BlockCount());
        // Eviction after returning everything to reset internal counters
        memoryPool.PerformEviction();

        // Eviction should happen gradually over multiple calls
        for (var i = 0; i < 10; i++)
        {
            var previousCount = memoryPool.BlockCount();
            memoryPool.PerformEviction();
            // Eviction while idle should remove 10-30% of blocks
            Assert.InRange(memoryPool.BlockCount(), previousCount - (previousCount / 10), previousCount - (previousCount / 30));
        }

        // Ensure all blocks are evicted eventually
        var count = memoryPool.BlockCount();
        do
        {
            count = memoryPool.BlockCount();
            memoryPool.PerformEviction();
        }
        // Make sure the loop makes forward progress
        while (count != 0 && count != memoryPool.BlockCount());

        Assert.Equal(0, memoryPool.BlockCount());
    }

    [Fact]
    public async Task EvictionsAreScheduled()
    {
        using var memoryPool = new PinnedBlockMemoryPool();

        var blocks = new List<IMemoryOwner<byte>>();
        for (var i = 0; i < 10000; i++)
        {
            blocks.Add(memoryPool.Rent());
        }
        Assert.Equal(0, memoryPool.BlockCount());

        foreach (var block in blocks)
        {
            block.Dispose();
        }
        blocks.Clear();
        Assert.Equal(10000, memoryPool.BlockCount());
        // Eviction after returning everything to reset internal counters
        memoryPool.PerformEviction();

        Assert.Equal(10000, memoryPool.BlockCount());

        var previousCount = memoryPool.BlockCount();

        // Scheduling only works every 10 seconds and is initialized to UtcNow + 10 when the pool is constructed
        var time = DateTime.UtcNow;
        Assert.False(memoryPool.TryScheduleEviction(time));

        Assert.True(memoryPool.TryScheduleEviction(time.AddSeconds(10)));

        var maxWait = TimeSpan.FromSeconds(5);
        while (memoryPool.BlockCount() > previousCount - (previousCount / 30) && maxWait > TimeSpan.Zero)
        {
            await Task.Delay(50);
            maxWait -= TimeSpan.FromMilliseconds(50);
        }

        Assert.InRange(memoryPool.BlockCount(), previousCount - (previousCount / 10), previousCount - (previousCount / 30));

        // Since we scheduled successfully, we now need to wait 10 seconds to schedule again.
        Assert.False(memoryPool.TryScheduleEviction(time.AddSeconds(10)));

        previousCount = memoryPool.BlockCount();
        Assert.True(memoryPool.TryScheduleEviction(time.AddSeconds(20)));

        maxWait = TimeSpan.FromSeconds(5);
        while (memoryPool.BlockCount() > previousCount - (previousCount / 30) && maxWait > TimeSpan.Zero)
        {
            await Task.Delay(50);
            maxWait -= TimeSpan.FromMilliseconds(50);
        }

        Assert.InRange(memoryPool.BlockCount(), previousCount - (previousCount / 10), previousCount - (previousCount / 30));
    }

    [Fact]
    public void CurrentMemoryMetricTracksPooledMemory()
    {
        var testMeterFactory = new TestMeterFactory();
        using var currentMemoryMetric = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.MemoryPool", "aspnetcore.memorypool.current_memory");

        var pool = new PinnedBlockMemoryPool(testMeterFactory);

        Assert.Empty(currentMemoryMetric.GetMeasurementSnapshot());

        var mem = pool.Rent();
        mem.Dispose();

        Assert.Collection(currentMemoryMetric.GetMeasurementSnapshot(), m => Assert.Equal(PinnedBlockMemoryPool.BlockSize, m.Value));

        mem = pool.Rent();

        Assert.Equal(-1 * PinnedBlockMemoryPool.BlockSize, currentMemoryMetric.LastMeasurement.Value);

        var mem2 = pool.Rent();

        mem.Dispose();
        mem2.Dispose();

        Assert.Equal(2 * PinnedBlockMemoryPool.BlockSize, currentMemoryMetric.GetMeasurementSnapshot().EvaluateAsCounter());

        // Eviction after returning everything to reset internal counters
        pool.PerformEviction();

        // Trigger eviction
        pool.PerformEviction();

        // Verify eviction updates current memory metric
        Assert.Equal(0, currentMemoryMetric.GetMeasurementSnapshot().EvaluateAsCounter());
    }

    [Fact]
    public void TotalAllocatedMetricTracksAllocatedMemory()
    {
        var testMeterFactory = new TestMeterFactory();
        using var totalMemoryMetric = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.MemoryPool", "aspnetcore.memorypool.total_allocated");

        var pool = new PinnedBlockMemoryPool(testMeterFactory);

        Assert.Empty(totalMemoryMetric.GetMeasurementSnapshot());

        var mem1 = pool.Rent();
        var mem2 = pool.Rent();

        // Each Rent that allocates a new block should increment total memory by block size
        Assert.Equal(2 * PinnedBlockMemoryPool.BlockSize, totalMemoryMetric.GetMeasurementSnapshot().EvaluateAsCounter());

        mem1.Dispose();
        mem2.Dispose();

        // Disposing (returning) blocks does not affect total memory
        Assert.Equal(2 * PinnedBlockMemoryPool.BlockSize, totalMemoryMetric.GetMeasurementSnapshot().EvaluateAsCounter());
    }

    [Fact]
    public void TotalRentedMetricTracksRentOperations()
    {
        var testMeterFactory = new TestMeterFactory();
        using var rentMetric = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.MemoryPool", "aspnetcore.memorypool.total_rented");

        var pool = new PinnedBlockMemoryPool(testMeterFactory);

        Assert.Empty(rentMetric.GetMeasurementSnapshot());

        var mem1 = pool.Rent();
        var mem2 = pool.Rent();

        // Each Rent should record the size of the block rented
        Assert.Collection(rentMetric.GetMeasurementSnapshot(),
            m => Assert.Equal(PinnedBlockMemoryPool.BlockSize, m.Value),
            m => Assert.Equal(PinnedBlockMemoryPool.BlockSize, m.Value));

        mem1.Dispose();
        mem2.Dispose();

        // Disposing does not affect rent metric
        Assert.Equal(2 * PinnedBlockMemoryPool.BlockSize, rentMetric.GetMeasurementSnapshot().EvaluateAsCounter());
    }

    [Fact]
    public void EvictedMemoryMetricTracksEvictedMemory()
    {
        var testMeterFactory = new TestMeterFactory();
        using var evictMetric = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.MemoryPool", "aspnetcore.memorypool.evicted_memory");

        var pool = new PinnedBlockMemoryPool(testMeterFactory);

        // Fill the pool with some blocks
        var blocks = new List<IMemoryOwner<byte>>();
        for (int i = 0; i < 10; i++)
        {
            blocks.Add(pool.Rent());
        }
        foreach (var block in blocks)
        {
            block.Dispose();
        }
        blocks.Clear();

        Assert.Empty(evictMetric.GetMeasurementSnapshot());

        // Eviction after returning everything to reset internal counters
        pool.PerformEviction();

        // Trigger eviction
        pool.PerformEviction();

        // At least some blocks should be evicted, each eviction records block size
        Assert.NotEmpty(evictMetric.GetMeasurementSnapshot());
        foreach (var measurement in evictMetric.GetMeasurementSnapshot())
        {
            Assert.Equal(PinnedBlockMemoryPool.BlockSize, measurement.Value);
        }
    }

    // Smoke test to ensure that metrics are aggregated across multiple pools if the same meter factory is used
    [Fact]
    public void MetricsAreAggregatedAcrossPoolsWithSameMeterFactory()
    {
        var testMeterFactory = new TestMeterFactory();
        using var rentMetric = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.MemoryPool", "aspnetcore.memorypool.total_rented");

        var pool1 = new PinnedBlockMemoryPool(testMeterFactory);
        var pool2 = new PinnedBlockMemoryPool(testMeterFactory);

        var mem1 = pool1.Rent();
        var mem2 = pool2.Rent();

        // Both pools should contribute to the same metric stream
        Assert.Equal(2 * PinnedBlockMemoryPool.BlockSize, rentMetric.GetMeasurementSnapshot().EvaluateAsCounter());

        mem1.Dispose();
        mem2.Dispose();

        // Renting and returning from both pools should not interfere with metric collection
        var mem3 = pool1.Rent();
        var mem4 = pool2.Rent();

        Assert.Equal(4 * PinnedBlockMemoryPool.BlockSize, rentMetric.GetMeasurementSnapshot().EvaluateAsCounter());

        mem3.Dispose();
        mem4.Dispose();
    }
}
