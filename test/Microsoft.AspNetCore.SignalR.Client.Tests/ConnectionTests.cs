// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Client.Tests;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Sockets.Client.Tests
{
    public class ConnectionTests
    {
        [Fact]
        public void CannotCreateConnectionWithNullUrl()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new HttpConnection(null));
            Assert.Equal("url", exception.ParamName);
        }

        [Fact]
        public void ConnectionReturnsUrlUsedToStartTheConnection()
        {
            var connectionUrl = new Uri("http://fakeuri.org/");
            Assert.Equal(connectionUrl, new HttpConnection(connectionUrl).Url);
        }

        [Theory]
        [InlineData((TransportType)0)]
        [InlineData(TransportType.All + 1)]
        public void CannotStartConnectionWithInvalidTransportType(TransportType requestedTransportType)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new HttpConnection(new Uri("http://fakeuri.org/"), requestedTransportType));
        }

        [Fact]
        public async Task CannotStartRunningConnection()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            try
            {
                await connection.StartAsync();
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await connection.StartAsync());
                Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task CannotStartStoppedConnection()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);

            await connection.StartAsync();
            await connection.DisposeAsync();
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await connection.StartAsync());

            Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);
        }

        [Fact]
        public async Task CannotStartDisposedConnection()
        {
            using (var httpClient = new HttpClient())
            {
                var connection = new HttpConnection(new Uri("http://fakeuri.org/"));
                await connection.DisposeAsync();
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await connection.StartAsync());

                Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);
            }
        }

        [Fact]
        public async Task CanStopStartingConnection()
        {
            // Used to make sure StartAsync is not completed before DisposeAsync is called
            var releaseNegotiateTcs = new TaskCompletionSource<object>();
            // Used to make sure that DisposeAsync runs after we check the state in StartAsync
            var allowDisposeTcs = new TaskCompletionSource<object>();
            // Used to make sure that DisposeAsync continues only after StartAsync finished
            var releaseDisposeTcs = new TaskCompletionSource<object>();

            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    // allow DisposeAsync to continue once we know we are past the connection state check
                    allowDisposeTcs.SetResult(null);
                    await releaseNegotiateTcs.Task;
                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });


            var transport = new Mock<ITransport>();
            transport.Setup(t => t.StopAsync()).Returns(async () => { await releaseDisposeTcs.Task; });
            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), new TestTransportFactory(transport.Object), loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);

            var startTask = connection.StartAsync();
            await allowDisposeTcs.Task;
            var disposeTask = connection.DisposeAsync();
            // allow StartAsync to continue once DisposeAsync has started
            releaseNegotiateTcs.SetResult(null);

            // unblock DisposeAsync only after StartAsync completed
            await startTask.OrTimeout();
            releaseDisposeTcs.SetResult(null);
            await disposeTask.OrTimeout();

            transport.Verify(t => t.StartAsync(It.IsAny<Uri>(), It.IsAny<Channel<byte[], SendMessage>>()), Times.Never);
        }

        [Fact]
        public async Task SendThrowsIfConnectionIsNotStarted()
        {
            var connection = new HttpConnection(new Uri("http://fakeuri.org/"));
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.SendAsync(new byte[0]));
            Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
        }

        [Fact]
        public async Task SendThrowsIfConnectionIsDisposed()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });


            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);

            await connection.StartAsync();
            await connection.DisposeAsync();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.SendAsync(new byte[0]));
            Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
        }

        [Fact]
        public async Task ConnectedEventRaisedWhenTheClientIsConnected()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });


            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            try
            {
                var connectedEventRaisedTcs = new TaskCompletionSource<object>();
                connection.Connected += () => connectedEventRaisedTcs.SetResult(null);

                await connection.StartAsync();

                await connectedEventRaisedTcs.Task.OrTimeout();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task ConnectedEventNotRaisedWhenTransportFailsToStart()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            var mockTransport = new Mock<ITransport>();
            mockTransport.Setup(t => t.StartAsync(It.IsAny<Uri>(), It.IsAny<Channel<byte[], SendMessage>>()))
                .Returns(Task.FromException(new InvalidOperationException("Transport failed to start")));


            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), new TestTransportFactory(mockTransport.Object), loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            var connectedEventRaised = false;

            try
            {
                connection.Connected += () => connectedEventRaised = true;

                await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await connection.StartAsync());
            }
            finally
            {
                await connection.DisposeAsync();
            }

            Assert.False(connectedEventRaised);
        }

        [Fact]
        public async Task ClosedEventRaisedWhenTheClientIsBeingStopped()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });


            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);

            var closedEventTcs = new TaskCompletionSource<Exception>();
            connection.Closed += e => closedEventTcs.SetResult(e);

            await connection.StartAsync();
            await connection.DisposeAsync();

            // in case of clean disconnect error should be null
            Assert.Null(await closedEventTcs.Task.OrTimeout());
        }

        [Fact]
        public async Task ClosedEventRaisedWhenConnectionToServerLost()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    return request.Method == HttpMethod.Get
                        ? ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError)
                        : request.Method == HttpMethod.Options
                            ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                            : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            var closedEventTcs = new TaskCompletionSource<Exception>();
            connection.Closed += e => closedEventTcs.TrySetResult(e);

            try
            {
                await connection.StartAsync();
                Assert.IsType<HttpRequestException>(await closedEventTcs.Task.OrTimeout());
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReceivedEventNotRaisedAfterConnectionIsDisposed()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            var mockTransport = new Mock<ITransport>();
            Channel<byte[], SendMessage> channel = null;
            mockTransport.Setup(t => t.StartAsync(It.IsAny<Uri>(), It.IsAny<Channel<byte[], SendMessage>>()))
                .Returns<Uri, Channel<byte[], SendMessage>>((url, c) =>
                {
                    channel = c;
                    return Task.CompletedTask;
                });
            mockTransport.Setup(t => t.StopAsync())
                .Returns(() =>
                {
                    // The connection is now in the Disconnected state so the Received event for
                    // this message should not be raised
                    channel.Out.TryWrite(Array.Empty<byte>());
                    channel.Out.TryComplete();
                    return Task.CompletedTask;
                });


            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), new TestTransportFactory(mockTransport.Object), loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            var receivedInvoked = false;
            connection.Received += m =>
            {
                receivedInvoked = true;
                return Task.CompletedTask;
            };

            await connection.StartAsync();
            await connection.DisposeAsync();
            Assert.False(receivedInvoked);
        }

        [Fact]
        public async Task EventsAreNotRunningOnMainLoop()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            var mockTransport = new Mock<ITransport>();
            Channel<byte[], SendMessage> channel = null;
            mockTransport.Setup(t => t.StartAsync(It.IsAny<Uri>(), It.IsAny<Channel<byte[], SendMessage>>()))
                .Returns<Uri, Channel<byte[], SendMessage>>((url, c) =>
                {
                    channel = c;
                    return Task.CompletedTask;
                });
            mockTransport.Setup(t => t.StopAsync())
                .Returns(() =>
                {
                    channel.Out.TryComplete();
                    return Task.CompletedTask;
                });


            var callbackInvokedTcs = new TaskCompletionSource<object>();
            var closedTcs = new TaskCompletionSource<object>();

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), new TestTransportFactory(mockTransport.Object), loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            connection.Received +=
                async m =>
                {
                    callbackInvokedTcs.SetResult(null);
                    await closedTcs.Task;
                };

            await connection.StartAsync();
            channel.Out.TryWrite(Array.Empty<byte>());

            // Ensure that the Received callback has been called before attempting the second write
            await callbackInvokedTcs.Task.OrTimeout();
            channel.Out.TryWrite(Array.Empty<byte>());

            // Ensure that SignalR isn't blocked by the receive callback
            Assert.False(channel.In.TryRead(out var message));

            closedTcs.SetResult(null);

            await connection.DisposeAsync();
        }

        [Fact]
        public async Task ClosedEventNotRaisedWhenTheClientIsStoppedButWasNeverStarted()
        {
            var connection = new HttpConnection(new Uri("http://fakeuri.org/"));

            bool closedEventRaised = false;
            connection.Closed += e => closedEventRaised = true;

            await connection.DisposeAsync();
            Assert.False(closedEventRaised);
        }

        [Fact]
        public async Task TransportIsStoppedWhenConnectionIsStopped()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new HttpConnection(new Uri("http://fakeuri.org/"), new TestTransportFactory(longPollingTransport), loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);

                try
                {
                    await connection.StartAsync();

                    Assert.False(longPollingTransport.Running.IsCompleted);
                }
                finally
                {
                    await connection.DisposeAsync();
                }

                await longPollingTransport.Running.OrTimeout();
            }
        }

        [Fact]
        public async Task CanSendData()
        {
            var data = new byte[] { 1, 1, 2, 3, 5, 8 };

            var sendTcs = new TaskCompletionSource<byte[]>();
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    if (request.Method == HttpMethod.Post)
                    {
                        sendTcs.SetResult(await request.Content.ReadAsByteArrayAsync());
                    }

                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            try
            {
                await connection.StartAsync();

                await connection.SendAsync(data);

                Assert.Equal(data, await sendTcs.Task.OrTimeout());
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task SendAsyncThrowsIfConnectionIsNotStarted()
        {
            var connection = new HttpConnection(new Uri("http://fakeuri.org/"));
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.SendAsync(new byte[0]));

            Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
        }

        [Fact]
        public async Task SendAsyncThrowsIfConnectionIsDisposed()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    var content = string.Empty;
                    if (request.Method == HttpMethod.Get)
                    {
                        content = "T2:T:42;";
                    }

                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK, content);
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);

            await connection.StartAsync();
            await connection.DisposeAsync();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.SendAsync(new byte[0]));

            Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
        }

        [Fact]
        public async Task CallerReceivesExceptionsFromSendAsync()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    return request.Method == HttpMethod.Post
                        ? ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError)
                        : request.Method == HttpMethod.Options
                            ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                            : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);

            await connection.StartAsync();

            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                async () => await connection.SendAsync(new byte[0]));

            await connection.DisposeAsync();
        }

        [Fact]
        public async Task CanReceiveData()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    var content = string.Empty;

                    if (request.Method == HttpMethod.Get)
                    {
                        content = "42";
                    }

                    return request.Method == HttpMethod.Options
                        ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK, content);
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            try
            {
                var receiveTcs = new TaskCompletionSource<string>();
                connection.Received += data =>
                {
                    receiveTcs.TrySetResult(Encoding.UTF8.GetString(data));
                    return Task.CompletedTask;
                };

                connection.Closed += e =>
                    {
                        if (e != null)
                        {
                            receiveTcs.TrySetException(e);
                        }
                        else
                        {
                            receiveTcs.TrySetCanceled();
                        }
                    };

                await connection.StartAsync();

                Assert.Equal("42", await receiveTcs.Task.OrTimeout());
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task CannotSendAfterReceiveThrewException()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    return request.Method == HttpMethod.Get
                        ? ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError)
                        : request.Method == HttpMethod.Options
                            ? ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationResponse())
                            : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            try
            {
                var closeTcs = new TaskCompletionSource<Exception>();
                connection.Closed += e => closeTcs.TrySetResult(e);

                await connection.StartAsync();

                // Exception in send should shutdown the connection
                await closeTcs.Task.OrTimeout();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await connection.SendAsync(new byte[0]));

                Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("Not Json")]
        public async Task StartThrowsFormatExceptionIfNegotiationResponseIsInvalid(string negotiatePayload)
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK, negotiatePayload);
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            var exception = await Assert.ThrowsAsync<FormatException>(
                () => connection.StartAsync());

            Assert.Equal("Invalid negotiation response received.", exception.Message);
        }

        [Fact]
        public async Task StartThrowsFormatExceptionIfNegotiationResponseHasNoConnectionId()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK,
                        ResponseUtils.CreateNegotiationResponse(connectionId: null));
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            var exception = await Assert.ThrowsAsync<FormatException>(
                () => connection.StartAsync());

            Assert.Equal("Invalid connection id returned in negotiation response.", exception.Message);
        }

        [Fact]
        public async Task StartThrowsFormatExceptionIfNegotiationResponseHasNoTransports()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK,
                        ResponseUtils.CreateNegotiationResponse(transportTypes: null));
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            var exception = await Assert.ThrowsAsync<FormatException>(
                () => connection.StartAsync());

            Assert.Equal("No transports returned in negotiation response.", exception.Message);
        }

        [Theory]
        [InlineData((TransportType)0)]
        [InlineData(TransportType.ServerSentEvents)]
        public async Task ConnectionCannotBeStartedIfNoCommonTransportsBetweenClientAndServer(TransportType serverTransports)
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK,
                        ResponseUtils.CreateNegotiationResponse(transportTypes: serverTransports));
                });

            var connection = new HttpConnection(new Uri("http://fakeuri.org/"), TransportType.LongPolling, loggerFactory: null, httpMessageHandler: mockHttpHandler.Object);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => connection.StartAsync());

            Assert.Equal("No requested transports available on the server.", exception.Message);
        }
    }
}
