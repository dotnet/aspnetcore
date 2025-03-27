// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class PinnedBlockMemoryPoolFactory : IMemoryPoolFactory<byte>, IHeartbeatHandler
{
    private readonly IMeterFactory _meterFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<PinnedBlockMemoryPool, PinnedBlockMemoryPool> _pools = new();

    public PinnedBlockMemoryPoolFactory(IMeterFactory meterFactory, TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _meterFactory = meterFactory;
    }

    public MemoryPool<byte> Create()
    {
        var pool = new PinnedBlockMemoryPool(_meterFactory);
        pool.DisposeCallback = (self) =>
        {
            _pools.TryRemove(self, out _);
        };

        _pools.TryAdd(pool, pool);

        return pool;
    }

    public void OnHeartbeat()
    {
        var now = _timeProvider.GetUtcNow();
        foreach (var pool in _pools)
        {
            pool.Value.TryScheduleEviction(now);
        }
    }
}
