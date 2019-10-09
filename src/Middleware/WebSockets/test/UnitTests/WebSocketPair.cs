using System;
using System.Net.WebSockets;

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
                clientSocket: WebSocket.CreateFromStream(clientStream, isServer: false, subProtocol: null, keepAliveInterval: TimeSpan.FromMinutes(2)),
                serverSocket: WebSocket.CreateFromStream(serverStream, isServer: true, subProtocol: null, keepAliveInterval: TimeSpan.FromMinutes(2)));
        }
    }
}
