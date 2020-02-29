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
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Moq.Protected;
using Xunit;
using HttpConnectionOptions = Microsoft.AspNetCore.Http.Connections.Client.HttpConnectionOptions;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class EndToEndTestsCollection : ICollectionFixture<InProcessTestServer<Startup>>
    {
        public const string Name = nameof(EndToEndTestsCollection);
    }

    [Collection(EndToEndTestsCollection.Name)]
    public class EndToEndTests : FunctionalTestBase
    {
        [Fact]
        public async Task CanStartAndStopConnectionUsingDefaultTransport()
        {
            using (var server = await StartServer<Startup>())
            {
                var url = server.Url + "/echo";
                // The test should connect to the server using WebSockets transport on Windows 8 and newer.
                // On Windows 7/2008R2 it should use ServerSentEvents transport to connect to the server.
                var connection = new HttpConnection(new Uri(url), HttpTransports.All, LoggerFactory);
                await connection.StartAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task TransportThatFallsbackCreatesNewConnection()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                       writeContext.EventId.Name == "ErrorStartingTransport";
            }

            using (var server = await StartServer<Startup>(expectedErrorsFilter: ExpectedErrors))
            {
                var url = server.Url + "/echo";
                // The test should connect to the server using WebSockets transport on Windows 8 and newer.
                // On Windows 7/2008R2 it should use ServerSentEvents transport to connect to the server.

                // The test logic lives in the TestTransportFactory and FakeTransport.
                var connection = new HttpConnection(new HttpConnectionOptions { Url = new Uri(url), DefaultTransferFormat = TransferFormat.Text }, LoggerFactory, new TestTransportFactory());
                await connection.StartAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(TransportTypes))]
        [LogLevel(LogLevel.Trace)]
        public async Task CanStartAndStopConnectionUsingGivenTransport(HttpTransportType transportType)
        {
            using (var server = await StartServer<Startup>())
            {
                var url = server.Url + "/echo";
                var connection = new HttpConnection(new HttpConnectionOptions { Url = new Uri(url), Transports = transportType, DefaultTransferFormat = TransferFormat.Text }, LoggerFactory);
                await connection.StartAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public async Task WebSocketsTest()
        {
            using (var server = await StartServer<Startup>())
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                const string message = "Hello, World!";
                using (var ws = new ClientWebSocket())
                {
                    var socketUrl = server.WebSocketsUrl + "/echo";

                    logger.LogInformation("Connecting WebSocket to {socketUrl}", socketUrl);
                    await ws.ConnectAsync(new Uri(socketUrl), CancellationToken.None).OrTimeout();

                    var bytes = Encoding.UTF8.GetBytes(message);
                    logger.LogInformation("Sending {length} byte frame", bytes.Length);
                    await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, endOfMessage: true, CancellationToken.None).OrTimeout();

                    logger.LogInformation("Receiving frame");
                    var buffer = new ArraySegment<byte>(new byte[1024]);
                    var result = await ws.ReceiveAsync(buffer, CancellationToken.None).OrTimeout();
                    logger.LogInformation("Received {length} byte frame", result.Count);

                    Assert.Equal(bytes, buffer.Array.AsSpan(0, result.Count).ToArray());

                    logger.LogInformation("Closing socket");
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).OrTimeout();
                    logger.LogInformation("Waiting for close");
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None).OrTimeout();
                    Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                    Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                    logger.LogInformation("Closed socket");
                }
            }
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public async Task WebSocketsReceivesAndSendsPartialFramesTest()
        {
            using (var server = await StartServer<Startup>())
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                const string message = "Hello, World!";
                using (var ws = new ClientWebSocket())
                {
                    var socketUrl = server.WebSocketsUrl + "/echo";

                    logger.LogInformation("Connecting WebSocket to {socketUrl}", socketUrl);
                    await ws.ConnectAsync(new Uri(socketUrl), CancellationToken.None).OrTimeout();

                    var bytes = Encoding.UTF8.GetBytes(message);
                    logger.LogInformation("Sending {length} byte frame", bytes.Length);
                    // We're sending a partial frame, we should still get the data
                    await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, endOfMessage: false, CancellationToken.None).OrTimeout();

                    logger.LogInformation("Receiving frame");
                    var buffer = new ArraySegment<byte>(new byte[1024]);
                    var result = await ws.ReceiveAsync(buffer, CancellationToken.None).OrTimeout();
                    logger.LogInformation("Received {length} byte frame", result.Count);

                    Assert.Equal(bytes, buffer.Array.AsSpan(0, result.Count).ToArray());

                    logger.LogInformation("Closing socket");
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).OrTimeout();
                    logger.LogInformation("Waiting for close");
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None).OrTimeout();
                    Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                    Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                    logger.LogInformation("Closed socket");
                }
            }
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public async Task HttpRequestsNotSentWhenWebSocketsTransportRequestedAndSkipNegotiationSet()
        {
            using (var server = await StartServer<Startup>())
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();
                var url = server.Url + "/echo";

                var mockHttpHandler = new Mock<HttpMessageHandler>();
                mockHttpHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .Returns<HttpRequestMessage, CancellationToken>(
                        (request, cancellationToken) => Task.FromException<HttpResponseMessage>(new InvalidOperationException("HTTP requests should not be sent.")));

                var httpOptions = new HttpConnectionOptions
                {
                    Url = new Uri(url),
                    Transports = HttpTransportType.WebSockets,
                    SkipNegotiation = true,
                    HttpMessageHandlerFactory = (httpMessageHandler) => mockHttpHandler.Object
                };

                var connection = new HttpConnection(httpOptions, LoggerFactory);

                try
                {
                    var message = new byte[] { 42 };
                    await connection.StartAsync().OrTimeout();

                    await connection.Transport.Output.WriteAsync(message).OrTimeout();

                    var receivedData = await connection.Transport.Input.ReadAsync(1);
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

        [Theory]
        [InlineData(HttpTransportType.LongPolling)]
        [InlineData(HttpTransportType.ServerSentEvents)]
        public async Task HttpConnectionThrowsIfSkipNegotiationSetAndTransportIsNotWebSockets(HttpTransportType transportType)
        {
            using (var server = await StartServer<Startup>())
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();
                var url = server.Url + "/echo";

                var mockHttpHandler = new Mock<HttpMessageHandler>();
                mockHttpHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .Returns<HttpRequestMessage, CancellationToken>(
                        (request, cancellationToken) => Task.FromException<HttpResponseMessage>(new InvalidOperationException("HTTP requests should not be sent.")));

                var httpOptions = new HttpConnectionOptions
                {
                    Url = new Uri(url),
                    Transports = transportType,
                    SkipNegotiation = true,
                    HttpMessageHandlerFactory = (httpMessageHandler) => mockHttpHandler.Object
                };

                var connection = new HttpConnection(httpOptions, LoggerFactory);

                try
                {
                    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.StartAsync().OrTimeout());
                    Assert.Equal("Negotiation can only be skipped when using the WebSocket transport directly.", exception.Message);
                }
                catch (Exception ex)
                {
                    logger.LogInformation(ex, "Test threw exception");
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(TransportTypesAndTransferFormats))]
        [LogLevel(LogLevel.Trace)]
        public async Task ConnectionCanSendAndReceiveMessages(HttpTransportType transportType, TransferFormat requestedTransferFormat)
        {
            using (var server = await StartServer<Startup>())
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                const string message = "Major Key";

                var url = server.Url + "/echo";
                var connection = new HttpConnection(new HttpConnectionOptions { Url = new Uri(url), Transports = transportType, DefaultTransferFormat = requestedTransferFormat }, LoggerFactory);
                try
                {
                    logger.LogInformation("Starting connection to {url}", url);
                    await connection.StartAsync().OrTimeout();
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
                    Assert.Equal(message, Encoding.UTF8.GetString(await connection.Transport.Input.ReadAsync(bytes.Length).OrTimeout()));
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

        [ConditionalTheory]
        [WebSocketsSupportedCondition]
        [InlineData(5 * 4096)]
        [InlineData(1000 * 4096 + 32)]
        [LogLevel(LogLevel.Trace)]
        public async Task ConnectionCanSendAndReceiveDifferentMessageSizesWebSocketsTransport(int length)
        {
            var message = new string('A', length);
            using (var server = await StartServer<Startup>())
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                var url = server.Url + "/echo";
                var connection = new HttpConnection(new Uri(url), HttpTransportType.WebSockets, LoggerFactory);

                try
                {
                    logger.LogInformation("Starting connection to {url}", url);
                    await connection.StartAsync().OrTimeout();
                    logger.LogInformation("Started connection to {url}", url);

                    var bytes = Encoding.UTF8.GetBytes(message);
                    logger.LogInformation("Sending {length} byte message", bytes.Length);
                    await connection.Transport.Output.WriteAsync(bytes).OrTimeout();
                    logger.LogInformation("Sent message");

                    logger.LogInformation("Receiving message");
                    // Big timeout here because it can take a while to receive all the bytes
                    var receivedData = await connection.Transport.Input.ReadAsync(bytes.Length).OrTimeout(TimeSpan.FromMinutes(2));
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
        [LogLevel(LogLevel.Trace)]
        public async Task UnauthorizedWebSocketsConnectionDoesNotConnect()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                       writeContext.EventId.Name == "ErrorWithNegotiation";
            }

            using (var server = await StartServer<Startup>(ExpectedErrors))
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                var url = server.Url + "/auth";
                var connection = new HttpConnection(new Uri(url), HttpTransportType.WebSockets, LoggerFactory);

                var exception = await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync().OrTimeout());

                Assert.Contains("401", exception.Message);
            }
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        [LogLevel(LogLevel.Trace)]
        public async Task UnauthorizedDirectWebSocketsConnectionDoesNotConnect()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                       writeContext.EventId.Name == "ErrorStartingTransport";
            }

            using (var server = await StartServer<Startup>(ExpectedErrors))
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                var url = server.Url + "/auth";
                var options = new HttpConnectionOptions
                {
                    Url = new Uri(url),
                    Transports = HttpTransportType.WebSockets,
                    SkipNegotiation = true
                };

                var connection = new HttpConnection(options, LoggerFactory);

                await Assert.ThrowsAsync<WebSocketException>(() => connection.StartAsync().OrTimeout());
            }
        }

        [Theory]
        [InlineData(HttpTransportType.LongPolling)]
        [InlineData(HttpTransportType.ServerSentEvents)]
        [LogLevel(LogLevel.Trace)]
        public async Task UnauthorizedConnectionDoesNotConnect(HttpTransportType transportType)
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                       writeContext.EventId.Name == "ErrorWithNegotiation";
            }

            using (var server = await StartServer<Startup>(ExpectedErrors))
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                var url = server.Url + "/auth";
                var connection = new HttpConnection(new Uri(url), transportType, LoggerFactory);

                try
                {
                    logger.LogInformation("Starting connection to {url}", url);
                    await connection.StartAsync().OrTimeout();
                    Assert.True(false);
                }
                catch (Exception ex)
                {
                    Assert.Equal("Response status code does not indicate success: 401 (Unauthorized).", ex.Message);
                }
                finally
                {
                    logger.LogInformation("Disposing Connection");
                    await connection.DisposeAsync().OrTimeout();
                    logger.LogInformation("Disposed Connection");
                }
            }
        }

        [Fact]
        [LogLevel(LogLevel.Trace)]
        public async Task AuthorizedConnectionCanConnect()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                       writeContext.EventId.Name == "ErrorWithNegotiation";
            }

            using (var server = await StartServer<Startup>(ExpectedErrors))
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                string token;
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(server.Url);

                    var response = await client.GetAsync("generatetoken?user=bob");
                    token = await response.Content.ReadAsStringAsync();
                }

                var url = server.Url + "/auth";
                var connection = new HttpConnection(
                    new HttpConnectionOptions()
                    {
                        Url = new Uri(url),
                        AccessTokenProvider = () => Task.FromResult(token),
                        Transports = HttpTransportType.ServerSentEvents,
                        DefaultTransferFormat = TransferFormat.Text,
                    },
                    LoggerFactory);

                try
                {
                    logger.LogInformation("Starting connection to {url}", url);
                    await connection.StartAsync().OrTimeout();
                    logger.LogInformation("Connected to {url}", url);
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
                await ServerClosesConnectionWithErrorIfHubCannotBeCreated(HttpTransportType.WebSockets);
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
                await ServerClosesConnectionWithErrorIfHubCannotBeCreated(HttpTransportType.LongPolling);
                Assert.True(false, "Expected error was not thrown.");
            }
            catch
            {
                // error is expected
            }
        }

        private async Task ServerClosesConnectionWithErrorIfHubCannotBeCreated(HttpTransportType transportType)
        {
            using (var server = await StartServer<Startup>())
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                var url = server.Url + "/uncreatable";
                var connection = new HubConnectionBuilder()
                        .WithLoggerFactory(LoggerFactory)
                        .WithUrl(url, transportType)
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
                        return Task.CompletedTask;
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

        [Fact]
        [LogLevel(LogLevel.Trace)]
        public async Task UnauthorizedHubConnectionDoesNotConnectWithEndpoints()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                       writeContext.EventId.Name == "ErrorWithNegotiation";
            }

            using (var server = await StartServer<Startup>(ExpectedErrors))
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                var url = server.Url + "/authHubEndpoints";
                var connection = new HubConnectionBuilder()
                        .WithLoggerFactory(LoggerFactory)
                        .WithUrl(url, HttpTransportType.LongPolling)
                        .Build();

                try
                {
                    logger.LogInformation("Starting connection to {url}", url);
                    await connection.StartAsync().OrTimeout();
                    Assert.True(false);
                }
                catch (Exception ex)
                {
                    Assert.Equal("Response status code does not indicate success: 401 (Unauthorized).", ex.Message);
                }
                finally
                {
                    logger.LogInformation("Disposing Connection");
                    await connection.DisposeAsync().OrTimeout();
                    logger.LogInformation("Disposed Connection");
                }
            }
        }

        [Fact]
        [LogLevel(LogLevel.Trace)]
        public async Task UnauthorizedHubConnectionDoesNotConnect()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                       writeContext.EventId.Name == "ErrorWithNegotiation";
            }

            using (var server = await StartServer<Startup>(ExpectedErrors))
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                var url = server.Url + "/authHub";
                var connection = new HubConnectionBuilder()
                        .WithLoggerFactory(LoggerFactory)
                        .WithUrl(url, HttpTransportType.LongPolling)
                        .Build();

                try
                {
                    logger.LogInformation("Starting connection to {url}", url);
                    await connection.StartAsync().OrTimeout();
                    Assert.True(false);
                }
                catch (Exception ex)
                {
                    Assert.Equal("Response status code does not indicate success: 401 (Unauthorized).", ex.Message);
                }
                finally
                {
                    logger.LogInformation("Disposing Connection");
                    await connection.DisposeAsync().OrTimeout();
                    logger.LogInformation("Disposed Connection");
                }
            }
        }

        [Fact]
        [LogLevel(LogLevel.Trace)]
        public async Task AuthorizedHubConnectionCanConnectWithEndpoints()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                       writeContext.EventId.Name == "ErrorWithNegotiation";
            }

            using (var server = await StartServer<Startup>(ExpectedErrors))
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                string token;
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(server.Url);

                    var response = await client.GetAsync("generatetoken?user=bob");
                    token = await response.Content.ReadAsStringAsync();
                }

                var url = server.Url + "/authHubEndpoints";
                var connection = new HubConnectionBuilder()
                        .WithLoggerFactory(LoggerFactory)
                        .WithUrl(url, HttpTransportType.LongPolling, o =>
                        {
                            o.AccessTokenProvider = () => Task.FromResult(token);
                        })
                        .Build();

                try
                {
                    logger.LogInformation("Starting connection to {url}", url);
                    await connection.StartAsync().OrTimeout();
                    logger.LogInformation("Connected to {url}", url);
                }
                finally
                {
                    logger.LogInformation("Disposing Connection");
                    await connection.DisposeAsync().OrTimeout();
                    logger.LogInformation("Disposed Connection");
                }
            }
        }

        [Fact]
        [LogLevel(LogLevel.Trace)]
        public async Task AuthorizedHubConnectionCanConnect()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                       writeContext.EventId.Name == "ErrorWithNegotiation";
            }

            using (var server = await StartServer<Startup>(ExpectedErrors))
            {
                var logger = LoggerFactory.CreateLogger<EndToEndTests>();

                string token;
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(server.Url);

                    var response = await client.GetAsync("generatetoken?user=bob");
                    token = await response.Content.ReadAsStringAsync();
                }

                var url = server.Url + "/authHub";
                var connection = new HubConnectionBuilder()
                        .WithLoggerFactory(LoggerFactory)
                        .WithUrl(url, HttpTransportType.LongPolling, o =>
                        {
                            o.AccessTokenProvider = () => Task.FromResult(token);
                        })
                        .Build();

                try
                {
                    logger.LogInformation("Starting connection to {url}", url);
                    await connection.StartAsync().OrTimeout();
                    logger.LogInformation("Connected to {url}", url);
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

            public ITransport CreateTransport(HttpTransportType availableServerTransports)
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
            private IDuplexPipe _transport;
            private readonly int availableTransports = 3;

            public PipeReader Input => _transport.Input;

            public PipeWriter Output => _transport.Output;

            public FakeTransport()
            {
                if (!TestHelpers.IsWebSocketsSupported())
                {
                    availableTransports -= 1;
                }
            }

            public Task StartAsync(Uri url, TransferFormat transferFormat, CancellationToken cancellationToken = default)
            {
                var options = ClientPipeOptions.DefaultOptions;
                var pair = DuplexPipe.CreateConnectionPair(options, options);

                _transport = pair.Transport;
                _application = pair.Application;
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

                if (_tries < availableTransports)
                {
                    return Task.FromException(new Exception());
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
                    yield return new object[] { HttpTransportType.WebSockets };
                }
                yield return new object[] { HttpTransportType.ServerSentEvents };
                yield return new object[] { HttpTransportType.LongPolling };
            }
        }

        public static IEnumerable<object[]> TransportTypesAndTransferFormats
        {
            get
            {
                foreach (var transport in TransportTypes)
                {
                    yield return new[] { transport[0], TransferFormat.Text };

                    if ((HttpTransportType)transport[0] != HttpTransportType.ServerSentEvents)
                    {
                        yield return new[] { transport[0], TransferFormat.Binary };
                    }
                }
            }
        }
    }
}
