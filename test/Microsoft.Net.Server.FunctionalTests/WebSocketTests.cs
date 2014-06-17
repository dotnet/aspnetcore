// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Net.Server
{
    public class WebSocketTests
    {
        private const string Address = "http://localhost:8080/";
        private const string WsAddress = "ws://localhost:8080/";

        [Fact]
        public async Task WebSocketAccept_AfterHeadersSent_Throws()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> clientTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                byte[] body = Encoding.UTF8.GetBytes("Hello World");
                context.Response.Body.Write(body, 0, body.Length);

                Assert.ThrowsAsync<InvalidOperationException>(async () => await context.AcceptWebSocketAsync());
                context.Dispose();
                HttpResponseMessage response = await clientTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task WebSocketAccept_Success()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<WebSocket> clientTask = SendWebSocketRequestAsync(WsAddress);

                var context = await server.GetContextAsync();
                Assert.True(context.IsUpgradableRequest);
                WebSocket serverWebSocket = await context.AcceptWebSocketAsync();
                WebSocket clientWebSocket = await clientTask;
                serverWebSocket.Dispose();
                clientWebSocket.Dispose();
            }
        }

        [Fact]
        public async Task WebSocketAccept_SendAndReceive_Success()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<WebSocket> clientTask = SendWebSocketRequestAsync(WsAddress);

                var context = await server.GetContextAsync();
                Assert.True(context.IsWebSocketRequest);
                WebSocket serverWebSocket = await context.AcceptWebSocketAsync();
                WebSocket clientWebSocket = await clientTask;

                byte[] clientBuffer = new byte[] { 0x00, 0x01, 0xFF, 0x00, 0x00 };
                await clientWebSocket.SendAsync(new ArraySegment<byte>(clientBuffer, 0, 3), WebSocketMessageType.Binary, true, CancellationToken.None);

                byte[] serverBuffer = new byte[clientBuffer.Length];
                var result = await serverWebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer, 0, serverBuffer.Length), CancellationToken.None);
                Assert.Equal(clientBuffer, serverBuffer);

                await serverWebSocket.SendAsync(new ArraySegment<byte>(serverBuffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                byte[] clientEchoBuffer = new byte[clientBuffer.Length];
                result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(clientEchoBuffer), CancellationToken.None);
                Assert.Equal(clientBuffer, clientEchoBuffer);

                serverWebSocket.Dispose();
                clientWebSocket.Dispose();
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }

        private async Task<WebSocket> SendWebSocketRequestAsync(string address)
        {
            ClientWebSocket client = new ClientWebSocket();
            await client.ConnectAsync(new Uri(address), CancellationToken.None);
            return client;
        }
    }
}