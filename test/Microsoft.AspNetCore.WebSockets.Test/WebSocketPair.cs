using System;
using System.Net.WebSockets;
using Microsoft.AspNetCore.WebSockets.Internal;

namespace Microsoft.AspNetCore.WebSockets.Test
{
    internal class WebSocketPair
    {
        public WebSocket ClientSocket { get; }
        public WebSocket ServerSocket { get; }
        public DuplexStream ServerStream { get; }
        public DuplexStream ClientStream { get; }

        public WebSocketPair(DuplexStream serverStream, DuplexStream clientStream, WebSocket clientSocket, WebSocket serverSocket)
        {
            ClientStream = clientStream;
            ServerStream = serverStream;
            ClientSocket = clientSocket;
            ServerSocket = serverSocket;
        }

        public static WebSocketPair Create()
        {
            // Create streams
            var serverStream = new DuplexStream();
            var clientStream = serverStream.CreateReverseDuplexStream();

            return new WebSocketPair(
                serverStream,
                clientStream,
                clientSocket: WebSocketFactory.CreateClientWebSocket(clientStream, null, TimeSpan.FromMinutes(2), 1024),
                serverSocket: WebSocketFactory.CreateServerWebSocket(serverStream, null, TimeSpan.FromMinutes(2), 1024));
        }
    }
}