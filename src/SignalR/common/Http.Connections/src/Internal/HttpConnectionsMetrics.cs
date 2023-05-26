// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Http.Connections.Internal;

internal readonly struct MetricsContext
{
    public MetricsContext(bool currentConnectionsCounterEnabled, bool connectionDurationEnabled, bool currentTransportsCounterEnabled)
    {
        CurrentConnectionsCounterEnabled = currentConnectionsCounterEnabled;
        ConnectionDurationEnabled = connectionDurationEnabled;
        CurrentTransportsCounterEnabled = currentTransportsCounterEnabled;
    }

    public bool CurrentConnectionsCounterEnabled { get; }
    public bool ConnectionDurationEnabled { get; }
    public bool CurrentTransportsCounterEnabled { get; }
}

internal sealed class HttpConnectionsMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Http.Connections";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _currentConnectionsCounter;
    private readonly Histogram<double> _connectionDuration;
    private readonly UpDownCounter<long> _currentTransportsCounter;

    public HttpConnectionsMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _currentConnectionsCounter = _meter.CreateUpDownCounter<long>(
            "current-connections",
            description: "Number of connections that are currently active on the server.");

        _connectionDuration = _meter.CreateHistogram<double>(
            "connection-duration",
            unit: "s",
            description: "The duration of connections on the server.");

        _currentTransportsCounter = _meter.CreateUpDownCounter<long>(
            "current-transports",
            description: "Number of negotiated transports that are currently active on the server.");
    }

    public void ConnectionStart(in MetricsContext metricsContext)
    {
        // Tags must match connection end.
        if (metricsContext.CurrentConnectionsCounterEnabled)
        {
            _currentConnectionsCounter.Add(1);
        }
    }

    public void ConnectionStop(in MetricsContext metricsContext, HttpTransportType transportType, HttpConnectionStopStatus status, long startTimestamp, long currentTimestamp)
    {
        // Tags must match connection start.
        if (metricsContext.CurrentConnectionsCounterEnabled)
        {
            _currentConnectionsCounter.Add(-1);
        }

        if (metricsContext.ConnectionDurationEnabled)
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _connectionDuration.Record(duration.TotalSeconds,
                new KeyValuePair<string, object?>("status", status.ToString()),
                new KeyValuePair<string, object?>("transport", transportType.ToString()));
        }
    }

    public void TransportStart(in MetricsContext metricsContext, HttpTransportType transportType)
    {
        Debug.Assert(transportType != HttpTransportType.None);

        // Tags must match transport end.
        if (metricsContext.CurrentTransportsCounterEnabled)
        {
            _currentTransportsCounter.Add(1, new KeyValuePair<string, object?>("transport", transportType.ToString()));
        }
    }

    public void TransportStop(in MetricsContext metricsContext, HttpTransportType transportType)
    {
        if (metricsContext.CurrentTransportsCounterEnabled)
        {
            // Tags must match transport start.
            // If the transport type is none then the transport was never started for this connection.
            if (transportType != HttpTransportType.None)
            {
                _currentTransportsCounter.Add(-1, new KeyValuePair<string, object?>("transport", transportType.ToString()));
            }
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public MetricsContext CreateContext()
    {
        return new MetricsContext(_currentConnectionsCounter.Enabled, _connectionDuration.Enabled, _currentTransportsCounter.Enabled);
    }
}
