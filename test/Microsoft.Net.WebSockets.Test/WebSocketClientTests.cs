using Microsoft.Net.WebSockets.Client;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Net.WebSockets.Test
{
    public class WebSocketClientTests
    {
        private static string ClientAddress = "ws://localhost:8080/";
        private static string ServerAddress = "http://localhost:8080/";

        [Fact]
        public async Task Connect_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;
                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task NegotiateSubProtocol_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                client.SubProtocols.Add("alpha");
                client.SubProtocols.Add("bravo");
                client.SubProtocols.Add("charlie");
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                Assert.Equal("alpha, bravo, charlie", serverContext.Request.Headers["Sec-WebSocket-Protocol"]);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync("Bravo");

                WebSocket clientSocket = await clientConnect;
                Assert.Equal("Bravo", clientSocket.SubProtocol);
                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task SendShortData_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;

                byte[] orriginalData = Encoding.UTF8.GetBytes("Hello World");
                await clientSocket.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);

                byte[] serverBuffer = new byte[orriginalData.Length];
                WebSocketReceiveResult result = await serverWebSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(orriginalData.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                Assert.Equal(orriginalData, serverBuffer);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task SendMediumData_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;

                byte[] orriginalData = Encoding.UTF8.GetBytes(new string('a', 130));
                await clientSocket.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Text, true, CancellationToken.None);

                byte[] serverBuffer = new byte[orriginalData.Length];
                WebSocketReceiveResult result = await serverWebSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(orriginalData.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);
                Assert.Equal(orriginalData, serverBuffer);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task SendLongData_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null, 0xFFFF, TimeSpan.FromMinutes(100));

                WebSocket clientSocket = await clientConnect;

                byte[] orriginalData = Encoding.UTF8.GetBytes(new string('a', 0x1FFFF));
                await clientSocket.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Text, true, CancellationToken.None);

                byte[] serverBuffer = new byte[orriginalData.Length];
                WebSocketReceiveResult result = await serverWebSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                int intermediateCount = result.Count;
                Assert.False(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                result = await serverWebSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer, intermediateCount, orriginalData.Length - intermediateCount), CancellationToken.None);
                intermediateCount += result.Count;
                Assert.False(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                result = await serverWebSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer, intermediateCount, orriginalData.Length - intermediateCount), CancellationToken.None);
                intermediateCount += result.Count;
                Assert.True(result.EndOfMessage);
                Assert.Equal(orriginalData.Length, intermediateCount);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(orriginalData, serverBuffer);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task SendFragmentedData_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);
                WebSocket serverSocket = serverWebSocketContext.WebSocket;

                WebSocket clientSocket = await clientConnect;

                byte[] orriginalData = Encoding.UTF8.GetBytes("Hello World");
                await clientSocket.SendAsync(new ArraySegment<byte>(orriginalData, 0, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
                await clientSocket.SendAsync(new ArraySegment<byte>(orriginalData, 2, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
                await clientSocket.SendAsync(new ArraySegment<byte>(orriginalData, 4, 7), WebSocketMessageType.Binary, true, CancellationToken.None);

                byte[] serverBuffer = new byte[orriginalData.Length];
                WebSocketReceiveResult result = await serverSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.False(result.EndOfMessage);
                Assert.Equal(2, result.Count);
                int totalReceived = result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                result = await serverSocket.ReceiveAsync(
                    new ArraySegment<byte>(serverBuffer, totalReceived, serverBuffer.Length - totalReceived), CancellationToken.None);
                Assert.False(result.EndOfMessage);
                Assert.Equal(2, result.Count);
                totalReceived += result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                result = await serverSocket.ReceiveAsync(
                    new ArraySegment<byte>(serverBuffer, totalReceived, serverBuffer.Length - totalReceived), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(7, result.Count);
                totalReceived += result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                Assert.Equal(orriginalData, serverBuffer);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task ReceiveShortData_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;

                byte[] orriginalData = Encoding.UTF8.GetBytes("Hello World");
                await serverWebSocketContext.WebSocket.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);

                byte[] clientBuffer = new byte[orriginalData.Length];
                WebSocketReceiveResult result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(orriginalData.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                Assert.Equal(orriginalData, clientBuffer);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task ReceiveMediumData_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;

                byte[] orriginalData = Encoding.UTF8.GetBytes(new string('a', 130));
                await serverWebSocketContext.WebSocket.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);

                byte[] clientBuffer = new byte[orriginalData.Length];
                WebSocketReceiveResult result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(orriginalData.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                Assert.Equal(orriginalData, clientBuffer);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task ReceiveLongDataInSmallBuffer_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;

                byte[] orriginalData = Encoding.UTF8.GetBytes(new string('a', 0x1FFFF));
                await serverWebSocketContext.WebSocket.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);

                byte[] clientBuffer = new byte[orriginalData.Length];
                WebSocketReceiveResult result;
                int receivedCount = 0;
                do
                {
                    result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(clientBuffer, receivedCount, clientBuffer.Length - receivedCount), CancellationToken.None);
                    receivedCount += result.Count;
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                }
                while (!result.EndOfMessage);

                Assert.Equal(orriginalData.Length, receivedCount);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                Assert.Equal(orriginalData, clientBuffer);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task ReceiveLongDataInLargeBuffer_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient() { ReceiveBufferSize = 0xFFFFFF };
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;

                byte[] orriginalData = Encoding.UTF8.GetBytes(new string('a', 0x1FFFF));
                await serverWebSocketContext.WebSocket.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);

                byte[] clientBuffer = new byte[orriginalData.Length];
                WebSocketReceiveResult result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(orriginalData.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                Assert.Equal(orriginalData, clientBuffer);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task ReceiveFragmentedData_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);
                WebSocket serverSocket = serverWebSocketContext.WebSocket;

                WebSocket clientSocket = await clientConnect;

                byte[] orriginalData = Encoding.UTF8.GetBytes("Hello World");
                await serverSocket.SendAsync(new ArraySegment<byte>(orriginalData, 0, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
                await serverSocket.SendAsync(new ArraySegment<byte>(orriginalData, 2, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
                await serverSocket.SendAsync(new ArraySegment<byte>(orriginalData, 4, 7), WebSocketMessageType.Binary, true, CancellationToken.None);

                byte[] serverBuffer = new byte[orriginalData.Length];
                WebSocketReceiveResult result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.False(result.EndOfMessage);
                Assert.Equal(2, result.Count);
                int totalReceived = result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                result = await clientSocket.ReceiveAsync(
                    new ArraySegment<byte>(serverBuffer, totalReceived, serverBuffer.Length - totalReceived), CancellationToken.None);
                Assert.False(result.EndOfMessage);
                Assert.Equal(2, result.Count);
                totalReceived += result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                result = await clientSocket.ReceiveAsync(
                    new ArraySegment<byte>(serverBuffer, totalReceived, serverBuffer.Length - totalReceived), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(7, result.Count);
                totalReceived += result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                Assert.Equal(orriginalData, serverBuffer);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task SendClose_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;

                string closeDescription = "Test Closed";
                await clientSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);

                byte[] serverBuffer = new byte[1024];
                WebSocketReceiveResult result = await serverWebSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                Assert.Equal(closeDescription, result.CloseStatusDescription);

                Assert.Equal(WebSocketState.CloseSent, clientSocket.State);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task ReceiveClose_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;

                string closeDescription = "Test Closed";
                await serverWebSocketContext.WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);

                byte[] serverBuffer = new byte[1024];
                WebSocketReceiveResult result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                Assert.Equal(closeDescription, result.CloseStatusDescription);

                Assert.Equal(WebSocketState.CloseReceived, clientSocket.State);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task CloseFromOpen_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;

                string closeDescription = "Test Closed";
                Task closeTask = clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);

                byte[] serverBuffer = new byte[1024];
                WebSocketReceiveResult result = await serverWebSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                Assert.Equal(closeDescription, result.CloseStatusDescription);

                await serverWebSocketContext.WebSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

                await closeTask;

                Assert.Equal(WebSocketState.Closed, clientSocket.State);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task CloseFromCloseSent_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;

                string closeDescription = "Test Closed";
                await clientSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);
                Assert.Equal(WebSocketState.CloseSent, clientSocket.State);

                byte[] serverBuffer = new byte[1024];
                WebSocketReceiveResult result = await serverWebSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                Assert.Equal(closeDescription, result.CloseStatusDescription);

                await serverWebSocketContext.WebSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

                await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);

                Assert.Equal(WebSocketState.Closed, clientSocket.State);

                clientSocket.Dispose();
            }
        }

        [Fact]
        public async Task CloseFromCloseReceived_Success()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(ServerAddress);
                listener.Start();
                Task<HttpListenerContext> serverAccept = listener.GetContextAsync();

                WebSocketClient client = new WebSocketClient();
                Task<WebSocket> clientConnect = client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);

                HttpListenerContext serverContext = await serverAccept;
                Assert.True(serverContext.Request.IsWebSocketRequest);
                HttpListenerWebSocketContext serverWebSocketContext = await serverContext.AcceptWebSocketAsync(null);

                WebSocket clientSocket = await clientConnect;

                string closeDescription = "Test Closed";
                await serverWebSocketContext.WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);

                byte[] serverBuffer = new byte[1024];
                WebSocketReceiveResult result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                Assert.Equal(closeDescription, result.CloseStatusDescription);

                Assert.Equal(WebSocketState.CloseReceived, clientSocket.State);

                await clientSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

                Assert.Equal(WebSocketState.Closed, clientSocket.State);

                clientSocket.Dispose();
            }
        }
    }
}
