// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.InternalTesting;

internal sealed class TestMeterFactory : IMeterFactory
{
    public List<Meter> Meters { get; } = new List<Meter>();

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options.Name, options.Version, Array.Empty<KeyValuePair<string, object>>(), scope: this);
        Meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var meter in Meters)
        {
            meter.Dispose();
        }

        Meters.Clear();
    }
}
