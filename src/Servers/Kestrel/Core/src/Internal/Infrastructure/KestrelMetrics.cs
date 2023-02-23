// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Metrics;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class KestrelMetrics
{
    // Note: Dot separated instead of dash.
    public const string MeterName = "Microsoft.AspNetCore.Server.Kestrel";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _currentConnectionsCounter;
    private readonly Histogram<double> _connectionDuration;
    private readonly Counter<long> _rejectedConnectionsCounter;
    private readonly UpDownCounter<long> _queuedConnectionsCounter;
    private readonly UpDownCounter<long> _queuedRequestsCounter;
    private readonly UpDownCounter<long> _currentUpgradedRequestsCounter;
    private readonly Histogram<double> _tlsHandshakeDuration;
    private readonly UpDownCounter<long> _currentTlsHandshakesCounter;

    public KestrelMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.CreateMeter(MeterName);

        _currentConnectionsCounter = _meter.CreateUpDownCounter<long>(
           "current-connections",
            description: "Number of concurrent connections that are currently active on the server.");

        _connectionDuration = _meter.CreateHistogram<double>(
            "connection-duration",
            unit: "s",
            description: "The duration of connections on the server.");

        _rejectedConnectionsCounter = _meter.CreateCounter<long>(
           "rejected-connections",
            description: "Number of connections rejected by the server. Connections are rejected when the currently active count exceeds the value configured with MaxConcurrentConnections.");

        _queuedConnectionsCounter = _meter.CreateUpDownCounter<long>(
           "queued-connections",
            description: "Number of connections that are currently queued and are waiting to start.");

        _queuedRequestsCounter = _meter.CreateUpDownCounter<long>(
           "queued-requests",
            description: "Number of HTTP requests on multiplexed connections (HTTP/2 and HTTP/3) that are currently queued and are waiting to start.");

        _currentUpgradedRequestsCounter = _meter.CreateUpDownCounter<long>(
           "current-upgraded-requests",
            description: "Number of HTTP requests that are currently upgraded (WebSockets).");

        _tlsHandshakeDuration = _meter.CreateHistogram<double>(
            "tls-handshake-duration",
            unit: "s",
            description: "The duration of TLS handshakes on the server.");

        _currentTlsHandshakesCounter = _meter.CreateUpDownCounter<long>(
           "current-tls-handshakes",
            description: "Number of TLS handshakes that are currently active on the server.");
    }

    public void ConnectionStart(BaseConnectionContext connection)
    {
        if (_currentConnectionsCounter.Enabled)
        {
            ConnectionStartCore(connection);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionStartCore(BaseConnectionContext connection)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, connection);
        _currentConnectionsCounter.Add(1, tags);
    }

    public void ConnectionStop(BaseConnectionContext connection, long startTimestamp, long currentTimestamp)
    {
        if (_currentConnectionsCounter.Enabled || _connectionDuration.Enabled)
        {
            ConnectionStopCore(connection, startTimestamp, currentTimestamp);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionStopCore(BaseConnectionContext connection, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, connection);

        // Decrease in connections counter must match tags from increase. No custom tags.
        _currentConnectionsCounter.Add(-1, tags);

        // Add custom tags for duration.
        var customTags = connection.Features.Get<IConnectionMetricsTagsFeature>()?.Tags;
        if (customTags != null)
        {
            for (var i = 0; i < customTags.Count; i++)
            {
                tags.Add(customTags[i]);
            }
        }

        var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
        _connectionDuration.Record(duration.TotalSeconds, tags);
    }

    public void ConnectionRejected(BaseConnectionContext connection)
    {
        if (_rejectedConnectionsCounter.Enabled)
        {
            ConnectionRejectedCore(connection);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionRejectedCore(BaseConnectionContext connection)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, connection);
        _rejectedConnectionsCounter.Add(1, tags);
    }

    public void ConnectionQueuedStart(BaseConnectionContext connection)
    {
        if (_queuedConnectionsCounter.Enabled)
        {
            ConnectionQueuedStartCore(connection);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionQueuedStartCore(BaseConnectionContext connection)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, connection);
        _queuedConnectionsCounter.Add(1, tags);
    }

    public void ConnectionQueuedStop(BaseConnectionContext connection)
    {
        if (_queuedConnectionsCounter.Enabled)
        {
            ConnectionQueuedStopCore(connection);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionQueuedStopCore(BaseConnectionContext connection)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, connection);
        _queuedConnectionsCounter.Add(-1, tags);
    }

    public void RequestQueuedStart(BaseConnectionContext connection, string httpVersion)
    {
        if (_queuedRequestsCounter.Enabled)
        {
            RequestQueuedStartCore(connection, httpVersion);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestQueuedStartCore(BaseConnectionContext connection, string httpVersion)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, connection);
        tags.Add("version", httpVersion);
        _queuedRequestsCounter.Add(1, tags);
    }

    public void RequestQueuedStop(BaseConnectionContext connection, string httpVersion)
    {
        if (_queuedRequestsCounter.Enabled)
        {
            RequestQueuedStopCore(connection, httpVersion);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestQueuedStopCore(BaseConnectionContext connection, string httpVersion)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, connection);
        tags.Add("version", httpVersion);
        _queuedRequestsCounter.Add(-1, tags);
    }

    public void RequestUpgradedStart(BaseConnectionContext connection)
    {
        if (_currentUpgradedRequestsCounter.Enabled)
        {
            RequestUpgradedStartCore(connection);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestUpgradedStartCore(BaseConnectionContext connection)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, connection);
        _currentUpgradedRequestsCounter.Add(1, tags);
    }

    public void RequestUpgradedStop(BaseConnectionContext connection)
    {
        if (_currentUpgradedRequestsCounter.Enabled)
        {
            RequestUpgradedStopCore(connection);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestUpgradedStopCore(BaseConnectionContext connection)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, connection);
        _currentUpgradedRequestsCounter.Add(-1, tags);
    }

    public void TlsHandshakeStart(BaseConnectionContext connection)
    {
        if (_currentTlsHandshakesCounter.Enabled)
        {
            TlsHandshakeStartCore(connection);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void TlsHandshakeStartCore(BaseConnectionContext connection)
    {
        // Tags must match TLS handshake end.
        var tags = new TagList();
        InitializeConnectionTags(ref tags, connection);
        _currentTlsHandshakesCounter.Add(1, tags);
    }

    public void TlsHandshakeStop(BaseConnectionContext connection, long startTimestamp, long currentTimestamp, Exception? exception = null)
    {
        if (_currentTlsHandshakesCounter.Enabled || _tlsHandshakeDuration.Enabled)
        {
            TlsHandshakeStopCore(connection, startTimestamp, currentTimestamp, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void TlsHandshakeStopCore(BaseConnectionContext connection, long startTimestamp, long currentTimestamp, Exception? exception = null)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, connection);

        // Tags must match TLS handshake start.
        _currentTlsHandshakesCounter.Add(-1, tags);

        if (exception != null)
        {
            tags.Add("exception-name", exception.GetType().FullName);
        }

        var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
        _tlsHandshakeDuration.Record(duration.TotalSeconds, tags);
    }

    private static void InitializeConnectionTags(ref TagList tags, BaseConnectionContext connection)
    {
        if (connection.LocalEndPoint is { } localEndpoint)
        {
            tags.Add("endpoint", localEndpoint.ToString());
        }
    }

    public bool IsConnectionDurationEnabled() => _connectionDuration.Enabled;
}
