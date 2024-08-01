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
            "signalr.server.connection.duration",
            unit: "s",
            description: "The duration of connections on the server.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.LongSecondsBucketBoundaries });

        _currentConnectionsCounter = _meter.CreateUpDownCounter<long>(
            "signalr.server.active_connections",
            unit: "{connection}",
            description: "Number of connections that are currently active on the server.");
    }

    public void ConnectionStop(in MetricsContext metricsContext, HttpTransportType transportType, HttpConnectionStopStatus status, long startTimestamp, long currentTimestamp)
    {
        if (metricsContext.ConnectionDurationEnabled)
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _connectionDuration.Record(duration.TotalSeconds,
                new KeyValuePair<string, object?>("signalr.connection.status", ResolveStopStatus(status)),
                new KeyValuePair<string, object?>("signalr.transport", ResolveTransportType(transportType)));
        }
    }

    public void ConnectionTransportStart(in MetricsContext metricsContext, HttpTransportType transportType)
    {
        Debug.Assert(transportType != HttpTransportType.None);

        // Tags must match transport end.
        if (metricsContext.CurrentConnectionsCounterEnabled)
        {
            _currentConnectionsCounter.Add(1, new KeyValuePair<string, object?>("signalr.transport", ResolveTransportType(transportType)));
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
                _currentConnectionsCounter.Add(-1, new KeyValuePair<string, object?>("signalr.transport", ResolveTransportType(transportType)));
            }
        }
    }

    private static string ResolveTransportType(HttpTransportType transportType)
    {
        return transportType switch
        {
            HttpTransportType.ServerSentEvents => "server_sent_events",
            HttpTransportType.LongPolling => "long_polling",
            HttpTransportType.WebSockets => "web_sockets",
            _ => throw new InvalidOperationException("Unexpected value: " + transportType)
        };
    }

    private static string ResolveStopStatus(HttpConnectionStopStatus connectionStopStatus)
    {
        return connectionStopStatus switch
        {
            HttpConnectionStopStatus.NormalClosure => "normal_closure",
            HttpConnectionStopStatus.Timeout => "timeout",
            HttpConnectionStopStatus.AppShutdown => "app_shutdown",
            _ => throw new InvalidOperationException("Unexpected value: " + connectionStopStatus)
        };
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
