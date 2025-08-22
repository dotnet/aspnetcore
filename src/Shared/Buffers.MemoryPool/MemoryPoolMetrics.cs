// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

#nullable enable

namespace Microsoft.AspNetCore;

internal sealed class MemoryPoolMetrics
{
    public const string MeterName = "Microsoft.AspNetCore.MemoryPool";

    public const string PooledMemoryName = "aspnetcore.memory_pool.pooled";
    public const string AllocatedMemoryName = "aspnetcore.memory_pool.allocated";
    public const string EvictedMemoryName = "aspnetcore.memory_pool.evicted";
    public const string RentedMemoryName = "aspnetcore.memory_pool.rented";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _pooledMemoryCounter;
    private readonly Counter<long> _allocatedMemoryCounter;
    private readonly Counter<long> _evictedMemoryCounter;
    private readonly Counter<long> _rentedMemoryCounter;

    public MemoryPoolMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _pooledMemoryCounter = _meter.CreateUpDownCounter<long>(
            PooledMemoryName,
            unit: "By",
            description: "Number of bytes currently pooled and available for reuse.");

        _allocatedMemoryCounter = _meter.CreateCounter<long>(
            AllocatedMemoryName,
            unit: "By",
            description: "Total number of bytes allocated by the memory pool. Allocation occurs when a memory rental request exceeds the available pooled memory.");

        _evictedMemoryCounter = _meter.CreateCounter<long>(
            EvictedMemoryName,
            unit: "By",
            description: "Total number of bytes evicted from the memory pool. Eviction occurs when idle pooled memory is reclaimed. Evicted memory is available for garbage collection.");

        _rentedMemoryCounter = _meter.CreateCounter<long>(
            RentedMemoryName,
            unit: "By",
            description: "Total number of bytes rented from the memory pool.");
    }

    public void UpdatePooledMemory(int bytes, string? owner)
    {
        if (_pooledMemoryCounter.Enabled)
        {
            UpdatePooledMemoryCore(bytes, owner);
        }
    }

    private void UpdatePooledMemoryCore(int bytes, string? owner)
    {
        var tags = new TagList();
        AddOwner(ref tags, owner);

        _pooledMemoryCounter.Add(bytes, tags);
    }

    public void AddAllocatedMemory(int bytes, string? owner)
    {
        if (_allocatedMemoryCounter.Enabled)
        {
            AddAllocatedMemoryCore(bytes, owner);
        }
    }

    private void AddAllocatedMemoryCore(int bytes, string? owner)
    {
        var tags = new TagList();
        AddOwner(ref tags, owner);

        _allocatedMemoryCounter.Add(bytes, tags);
    }

    public void AddEvictedMemory(int bytes, string? owner)
    {
        if (_evictedMemoryCounter.Enabled)
        {
            AddEvictedMemoryCore(bytes, owner);
        }
    }

    private void AddEvictedMemoryCore(int bytes, string? owner)
    {
        var tags = new TagList();
        AddOwner(ref tags, owner);

        _evictedMemoryCounter.Add(bytes, tags);
    }

    public void AddRentedMemory(int bytes, string? owner)
    {
        if (_rentedMemoryCounter.Enabled)
        {
            AddRentedMemoryCore(bytes, owner);
        }
    }

    private void AddRentedMemoryCore(int bytes, string? owner)
    {
        var tags = new TagList();
        AddOwner(ref tags, owner);

        _rentedMemoryCounter.Add(bytes, tags);
    }

    private static void AddOwner(ref TagList tags, string? owner)
    {
        if (!string.IsNullOrEmpty(owner))
        {
            tags.Add("aspnetcore.memory_pool.owner", owner);
        }
    }
}
