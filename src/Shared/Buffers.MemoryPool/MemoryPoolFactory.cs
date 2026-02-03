// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.Metrics;

namespace Microsoft.AspNetCore;

#nullable enable

internal static class TestMemoryPoolFactory
{
    public static MemoryPool<byte> Create(string? owner = null, IMeterFactory? meterFactory = null)
    {
#if DEBUG
        return new DiagnosticMemoryPool(CreatePinnedBlockMemoryPool(owner, meterFactory));
#else
        return CreatePinnedBlockMemoryPool(owner, meterFactory);
#endif
    }

    public static MemoryPool<byte> CreatePinnedBlockMemoryPool(string? owner = null, IMeterFactory? meterFactory = null)
    {
        return new PinnedBlockMemoryPool(owner: owner, metrics: meterFactory != null ? new MemoryPoolMetrics(meterFactory) : null);
    }
}
