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
    private readonly UpDownCounter<long> _circuitActiveCounter;
    private readonly UpDownCounter<long> _circuitConnectedCounter;
    private readonly Histogram<double> _circuitDuration;

    public CircuitMetrics(IMeterFactory meterFactory)
    {
        Debug.Assert(meterFactory != null);

        _meter = meterFactory.Create(MeterName);

        _circuitActiveCounter = _meter.CreateUpDownCounter<long>(
            "aspnetcore.components.circuit.active",
            unit: "{circuit}",
            description: "Number of active circuits in memory.");

        _circuitConnectedCounter = _meter.CreateUpDownCounter<long>(
            "aspnetcore.components.circuit.connected",
            unit: "{circuit}",
            description: "Number of circuits connected to client.");

        _circuitDuration = _meter.CreateHistogram<double>(
            "aspnetcore.components.circuit.duration",
            unit: "s",
            description: "Duration of circuit lifetime and their total count.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.BlazorCircuitSecondsBucketBoundaries });
    }

    public void OnCircuitOpened()
    {
        if (_circuitActiveCounter.Enabled)
        {
            _circuitActiveCounter.Add(1);
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
