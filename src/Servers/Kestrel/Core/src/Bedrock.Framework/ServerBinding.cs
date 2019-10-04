using System.Net;
using Microsoft.AspNetCore.Connections;

namespace Bedrock.Framework
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
        public EndPoint EndPoint { get; internal set; }
        public IConnectionListenerFactory ConnectionListenerFactory { get; }
        public ConnectionDelegate Application { get; }
    }
}
