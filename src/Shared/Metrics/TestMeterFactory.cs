// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Testing;

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

internal sealed class MeasurementReporter<T> : IDisposable where T : struct
{
    private readonly string _meterName;
    private readonly string _instrumentName;
    private readonly List<Action<Measurement<T>>> _callbacks;
    private readonly MeterListener _meterListener;

    public MeasurementReporter(IMeterFactory factory, string meterName, string instrumentName, object state = null)
    {
        _meterName = meterName;
        _instrumentName = instrumentName;
        _callbacks = new List<Action<Measurement<T>>>();
        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == _meterName && instrument.Meter.Scope == factory && instrument.Name == _instrumentName)
            {
                listener.EnableMeasurementEvents(instrument, state);
            }
        };
        _meterListener.SetMeasurementEventCallback<T>(OnMeasurementRecorded);
        _meterListener.Start();
    }

    private void OnMeasurementRecorded(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object>> tags, object state)
    {
        var m = new Measurement<T>(measurement, tags);
        foreach (var callback in _callbacks)
        {
            callback(m);
        }
    }

    public void Register(Action<Measurement<T>> callback)
    {
        _callbacks.Add(callback);
    }

    public void Dispose()
    {
        _meterListener.Dispose();
    }
}
