// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal sealed class CircuitMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Components.Server.Circuits";

    private readonly Meter _meter;
    private readonly Counter<long> _circuitTotalCounter;
    private readonly UpDownCounter<long> _circuitActiveCounter;
    private readonly UpDownCounter<long> _circuitConnectedCounter;
    private readonly Histogram<double> _circuitDuration;

    public CircuitMetrics(IMeterFactory meterFactory)
    {
        Debug.Assert(meterFactory != null);

        _meter = meterFactory.Create(MeterName);

        _circuitTotalCounter = _meter.CreateCounter<long>(
            "aspnetcore.components.circuits.count",
            unit: "{circuits}",
            description: "Number of active circuits.");

        _circuitActiveCounter = _meter.CreateUpDownCounter<long>(
            "aspnetcore.components.circuits.active_circuits",
            unit: "{circuits}",
            description: "Number of active circuits.");

        _circuitConnectedCounter = _meter.CreateUpDownCounter<long>(
            "aspnetcore.components.circuits.connected_circuits",
            unit: "{circuits}",
            description: "Number of disconnected circuits.");

        _circuitDuration = _meter.CreateHistogram<double>(
            "aspnetcore.components.circuits.duration",
            unit: "s",
            description: "Duration of circuit.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.VeryLongSecondsBucketBoundaries });
    }

    public void OnCircuitOpened()
    {
        var tags = new TagList();

        if (_circuitActiveCounter.Enabled)
        {
            _circuitActiveCounter.Add(1, tags);
        }
        if (_circuitTotalCounter.Enabled)
        {
            _circuitTotalCounter.Add(1, tags);
        }
    }

    public void OnConnectionUp()
    {
        var tags = new TagList();

        if (_circuitConnectedCounter.Enabled)
        {
            _circuitConnectedCounter.Add(1, tags);
        }
    }

    public void OnConnectionDown()
    {
        var tags = new TagList();

        if (_circuitConnectedCounter.Enabled)
        {
            _circuitConnectedCounter.Add(-1, tags);
        }
    }

    public void OnCircuitDown(long startTimestamp, long currentTimestamp)
    {
        // Tags must match request start.
        var tags = new TagList();

        if (_circuitActiveCounter.Enabled)
        {
            _circuitActiveCounter.Add(-1, tags);
        }

        if (_circuitConnectedCounter.Enabled)
        {
            _circuitConnectedCounter.Add(-1, tags);
        }

        if (_circuitDuration.Enabled)
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _circuitDuration.Record(duration.TotalSeconds, tags);
        }
    }

    public bool IsDurationEnabled() => _circuitDuration.Enabled;

    public void Dispose()
    {
        _meter.Dispose();
    }
}
