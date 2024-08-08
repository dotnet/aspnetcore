// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

/// <remarks>
/// Instantiated by <see cref="HttpConnectionMiddleware{TContext}"/> when a connection is received.
/// <para/>
/// Not related, type-wise, to <see cref="Http1Connection{TContext}"/>, <see cref="Http2Connection"/>,
/// or <see cref="Http3Connection"/>. It does, however, instantiate one of those types as its
/// <see cref="_requestProcessor"/> based on the protocol.
/// </remarks>
internal sealed class HttpConnection : ITimeoutHandler
{
    private static ReadOnlySpan<byte> Http2Id => "h2"u8;

    private readonly BaseHttpConnectionContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly TimeoutControl _timeoutControl;

    private readonly Lock _protocolSelectionLock = new();
    private ProtocolSelectionState _protocolSelectionState = ProtocolSelectionState.Initializing;
    private Http1Connection? _http1Connection;

    // Internal for testing
    internal IRequestProcessor? _requestProcessor;

    public HttpConnection(BaseHttpConnectionContext context)
    {
        _context = context;
        _timeProvider = _context.ServiceContext.TimeProvider;

        _timeoutControl = new TimeoutControl(this, _timeProvider);

        // Tests override the timeout control sometimes
        _context.TimeoutControl ??= _timeoutControl;
    }

    private KestrelTrace Log => _context.ServiceContext.Log;

    public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> httpApplication) where TContext : notnull
    {
        IConnectionMetricsTagsFeature? connectionMetricsTagsFeature = null;

        try
        {
            connectionMetricsTagsFeature = _context.ConnectionFeatures.Get<IConnectionMetricsTagsFeature>();

            // Ensure TimeoutControl._lastTimestamp is initialized before anything that could set timeouts runs.
            _timeoutControl.Initialize();

            IRequestProcessor? requestProcessor = null;

            switch (SelectProtocol())
            {
                case HttpProtocols.Http1:
                    // _http1Connection must be initialized before adding the connection to the connection manager
                    requestProcessor = _http1Connection = new Http1Connection<TContext>((HttpConnectionContext)_context);
                    _protocolSelectionState = ProtocolSelectionState.Selected;
                    AddMetricsHttpProtocolTag(KestrelMetrics.Http11);
                    break;
                case HttpProtocols.Http2:
                    // _http2Connection must be initialized before yielding control to the transport thread,
                    // to prevent a race condition where _http2Connection.Abort() is called just as
                    // _http2Connection is about to be initialized.
                    requestProcessor = new Http2Connection((HttpConnectionContext)_context);
                    _protocolSelectionState = ProtocolSelectionState.Selected;
                    AddMetricsHttpProtocolTag(KestrelMetrics.Http2);
                    break;
                case HttpProtocols.Http3:
                    requestProcessor = new Http3Connection((HttpMultiplexedConnectionContext)_context);
                    _protocolSelectionState = ProtocolSelectionState.Selected;
                    AddMetricsHttpProtocolTag(KestrelMetrics.Http3);
                    break;
                case HttpProtocols.None:
                    // An error was already logged in SelectProtocol(), but we should close the connection.
                    break;

                default:
                    // SelectProtocol() only returns Http1, Http2, Http3 or None.
                    throw new NotSupportedException($"{nameof(SelectProtocol)} returned something other than Http1, Http2 or None.");
            }

            _requestProcessor = requestProcessor;

            if (requestProcessor != null)
            {
                var connectionHeartbeatFeature = _context.ConnectionFeatures.Get<IConnectionHeartbeatFeature>();
                var connectionLifetimeNotificationFeature = _context.ConnectionFeatures.Get<IConnectionLifetimeNotificationFeature>();

                // These features should never be null in Kestrel itself, if this middleware is ever refactored to run outside of kestrel,
                // we'll need to handle these missing.
                Debug.Assert(connectionHeartbeatFeature != null, nameof(IConnectionHeartbeatFeature) + " is missing!");
                Debug.Assert(connectionLifetimeNotificationFeature != null, nameof(IConnectionLifetimeNotificationFeature) + " is missing!");

                // Register the various callbacks once we're going to start processing requests

                // The heart beat for various timeouts
                connectionHeartbeatFeature?.OnHeartbeat(state => ((HttpConnection)state).Tick(), this);

                // Register for graceful shutdown of the server
                using var shutdownRegistration = connectionLifetimeNotificationFeature?.ConnectionClosedRequested.Register(state => ((HttpConnection)state!).StopProcessingNextRequest(ConnectionEndReason.GracefulAppShutdown), this);

                // Register for connection close
                using var closedRegistration = _context.ConnectionContext.ConnectionClosed.Register(state => ((HttpConnection)state!).OnConnectionClosed(), this);

                await requestProcessor.ProcessRequestsAsync(httpApplication);
            }
        }
        catch (Exception ex)
        {
            Log.LogCritical(0, ex, $"Unexpected exception in {nameof(HttpConnection)}.{nameof(ProcessRequestsAsync)}.");
        }
        finally
        {
            // Before exiting HTTP layer, set the end reason on the context as a connection metrics tag.
            if (_context.MetricsContext.ConnectionEndReason is { } connectionEndReason)
            {
                KestrelMetrics.AddConnectionEndReason(connectionMetricsTagsFeature, connectionEndReason);
            }
        }
    }

    private void AddMetricsHttpProtocolTag(string httpVersion)
    {
        if (_context.ConnectionContext.Features.Get<IConnectionMetricsTagsFeature>() is { } metricsTags)
        {
            metricsTags.Tags.Add(new KeyValuePair<string, object?>("network.protocol.name", "http"));
            metricsTags.Tags.Add(new KeyValuePair<string, object?>("network.protocol.version", httpVersion));
        }
    }

    // For testing only
    internal void Initialize(IRequestProcessor requestProcessor)
    {
        _requestProcessor = requestProcessor;
        _http1Connection = requestProcessor as Http1Connection;
        _protocolSelectionState = ProtocolSelectionState.Selected;
    }

    private void StopProcessingNextRequest(ConnectionEndReason reason)
    {
        ProtocolSelectionState previousState;
        lock (_protocolSelectionLock)
        {
            previousState = _protocolSelectionState;
            Debug.Assert(previousState != ProtocolSelectionState.Initializing, "The state should never be initializing");
        }

        switch (previousState)
        {
            case ProtocolSelectionState.Selected:
                _requestProcessor!.StopProcessingNextRequest(reason);
                break;
            case ProtocolSelectionState.Aborted:
                break;
        }
    }

    private void OnConnectionClosed()
    {
        ProtocolSelectionState previousState;
        lock (_protocolSelectionLock)
        {
            previousState = _protocolSelectionState;
            Debug.Assert(previousState != ProtocolSelectionState.Initializing, "The state should never be initializing");
        }

        switch (previousState)
        {
            case ProtocolSelectionState.Selected:
                _requestProcessor!.OnInputOrOutputCompleted();
                break;
            case ProtocolSelectionState.Aborted:
                break;
        }
    }

    private void Abort(ConnectionAbortedException ex, ConnectionEndReason reason)
    {
        ProtocolSelectionState previousState;

        lock (_protocolSelectionLock)
        {
            previousState = _protocolSelectionState;
            Debug.Assert(previousState != ProtocolSelectionState.Initializing, "The state should never be initializing");

            _protocolSelectionState = ProtocolSelectionState.Aborted;
        }

        switch (previousState)
        {
            case ProtocolSelectionState.Selected:
                _requestProcessor!.Abort(ex, reason);
                break;
            case ProtocolSelectionState.Aborted:
                break;
        }
    }

    private HttpProtocols SelectProtocol()
    {
        var hasTls = _context.ConnectionFeatures.Get<ITlsConnectionFeature>() != null;
        var applicationProtocol = _context.ConnectionFeatures.Get<ITlsApplicationProtocolFeature>()?.ApplicationProtocol
            ?? new ReadOnlyMemory<byte>();
        var isMultiplexTransport = _context is HttpMultiplexedConnectionContext;
        var http1Enabled = _context.Protocols.HasFlag(HttpProtocols.Http1);
        var http2Enabled = _context.Protocols.HasFlag(HttpProtocols.Http2);
        var http3Enabled = _context.Protocols.HasFlag(HttpProtocols.Http3);

        string? error = null;

        if (_context.Protocols == HttpProtocols.None)
        {
            error = CoreStrings.EndPointRequiresAtLeastOneProtocol;
        }

        if (isMultiplexTransport)
        {
            if (http3Enabled)
            {
                return HttpProtocols.Http3;
            }

            error = $"Protocols {_context.Protocols} not supported on multiplexed transport.";
        }

        if (!http1Enabled && http2Enabled && hasTls && !Http2Id.SequenceEqual(applicationProtocol.Span))
        {
            error = CoreStrings.EndPointHttp2NotNegotiated;
        }

        if (error != null)
        {
            Log.LogError(0, error);
            return HttpProtocols.None;
        }

        if (!hasTls && http1Enabled)
        {
            // Even if Http2 was enabled, default to Http1 because it's ambiguous without ALPN.
            return HttpProtocols.Http1;
        }

        return http2Enabled && (!hasTls || Http2Id.SequenceEqual(applicationProtocol.Span)) ? HttpProtocols.Http2 : HttpProtocols.Http1;
    }

    private void Tick()
    {
        if (_protocolSelectionState == ProtocolSelectionState.Aborted)
        {
            // It's safe to check for timeouts on a dead connection,
            // but try not to in order to avoid extraneous logs.
            return;
        }

        var timestamp = _timeProvider.GetTimestamp();
        _timeoutControl.Tick(timestamp);
        _requestProcessor!.Tick(timestamp);
    }

    public void OnTimeout(TimeoutReason reason)
    {
        // In the cases that don't log directly here, we expect the setter of the timeout to also be the input
        // reader, so when the read is canceled or aborted, the reader should write the appropriate log.
        switch (reason)
        {
            case TimeoutReason.KeepAlive:
                _requestProcessor!.StopProcessingNextRequest(ConnectionEndReason.KeepAliveTimeout);
                break;
            case TimeoutReason.RequestHeaders:
                _requestProcessor!.HandleRequestHeadersTimeout();
                break;
            case TimeoutReason.ReadDataRate:
                _requestProcessor!.HandleReadDataRateTimeout();
                break;
            case TimeoutReason.WriteDataRate:
                Log.ResponseMinimumDataRateNotSatisfied(_context.ConnectionId, _http1Connection?.TraceIdentifier);
                Abort(new ConnectionAbortedException(CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied), ConnectionEndReason.MinResponseDataRate);
                break;
            case TimeoutReason.RequestBodyDrain:
            case TimeoutReason.TimeoutFeature:
                Abort(new ConnectionAbortedException(CoreStrings.ConnectionTimedOutByServer), ConnectionEndReason.ServerTimeout);
                break;
            default:
                Debug.Assert(false, "Invalid TimeoutReason");
                break;
        }
    }

    private enum ProtocolSelectionState
    {
        Initializing,
        Selected,
        Aborted
    }
}
