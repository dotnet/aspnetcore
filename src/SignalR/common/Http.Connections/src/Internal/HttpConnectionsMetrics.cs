// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Metrics;

namespace Microsoft.AspNetCore.Http.Connections.Internal;

internal sealed class HttpConnectionsMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Http.Connections";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _currentConnectionsCounter;
    private readonly Histogram<double> _connectionDuration;
    private readonly UpDownCounter<long> _currentTransportsCounter;

    public HttpConnectionsMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.CreateMeter(MeterName);

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

    public void ConnectionStart()
    {
        // Tags must match connection end.
        _currentConnectionsCounter.Add(1);
    }

    public void ConnectionStop(HttpTransportType transportType, HttpConnectionStopStatus status, long startTimestamp, long currentTimestamp)
    {
        // Tags must match connection start.
        _currentConnectionsCounter.Add(-1);

        if (_connectionDuration.Enabled)
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _connectionDuration.Record(duration.TotalSeconds,
                new KeyValuePair<string, object?>("status", status.ToString()),
                new KeyValuePair<string, object?>("transport", transportType.ToString()));
        }
    }

    public void TransportStart(HttpTransportType transportType)
    {
        Debug.Assert(transportType != HttpTransportType.None);

        // Tags must match transport end.
        _currentTransportsCounter.Add(1, new KeyValuePair<string, object?>("transport", transportType.ToString()));
    }

    public void TransportStop(HttpTransportType transportType)
    {
        // Tags must match transport start.
        // If the transport type is none then the transport was never started for this connection.
        if (transportType != HttpTransportType.None)
        {
            _currentTransportsCounter.Add(-1, new KeyValuePair<string, object?>("transport", transportType.ToString()));
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public bool IsEnabled() => _currentConnectionsCounter.Enabled || _connectionDuration.Enabled;
}
