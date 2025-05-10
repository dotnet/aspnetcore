// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

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
        Assert.False(memoryPool.TryScheduleEviction(DateTime.UtcNow));

        Assert.True(memoryPool.TryScheduleEviction(DateTime.UtcNow.AddSeconds(10)));

        var maxWait = TimeSpan.FromSeconds(5);
        while (memoryPool.BlockCount() > previousCount - (previousCount / 30) && maxWait > TimeSpan.Zero)
        {
            await Task.Delay(50);
            maxWait -= TimeSpan.FromMilliseconds(50);
        }

        Assert.InRange(memoryPool.BlockCount(), previousCount - (previousCount / 10), previousCount - (previousCount / 30));

        // Since we scheduled successfully, we now need to wait 10 seconds to schedule again.
        Assert.False(memoryPool.TryScheduleEviction(DateTime.UtcNow.AddSeconds(10)));

        previousCount = memoryPool.BlockCount();
        Assert.True(memoryPool.TryScheduleEviction(DateTime.UtcNow.AddSeconds(20)));

        maxWait = TimeSpan.FromSeconds(5);
        while (memoryPool.BlockCount() > previousCount - (previousCount / 30) && maxWait > TimeSpan.Zero)
        {
            await Task.Delay(50);
            maxWait -= TimeSpan.FromMilliseconds(50);
        }

        Assert.InRange(memoryPool.BlockCount(), previousCount - (previousCount / 10), previousCount - (previousCount / 30));
    }
}
