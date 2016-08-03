using System;
using System.Net.WebSockets;
using Microsoft.AspNetCore.WebSockets.Internal;

namespace Microsoft.AspNetCore.WebSockets.Test
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
                clientSocket: WebSocketFactory.CreateClientWebSocket(clientStream, null, TimeSpan.FromMinutes(2), 1024),
                serverSocket: WebSocketFactory.CreateServerWebSocket(serverStream, null, TimeSpan.FromMinutes(2), 1024));
        }
    }
}