// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Http.Connections.Internal;

internal readonly struct MetricsContext
{
    public MetricsContext(bool connectionDurationEnabled, bool currentNegotiatedConnectionsCounterEnabled)
    {
        ConnectionDurationEnabled = connectionDurationEnabled;
        CurrentNegotiatedConnectionsCounterEnabled = currentNegotiatedConnectionsCounterEnabled;
    }

    public bool ConnectionDurationEnabled { get; }
    public bool CurrentNegotiatedConnectionsCounterEnabled { get; }
}

internal sealed class HttpConnectionsMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Http.Connections";

    private readonly Meter _meter;
    private readonly Histogram<double> _connectionDuration;
    private readonly UpDownCounter<long> _currentNegotiatedConnectionsCounter;

    public HttpConnectionsMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _connectionDuration = _meter.CreateHistogram<double>(
            "signalr-http-transport-connection-duration",
            unit: "s",
            description: "The duration of connections on the server.");

        _currentNegotiatedConnectionsCounter = _meter.CreateUpDownCounter<long>(
            "signalr-http-transport-current-negotiated-connections",
            description: "Number of negotiated connections that are currently active on the server.");
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
        if (metricsContext.CurrentNegotiatedConnectionsCounterEnabled)
        {
            _currentNegotiatedConnectionsCounter.Add(1, new KeyValuePair<string, object?>("transport", transportType.ToString()));
        }
    }

    public void TransportStop(in MetricsContext metricsContext, HttpTransportType transportType)
    {
        if (metricsContext.CurrentNegotiatedConnectionsCounterEnabled)
        {
            // Tags must match transport start.
            // If the transport type is none then the transport was never started for this connection.
            if (transportType != HttpTransportType.None)
            {
                _currentNegotiatedConnectionsCounter.Add(-1, new KeyValuePair<string, object?>("transport", transportType.ToString()));
            }
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public MetricsContext CreateContext()
    {
        return new MetricsContext(_connectionDuration.Enabled, _currentNegotiatedConnectionsCounter.Enabled);
    }
}
