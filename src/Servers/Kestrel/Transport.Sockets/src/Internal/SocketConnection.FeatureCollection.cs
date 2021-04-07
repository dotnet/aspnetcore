using System.Net.Sockets;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed partial class SocketConnection : IConnectionSocketFeature
    {
        public Socket Socket => _socket;

        private void InitiaizeFeatures()
        {
            _currentIConnectionSocketFeature = this;
        }
    }
}
