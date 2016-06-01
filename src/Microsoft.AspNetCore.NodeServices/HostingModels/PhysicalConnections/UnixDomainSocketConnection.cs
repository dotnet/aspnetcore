using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.HostingModels.PhysicalConnections
{
    internal class UnixDomainSocketConnection : StreamConnection
    {
        private bool _disposedValue = false;
        private NetworkStream _networkStream;
        private Socket _socket;

#if NET451
        public override Task<Stream> Open(string address)
        {
            // The 'null' assignments avoid the compiler warnings about unassigned fields.
            // Note that this whole class isn't supported on .NET 4.5.1, since that's not cross-platform.
            _networkStream = null;
            _socket = null;
            throw new System.PlatformNotSupportedException();
        }
#else
        public override async Task<Stream> Open(string address)
        {
            var endPoint = new UnixDomainSocketEndPoint("/tmp/" + address);
            _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Unspecified);
            await _socket.ConnectAsync(endPoint).ConfigureAwait(false);
            _networkStream = new NetworkStream(_socket);
            return _networkStream;
        }
#endif

        public override void Dispose()
        {
            if (!_disposedValue)
            {
                if (_networkStream != null)
                {
                    _networkStream.Dispose();
                }

                if (_socket != null)
                {
                    _socket.Dispose();
                }

                _disposedValue = true;
            }
        }
    }
}