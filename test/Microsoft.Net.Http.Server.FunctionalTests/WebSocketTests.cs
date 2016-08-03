// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.Net.Http.Server
{
    public class WebSocketTests
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        public async Task WebSocketAccept_AfterHeadersSent_Throws()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> clientTask = SendRequestAsync(address);

                var context = await server.AcceptAsync();
                byte[] body = Encoding.UTF8.GetBytes("Hello World");
                context.Response.Body.Write(body, 0, body.Length);

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await context.AcceptWebSocketAsync());
                context.Dispose();
                HttpResponseMessage response = await clientTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        public async Task WebSocketAccept_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<WebSocket> clientTask = SendWebSocketRequestAsync(ConvertToWebSocketAddress(address));

                var context = await server.AcceptAsync();
                Assert.True(context.IsUpgradableRequest);
                WebSocket serverWebSocket = await context.AcceptWebSocketAsync();
                WebSocket clientWebSocket = await clientTask;
                serverWebSocket.Dispose();
                clientWebSocket.Dispose();
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        public async Task WebSocketAccept_SendAndReceive_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<WebSocket> clientTask = SendWebSocketRequestAsync(ConvertToWebSocketAddress(address));

                var context = await server.AcceptAsync();
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

        private string ConvertToWebSocketAddress(string address)
        {
            var builder = new UriBuilder(address);
            builder.Scheme = "ws";
            return builder.ToString();
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