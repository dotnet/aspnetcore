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
    private readonly ConcurrentDictionary<PinnedBlockMemoryPool, PinnedBlockMemoryPool> _pools = new();

    public PinnedBlockMemoryPoolFactory(IMeterFactory meterFactory)
    {
        _meterFactory = meterFactory;
    }

    public MemoryPool<byte> Create()
    {
        // TODO: wire up PinnedBlockMemoryPool's dispose to remove from _pools
        var pool = new PinnedBlockMemoryPool(_meterFactory);

        _pools.TryAdd(pool, pool);

#if DEBUG
        return new DiagnosticMemoryPool(pool);
#else
        return pool;
#endif
    }

    public void OnHeartbeat()
    {
        foreach (var pool in _pools)
        {
            pool.Value.PerformEviction();
        }
    }
}
