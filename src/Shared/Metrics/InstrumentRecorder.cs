// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Metrics;

// TODO: Remove when Metrics DI intergration package is available https://github.com/dotnet/aspnetcore/issues/47618
internal sealed class InstrumentRecorder<T> : IDisposable where T : struct
{
    private readonly object _lock = new object();
    private readonly string _meterName;
    private readonly string _instrumentName;
    private readonly MeterListener _meterListener;
    private readonly List<Measurement<T>> _values;
    private readonly List<Action<Measurement<T>>> _callbacks;

    public InstrumentRecorder(IMeterRegistry registry, string meterName, string instrumentName, object? state = null)
    {
        _meterName = meterName;
        _instrumentName = instrumentName;
        _callbacks = new List<Action<Measurement<T>>>();
        _values = new List<Measurement<T>>();
        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == _meterName && registry.Contains(instrument.Meter) && instrument.Name == _instrumentName)
            {
                listener.EnableMeasurementEvents(instrument, state);
            }
        };
        _meterListener.SetMeasurementEventCallback<T>(OnMeasurementRecorded);
        _meterListener.Start();
    }

    private void OnMeasurementRecorded(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        lock (_lock)
        {
            var m = new Measurement<T>(measurement, tags);
            _values.Add(m);

            // Should this happen in the lock?
            // Is there a better way to notify listeners that there are new measurements?
            foreach (var callback in _callbacks)
            {
                callback(m);
            }
        }
    }

    public void Register(Action<Measurement<T>> callback)
    {
        _callbacks.Add(callback);
    }

    public IReadOnlyList<Measurement<T>> GetMeasurements()
    {
        lock (_lock)
        {
            return _values.ToArray();
        }
    }

    public void Dispose()
    {
        _meterListener.Dispose();
    }
}
