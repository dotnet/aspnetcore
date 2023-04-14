// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Metrics;

// TODO: Remove when Metrics DI intergration package is available https://github.com/dotnet/aspnetcore/issues/47618
internal class TestMeterFactory : IMeterFactory
{
    public List<Meter> Meters { get; } = new List<Meter>();

    public Meter CreateMeter(string name)
    {
        var meter = new Meter(name);
        Meters.Add(meter);
        return meter;
    }

    public Meter CreateMeter(MeterOptions options)
    {
        var meter = new Meter(options.Name, options.Version);
        Meters.Add(meter);
        return meter;
    }
}

internal class TestMeterRegistry : IMeterRegistry
{
    private readonly List<Meter> _meters;

    public TestMeterRegistry() : this(new List<Meter>())
    {
    }

    public TestMeterRegistry(List<Meter> meters)
    {
        _meters = meters;
    }

    public void Add(Meter meter) => _meters.Add(meter);

    public bool Contains(Meter meter) => _meters.Contains(meter);
}
