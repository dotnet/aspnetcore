// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class KestrelMetrics
{
    // Note: Dot separated instead of dash.
    public const string MeterName = "Microsoft.AspNetCore.Server.Kestrel";

    public const string ErrorTypeAttributeName = "error.type";

    public const string Http11 = "1.1";
    public const string Http2 = "2";
    public const string Http3 = "3";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _activeConnectionsCounter;
    private readonly Histogram<double> _connectionDuration;
    private readonly Counter<long> _rejectedConnectionsCounter;
    private readonly UpDownCounter<long> _queuedConnectionsCounter;
    private readonly UpDownCounter<long> _queuedRequestsCounter;
    private readonly UpDownCounter<long> _currentUpgradedRequestsCounter;
    private readonly Histogram<double> _tlsHandshakeDuration;
    private readonly UpDownCounter<long> _activeTlsHandshakesCounter;

    public KestrelMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _activeConnectionsCounter = _meter.CreateUpDownCounter<long>(
            "kestrel.active_connections",
            unit: "{connection}",
            description: "Number of connections that are currently active on the server.");

        _connectionDuration = _meter.CreateHistogram<double>(
            "kestrel.connection.duration",
            unit: "s",
            description: "The duration of connections on the server.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.LongSecondsBucketBoundaries });

        _rejectedConnectionsCounter = _meter.CreateCounter<long>(
           "kestrel.rejected_connections",
            unit: "{connection}",
            description: "Number of connections rejected by the server. Connections are rejected when the currently active count exceeds the value configured with MaxConcurrentConnections.");

        _queuedConnectionsCounter = _meter.CreateUpDownCounter<long>(
           "kestrel.queued_connections",
            unit: "{connection}",
            description: "Number of connections that are currently queued and are waiting to start.");

        _queuedRequestsCounter = _meter.CreateUpDownCounter<long>(
           "kestrel.queued_requests",
            unit: "{request}",
            description: "Number of HTTP requests on multiplexed connections (HTTP/2 and HTTP/3) that are currently queued and are waiting to start.");

        _currentUpgradedRequestsCounter = _meter.CreateUpDownCounter<long>(
           "kestrel.upgraded_connections",
            unit: "{connection}",
            description: "Number of HTTP connections that are currently upgraded (WebSockets). The number only tracks HTTP/1.1 connections.");

        _tlsHandshakeDuration = _meter.CreateHistogram<double>(
            "kestrel.tls_handshake.duration",
            unit: "s",
            description: "The duration of TLS handshakes on the server.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _activeTlsHandshakesCounter = _meter.CreateUpDownCounter<long>(
           "kestrel.active_tls_handshakes",
            unit: "{handshake}",
            description: "Number of TLS handshakes that are currently in progress on the server.");
    }

    public void ConnectionStart(ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.CurrentConnectionsCounterEnabled)
        {
            ConnectionStartCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionStartCore(ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _activeConnectionsCounter.Add(1, tags);
    }

    public void ConnectionStop(ConnectionMetricsContext metricsContext, Exception? exception, List<KeyValuePair<string, object?>>? customTags, long startTimestamp, long currentTimestamp)
    {
        if (metricsContext.CurrentConnectionsCounterEnabled || metricsContext.ConnectionDurationEnabled)
        {
            ConnectionStopCore(metricsContext, exception, customTags, startTimestamp, currentTimestamp);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionStopCore(ConnectionMetricsContext metricsContext, Exception? exception, List<KeyValuePair<string, object?>>? customTags, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);

        if (metricsContext.CurrentConnectionsCounterEnabled)
        {
            // Decrease in connections counter must match tags from increase. No custom tags.
            _activeConnectionsCounter.Add(-1, tags);
        }

        if (metricsContext.ConnectionDurationEnabled)
        {
            // Check if there is an end reason on the context. For example, the connection could have been aborted by shutdown.
            if (metricsContext.ConnectionEndReason is { } reason && TryGetErrorType(reason, out var errorValue))
            {
                tags.TryAddTag(ErrorTypeAttributeName, errorValue);
            }
            else if (exception != null)
            {
                tags.TryAddTag(ErrorTypeAttributeName, exception.GetType().FullName);
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

    public void ConnectionRejected(ConnectionMetricsContext metricsContext)
    {
        AddConnectionEndReason(metricsContext, ConnectionEndReason.MaxConcurrentConnectionsExceeded);

        // Check live rather than cached state because this is just a counter, it's not a start/stop event like the other metrics.
        if (_rejectedConnectionsCounter.Enabled)
        {
            ConnectionRejectedCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionRejectedCore(ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _rejectedConnectionsCounter.Add(1, tags);
    }

    public void ConnectionQueuedStart(ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.QueuedConnectionsCounterEnabled)
        {
            ConnectionQueuedStartCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionQueuedStartCore(ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _queuedConnectionsCounter.Add(1, tags);
    }

    public void ConnectionQueuedStop(ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.QueuedConnectionsCounterEnabled)
        {
            ConnectionQueuedStopCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionQueuedStopCore(ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _queuedConnectionsCounter.Add(-1, tags);
    }

    public void RequestQueuedStart(ConnectionMetricsContext metricsContext, string httpVersion)
    {
        if (metricsContext.QueuedRequestsCounterEnabled)
        {
            RequestQueuedStartCore(metricsContext, httpVersion);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestQueuedStartCore(ConnectionMetricsContext metricsContext, string httpVersion)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        tags.Add("network.protocol.name", "http");
        tags.Add("network.protocol.version", httpVersion);
        _queuedRequestsCounter.Add(1, tags);
    }

    public void RequestQueuedStop(ConnectionMetricsContext metricsContext, string httpVersion)
    {
        if (metricsContext.QueuedRequestsCounterEnabled)
        {
            RequestQueuedStopCore(metricsContext, httpVersion);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestQueuedStopCore(ConnectionMetricsContext metricsContext, string httpVersion)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        tags.Add("network.protocol.name", "http");
        tags.Add("network.protocol.version", httpVersion);
        _queuedRequestsCounter.Add(-1, tags);
    }

    public void RequestUpgradedStart(ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.CurrentUpgradedRequestsCounterEnabled)
        {
            RequestUpgradedStartCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestUpgradedStartCore(ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _currentUpgradedRequestsCounter.Add(1, tags);
    }

    public void RequestUpgradedStop(ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.CurrentUpgradedRequestsCounterEnabled)
        {
            RequestUpgradedStopCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestUpgradedStopCore(ConnectionMetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _currentUpgradedRequestsCounter.Add(-1, tags);
    }

    public void TlsHandshakeStart(ConnectionMetricsContext metricsContext)
    {
        if (metricsContext.CurrentTlsHandshakesCounterEnabled)
        {
            TlsHandshakeStartCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void TlsHandshakeStartCore(ConnectionMetricsContext metricsContext)
    {
        // Tags must match TLS handshake end.
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);
        _activeTlsHandshakesCounter.Add(1, tags);
    }

    public void TlsHandshakeStop(ConnectionMetricsContext metricsContext, long startTimestamp, long currentTimestamp, SslProtocols? protocol = null, Exception? exception = null)
    {
        if (metricsContext.CurrentTlsHandshakesCounterEnabled || _tlsHandshakeDuration.Enabled)
        {
            TlsHandshakeStopCore(metricsContext, startTimestamp, currentTimestamp, protocol, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void TlsHandshakeStopCore(ConnectionMetricsContext metricsContext, long startTimestamp, long currentTimestamp, SslProtocols? protocol = null, Exception? exception = null)
    {
        var tags = new TagList();
        InitializeConnectionTags(ref tags, metricsContext);

        if (metricsContext.CurrentTlsHandshakesCounterEnabled)
        {
            // Tags must match TLS handshake start.
            _activeTlsHandshakesCounter.Add(-1, tags);
        }

        if (protocol != null && TryGetHandshakeProtocol(protocol.Value, out var protocolName, out var protocolVersion))
        {
            // Protocol name should always be TLS. Have logic to a tls.protocol.name tag if not TLS just in case.
            if (protocolName != "tls")
            {
                tags.Add("tls.protocol.name", protocolName);
            }
            tags.Add("tls.protocol.version", protocolVersion);
        }
        if (exception != null)
        {
            // Set exception name as error.type if there isn't already a value.
            tags.TryAddTag(ErrorTypeAttributeName, exception.GetType().FullName);
        }

        var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
        _tlsHandshakeDuration.Record(duration.TotalSeconds, tags);
    }

    private static void InitializeConnectionTags(ref TagList tags, in ConnectionMetricsContext metricsContext)
    {
        var localEndpoint = metricsContext.ConnectionContext.LocalEndPoint;
        if (localEndpoint is IPEndPoint localIPEndPoint)
        {
            tags.Add("server.address", localIPEndPoint.Address.ToString());
            tags.Add("server.port", localIPEndPoint.Port);

            switch (localIPEndPoint.Address.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    tags.Add("network.type", "ipv4");
                    break;
                case AddressFamily.InterNetworkV6:
                    tags.Add("network.type", "ipv6");
                    break;
            }

            // There isn't an easy way to detect whether QUIC is the underlying transport.
            // This code assumes that a multiplexed connection is QUIC.
            // Improve in the future if there are additional multiplexed connection types.
            var transport = metricsContext.ConnectionContext is not MultiplexedConnectionContext ? "tcp" : "udp";
            tags.Add("network.transport", transport);
        }
        else if (localEndpoint is UnixDomainSocketEndPoint udsEndPoint)
        {
            tags.Add("server.address", udsEndPoint.ToString());
            tags.Add("network.transport", "unix");
        }
        else if (localEndpoint is NamedPipeEndPoint namedPipeEndPoint)
        {
            tags.Add("server.address", namedPipeEndPoint.ToString());
            tags.Add("network.transport", "pipe");
        }
        else if (localEndpoint != null)
        {
            tags.Add("server.address", localEndpoint.ToString());
            tags.Add("network.transport", localEndpoint.AddressFamily.ToString());
        }
    }

    public ConnectionMetricsContext CreateContext(BaseConnectionContext connection)
    {
        // Cache the state at the start of the connection so we produce consistent start/stop events.
        return new ConnectionMetricsContext
        {
            ConnectionContext = connection,
            CurrentConnectionsCounterEnabled = _activeConnectionsCounter.Enabled,
            ConnectionDurationEnabled = _connectionDuration.Enabled,
            QueuedConnectionsCounterEnabled = _queuedConnectionsCounter.Enabled,
            QueuedRequestsCounterEnabled = _queuedRequestsCounter.Enabled,
            CurrentUpgradedRequestsCounterEnabled = _currentUpgradedRequestsCounter.Enabled,
            CurrentTlsHandshakesCounterEnabled = _activeTlsHandshakesCounter.Enabled
        };
    }

    public static bool TryGetHandshakeProtocol(SslProtocols protocols, [NotNullWhen(true)] out string? name, [NotNullWhen(true)] out string? version)
    {
        // Protocol should be either TLS 1.2 or 1.3. Many older SslProtocols are no longer supported.
        // Logic for resolving older known values is still here out of an abundence of caution.

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable SYSLIB0039 // Type or member is obsolete
        switch (protocols)
        {
            case SslProtocols.Ssl2:
                name = "ssl";
                version = "2.0";
                return true;
            case SslProtocols.Ssl3:
                name = "ssl";
                version = "3.0";
                return true;
            case SslProtocols.Tls:
                name = "tls";
                version = "1.0";
                return true;
            case SslProtocols.Tls11:
                name = "tls";
                version = "1.1";
                return true;
            case SslProtocols.Tls12:
                name = "tls";
                version = "1.2";
                return true;
            case SslProtocols.Tls13:
                name = "tls";
                version = "1.3";
                return true;
        }
#pragma warning restore SYSLIB0039 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

        name = null;
        version = null;
        return false;
    }

    public static void AddConnectionEndReason(IConnectionMetricsTagsFeature? feature, ConnectionEndReason reason)
    {
        Debug.Assert(reason != ConnectionEndReason.Unset);

        if (feature != null)
        {
            if (TryGetErrorType(reason, out var errorTypeValue))
            {
                feature.TryAddTag(ErrorTypeAttributeName, errorTypeValue);
            }
        }
    }

    public static void AddConnectionEndReason(ConnectionMetricsContext? context, ConnectionEndReason reason, bool overwrite = false)
    {
        Debug.Assert(reason != ConnectionEndReason.Unset);

        if (context != null)
        {
            // Set end reason when either:
            // - Overwrite is true. For example, AppShutdownTimeout reason is forced when shutting down
            //   the app reguardless of whether there is already a value.
            // - New reason is an error type and there isn't already an error type set.
            //   In other words, first error wins.
            if (overwrite)
            {
                Debug.Assert(TryGetErrorType(reason, out _), "Overwrite should only be set for an error reason.");
                context.ConnectionEndReason = reason;
            }
            else if (TryGetErrorType(reason, out _))
            {
                if (context.ConnectionEndReason == null)
                {
                    context.ConnectionEndReason = reason;
                }
            }
        }
    }

    internal static string? GetErrorType(ConnectionEndReason reason)
    {
        TryGetErrorType(reason, out var errorTypeValue);
        return errorTypeValue;
    }

    internal static bool TryGetErrorType(ConnectionEndReason reason, [NotNullWhen(true)]out string? errorTypeValue)
    {
        errorTypeValue = reason switch
        {
            ConnectionEndReason.Unset => null, // Not an error
            ConnectionEndReason.ClientGoAway => null, // Not an error
            ConnectionEndReason.TransportCompleted => null, // Not an error
            ConnectionEndReason.GracefulAppShutdown => null, // Not an error
            ConnectionEndReason.RequestNoKeepAlive => null, // Not an error
            ConnectionEndReason.ResponseNoKeepAlive => null, // Not an error
            ConnectionEndReason.ErrorAfterStartingResponse => "error_after_starting_response",
            ConnectionEndReason.ConnectionReset => "connection_reset",
            ConnectionEndReason.FlowControlWindowExceeded => "flow_control_window_exceeded",
            ConnectionEndReason.KeepAliveTimeout => "keep_alive_timeout",
            ConnectionEndReason.InsufficientTlsVersion => "insufficient_tls_version",
            ConnectionEndReason.InvalidHandshake => "invalid_handshake",
            ConnectionEndReason.InvalidStreamId => "invalid_stream_id",
            ConnectionEndReason.FrameAfterStreamClose => "frame_after_stream_close",
            ConnectionEndReason.UnknownStream => "unknown_stream",
            ConnectionEndReason.UnexpectedFrame => "unexpected_frame",
            ConnectionEndReason.InvalidFrameLength => "invalid_frame_length",
            ConnectionEndReason.InvalidDataPadding => "invalid_data_padding",
            ConnectionEndReason.InvalidRequestHeaders => "invalid_request_headers",
            ConnectionEndReason.StreamResetLimitExceeded => "stream_reset_limit_exceeded",
            ConnectionEndReason.InvalidWindowUpdateSize => "invalid_window_update_size",
            ConnectionEndReason.StreamSelfDependency => "stream_self_dependency",
            ConnectionEndReason.InvalidSettings => "invalid_settings",
            ConnectionEndReason.MissingStreamEnd => "missing_stream_end",
            ConnectionEndReason.MaxFrameLengthExceeded => "max_frame_length_exceeded",
            ConnectionEndReason.ErrorReadingHeaders => "error_reading_headers",
            ConnectionEndReason.ErrorWritingHeaders => "error_writing_headers",
            ConnectionEndReason.OtherError => "other_error",
            ConnectionEndReason.InvalidHttpVersion => "invalid_http_version",
            ConnectionEndReason.RequestHeadersTimeout => "request_headers_timeout",
            ConnectionEndReason.MinRequestBodyDataRate => "min_request_body_data_rate",
            ConnectionEndReason.MinResponseDataRate => "min_response_data_rate",
            ConnectionEndReason.FlowControlQueueSizeExceeded => "flow_control_queue_size_exceeded",
            ConnectionEndReason.OutputQueueSizeExceeded => "output_queue_size_exceeded",
            ConnectionEndReason.ClosedCriticalStream => "closed_critical_stream",
            ConnectionEndReason.AbortedByApp => "aborted_by_app",
            ConnectionEndReason.WriteCanceled => "write_canceled",
            ConnectionEndReason.InvalidBodyReaderState => "invalid_body_reader_state",
            ConnectionEndReason.ServerTimeout => "server_timeout",
            ConnectionEndReason.StreamCreationError => "stream_creation_error",
            ConnectionEndReason.IOError => "io_error",
            ConnectionEndReason.AppShutdownTimeout => "app_shutdown_timeout",
            ConnectionEndReason.TlsHandshakeFailed => "tls_handshake_failed",
            ConnectionEndReason.InvalidRequestLine => "invalid_request_line",
            ConnectionEndReason.TlsNotSupported => "tls_not_supported",
            ConnectionEndReason.MaxRequestBodySizeExceeded => "max_request_body_size_exceeded",
            ConnectionEndReason.UnexpectedEndOfRequestContent => "unexpected_end_of_request_content",
            ConnectionEndReason.MaxConcurrentConnectionsExceeded => "max_concurrent_connections_exceeded",
            ConnectionEndReason.MaxRequestHeadersTotalSizeExceeded => "max_request_headers_total_size_exceeded",
            ConnectionEndReason.MaxRequestHeaderCountExceeded => "max_request_header_count_exceeded",
            ConnectionEndReason.ResponseContentLengthMismatch => "response_content_length_mismatch",
            _ => throw new InvalidOperationException($"Unable to calculate whether {reason} resolves to error.type value.")
        };

        return errorTypeValue != null;
    }
}
