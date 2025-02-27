// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace System.Buffers;

#nullable enable

internal static class PinnedBlockMemoryPoolFactory
{
    public static MemoryPool<byte> Create(IMeterFactory? meterFactory = null)
    {
        meterFactory ??= NoopMeterFactory.Instance;
#if DEBUG
        return new DiagnosticMemoryPool(CreatePinnedBlockMemoryPool(meterFactory));
#else
        return CreatePinnedBlockMemoryPool(meterFactory);
#endif
    }

    public static MemoryPool<byte> CreatePinnedBlockMemoryPool(IMeterFactory? meterFactory = null)
    {
        meterFactory ??= NoopMeterFactory.Instance;
        return new PinnedBlockMemoryPool(meterFactory);
    }

    private sealed class NoopMeterFactory : IMeterFactory
    {
        public static NoopMeterFactory Instance = new NoopMeterFactory();

        public Meter Create(MeterOptions options) => new Meter(options);

        public void Dispose()
        {
        }
    }
}
