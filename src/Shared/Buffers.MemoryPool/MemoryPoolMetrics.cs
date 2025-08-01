// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

#nullable enable

namespace Microsoft.AspNetCore;

internal sealed class MemoryPoolMetrics
{
    public const string MeterName = "Microsoft.AspNetCore.MemoryPool";

    public const string UsedMemoryName = "aspnetcore.memory_pool.used";
    public const string AllocatedMemoryName = "aspnetcore.memory_pool.allocated";
    public const string EvictedMemoryName = "aspnetcore.memory_pool.evicted";
    public const string RentedMemoryName = "aspnetcore.memory_pool.rented";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _usedMemoryCounter;
    private readonly Counter<long> _allocatedMemoryCounter;
    private readonly Counter<long> _evictedMemoryCounter;
    private readonly Counter<long> _rentedMemoryCounter;

    public MemoryPoolMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _usedMemoryCounter = _meter.CreateUpDownCounter<long>(
            UsedMemoryName,
            unit: "By",
            description: "Number of bytes that are currently used by the pool.");

        _allocatedMemoryCounter = _meter.CreateCounter<long>(
           AllocatedMemoryName,
            unit: "By",
            description: "Total number of allocations made by the pool.");

        _evictedMemoryCounter = _meter.CreateCounter<long>(
           EvictedMemoryName,
            unit: "By",
            description: "Total number of bytes that have been evicted from the pool.");

        _rentedMemoryCounter = _meter.CreateCounter<long>(
            RentedMemoryName,
            unit: "By",
            description: "Total number of bytes rented from the pool.");
    }

    public void UpdateUsedMemory(int bytes, string? owner)
    {
        if (_usedMemoryCounter.Enabled)
        {
            UpdateUsedMemoryCore(bytes, owner);
        }
    }

    private void UpdateUsedMemoryCore(int bytes, string? owner)
    {
        var tags = new TagList();
        AddOwner(ref tags, owner);

        _usedMemoryCounter.Add(bytes, tags);
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
