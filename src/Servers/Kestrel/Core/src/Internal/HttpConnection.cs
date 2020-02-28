// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
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

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class HttpConnection : ITimeoutHandler
    {
        // Use C#7.3's ReadOnlySpan<byte> optimization for static data https://vcsjones.com/2019/02/01/csharp-readonly-span-bytes-static/
        private static ReadOnlySpan<byte> Http2Id => new[] { (byte)'h', (byte)'2' };

        private readonly HttpConnectionContext _context;
        private readonly ISystemClock _systemClock;
        private readonly TimeoutControl _timeoutControl;

        private readonly object _protocolSelectionLock = new object();
        private ProtocolSelectionState _protocolSelectionState = ProtocolSelectionState.Initializing;
        private IRequestProcessor _requestProcessor;
        private Http1Connection _http1Connection;

        public HttpConnection(HttpConnectionContext context)
        {
            _context = context;
            _systemClock = _context.ServiceContext.SystemClock;

            _timeoutControl = new TimeoutControl(this);

            // Tests override the timeout control sometimes
            _context.TimeoutControl ??= _timeoutControl;
        }

        private IKestrelTrace Log => _context.ServiceContext.Log;

        public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> httpApplication)
        {
            try
            {
                // Ensure TimeoutControl._lastTimestamp is initialized before anything that could set timeouts runs.
                _timeoutControl.Initialize(_systemClock.UtcNowTicks);

                IRequestProcessor requestProcessor = null;

                switch (SelectProtocol())
                {
                    case HttpProtocols.Http1:
                        // _http1Connection must be initialized before adding the connection to the connection manager
                        requestProcessor = _http1Connection = new Http1Connection<TContext>(_context);
                        _protocolSelectionState = ProtocolSelectionState.Selected;
                        break;
                    case HttpProtocols.Http2:
                        // _http2Connection must be initialized before yielding control to the transport thread,
                        // to prevent a race condition where _http2Connection.Abort() is called just as
                        // _http2Connection is about to be initialized.
                        requestProcessor = new Http2Connection(_context);
                        _protocolSelectionState = ProtocolSelectionState.Selected;
                        break;
                    case HttpProtocols.None:
                        // An error was already logged in SelectProtocol(), but we should close the connection.
                        break;

                    default:
                        // SelectProtocol() only returns Http1, Http2 or None.
                        throw new NotSupportedException($"{nameof(SelectProtocol)} returned something other than Http1, Http2, Http3 or None.");
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
                    using var shutdownRegistration = connectionLifetimeNotificationFeature?.ConnectionClosedRequested.Register(state => ((HttpConnection)state).StopProcessingNextRequest(), this);

                    // Register for connection close
                    using var closedRegistration = _context.ConnectionContext.ConnectionClosed.Register(state => ((HttpConnection)state).OnConnectionClosed(), this);

                    await requestProcessor.ProcessRequestsAsync(httpApplication);
                }
            }
            catch (Exception ex)
            {
                Log.LogCritical(0, ex, $"Unexpected exception in {nameof(HttpConnection)}.{nameof(ProcessRequestsAsync)}.");
            }
            finally
            {
                if (_http1Connection?.IsUpgraded == true)
                {
                    _context.ServiceContext.ConnectionManager.UpgradedConnectionCount.ReleaseOne();
                }
            }
        }

        // For testing only
        internal void Initialize(IRequestProcessor requestProcessor)
        {
            _requestProcessor = requestProcessor;
            _http1Connection = requestProcessor as Http1Connection;
            _protocolSelectionState = ProtocolSelectionState.Selected;
        }

        private void StopProcessingNextRequest()
        {
            ProtocolSelectionState previousState;
            lock (_protocolSelectionLock)
            {
                previousState = _protocolSelectionState;
                Debug.Assert(previousState != ProtocolSelectionState.Initializing, "The state should never be initializing");

                switch (_protocolSelectionState)
                {
                    case ProtocolSelectionState.Selected:
                    case ProtocolSelectionState.Aborted:
                        break;
                }
            }

            switch (previousState)
            {
                case ProtocolSelectionState.Selected:
                    _requestProcessor.StopProcessingNextRequest();
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

                switch (_protocolSelectionState)
                {
                    case ProtocolSelectionState.Selected:
                    case ProtocolSelectionState.Aborted:
                        break;
                }
            }

            switch (previousState)
            {
                case ProtocolSelectionState.Selected:
                    _requestProcessor.OnInputOrOutputCompleted();
                    break;
                case ProtocolSelectionState.Aborted:
                    break;
            }
        }

        private void Abort(ConnectionAbortedException ex)
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
                    _requestProcessor.Abort(ex);
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
            var http1Enabled = (_context.Protocols & HttpProtocols.Http1) == HttpProtocols.Http1;
            var http2Enabled = (_context.Protocols & HttpProtocols.Http2) == HttpProtocols.Http2;

            string error = null;

            if (_context.Protocols == HttpProtocols.None)
            {
                error = CoreStrings.EndPointRequiresAtLeastOneProtocol;
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

            // It's safe to use UtcNowUnsynchronized since Tick is called by the Heartbeat.
            var now = _systemClock.UtcNowUnsynchronized;
            _timeoutControl.Tick(now);
            _requestProcessor.Tick(now);
        }

        public void OnTimeout(TimeoutReason reason)
        {
            // In the cases that don't log directly here, we expect the setter of the timeout to also be the input
            // reader, so when the read is canceled or aborted, the reader should write the appropriate log.
            switch (reason)
            {
                case TimeoutReason.KeepAlive:
                    _requestProcessor.StopProcessingNextRequest();
                    break;
                case TimeoutReason.RequestHeaders:
                    _requestProcessor.HandleRequestHeadersTimeout();
                    break;
                case TimeoutReason.ReadDataRate:
                    _requestProcessor.HandleReadDataRateTimeout();
                    break;
                case TimeoutReason.WriteDataRate:
                    Log.ResponseMinimumDataRateNotSatisfied(_context.ConnectionId, _http1Connection?.TraceIdentifier);
                    Abort(new ConnectionAbortedException(CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied));
                    break;
                case TimeoutReason.RequestBodyDrain:
                case TimeoutReason.TimeoutFeature:
                    Abort(new ConnectionAbortedException(CoreStrings.ConnectionTimedOutByServer));
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
}
