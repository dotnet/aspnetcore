using System;
using System.Net.WebSockets;

namespace Microsoft.AspNetCore.WebSockets.Protocol.Test
{
    internal class WebSocketPair
    {
        public WebSocket ClientSocket { get; }
        public WebSocket ServerSocket { get; }

        public WebSocketPair(WebSocket clientSocket, WebSocket serverSocket)
        {
            ClientSocket = clientSocket;
            ServerSocket = serverSocket;
        }

        public static WebSocketPair Create()
        {
            // Create streams
            var serverStream = new DuplexStream();
            var clientStream = serverStream.CreateReverseDuplexStream();

            return new WebSocketPair(
                clientSocket: CommonWebSocket.CreateClientWebSocket(clientStream, null, TimeSpan.FromMinutes(2), 1024),
                serverSocket: CommonWebSocket.CreateServerWebSocket(serverStream, null, TimeSpan.FromMinutes(2), 1024));
        }
    }
}