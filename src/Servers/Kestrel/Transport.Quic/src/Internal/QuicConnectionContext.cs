// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Quic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions.Features;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal class QuicConnectionContext : TransportConnection, IQuicStreamListenerFeature, IQuicCreateStreamFeature
    {
        private bool _disposed;
        private QuicConnection _connection;
        private readonly QuicTransportContext _context;
        private readonly IQuicTrace _log;

        public QuicConnectionContext(QuicConnection connection, QuicTransportContext context)
        {
            _log = context.Log;
            _context = context;
            _connection = connection;
            Features.Set<ITlsConnectionFeature>(new FakeTlsConnectionFeature());
            Features.Set<IQuicStreamListenerFeature>(this);
            Features.Set<IQuicCreateStreamFeature>(this);

            _log.NewConnection(ConnectionId);
        }

        public ValueTask<ConnectionContext> StartUnidirectionalStreamAsync()
        {
            var stream = _connection.OpenUnidirectionalStream();
            return new ValueTask<ConnectionContext>(new QuicStreamContext(stream, this, _context));
        }

        public ValueTask<ConnectionContext> StartBidirectionalStreamAsync()
        {
            var stream = _connection.OpenBidirectionalStream();
            return new ValueTask<ConnectionContext>(new QuicStreamContext(stream, this, _context));
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            await _connection.CloseAsync(errorCode: 0);

            _connection.Dispose();
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            // TODO add Abort to QuicConnection.
            _connection.Dispose();
        }

        public async ValueTask<ConnectionContext> AcceptAsync()
        {
            var stream = await _connection.AcceptStreamAsync();
            return new QuicStreamContext(stream, this, _context);
        }
    }
}
