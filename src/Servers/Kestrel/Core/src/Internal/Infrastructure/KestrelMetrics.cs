// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Diagnostics.Metrics;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Security.Authentication;

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
        _meter = meterFactory.Create(MeterName);

        _currentConnectionsCounter = _meter.CreateUpDownCounter<long>(
           "current-connections",
            description: "Number of connections that are currently active on the server.");

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
           "current-upgraded-connections",
            description: "Number of HTTP connections that are currently upgraded (WebSockets). The number only tracks HTTP/1.1 connections.");

        _tlsHandshakeDuration = _meter.CreateHistogram<double>(
            "tls-handshake-duration",
            unit: "s",
            description: "The duration of TLS handshakes on the server.");

        _currentTlsHandshakesCounter = _meter.CreateUpDownCounter<long>(
           "current-tls-handshakes",
            description: "Number of TLS handshakes that are currently in progress on the server.");
    }

    public void ConnectionStart(in ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.CurrentConnectionsCounterEnabled)
        {
            ConnectionStartCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionStartCore(in ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _currentConnectionsCounter.Add(1, tags);
    }

    public void ConnectionStop(in ConnectionMetricsContext metricsContext, Exception? exception, List<KeyValuePair<string, object?>>? customTags, long startTimestamp, long currentTimestamp)
    {
        if (metricsContext.CurrentConnectionsCounterEnabled || metricsContext.ConnectionDurationEnabled)
        {
            ConnectionStopCore(metricsContext, exception, customTags, startTimestamp, currentTimestamp);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionStopCore(in ConnectionMetricsContext metricsContext, Exception? exception, List<KeyValuePair<string, object?>>? customTags, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);

        if (metricsContext.CurrentConnectionsCounterEnabled)
        {
            // Decrease in connections counter must match tags from increase. No custom tags.
            _currentConnectionsCounter.Add(-1, tags);
        }

        if (metricsContext.ConnectionDurationEnabled)
        {
            if (exception != null)
            {
                tags.Add("exception-name", exception.GetType().FullName);
            }

            // Add custom tags for duration.
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
    }

    public void ConnectionRejected(in ConnectionMetricsContext metricsContext)
    {
        // Check live rather than cached state because this is just a counter, it's not a start/stop event like the other metrics.
        if (_rejectedConnectionsCounter.Enabled)
        {
            ConnectionRejectedCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionRejectedCore(in ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _rejectedConnectionsCounter.Add(1, tags);
    }

    public void ConnectionQueuedStart(in ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.QueuedConnectionsCounterEnabled)
        {
            ConnectionQueuedStartCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionQueuedStartCore(in ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _queuedConnectionsCounter.Add(1, tags);
    }

    public void ConnectionQueuedStop(in ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.QueuedConnectionsCounterEnabled)
        {
            ConnectionQueuedStopCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionQueuedStopCore(in ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _queuedConnectionsCounter.Add(-1, tags);
    }

    public void RequestQueuedStart(in ConnectionMetricsContext metricsContext, string httpVersion)
    {
        if (metricsContext.QueuedRequestsCounterEnabled)
        {
            RequestQueuedStartCore(metricsContext, httpVersion);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestQueuedStartCore(in ConnectionMetricsContext metricsContext, string httpVersion)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        tags.Add("version", httpVersion);
        _queuedRequestsCounter.Add(1, tags);
    }

    public void RequestQueuedStop(in ConnectionMetricsContext metricsContext, string httpVersion)
    {
        if (metricsContext.QueuedRequestsCounterEnabled)
        {
            RequestQueuedStopCore(metricsContext, httpVersion);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestQueuedStopCore(in ConnectionMetricsContext metricsContext, string httpVersion)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        tags.Add("version", httpVersion);
        _queuedRequestsCounter.Add(-1, tags);
    }

    public void RequestUpgradedStart(in ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.CurrentUpgradedRequestsCounterEnabled)
        {
            RequestUpgradedStartCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestUpgradedStartCore(in ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _currentUpgradedRequestsCounter.Add(1, tags);
    }

    public void RequestUpgradedStop(in ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.CurrentUpgradedRequestsCounterEnabled)
        {
            RequestUpgradedStopCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestUpgradedStopCore(in ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _currentUpgradedRequestsCounter.Add(-1, tags);
    }

    public void TlsHandshakeStart(in ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.CurrentTlsHandshakesCounterEnabled)
        {
            TlsHandshakeStartCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void TlsHandshakeStartCore(in ConnectionMetricsContext metricsContext)
    {
        // Tags must match TLS handshake end.
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _currentTlsHandshakesCounter.Add(1, tags);
    }

    public void TlsHandshakeStop(in ConnectionMetricsContext metricsContext, long startTimestamp, long currentTimestamp, SslProtocols? protocol = null, Exception? exception = null)
    {
        if (metricsContext.CurrentTlsHandshakesCounterEnabled || _tlsHandshakeDuration.Enabled)
        {
            TlsHandshakeStopCore(metricsContext, startTimestamp, currentTimestamp, protocol, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void TlsHandshakeStopCore(in ConnectionMetricsContext metricsContext, long startTimestamp, long currentTimestamp, SslProtocols? protocol = null, Exception? exception = null)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);

        if (metricsContext.CurrentTlsHandshakesCounterEnabled)
        {
            // Tags must match TLS handshake start.
            _currentTlsHandshakesCounter.Add(-1, tags);
        }

        if (protocol != null)
        {
            tags.Add("protocol", protocol.ToString());
        }
        if (exception != null)
        {
            tags.Add("exception-name", exception.GetType().FullName);
        }

        var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
        _tlsHandshakeDuration.Record(duration.TotalSeconds, tags);
    }

    private static void InitializeConnectionTags(ref TagList tags, in ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.ConnectionContext.LocalEndPoint is { } localEndpoint)
        {
            // TODO: Improve getting string allocation for endpoint. Currently allocates.
            // Considering adding a way to cache on ConnectionContext.
            tags.Add("endpoint", localEndpoint.ToString());
        }
    }

    public ConnectionMetricsContext CreateContext(BaseConnectionContext connection)
    {
        // Cache the state at the start of the connection so we produce consistent start/stop events.
        return new ConnectionMetricsContext(connection,
            _currentConnectionsCounter.Enabled, _connectionDuration.Enabled, _queuedConnectionsCounter.Enabled,
            _queuedRequestsCounter.Enabled, _currentUpgradedRequestsCounter.Enabled, _currentTlsHandshakesCounter.Enabled);
    }
}
