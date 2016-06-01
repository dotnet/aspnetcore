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

        public override async Task<Stream> Open(string address)
        {
            var endPoint = new UnixDomainSocketEndPoint("/tmp/" + address);
            _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Unspecified);
            await _socket.ConnectAsync(endPoint).ConfigureAwait(false);
            _networkStream = new NetworkStream(_socket);
            return _networkStream;
        }

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