// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    [CollectionDefinition(Name)]
    public class EndToEndTestsCollection : ICollectionFixture<ServerFixture<Startup>>
    {
        public const string Name = "EndToEndTests";
    }

    [Collection(EndToEndTestsCollection.Name)]
    public class EndToEndTests : LoggedTest
    {
        private readonly ServerFixture<Startup> _serverFixture;

        public EndToEndTests(ServerFixture<Startup> serverFixture, ITestOutputHelper output) : base(output)
        {
            if (serverFixture == null)
            {
                throw new ArgumentNullException(nameof(serverFixture));
            }

            _serverFixture = serverFixture;
        }

        [Fact]
        public async Task CanStartAndStopConnectionUsingDefaultTransport()
        {
            var url = _serverFixture.Url + "/echo";
            // The test should connect to the server using WebSockets transport on Windows 8 and newer.
            // On Windows 7/2008R2 it should use ServerSentEvents transport to connect to the server.
            var connection = new HttpConnection(new Uri(url));
            await connection.StartAsync(TransferFormat.Binary).OrTimeout();
            await connection.DisposeAsync().OrTimeout();
        }

        [Fact]
        public async Task TransportThatFallsbackCreatesNewConnection()
        {
            var url = _serverFixture.Url + "/echo";
            // The test should connect to the server using WebSockets transport on Windows 8 and newer.
            // On Windows 7/2008R2 it should use ServerSentEvents transport to connect to the server.

            // The test logic lives in the TestTransportFactory and FakeTransport.
            var connection = new HttpConnection(new Uri(url), new TestTransportFactory(), null, null);
            await connection.StartAsync(TransferFormat.Text).OrTimeout();
            await connection.DisposeAsync().OrTimeout();
        }

        [Theory]
        [MemberData(nameof(TransportTypes))]
        public async Task CanStartAndStopConnectionUsingGivenTransport(TransportType transportType)
        {
            var url = _serverFixture.Url + "/echo";
            var connection = new HttpConnection(new Uri(url), transportType);
            await connection.StartAsync(TransferFormat.Text).OrTimeout();
            await connection.DisposeAsync().OrTimeout();
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public async Task WebSocketsTest()
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger<EndToEndTests>();

                const string message = "Hello, World!";
                using (var ws = new ClientWebSocket())
                {
                    var socketUrl = _serverFixture.WebSocketsUrl + "/echo";

                    logger.LogInformation("Connecting WebSocket to {socketUrl}", socketUrl);
                    await ws.ConnectAsync(new Uri(socketUrl), CancellationToken.None).OrTimeout();

                    var bytes = Encoding.UTF8.GetBytes(message);
                    logger.LogInformation("Sending {length} byte frame", bytes.Length);
                    await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, CancellationToken.None).OrTimeout();

                    logger.LogInformation("Receiving frame");
                    var buffer = new ArraySegment<byte>(new byte[1024]);
                    var result = await ws.ReceiveAsync(buffer, CancellationToken.None).OrTimeout();
                    logger.LogInformation("Received {length} byte frame", result.Count);

                    Assert.Equal(bytes, buffer.Array.AsSpan().Slice(0, result.Count).ToArray());

                    logger.LogInformation("Waiting for close");
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None).OrTimeout();
                    Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                    logger.LogInformation("Closing socket");
                    await ws.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None).OrTimeout();
                    logger.LogInformation("Closed socket");
                }
            }
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public async Task HttpRequestsNotSentWhenWebSocketsTransportRequested()
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger<EndToEndTests>();
                var url = _serverFixture.Url + "/echo";

                var mockHttpHandler = new Mock<HttpMessageHandler>();
                mockHttpHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .Returns<HttpRequestMessage, CancellationToken>(
                        (request, cancellationToken) => Task.FromException<HttpResponseMessage>(new InvalidOperationException("HTTP requests should not be sent.")));

                var connection = new HttpConnection(new Uri(url), TransportType.WebSockets, loggerFactory, new HttpOptions { HttpMessageHandler = (httpMessageHandler) => mockHttpHandler.Object });

                try
                {
                    var message = new byte[] { 42 };
                    await connection.StartAsync(TransferFormat.Binary).OrTimeout();

                    await connection.Transport.Output.WriteAsync(message).OrTimeout();

                    var receivedData = await connection.Transport.Input.ReadAllAsync();
                    Assert.Equal(message, receivedData);
                }
                catch (Exception ex)
                {
                    logger.LogInformation(ex, "Test threw exception");
                    throw;
                }
                finally
                {
                    logger.LogInformation("Disposing Connection");
                    await connection.DisposeAsync().OrTimeout();
                    logger.LogInformation("Disposed Connection");
                }
            }
        }

        [Theory(Skip = "https://github.com/aspnet/SignalR/issues/1485")]
        [MemberData(nameof(TransportTypesAndTransferFormats))]
        public async Task ConnectionCanSendAndReceiveMessages(TransportType transportType, TransferFormat requestedTransferFormat)
        {
            using (StartLog(out var loggerFactory, testName: $"ConnectionCanSendAndReceiveMessages_{transportType.ToString()}"))
            {
                var logger = loggerFactory.CreateLogger<EndToEndTests>();

                const string message = "Major Key";

                var url = _serverFixture.Url + "/echo";
                var connection = new HttpConnection(new Uri(url), transportType, loggerFactory);
                try
                {
                    logger.LogInformation("Starting connection to {url}", url);
                    await connection.StartAsync(requestedTransferFormat).OrTimeout();
                    logger.LogInformation("Started connection to {url}", url);

                    var bytes = Encoding.UTF8.GetBytes(message);

                    logger.LogInformation("Sending {length} byte message", bytes.Length);
                    try
                    {
                        await connection.Transport.Output.WriteAsync(bytes).OrTimeout();
                    }
                    catch (OperationCanceledException)
                    {
                        // Because the server and client are run in the same process there is a race where websocket.SendAsync
                        // can send a message but before returning be suspended allowing the server to run the EchoConnectionHandler and
                        // send a close frame which triggers a cancellation token on the client and cancels the websocket.SendAsync.
                        // Our solution to this is to just catch OperationCanceledException from the sent message if the race happens
                        // because we know the send went through, and its safe to check the response.
                    }
                    logger.LogInformation("Sent message");

                    logger.LogInformation("Receiving message");
                    Assert.Equal(message, Encoding.UTF8.GetString(await connection.Transport.Input.ReadAllAsync()));
                    logger.LogInformation("Completed receive");
                }
                catch (Exception ex)
                {
                    logger.LogInformation(ex, "Test threw exception");
                    throw;
                }
                finally
                {
                    logger.LogInformation("Disposing Connection");
                    await connection.DisposeAsync().OrTimeout();
                    logger.LogInformation("Disposed Connection");
                }
            }
        }

        public static IEnumerable<object[]> MessageSizesData
        {
            get
            {
                yield return new object[] { new string('A', 5 * 4096) };
                yield return new object[] { new string('A', 1000 * 4096 + 32) };
            }
        }

        [ConditionalTheory]
        [WebSocketsSupportedCondition]
        [MemberData(nameof(MessageSizesData))]
        public async Task ConnectionCanSendAndReceiveDifferentMessageSizesWebSocketsTransport(string message)
        {
            using (StartLog(out var loggerFactory, LogLevel.Trace, testName: $"ConnectionCanSendAndReceiveDifferentMessageSizesWebSocketsTransport_{message.Length}"))
            {
                var logger = loggerFactory.CreateLogger<EndToEndTests>();

                var url = _serverFixture.Url + "/echo";
                var connection = new HttpConnection(new Uri(url), TransportType.WebSockets, loggerFactory);

                try
                {
                    logger.LogInformation("Starting connection to {url}", url);
                    await connection.StartAsync(TransferFormat.Binary).OrTimeout();
                    logger.LogInformation("Started connection to {url}", url);

                    var bytes = Encoding.UTF8.GetBytes(message);
                    logger.LogInformation("Sending {length} byte message", bytes.Length);
                    await connection.Transport.Output.WriteAsync(bytes).OrTimeout();
                    logger.LogInformation("Sent message");

                    logger.LogInformation("Receiving message");
                    // Big timeout here because it can take a while to receive all the bytes
                    var receivedData = await connection.Transport.Input.ReadAllAsync();
                    Assert.Equal(message, Encoding.UTF8.GetString(receivedData));
                    logger.LogInformation("Completed receive");
                }
                catch (Exception ex)
                {
                    logger.LogInformation(ex, "Test threw exception");
                    throw;
                }
                finally
                {
                    logger.LogInformation("Disposing Connection");
                    await connection.DisposeAsync().OrTimeout();
                    logger.LogInformation("Disposed Connection");
                }
            }
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public async Task ServerClosesConnectionWithErrorIfHubCannotBeCreated_WebSocket()
        {
            try
            {
                await ServerClosesConnectionWithErrorIfHubCannotBeCreated(TransportType.WebSockets);
                Assert.True(false, "Expected error was not thrown.");
            }
            catch
            {
                // error is expected
            }
        }

        [Fact]
        public async Task ServerClosesConnectionWithErrorIfHubCannotBeCreated_LongPolling()
        {
            try
            {
                await ServerClosesConnectionWithErrorIfHubCannotBeCreated(TransportType.LongPolling);
                Assert.True(false, "Expected error was not thrown.");
            }
            catch
            {
                // error is expected
            }
        }

        private async Task ServerClosesConnectionWithErrorIfHubCannotBeCreated(TransportType transportType)
        {
            using (StartLog(out var loggerFactory, testName: $"ConnectionCanSendAndReceiveMessages_{transportType.ToString()}"))
            {
                var logger = loggerFactory.CreateLogger<EndToEndTests>();

                var url = _serverFixture.Url + "/uncreatable";
                var connection = new HubConnectionBuilder()
                        .WithUrl(new Uri(url))
                        .WithTransport(transportType)
                        .WithLoggerFactory(loggerFactory)
                        .Build();
                try
                {
                    var closeTcs = new TaskCompletionSource<object>();
                    connection.Closed += e =>
                    {
                        if (e != null)
                        {
                            closeTcs.SetException(e);
                        }
                        else
                        {
                            closeTcs.SetResult(null);
                        }
                    };

                    logger.LogInformation("Starting connection to {url}", url);

                    try
                    {
                        await connection.StartAsync().OrTimeout();
                    }
                    catch (OperationCanceledException)
                    {
                        // Due to a race, this can fail with OperationCanceledException in the SendAsync
                        // call that HubConnection does to send the handshake message.
                        // This has only been happening on AppVeyor, likely due to a slower CI machine
                        // The closed event will still fire with the exception we care about.
                    }

                    await closeTcs.Task.OrTimeout();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Test threw {exceptionType}: {message}", ex.GetType(), ex.Message);
                    throw;
                }
                finally
                {
                    logger.LogInformation("Disposing Connection");
                    await connection.DisposeAsync().OrTimeout();
                    logger.LogInformation("Disposed Connection");
                }
            }
        }

        // Serves a fake transport that lets us verify fallback behavior 
        private class TestTransportFactory : ITransportFactory
        {
            private ITransport _transport;

            public ITransport CreateTransport(TransportType availableServerTransports)
            {
                if (_transport == null)
                {
                    _transport = new FakeTransport();
                }

                return _transport;
            }
        }

        private class FakeTransport : ITransport
        {
            private int _tries;
            private string _prevConnectionId = null;
            private IDuplexPipe _application;

            public Task StartAsync(Uri url, IDuplexPipe application, TransferFormat transferFormat, IConnection connection)
            {
                _application = application;
                _tries++;
                Assert.True(QueryHelpers.ParseQuery(url.Query).TryGetValue("id", out var id));
                if (_prevConnectionId == null)
                {
                    _prevConnectionId = id;
                }
                else
                {
                    Assert.True(_prevConnectionId != id);
                    _prevConnectionId = id;
                }

                if (_tries < 3)
                {
                    throw new Exception();
                }
                else
                {
                    return Task.CompletedTask;
                }
            }

            public Task StopAsync()
            {
                _application.Output.Complete();
                _application.Input.Complete();
                return Task.CompletedTask;
            }
        }

        public static IEnumerable<object[]> TransportTypes
        {
            get
            {
                if (TestHelpers.IsWebSocketsSupported())
                {
                    yield return new object[] { TransportType.WebSockets };
                }
                yield return new object[] { TransportType.ServerSentEvents };
                yield return new object[] { TransportType.LongPolling };
            }
        }

        public static IEnumerable<object[]> TransportTypesAndTransferFormats
        {
            get
            {
                foreach (var transport in TransportTypes)
                {
                    yield return new[] { transport[0], TransferFormat.Text };

                    if ((TransportType)transport[0] != TransportType.ServerSentEvents)
                    {
                        yield return new[] { transport[0], TransferFormat.Binary };
                    }
                }
            }
        }
    }
}
