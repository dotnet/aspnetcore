// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#nullable enable

namespace System.Buffers;

internal sealed class PinnedBlockMemoryPoolMetrics
{
    // Note: Dot separated instead of dash.
    public const string MeterName = "System.Buffers.PinnedBlockMemoryPool";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _currentMemory;
    private readonly UpDownCounter<long> _totalAllocatedMemory;
    private readonly UpDownCounter<long> _evictedBlocks;
    private readonly UpDownCounter<long> _evictedMemory;
    private readonly UpDownCounter<long> _evictionAttempts;
    private readonly Counter<long> _usageRate;

    public PinnedBlockMemoryPoolMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _currentMemory = _meter.CreateUpDownCounter<long>(
            "pinnedblockmemorypool.current_memory",
            unit: "{bytes}",
            description: "Number of bytes that are currently pooled by the pinned block memory pool.");

        _totalAllocatedMemory = _meter.CreateUpDownCounter<long>(
           "pinnedblockmemorypool.total_allocated",
            unit: "{bytes}",
            description: "Total number of allocations made by the pinned block memory pool.");

        _evictedBlocks = _meter.CreateUpDownCounter<long>(
           "pinnedblockmemorypool.evicted_blocks",
            unit: "{blocks}",
            description: "Total number of pooled blocks that have been evicted.");

        _evictedMemory = _meter.CreateUpDownCounter<long>(
           "pinnedblockmemorypool.evicted_memory",
            unit: "{bytes}",
            description: "Total number of bytes that have been evicted.");

        _evictionAttempts = _meter.CreateUpDownCounter<long>(
           "pinnedblockmemorypool.eviction_attempts",
            unit: "{eviction}",
            description: "Total number of eviction attempts.");

        _usageRate = _meter.CreateCounter<long>(
            "pinnedblockmemorypool.usage_rate",
            unit: "bytes",
            description: "Rate of bytes rented from the pool."
            );
    }

    public void UpdateCurrentMemory(int bytes)
    {
        _currentMemory.Add(bytes);
    }

    public void IncrementTotalMemory(int bytes)
    {
        _totalAllocatedMemory.Add(bytes);
    }

    public void EvictBlock(int bytes)
    {
        _evictedBlocks.Add(1);
        _evictedMemory.Add(bytes);
    }

    public void StartEviction()
    {
        _evictionAttempts.Add(1);
    }

    public void Rent(int bytes)
    {
        _usageRate.Add(bytes);
    }
}
