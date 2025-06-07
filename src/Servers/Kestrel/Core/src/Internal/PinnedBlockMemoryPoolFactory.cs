// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class PinnedBlockMemoryPoolFactory : IMemoryPoolFactory<byte>, IHeartbeatHandler
{
    private readonly IMeterFactory _meterFactory;
    private readonly ILogger? _logger;
    private readonly TimeProvider _timeProvider;
    // micro-optimization: Using nuint as the value type to avoid GC write barriers; could replace with ConcurrentHashSet if that becomes available
    private readonly ConcurrentDictionary<PinnedBlockMemoryPool, nuint> _pools = new();

    public PinnedBlockMemoryPoolFactory(IMeterFactory meterFactory, TimeProvider? timeProvider = null, ILogger<PinnedBlockMemoryPoolFactory>? logger = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _meterFactory = meterFactory;
        _logger = logger;
    }

    public MemoryPool<byte> Create()
    {
        var pool = new PinnedBlockMemoryPool(_meterFactory, _logger);

        _pools.TryAdd(pool, nuint.Zero);

        pool.OnPoolDisposed(static (state, self) =>
        {
            ((ConcurrentDictionary<PinnedBlockMemoryPool, nuint>)state!).TryRemove(self, out _);
        }, _pools);

        return pool;
    }

    public void OnHeartbeat()
    {
        var now = _timeProvider.GetUtcNow();
        foreach (var pool in _pools)
        {
            pool.Key.TryScheduleEviction(now);
        }
    }
}
