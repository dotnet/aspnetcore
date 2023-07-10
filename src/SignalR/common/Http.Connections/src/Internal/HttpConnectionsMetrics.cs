// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Http.Connections.Internal;

internal readonly struct MetricsContext
{
    public MetricsContext(bool connectionDurationEnabled, bool currentConnectionsCounterEnabled)
    {
        ConnectionDurationEnabled = connectionDurationEnabled;
        CurrentConnectionsCounterEnabled = currentConnectionsCounterEnabled;
    }

    public bool ConnectionDurationEnabled { get; }
    public bool CurrentConnectionsCounterEnabled { get; }
}

internal sealed class HttpConnectionsMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Http.Connections";

    private readonly Meter _meter;
    private readonly Histogram<double> _connectionDuration;
    private readonly UpDownCounter<long> _currentConnectionsCounter;

    public HttpConnectionsMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _connectionDuration = _meter.CreateHistogram<double>(
            "signalr-http-transport-connection-duration",
            unit: "s",
            description: "The duration of connections on the server.");

        _currentConnectionsCounter = _meter.CreateUpDownCounter<long>(
            "signalr-http-transport-current-connections",
            description: "Number of connections that are currently active on the server.");
    }

    public void ConnectionStop(in MetricsContext metricsContext, HttpTransportType transportType, HttpConnectionStopStatus status, long startTimestamp, long currentTimestamp)
    {
        if (metricsContext.ConnectionDurationEnabled)
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _connectionDuration.Record(duration.TotalSeconds,
                new KeyValuePair<string, object?>("status", status.ToString()),
                new KeyValuePair<string, object?>("transport", transportType.ToString()));
        }
    }

    public void ConnectionTransportStart(in MetricsContext metricsContext, HttpTransportType transportType)
    {
        Debug.Assert(transportType != HttpTransportType.None);

        // Tags must match transport end.
        if (metricsContext.CurrentConnectionsCounterEnabled)
        {
            _currentConnectionsCounter.Add(1, new KeyValuePair<string, object?>("transport", transportType.ToString()));
        }
    }

    public void TransportStop(in MetricsContext metricsContext, HttpTransportType transportType)
    {
        if (metricsContext.CurrentConnectionsCounterEnabled)
        {
            // Tags must match transport start.
            // If the transport type is none then the transport was never started for this connection.
            if (transportType != HttpTransportType.None)
            {
                _currentConnectionsCounter.Add(-1, new KeyValuePair<string, object?>("transport", transportType.ToString()));
            }
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public MetricsContext CreateContext()
    {
        return new MetricsContext(_connectionDuration.Enabled, _currentConnectionsCounter.Enabled);
    }
}
