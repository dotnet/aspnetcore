// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;

public class DiagnosticMemoryPoolFactory
{
    private readonly bool _allowLateReturn;

    private readonly bool _rentTracking;

    private readonly List<DiagnosticMemoryPool> _pools;

    public DiagnosticMemoryPoolFactory(bool allowLateReturn = false, bool rentTracking = false)
    {
        _allowLateReturn = allowLateReturn;
        _rentTracking = rentTracking;
        _pools = new List<DiagnosticMemoryPool>();
    }

    public MemoryPool<byte> Create()
    {
        lock (_pools)
        {
            var pool = new DiagnosticMemoryPool(new PinnedBlockMemoryPool(), _allowLateReturn, _rentTracking);
            _pools.Add(pool);
            return pool;
        }
    }

    public Task WhenAllBlocksReturned(TimeSpan span)
    {
        lock (_pools)
        {
            return Task.WhenAll(_pools.Select(p => p.WhenAllBlocksReturnedAsync(span)));
        }
    }
}
