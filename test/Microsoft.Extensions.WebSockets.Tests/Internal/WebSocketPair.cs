using System;
using Channels;

namespace Microsoft.Extensions.WebSockets.Test
{
    internal class WebSocketPair : IDisposable
    {
        private ChannelFactory _factory;

        private Channel _serverToClient;
        private Channel _clientToServer;

        public IWebSocketConnection ClientSocket { get; }
        public IWebSocketConnection ServerSocket { get; }

        public WebSocketPair(ChannelFactory factory, Channel serverToClient, Channel clientToServer, IWebSocketConnection clientSocket, IWebSocketConnection serverSocket)
        {
            _factory = factory;
            _serverToClient = serverToClient;
            _clientToServer = clientToServer;
            ClientSocket = clientSocket;
            ServerSocket = serverSocket;
        }

        public static WebSocketPair Create()
        {
            // Create channels
            var factory = new ChannelFactory();
            var serverToClient = factory.CreateChannel();
            var clientToServer = factory.CreateChannel();

            var serverSocket = new WebSocketConnection(clientToServer, serverToClient, masked: true);
            var clientSocket = new WebSocketConnection(serverToClient, clientToServer, masked: false);

            return new WebSocketPair(factory, serverToClient, clientToServer, clientSocket, serverSocket);
        }

        public void Dispose()
        {
            _factory.Dispose();
            ServerSocket.Dispose();
            ClientSocket.Dispose();
        }

        public void TerminateFromClient(Exception ex = null)
        {
            _clientToServer.CompleteWriter(ex);
        }
    }
}