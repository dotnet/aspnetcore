// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace System.Buffers;

internal static class PinnedBlockMemoryPoolFactory
{
    public static MemoryPool<byte> Create(IMeterFactory meterFactory)
    {
#if DEBUG
        return new DiagnosticMemoryPool(CreatePinnedBlockMemoryPool(meterFactory));
#else
        return CreatePinnedBlockMemoryPool(meterFactory);
#endif
    }

    public static MemoryPool<byte> CreatePinnedBlockMemoryPool(IMeterFactory meterFactory)
    {
        return new PinnedBlockMemoryPool(meterFactory);
    }
}
