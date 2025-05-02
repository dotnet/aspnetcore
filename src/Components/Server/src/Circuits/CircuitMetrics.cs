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
            description: "Total number of circuits.");

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
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.BlazorCircuitSecondsBucketBoundaries });
    }

    public void OnCircuitOpened()
    {
        if (_circuitActiveCounter.Enabled)
        {
            _circuitActiveCounter.Add(1);
        }
        if (_circuitTotalCounter.Enabled)
        {
            _circuitTotalCounter.Add(1);
        }
    }

    public void OnConnectionUp()
    {
        if (_circuitConnectedCounter.Enabled)
        {
            _circuitConnectedCounter.Add(1);
        }
    }

    public void OnConnectionDown()
    {
        if (_circuitConnectedCounter.Enabled)
        {
            _circuitConnectedCounter.Add(-1);
        }
    }

    public void OnCircuitDown(long startTimestamp, long currentTimestamp)
    {
        if (_circuitActiveCounter.Enabled)
        {
            _circuitActiveCounter.Add(-1);
        }

        if (_circuitConnectedCounter.Enabled)
        {
            _circuitConnectedCounter.Add(-1);
        }

        if (_circuitDuration.Enabled)
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _circuitDuration.Record(duration.TotalSeconds);
        }
    }

    public bool IsDurationEnabled() => _circuitDuration.Enabled;

    public void Dispose()
    {
        _meter.Dispose();
    }
}
