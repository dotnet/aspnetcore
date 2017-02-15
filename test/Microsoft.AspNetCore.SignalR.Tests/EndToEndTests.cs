// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;

using ClientConnection = Microsoft.AspNetCore.Sockets.Client.Connection;
using Microsoft.AspNetCore.SignalR.Tests.Common;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    [CollectionDefinition(Name)]
    public class EndToEndTestsCollection : ICollectionFixture<ServerFixture>
    {
        public const string Name = "EndToEndTests";
    }

    [Collection(EndToEndTestsCollection.Name)]
    public class EndToEndTests
    {
        private readonly ServerFixture _serverFixture;

        public EndToEndTests(ServerFixture serverFixture)
        {
            _serverFixture = serverFixture;
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        public async Task WebSocketsTest()
        {
            const string message = "Hello, World!";
            using (var ws = new ClientWebSocket())
            {
                await ws.ConnectAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo/ws"), CancellationToken.None);
                var bytes = Encoding.UTF8.GetBytes(message);
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, CancellationToken.None);
                var buffer = new ArraySegment<byte>(new byte[1024]);
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);

                Assert.Equal(bytes, buffer.Array.Slice(0, message.Length).ToArray());

                await ws.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
            }
        }

        [Fact]
        public async Task ConnectionCanSendAndReceiveMessages()
        {
            const string message = "Major Key";
            var baseUrl = _serverFixture.BaseUrl;
            var loggerFactory = new LoggerFactory();

            using (var httpClient = new HttpClient())
            {
                var transport = new LongPollingTransport(httpClient, loggerFactory);
                using (var connection = await ClientConnection.ConnectAsync(new Uri(baseUrl + "/echo"), transport, httpClient, loggerFactory))
                {
                    await connection.SendAsync(Encoding.UTF8.GetBytes(message), MessageType.Text);

                    var receiveData = new ReceiveData();

                    Assert.True(await connection.ReceiveAsync(receiveData).OrTimeout());
                    Assert.Equal(message, Encoding.UTF8.GetString(receiveData.Data));
                }
            }
        }
    }
}
