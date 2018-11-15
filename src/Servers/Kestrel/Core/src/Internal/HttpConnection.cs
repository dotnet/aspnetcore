// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class HttpConnection : ITimeoutHandler
    {
        private static readonly ReadOnlyMemory<byte> Http2Id = new[] { (byte)'h', (byte)'2' };

        private readonly HttpConnectionContext _context;
        private readonly ISystemClock _systemClock;
        private readonly TimeoutControl _timeoutControl;

        private IList<IAdaptedConnection> _adaptedConnections;
        private IDuplexPipe _adaptedTransport;

        private readonly object _protocolSelectionLock = new object();
        private ProtocolSelectionState _protocolSelectionState = ProtocolSelectionState.Initializing;
        private IRequestProcessor _requestProcessor;
        private Http1Connection _http1Connection;

        public HttpConnection(HttpConnectionContext context)
        {
            _context = context;
            _systemClock = _context.ServiceContext.SystemClock;

            _timeoutControl = new TimeoutControl(this);
        }

        public string ConnectionId => _context.ConnectionId;
        public IPEndPoint LocalEndPoint => _context.LocalEndPoint;
        public IPEndPoint RemoteEndPoint => _context.RemoteEndPoint;

        private MemoryPool<byte> MemoryPool => _context.MemoryPool;

        // Internal for testing
        internal PipeOptions AdaptedInputPipeOptions => new PipeOptions
        (
            pool: MemoryPool,
            readerScheduler: _context.ServiceContext.Scheduler,
            writerScheduler: PipeScheduler.Inline,
            pauseWriterThreshold: _context.ServiceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
            resumeWriterThreshold: _context.ServiceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
            useSynchronizationContext: false,
            minimumSegmentSize: KestrelMemoryPool.MinimumSegmentSize
        );

        internal PipeOptions AdaptedOutputPipeOptions => new PipeOptions
        (
            pool: MemoryPool,
            readerScheduler: PipeScheduler.Inline,
            writerScheduler: PipeScheduler.Inline,
            pauseWriterThreshold: _context.ServiceContext.ServerOptions.Limits.MaxResponseBufferSize ?? 0,
            resumeWriterThreshold: _context.ServiceContext.ServerOptions.Limits.MaxResponseBufferSize ?? 0,
            useSynchronizationContext: false,
            minimumSegmentSize: KestrelMemoryPool.MinimumSegmentSize
        );

        private IKestrelTrace Log => _context.ServiceContext.Log;

        public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> httpApplication)
        {
            try
            {
                AdaptedPipeline adaptedPipeline = null;
                var adaptedPipelineTask = Task.CompletedTask;

                // _adaptedTransport must be set prior to wiring up callbacks
                // to allow the connection to be aborted prior to protocol selection.
                _adaptedTransport = _context.Transport;

                if (_context.ConnectionAdapters.Count > 0)
                {
                    adaptedPipeline = new AdaptedPipeline(_adaptedTransport,
                                                          new Pipe(AdaptedInputPipeOptions),
                                                          new Pipe(AdaptedOutputPipeOptions),
                                                          Log);

                    _adaptedTransport = adaptedPipeline;
                }

                // This feature should never be null in Kestrel
                var connectionHeartbeatFeature = _context.ConnectionFeatures.Get<IConnectionHeartbeatFeature>();

                Debug.Assert(connectionHeartbeatFeature != null, nameof(IConnectionHeartbeatFeature) + " is missing!");

                connectionHeartbeatFeature?.OnHeartbeat(state => ((HttpConnection)state).Tick(), this);

                var connectionLifetimeNotificationFeature = _context.ConnectionFeatures.Get<IConnectionLifetimeNotificationFeature>();

                Debug.Assert(connectionLifetimeNotificationFeature != null, nameof(IConnectionLifetimeNotificationFeature) + " is missing!");

                using (connectionLifetimeNotificationFeature?.ConnectionClosedRequested.Register(state => ((HttpConnection)state).StopProcessingNextRequest(), this))
                {
                    // Ensure TimeoutControl._lastTimestamp is initialized before anything that could set timeouts runs.
                    _timeoutControl.Initialize(_systemClock.UtcNowTicks);

                    _context.ConnectionFeatures.Set<IConnectionTimeoutFeature>(_timeoutControl);

                    if (adaptedPipeline != null)
                    {
                        // Stream can be null here and run async will close the connection in that case
                        var stream = await ApplyConnectionAdaptersAsync();
                        adaptedPipelineTask = adaptedPipeline.RunAsync(stream);
                    }

                    IRequestProcessor requestProcessor = null;

                    lock (_protocolSelectionLock)
                    {
                        // Ensure that the connection hasn't already been stopped.
                        if (_protocolSelectionState == ProtocolSelectionState.Initializing)
                        {
                            var derivedContext = CreateDerivedContext(_adaptedTransport);

                            switch (SelectProtocol())
                            {
                                case HttpProtocols.Http1:
                                    // _http1Connection must be initialized before adding the connection to the connection manager
                                    requestProcessor = _http1Connection = new Http1Connection(derivedContext);
                                    _protocolSelectionState = ProtocolSelectionState.Selected;
                                    break;
                                case HttpProtocols.Http2:
                                    // _http2Connection must be initialized before yielding control to the transport thread,
                                    // to prevent a race condition where _http2Connection.Abort() is called just as
                                    // _http2Connection is about to be initialized.
                                    requestProcessor = new Http2Connection(derivedContext);
                                    _protocolSelectionState = ProtocolSelectionState.Selected;
                                    break;
                                case HttpProtocols.None:
                                    // An error was already logged in SelectProtocol(), but we should close the connection.
                                    Abort(ex: null);
                                    break;
                                default:
                                    // SelectProtocol() only returns Http1, Http2 or None.
                                    throw new NotSupportedException($"{nameof(SelectProtocol)} returned something other than Http1, Http2 or None.");
                            }

                            _requestProcessor = requestProcessor;
                        }
                    }

                    _context.Transport.Input.OnWriterCompleted(
                        (_, state) => ((HttpConnection)state).OnInputOrOutputCompleted(),
                        this);

                    _context.Transport.Output.OnReaderCompleted(
                        (_, state) => ((HttpConnection)state).OnInputOrOutputCompleted(),
                        this);

                    if (requestProcessor != null)
                    {
                        await requestProcessor.ProcessRequestsAsync(httpApplication);
                    }

                    await adaptedPipelineTask;
                }
            }
            catch (Exception ex)
            {
                Log.LogCritical(0, ex, $"Unexpected exception in {nameof(HttpConnection)}.{nameof(ProcessRequestsAsync)}.");
            }
            finally
            {
                DisposeAdaptedConnections();

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

        private HttpConnectionContext CreateDerivedContext(IDuplexPipe transport)
        {
            return new HttpConnectionContext
            {
                ConnectionId = _context.ConnectionId,
                ConnectionFeatures = _context.ConnectionFeatures,
                MemoryPool = _context.MemoryPool,
                LocalEndPoint = _context.LocalEndPoint,
                RemoteEndPoint = _context.RemoteEndPoint,
                ServiceContext = _context.ServiceContext,
                ConnectionContext = _context.ConnectionContext,
                TimeoutControl = _timeoutControl,
                Transport = transport
            };
        }

        private void StopProcessingNextRequest()
        {
            lock (_protocolSelectionLock)
            {
                switch (_protocolSelectionState)
                {
                    case ProtocolSelectionState.Initializing:
                        CloseUninitializedConnection(abortReason: null);
                        _protocolSelectionState = ProtocolSelectionState.Aborted;
                        break;
                    case ProtocolSelectionState.Selected:
                        _requestProcessor.StopProcessingNextRequest();
                        break;
                    case ProtocolSelectionState.Aborted:
                        break;
                }
            }
        }

        private void OnInputOrOutputCompleted()
        {
            lock (_protocolSelectionLock)
            {
                switch (_protocolSelectionState)
                {
                    case ProtocolSelectionState.Initializing:
                        CloseUninitializedConnection(abortReason: null);
                        _protocolSelectionState = ProtocolSelectionState.Aborted;
                        break;
                    case ProtocolSelectionState.Selected:
                        _requestProcessor.OnInputOrOutputCompleted();
                        break;
                    case ProtocolSelectionState.Aborted:
                        break;
                }

            }
        }

        private void Abort(ConnectionAbortedException ex)
        {
            lock (_protocolSelectionLock)
            {
                switch (_protocolSelectionState)
                {
                    case ProtocolSelectionState.Initializing:
                        CloseUninitializedConnection(ex);
                        break;
                    case ProtocolSelectionState.Selected:
                        _requestProcessor.Abort(ex);
                        break;
                    case ProtocolSelectionState.Aborted:
                        break;
                }

                _protocolSelectionState = ProtocolSelectionState.Aborted;
            }
        }

        private async Task<Stream> ApplyConnectionAdaptersAsync()
        {
            var connectionAdapters = _context.ConnectionAdapters;
            var stream = new RawStream(_context.Transport.Input, _context.Transport.Output);
            var adapterContext = new ConnectionAdapterContext(_context.ConnectionContext, stream);
            _adaptedConnections = new List<IAdaptedConnection>(connectionAdapters.Count);

            try
            {
                for (var i = 0; i < connectionAdapters.Count; i++)
                {
                    var adaptedConnection = await connectionAdapters[i].OnConnectionAsync(adapterContext);
                    _adaptedConnections.Add(adaptedConnection);
                    adapterContext = new ConnectionAdapterContext(_context.ConnectionContext, adaptedConnection.ConnectionStream);
                }
            }
            catch (Exception ex)
            {
                Log.LogError(0, ex, $"Uncaught exception from the {nameof(IConnectionAdapter.OnConnectionAsync)} method of an {nameof(IConnectionAdapter)}.");

                return null;
            }

            return adapterContext.ConnectionStream;
        }

        private void DisposeAdaptedConnections()
        {
            var adaptedConnections = _adaptedConnections;
            if (adaptedConnections != null)
            {
                for (var i = adaptedConnections.Count - 1; i >= 0; i--)
                {
                    adaptedConnections[i].Dispose();
                }
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

            if (!http1Enabled && http2Enabled && hasTls && !Http2Id.Span.SequenceEqual(applicationProtocol.Span))
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

            return http2Enabled && (!hasTls || Http2Id.Span.SequenceEqual(applicationProtocol.Span)) ? HttpProtocols.Http2 : HttpProtocols.Http1;
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
            _requestProcessor?.Tick(now);
        }

        private void CloseUninitializedConnection(ConnectionAbortedException abortReason)
        {
            Debug.Assert(_adaptedTransport != null);

            _context.ConnectionContext.Abort(abortReason);

            _adaptedTransport.Input.Complete();
            _adaptedTransport.Output.Complete();
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
