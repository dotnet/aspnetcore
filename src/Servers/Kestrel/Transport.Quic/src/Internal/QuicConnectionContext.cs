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
        private long _currentTicks;
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

                QuicStreamContext? context;

                lock (_poolLock)
                {
                    StreamPool.TryPop(out context);
                }
                if (context == null)
                {
                    context = new QuicStreamContext(this, _context);
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

            // TODO - pool connect streams?
            QuicStreamContext? context = new QuicStreamContext(this, _context);
            context.Initialize(quicStream);
            context.Start();

            _log.ConnectedStream(context);

            return new ValueTask<ConnectionContext>(context);
        }

        internal void ReturnStream(QuicStreamContext stream)
        {
            lock (_poolLock)
            {
                if (!_streamPoolHeartbeatInitialized)
                {
                    // Heartbeat feature is added to connection features by Kestrel.
                    var heartbeatFeature = Features.Get<IConnectionHeartbeatFeature>();
                    if (heartbeatFeature != null)
                    {
                        heartbeatFeature.OnHeartbeat(state => ((QuicConnectionContext)state).RemoveExpiredStreams(), this);
                    }

                    var now = _context.Options.SystemClock.UtcNow.Ticks;
                    Volatile.Write(ref _currentTicks, now);

                    _streamPoolHeartbeatInitialized = true;
                }

                stream.PoolExpirationTicks = Volatile.Read(ref _currentTicks) + StreamPoolExpiryTicks;
                StreamPool.Push(stream);
            }
        }

        private void RemoveExpiredStreams()
        {
            lock (_poolLock)
            {
                var now = _context.Options.SystemClock.UtcNow.Ticks;
                Volatile.Write(ref _currentTicks, now);

                StreamPool.RemoveExpired(now);
            }
        }
    }
}
