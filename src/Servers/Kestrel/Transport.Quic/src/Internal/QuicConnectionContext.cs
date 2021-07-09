// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        private readonly QuicConnection _connection;
        private readonly QuicTransportContext _context;
        private readonly IQuicTrace _log;
        private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();

        private Task? _closeTask;

        public long Error { get; set; }

        public QuicConnectionContext(QuicConnection connection, QuicTransportContext context)
        {
            _log = context.Log;
            _context = context;
            _connection = connection;
            ConnectionClosed = _connectionClosedTokenSource.Token;
            Features.Set<ITlsConnectionFeature>(new FakeTlsConnectionFeature());
            Features.Set<IProtocolErrorCodeFeature>(this);
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
                var context = new QuicStreamContext(stream, this, _context);
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

            var context = new QuicStreamContext(quicStream, this, _context);
            context.Start();

            _log.ConnectedStream(context);

            return new ValueTask<ConnectionContext>(context);
        }
    }
}
