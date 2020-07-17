// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Buffers.Binary;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Mock
{
    internal sealed class MockConnection : QuicConnectionProvider
    {
        private readonly bool _isClient;
        private bool _disposed;
        private IPEndPoint? _remoteEndPoint;
        private IPEndPoint? _localEndPoint;
        private object _syncObject = new object();
        private Socket? _socket;
        private IPEndPoint? _peerListenEndPoint;
        private TcpListener? _inboundListener;
        private long _nextOutboundBidirectionalStream;
        private long _nextOutboundUnidirectionalStream;

        // Constructor for outbound connections
        internal MockConnection(IPEndPoint? remoteEndPoint, SslClientAuthenticationOptions? sslClientAuthenticationOptions, IPEndPoint? localEndPoint = null)
        {
            _remoteEndPoint = remoteEndPoint;
            _localEndPoint = localEndPoint;

            _isClient = true;
            _nextOutboundBidirectionalStream = 0;
            _nextOutboundUnidirectionalStream = 2;
        }

        // Constructor for accepted inbound connections
        internal MockConnection(Socket socket, IPEndPoint peerListenEndPoint, TcpListener inboundListener)
        {
            _isClient = false;
            _nextOutboundBidirectionalStream = 1;
            _nextOutboundUnidirectionalStream = 3;
            _socket = socket;
            _peerListenEndPoint = peerListenEndPoint;
            _inboundListener = inboundListener;
            _localEndPoint = (IPEndPoint?)socket.LocalEndPoint;
            _remoteEndPoint = (IPEndPoint?)socket.RemoteEndPoint;
        }

        internal override bool Connected
        {
            get
            {
                CheckDisposed();

                return _socket != null;
            }
        }

        internal override IPEndPoint LocalEndPoint => new IPEndPoint(_localEndPoint!.Address, _localEndPoint.Port);

        internal override IPEndPoint RemoteEndPoint => new IPEndPoint(_remoteEndPoint!.Address, _remoteEndPoint.Port);

        internal override SslApplicationProtocol NegotiatedApplicationProtocol => throw new NotImplementedException();

        internal override async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (Connected)
            {
                // TODO: Exception text
                throw new InvalidOperationException("Already connected");
            }

            Socket socket = new Socket(_remoteEndPoint!.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(_remoteEndPoint).ConfigureAwait(false);
            socket.NoDelay = true;

            _localEndPoint = (IPEndPoint?)socket.LocalEndPoint;

            // Listen on a new local endpoint for inbound streams
            TcpListener inboundListener = new TcpListener(_localEndPoint!.Address, 0);
            inboundListener.Start();
            int inboundListenPort = ((IPEndPoint)inboundListener.LocalEndpoint).Port;

            // Write inbound listen port to socket so server can read it
            byte[] buffer = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, inboundListenPort);
            await socket.SendAsync(buffer, SocketFlags.None).ConfigureAwait(false);

            // Read first 4 bytes to get server listen port
            int bytesRead = 0;
            do
            {
                bytesRead += await socket.ReceiveAsync(buffer.AsMemory().Slice(bytesRead), SocketFlags.None, cancellationToken).ConfigureAwait(false);
            } while (bytesRead != buffer.Length);

            int peerListenPort = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            IPEndPoint peerListenEndPoint = new IPEndPoint(((IPEndPoint)socket.RemoteEndPoint!).Address, peerListenPort);

            _socket = socket;
            _peerListenEndPoint = peerListenEndPoint;
            _inboundListener = inboundListener;
        }

        internal override QuicStreamProvider OpenUnidirectionalStream()
        {
            long streamId;
            lock (_syncObject)
            {
                streamId = _nextOutboundUnidirectionalStream;
                _nextOutboundUnidirectionalStream += 4;
            }

            return new MockStream(this, streamId, bidirectional: false);
        }

        internal override QuicStreamProvider OpenBidirectionalStream()
        {
            long streamId;
            lock (_syncObject)
            {
                streamId = _nextOutboundBidirectionalStream;
                _nextOutboundBidirectionalStream += 4;
            }

            return new MockStream(this, streamId, bidirectional: true);
        }

        internal override long GetRemoteAvailableUnidirectionalStreamCount()
        {
            throw new NotImplementedException();
        }

        internal override long GetRemoteAvailableBidirectionalStreamCount()
        {
            throw new NotImplementedException();
        }

        internal async Task<Socket> CreateOutboundMockStreamAsync(long streamId)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(_peerListenEndPoint!).ConfigureAwait(false);
            socket.NoDelay = true;

            // Write stream ID to socket so server can read it
            byte[] buffer = new byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, streamId);
            await socket.SendAsync(buffer, SocketFlags.None).ConfigureAwait(false);

            return socket;
        }

        internal override async ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            Socket socket = await _inboundListener!.AcceptSocketAsync().ConfigureAwait(false);

            // Read first bytes to get stream ID
            byte[] buffer = new byte[8];
            int bytesRead = 0;
            do
            {
                bytesRead += await socket.ReceiveAsync(buffer.AsMemory().Slice(bytesRead), SocketFlags.None, cancellationToken).ConfigureAwait(false);
            } while (bytesRead != buffer.Length);

            long streamId = BinaryPrimitives.ReadInt64LittleEndian(buffer);

            bool clientInitiated = ((streamId & 0b01) == 0);
            if (clientInitiated == _isClient)
            {
                throw new Exception($"Wrong initiator on accepted stream??? streamId={streamId}, _isClient={_isClient}");
            }

            bool bidirectional = ((streamId & 0b10) == 0);
            return new MockStream(socket, streamId, bidirectional: bidirectional);
        }

        internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
        {
            Dispose();
            return default;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(QuicConnection));
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _socket?.Dispose();
                    _socket = null;

                    _inboundListener?.Stop();
                    _inboundListener = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposed = true;
            }
        }

        ~MockConnection()
        {
            Dispose(false);
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
