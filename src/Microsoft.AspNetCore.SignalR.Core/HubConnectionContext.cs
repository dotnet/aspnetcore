// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR.Core;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubConnectionContext
    {
        private static Action<object> _abortedCallback = AbortConnection;
        private static readonly Base64Encoder Base64Encoder = new Base64Encoder();
        private static readonly PassThroughEncoder PassThroughEncoder = new PassThroughEncoder();

        private readonly ConnectionContext _connectionContext;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _connectionAbortedTokenSource = new CancellationTokenSource();
        private readonly TaskCompletionSource<object> _abortCompletedTcs = new TaskCompletionSource<object>();
        private readonly long _keepAliveDuration;

        private Task _writingTask = Task.CompletedTask;
        private long _lastSendTimestamp = Stopwatch.GetTimestamp();
        private byte[] _pingMessage;

        public HubConnectionContext(ConnectionContext connectionContext, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory)
        {
            Output = Channel.CreateUnbounded<HubMessage>();
            _connectionContext = connectionContext;
            _logger = loggerFactory.CreateLogger<HubConnectionContext>();
            ConnectionAbortedToken = _connectionAbortedTokenSource.Token;
            _keepAliveDuration = (int)keepAliveInterval.TotalMilliseconds * (Stopwatch.Frequency / 1000);
        }

        public virtual CancellationToken ConnectionAbortedToken { get; }

        public virtual string ConnectionId => _connectionContext.ConnectionId;

        public virtual ClaimsPrincipal User => Features.Get<IConnectionUserFeature>()?.User;

        public virtual IFeatureCollection Features => _connectionContext.Features;

        public virtual IDictionary<object, object> Metadata => _connectionContext.Metadata;

        public virtual HubProtocolReaderWriter ProtocolReaderWriter { get; set; }

        public virtual ChannelReader<byte[]> Input => _connectionContext.Transport.Reader;

        public string UserIdentifier { get; private set; }

        internal virtual Channel<HubMessage> Output { get; set; }

        internal ExceptionDispatchInfo AbortException { get; private set; }

        // Currently used only for streaming methods
        internal ConcurrentDictionary<string, CancellationTokenSource> ActiveRequestCancellationSources { get; } = new ConcurrentDictionary<string, CancellationTokenSource>();

        public IPAddress RemoteIpAddress => Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress;

        public IPAddress LocalIpAddress => Features.Get<IHttpConnectionFeature>()?.LocalIpAddress;

        public int? RemotePort => Features.Get<IHttpConnectionFeature>()?.RemotePort;

        public int? LocalPort => Features.Get<IHttpConnectionFeature>()?.LocalPort;

        public async Task WriteAsync(HubInvocationMessage message)
        {
            while (await Output.Writer.WaitToWriteAsync())
            {
                if (Output.Writer.TryWrite(message))
                {
                    return;
                }
            }
        }

        public async Task DisposeAsync()
        {
            // Nothing should be writing to the HubConnectionContext
            Output.Writer.TryComplete();

            // This should unwind once we complete the output
            await _writingTask;
        }

        public virtual void Abort()
        {
            // If we already triggered the token then noop, this isn't thread safe but it's good enough
            // to avoid spawning a new task in the most common cases
            if (_connectionAbortedTokenSource.IsCancellationRequested)
            {
                return;
            }

            // We fire and forget since this can trigger user code to run
            Task.Factory.StartNew(_abortedCallback, this);
        }

        // Hubs support multiple producers so we set up this loop to copy
        // data written to the HubConnectionContext's channel to the transport channel
        internal Task StartAsync()
        {
            return _writingTask = StartAsyncCore();
        }

        internal async Task<bool> NegotiateAsync(TimeSpan timeout, IHubProtocolResolver protocolResolver, IUserIdProvider userIdProvider)
        {
            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(timeout);
                    while (await _connectionContext.Transport.Reader.WaitToReadAsync(cts.Token))
                    {
                        while (_connectionContext.Transport.Reader.TryRead(out var buffer))
                        {
                            if (NegotiationProtocol.TryParseMessage(buffer, out var negotiationMessage))
                            {
                                var protocol = protocolResolver.GetProtocol(negotiationMessage.Protocol, this);

                                var transportCapabilities = Features.Get<IConnectionTransportFeature>()?.TransportCapabilities
                                    ?? throw new InvalidOperationException("Unable to read transport capabilities.");

                                var dataEncoder = (protocol.Type == ProtocolType.Binary && (transportCapabilities & TransferMode.Binary) == 0)
                                    ? (IDataEncoder)Base64Encoder
                                    : PassThroughEncoder;

                                var transferModeFeature = Features.Get<ITransferModeFeature>() ??
                                    throw new InvalidOperationException("Unable to read transfer mode.");

                                transferModeFeature.TransferMode =
                                    (protocol.Type == ProtocolType.Binary && (transportCapabilities & TransferMode.Binary) != 0)
                                        ? TransferMode.Binary
                                        : TransferMode.Text;

                                ProtocolReaderWriter = new HubProtocolReaderWriter(protocol, dataEncoder);

                                _logger.UsingHubProtocol(protocol.Name);

                                UserIdentifier = userIdProvider.GetUserId(this);

                                return true;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.NegotiateCanceled();
            }

            return false;
        }

        internal void Abort(Exception exception)
        {
            AbortException = ExceptionDispatchInfo.Capture(exception);
            Abort();
        }

        // Used by the HubEndPoint only
        internal Task AbortAsync()
        {
            Abort();
            return _abortCompletedTcs.Task;
        }

        private async Task StartAsyncCore()
        {
            if (Features.Get<IConnectionInherentKeepAliveFeature>() == null)
            {
                Debug.Assert(ProtocolReaderWriter != null, "Expected the ProtocolReaderWriter to be set before StartAsync is called");
                _pingMessage = ProtocolReaderWriter.WriteMessage(PingMessage.Instance);
                _connectionContext.Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(state => ((HubConnectionContext)state).KeepAliveTick(), this);
            }

            try
            {
                while (await Output.Reader.WaitToReadAsync())
                {
                    while (Output.Reader.TryRead(out var hubMessage))
                    {
                        var buffer = ProtocolReaderWriter.WriteMessage(hubMessage);
                        while (await _connectionContext.Transport.Writer.WaitToWriteAsync())
                        {
                            if (_connectionContext.Transport.Writer.TryWrite(buffer))
                            {
                                Interlocked.Exchange(ref _lastSendTimestamp, Stopwatch.GetTimestamp());
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Abort(ex);
            }
        }

        private void KeepAliveTick()
        {
            // Implements the keep-alive tick behavior
            // Each tick, we check if the time since the last send is larger than the keep alive duration (in ticks).
            // If it is, we send a ping frame, if not, we no-op on this tick. This means that in the worst case, the
            // true "ping rate" of the server could be (_hubOptions.KeepAliveInterval + HubEndPoint.KeepAliveTimerInterval),
            // because if the interval elapses right after the last tick of this timer, it won't be detected until the next tick.
            Debug.Assert(_pingMessage != null, "Expected the ping message to be prepared before the first heartbeat tick");

            if (Stopwatch.GetTimestamp() - Interlocked.Read(ref _lastSendTimestamp) > _keepAliveDuration)
            {
                // Haven't sent a message for the entire keep-alive duration, so send a ping.
                // If the transport channel is full, this will fail, but that's OK because
                // adding a Ping message when the transport is full is unnecessary since the
                // transport is still in the process of sending frames.
                if (_connectionContext.Transport.Writer.TryWrite(_pingMessage))
                {
                    _logger.SentPing();
                }
                else
                {
                    // This isn't necessarily an error, it just indicates that the transport is applying backpressure right now.
                    _logger.TransportBufferFull();
                }

                Interlocked.Exchange(ref _lastSendTimestamp, Stopwatch.GetTimestamp());
            }
        }

        private static void AbortConnection(object state)
        {
            var connection = (HubConnectionContext)state;
            try
            {
                connection._connectionAbortedTokenSource.Cancel();

                // Communicate the fact that we're finished triggering abort callbacks
                connection._abortCompletedTcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                // TODO: Should we log if the cancellation callback fails? This is more preventative to make sure
                // we don't end up with an unobserved task
                connection._abortCompletedTcs.TrySetException(ex);
            }
        }
    }
}
