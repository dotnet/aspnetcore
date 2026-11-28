// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Identity.Test;

// Counts IMeterFactory.Create calls so tests can verify how many metrics instances were created for a factory.
internal sealed class CountingMeterFactory(IMeterFactory inner) : IMeterFactory
{
    private int _createCount;

    public int CreateCount => Volatile.Read(ref _createCount);

    public Meter Create(MeterOptions options)
    {
        Interlocked.Increment(ref _createCount);
        return inner.Create(options);
    }

    public void Dispose() => inner.Dispose();
}
