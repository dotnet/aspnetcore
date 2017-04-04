// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using Xunit.Abstractions;

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
        private readonly ITestOutputHelper _output;

        private readonly ServerFixture _serverFixture;

        public EndToEndTests(ServerFixture serverFixture, ITestOutputHelper output)
        {
            if (serverFixture == null)
            {
                throw new ArgumentNullException(nameof(serverFixture));
            }

            _serverFixture = serverFixture;
            _output = output;
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

                Assert.Equal(bytes, buffer.Array.AsSpan().Slice(0, message.Length).ToArray());

                await ws.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        [MemberData(nameof(TransportTypes))]
        public async Task ConnectionCanSendAndReceiveMessages(TransportType transportType)
        {
            const string message = "Major Key";
            var baseUrl = _serverFixture.BaseUrl;
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddXunit(_output, LogLevel.Trace);

            var connection = new ClientConnection(new Uri(baseUrl + "/echo"), loggerFactory);
            try
            {
                var receiveTcs = new TaskCompletionSource<string>();
                connection.Received += (data, format) => receiveTcs.TrySetResult(Encoding.UTF8.GetString(data));
                connection.Closed += e =>
                    {
                        if (e != null)
                        {
                            receiveTcs.TrySetException(e);
                        }
                        else
                        {
                            receiveTcs.TrySetResult(null);
                        }
                    };

                await connection.StartAsync(transportType);

                await connection.SendAsync(Encoding.UTF8.GetBytes(message), MessageType.Text);

                var receiveData = new ReceiveData();

                Assert.Equal(message, await receiveTcs.Task.OrTimeout());
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        public static IEnumerable<object[]> MessageSizesData
        {
            get
            {
                yield return new object[] { new string('A', 5 * 1024) };
                yield return new object[] { new string('A', 5 * 1024 * 1024 + 32) };
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        [MemberData(nameof(MessageSizesData))]
        public async Task ConnectionCanSendAndReceiveDifferentMessageSizesWebSocketsTransport(string message)
        {
            var baseUrl = _serverFixture.BaseUrl;
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddXunit(_output, LogLevel.Debug);

            var connection = new ClientConnection(new Uri(baseUrl + "/echo"), loggerFactory);
            try
            {
                var receiveTcs = new TaskCompletionSource<byte[]>();
                connection.Received += (data, messageType) => receiveTcs.SetResult(data);

                await connection.StartAsync(TransportType.WebSockets);

                await connection.SendAsync(Encoding.UTF8.GetBytes(message), MessageType.Text);

                var receiveData = new ReceiveData();

                var receivedData = await receiveTcs.Task.OrTimeout();
                Assert.Equal(message, Encoding.UTF8.GetString(receivedData));
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        public static IEnumerable<object[]> TransportTypes() =>
            new[]
            {
                new object[] { TransportType.WebSockets },
                new object[] { TransportType.LongPolling }
            };
    }
}
