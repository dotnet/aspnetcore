using System.Net;

namespace Microsoft.AspNetCore.Connections
{
    public class ServerBinding
    {
        public ServerBinding(EndPoint endPoint, ConnectionDelegate application, IConnectionListenerFactory connectionListenerFactory)
        {
            EndPoint = endPoint;
            Application = application;
            ConnectionListenerFactory = connectionListenerFactory;
        }

        // Mutable because it can change after binding
        public EndPoint EndPoint { get; set; }
        public IConnectionListenerFactory ConnectionListenerFactory { get; }
        public ConnectionDelegate Application { get; }
    }
}
