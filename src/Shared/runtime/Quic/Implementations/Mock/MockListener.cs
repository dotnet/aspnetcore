// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Net.Sockets;
using System.Net.Security;
using System.Threading.Tasks;
using System.Threading;
using System.Buffers.Binary;

namespace System.Net.Quic.Implementations.Mock
{
    internal sealed class MockListener : QuicListenerProvider
    {
        private bool _disposed = false;
        private SslServerAuthenticationOptions? _sslOptions;
        private IPEndPoint _listenEndPoint;
        private TcpListener _tcpListener;

        internal MockListener(IPEndPoint listenEndPoint, SslServerAuthenticationOptions? sslServerAuthenticationOptions)
        {
            if (listenEndPoint == null)
            {
                throw new ArgumentNullException(nameof(listenEndPoint));
            }

            _sslOptions = sslServerAuthenticationOptions;
            _listenEndPoint = listenEndPoint;

            _tcpListener = new TcpListener(listenEndPoint);
        }

        // IPEndPoint is mutable, so we must create a new instance every time this is retrieved.
        internal override IPEndPoint ListenEndPoint => new IPEndPoint(_listenEndPoint.Address, _listenEndPoint.Port);

        internal override async ValueTask<QuicConnectionProvider> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            Socket socket = await _tcpListener.AcceptSocketAsync().ConfigureAwait(false);
            socket.NoDelay = true;

            // Read first 4 bytes to get client listen port
            byte[] buffer = new byte[4];
            int bytesRead = 0;
            do
            {
                bytesRead += await socket.ReceiveAsync(buffer.AsMemory().Slice(bytesRead), SocketFlags.None).ConfigureAwait(false);
            } while (bytesRead != buffer.Length);

            int peerListenPort = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            IPEndPoint peerListenEndPoint = new IPEndPoint(((IPEndPoint)socket.RemoteEndPoint!).Address, peerListenPort);

            // Listen on a new local endpoint for inbound streams
            TcpListener inboundListener = new TcpListener(_listenEndPoint.Address, 0);
            inboundListener.Start();
            int inboundListenPort = ((IPEndPoint)inboundListener.LocalEndpoint).Port;

            // Write inbound listen port to socket so client can read it
            BinaryPrimitives.WriteInt32LittleEndian(buffer, inboundListenPort);
            await socket.SendAsync(buffer, SocketFlags.None).ConfigureAwait(false);

            return new MockConnection(socket, peerListenEndPoint, inboundListener);
        }

        internal override void Start()
        {
            CheckDisposed();

            _tcpListener.Start();

            if (_listenEndPoint.Port == 0)
            {
                // Get auto-assigned port
                _listenEndPoint = (IPEndPoint)_tcpListener.LocalEndpoint;
            }
        }

        internal override void Close()
        {
            Dispose();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(QuicListener));
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _tcpListener?.Stop();
                    _tcpListener = null!;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposed = true;
            }
        }

        ~MockListener()
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
