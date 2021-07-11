// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net.Quic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal class QuicConnectionContext : TransportMultiplexedConnection, IProtocolErrorCodeFeature
    {
        // Internal for testing.
        internal QuicStreamStack StreamPool;

        private bool _streamPoolHeartbeatInitialized;
        // Ticks updated once per-second in heartbeat event.
        private long _heartbeatTicks;
        private readonly object _poolLock = new object();

        private readonly QuicConnection _connection;
        private readonly QuicTransportContext _context;
        private readonly IQuicTrace _log;
        private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();

        private Task? _closeTask;

        public long Error { get; set; }

        internal const int InitialStreamPoolSize = 5;
        internal const int MaxStreamPoolSize = 100;
        internal const long StreamPoolExpiryTicks = TimeSpan.TicksPerSecond * 5;

        public QuicConnectionContext(QuicConnection connection, QuicTransportContext context)
        {
            _log = context.Log;
            _context = context;
            _connection = connection;
            ConnectionClosed = _connectionClosedTokenSource.Token;
            Features.Set<ITlsConnectionFeature>(new FakeTlsConnectionFeature());
            Features.Set<IProtocolErrorCodeFeature>(this);

            StreamPool = new QuicStreamStack(InitialStreamPoolSize);
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                _closeTask ??= _connection.CloseAsync(errorCode: 0).AsTask();
                await _closeTask;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to gracefully shutdown connection.");
            }

            _connection.Dispose();
        }

        public override void Abort() => Abort(new ConnectionAbortedException("The connection was aborted by the application via MultiplexedConnectionContext.Abort()."));

        public override void Abort(ConnectionAbortedException abortReason)
        {
            // dedup calls to abort here.
            _log.ConnectionAbort(this, abortReason.Message);
            _closeTask = _connection.CloseAsync(errorCode: Error).AsTask();
        }

        public override async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var stream = await _connection.AcceptStreamAsync(cancellationToken);

                QuicStreamContext? context = null;

                // Only use pool for bidirectional streams. Just a handful of unidirecitonal
                // streams are created for a connection and they live for the lifetime of the connection.
                if (stream.CanRead && stream.CanWrite)
                {
                    lock (_poolLock)
                    {
                        StreamPool.TryPop(out context);
                    }
                }

                if (context == null)
                {
                    context = new QuicStreamContext(this, _context);
                }
                else
                {
                    context.ResetFeatureCollection();
                    context.ResetItems();
                }

                context.Initialize(stream);
                context.Start();

                _log.AcceptedStream(context);

                return context;
            }
            catch (QuicConnectionAbortedException ex)
            {
                // Shutdown initiated by peer, abortive.
                _log.ConnectionAborted(this, ex);

                ThreadPool.UnsafeQueueUserWorkItem(state =>
                {
                    state.CancelConnectionClosedToken();
                },
                this,
                preferLocal: false);
            }
            catch (QuicOperationAbortedException)
            {
                // Shutdown initiated by us

                // Allow for graceful closure.
            }

            return null;
        }

        private void CancelConnectionClosedToken()
        {
            try
            {
                _connectionClosedTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                _log.LogError(0, ex, $"Unexpected exception in {nameof(QuicConnectionContext)}.{nameof(CancelConnectionClosedToken)}.");
            }
        }

        public override ValueTask<ConnectionContext> ConnectAsync(IFeatureCollection? features = null, CancellationToken cancellationToken = default)
        {
            QuicStream quicStream;

            var streamDirectionFeature = features?.Get<IStreamDirectionFeature>();
            if (streamDirectionFeature != null)
            {
                if (streamDirectionFeature.CanRead)
                {
                    quicStream = _connection.OpenBidirectionalStream();
                }
                else
                {
                    quicStream = _connection.OpenUnidirectionalStream();
                }
            }
            else
            {
                quicStream = _connection.OpenBidirectionalStream();
            }

            // Only a handful of control streams are created by the server and they last for the
            // lifetime of the connection. No value in pooling them.
            QuicStreamContext? context = new QuicStreamContext(this, _context);
            context.Initialize(quicStream);
            context.Start();

            _log.ConnectedStream(context);

            return new ValueTask<ConnectionContext>(context);
        }

        internal bool TryReturnStream(QuicStreamContext stream)
        {
            lock (_poolLock)
            {
                if (!_streamPoolHeartbeatInitialized)
                {
                    // Heartbeat feature is added to connection features by Kestrel.
                    // No event is on the context is raised between feature being added and serving
                    // connections so initialize heartbeat the first time a stream is added to
                    // the connection's stream pool.
                    var heartbeatFeature = Features.Get<IConnectionHeartbeatFeature>();
                    if (heartbeatFeature != null)
                    {
                        heartbeatFeature.OnHeartbeat(static state => ((QuicConnectionContext)state).RemoveExpiredStreams(), this);
                    }

                    // Set ticks for the first time. Ticks are then updated in heartbeat.
                    var now = _context.Options.SystemClock.UtcNow.Ticks;
                    Volatile.Write(ref _heartbeatTicks, now);

                    _streamPoolHeartbeatInitialized = true;
                }

                if (stream.CanReuse && StreamPool.Count < MaxStreamPoolSize)
                {
                    stream.PoolExpirationTicks = Volatile.Read(ref _heartbeatTicks) + StreamPoolExpiryTicks;
                    StreamPool.Push(stream);
                    return true;
                }
            }

            return false;
        }

        private void RemoveExpiredStreams()
        {
            lock (_poolLock)
            {
                // Update ticks on heartbeat. A precise value isn't necessary.
                var now = _context.Options.SystemClock.UtcNow.Ticks;
                Volatile.Write(ref _heartbeatTicks, now);

                StreamPool.RemoveExpired(now);
            }
        }
    }
}
