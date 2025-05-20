// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

#nullable enable

namespace Microsoft.AspNetCore;

internal sealed class PinnedBlockMemoryPoolMetrics
{
    public const string MeterName = "Microsoft.AspNetCore.MemoryPool";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _currentMemory;
    private readonly Counter<long> _totalAllocatedMemory;
    private readonly Counter<long> _evictedMemory;
    private readonly Counter<long> _totalRented;

    public PinnedBlockMemoryPoolMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _currentMemory = _meter.CreateUpDownCounter<long>(
            "aspnetcore.memorypool.current_memory",
            unit: "{bytes}",
            description: "Number of bytes that are currently pooled by the pool.");

        _totalAllocatedMemory = _meter.CreateCounter<long>(
           "aspnetcore.memorypool.total_allocated",
            unit: "{bytes}",
            description: "Total number of allocations made by the pool.");

        _evictedMemory = _meter.CreateCounter<long>(
           "aspnetcore.memorypool.evicted_memory",
            unit: "{bytes}",
            description: "Total number of bytes that have been evicted.");

        _totalRented = _meter.CreateCounter<long>(
            "aspnetcore.memorypool.total_rented",
            unit: "{bytes}",
            description: "Total number of rented bytes from the pool.");
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
        _evictedMemory.Add(bytes);
    }

    public void Rent(int bytes)
    {
        _totalRented.Add(bytes);
    }
}
