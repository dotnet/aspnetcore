// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.AspNetCore.Sockets.Tests.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Client.Tests
{
    public class ConnectionTests
    {
        [Fact]
        public void CannotCreateConnectionWithNullUrl()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new Connection(null));
            Assert.Equal("url", exception.ParamName);
        }

        [Fact]
        public void ConnectionReturnsUrlUsedToStartTheConnection()
        {
            var connectionUrl = new Uri("http://fakeuri.org/");
            Assert.Equal(connectionUrl, new Connection(connectionUrl).Url);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(TransportType.All + 1)]
        public async Task CannotStartConnectionWithInvalidTransportType(TransportType requestedTransportType)
        {
            var connection = new Connection(new Uri("http://fakeuri.org/"));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => connection.StartAsync(requestedTransportType));
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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                try
                {
                    await connection.StartAsync(TransportType.LongPolling, httpClient);
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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                await connection.StartAsync(TransportType.LongPolling, httpClient);
                await connection.DisposeAsync();
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await connection.StartAsync());

                Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);
            }
        }

        [Fact]
        public async Task CannotStartDisposedConnection()
        {
            using (var httpClient = new HttpClient())
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var transport = new Mock<ITransport>();
                transport.Setup(t => t.StopAsync()).Returns(async () => { await releaseDisposeTcs.Task; });
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                var startTask = connection.StartAsync(new TestTransportFactory(transport.Object), httpClient);
                await allowDisposeTcs.Task;
                var disposeTask = connection.DisposeAsync();
                // allow StartAsync to continue once DisposeAsync has started
                releaseNegotiateTcs.SetResult(null);

                // unblock DisposeAsync only after StartAsync completed
                await startTask.OrTimeout();
                releaseDisposeTcs.SetResult(null);
                await disposeTask.OrTimeout();

                transport.Verify(t => t.StartAsync(It.IsAny<Uri>(), It.IsAny<IChannelConnection<SendMessage, Message>>()), Times.Never);
            }
        }

        [Fact]
        public async Task SendThrowsIfConnectionIsNotStarted()
        {
            var connection = new Connection(new Uri("http://fakeuri.org/"));
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.SendAsync(new byte[0], MessageType.Binary));
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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                await connection.StartAsync(TransportType.LongPolling, httpClient);
                await connection.DisposeAsync();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await connection.SendAsync(new byte[0], MessageType.Binary));
                Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
            }
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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                try
                {
                    var connectedEventRaisedTcs = new TaskCompletionSource<object>();
                    connection.Connected += () => connectedEventRaisedTcs.SetResult(null);

                    await connection.StartAsync(TransportType.LongPolling, httpClient);

                    await connectedEventRaisedTcs.Task.OrTimeout();
                }
                finally
                {
                    await connection.DisposeAsync();
                }
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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            var mockTransport = new Mock<ITransport>();
            mockTransport.Setup(t => t.StartAsync(It.IsAny<Uri>(), It.IsAny<IChannelConnection<SendMessage, Message>>()))
                .Returns(Task.FromException(new InvalidOperationException("Transport failed to start")));

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                var connectedEventRaised = false;

                try
                {
                    connection.Connected += () => connectedEventRaised = true;

                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await connection.StartAsync(new TestTransportFactory(mockTransport.Object), httpClient));
                }
                finally
                {
                    await connection.DisposeAsync();
                }

                Assert.False(connectedEventRaised);
            }
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
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                var closedEventTcs = new TaskCompletionSource<Exception>();
                connection.Closed += e => closedEventTcs.SetResult(e);

                await connection.StartAsync(TransportType.LongPolling, httpClient);
                await connection.DisposeAsync();

                // in case of clean disconnect error should be null
                Assert.Null(await closedEventTcs.Task.OrTimeout());
            }
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
                    if (request.RequestUri.AbsolutePath.EndsWith("/poll"))
                    {
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(string.Empty) };
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                var closedEventTcs = new TaskCompletionSource<Exception>();
                connection.Closed += e => closedEventTcs.TrySetResult(e);

                try
                {
                    await connection.StartAsync(TransportType.LongPolling, httpClient);
                    Assert.IsType<HttpRequestException>(await closedEventTcs.Task.OrTimeout());
                }
                finally
                {
                    await connection.DisposeAsync();
                }
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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            var mockTransport = new Mock<ITransport>();
            IChannelConnection<SendMessage, Message> channel = null;
            mockTransport.Setup(t => t.StartAsync(It.IsAny<Uri>(), It.IsAny<IChannelConnection<SendMessage, Message>>()))
                .Returns<Uri, IChannelConnection<SendMessage, Message>>((url, c) =>
                {
                    channel = c;
                    return Task.CompletedTask;
                });
            mockTransport.Setup(t => t.StopAsync())
                .Returns(() =>
                {
                    // The connection is now in the Disconnected state so the Received event for
                    // this message should not be raised
                    channel.Output.TryWrite(new Message());
                    channel.Output.TryComplete();
                    return Task.CompletedTask;
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                var receivedInvoked = false;
                connection.Received += (m, t) => receivedInvoked = true;

                await connection.StartAsync(new TestTransportFactory(mockTransport.Object), httpClient);
                await connection.DisposeAsync();
                Assert.False(receivedInvoked);
            }
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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            var mockTransport = new Mock<ITransport>();
            IChannelConnection<SendMessage, Message> channel = null;
            mockTransport.Setup(t => t.StartAsync(It.IsAny<Uri>(), It.IsAny<IChannelConnection<SendMessage, Message>>()))
                .Returns<Uri, IChannelConnection<SendMessage, Message>>((url, c) =>
                {
                    channel = c;
                    return Task.CompletedTask;
                });
            mockTransport.Setup(t => t.StopAsync())
                .Returns(() =>
                {
                    channel.Output.TryComplete();
                    return Task.CompletedTask;
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var closedTcs = new TaskCompletionSource<object>();
                var allowDisposeTcs = new TaskCompletionSource<object>();
                int receivedInvocationCount = 0;

                var connection = new Connection(new Uri("http://fakeuri.org/"));
                connection.Received +=
                    async (m, t) =>
                    {
                        if (Interlocked.Increment(ref receivedInvocationCount) == 2)
                        {
                            allowDisposeTcs.TrySetResult(null);
                        }
                        await closedTcs.Task;
                    };
                connection.Closed += e => closedTcs.SetResult(null);

                await connection.StartAsync(new TestTransportFactory(mockTransport.Object), httpClient);
                channel.Output.TryWrite(new Message());
                channel.Output.TryWrite(new Message());
                await allowDisposeTcs.Task.OrTimeout();
                await connection.DisposeAsync();
                Assert.Equal(2, receivedInvocationCount);
                // if the events were running on the main loop they would deadlock
                await closedTcs.Task.OrTimeout();
            }
        }

        [Fact]
        public async Task ClosedEventNotRaisedWhenTheClientIsStoppedButWasNeverStarted()
        {
            var connection = new Connection(new Uri("http://fakeuri.org/"));

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
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                try
                {
                    await connection.StartAsync(new TestTransportFactory(longPollingTransport), httpClient);

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
            var message = new Message(data, MessageType.Binary);
            var expectedPayload = FormatMessageToArray(message, MessageFormat.Binary);

            var sendTcs = new TaskCompletionSource<byte[]>();
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    if (request.RequestUri.AbsolutePath.EndsWith("/send"))
                    {
                        sendTcs.SetResult(await request.Content.ReadAsByteArrayAsync());
                    }
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                try
                {
                    await connection.StartAsync(TransportType.LongPolling, httpClient);

                    await connection.SendAsync(data, MessageType.Binary);

                    Assert.Equal(expectedPayload, await sendTcs.Task.OrTimeout());
                }
                finally
                {
                    await connection.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task SendAsyncThrowsIfConnectionIsNotStarted()
        {
            var connection = new Connection(new Uri("http://fakeuri.org/"));
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.SendAsync(new byte[0], MessageType.Binary));

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
                    if (request.RequestUri.AbsolutePath.EndsWith("/poll"))
                    {
                        content = "T2:T:42;";
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                await connection.StartAsync(TransportType.LongPolling, httpClient);
                await connection.DisposeAsync();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await connection.SendAsync(new byte[0], MessageType.Binary));

                Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
            }
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
                    if (request.RequestUri.AbsolutePath.EndsWith("/send"))
                    {
                        return ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError);
                    }
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                await connection.StartAsync(TransportType.LongPolling, httpClient);

                var exception = await Assert.ThrowsAsync<HttpRequestException>(
                    async () => await connection.SendAsync(new byte[0], MessageType.Binary));

                await connection.DisposeAsync();
            }
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
                    if (request.RequestUri.AbsolutePath.EndsWith("/poll"))
                    {
                        content = "T2:T:42;";
                    }
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK, MessageFormatter.TextContentType, content);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
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
                                receiveTcs.TrySetCanceled();
                            }
                        };

                    await connection.StartAsync(TransportType.LongPolling, httpClient);

                    Assert.Equal("42", await receiveTcs.Task.OrTimeout());
                }
                finally
                {
                    await connection.DisposeAsync();
                }
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
                    if (request.RequestUri.AbsolutePath.EndsWith("/poll"))
                    {
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(string.Empty) };
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                try
                {
                    var closeTcs = new TaskCompletionSource<Exception>();
                    connection.Closed += e => closeTcs.TrySetResult(e);

                    await connection.StartAsync(TransportType.LongPolling, httpClient);

                    // Exception in send should shutdown the connection
                    await closeTcs.Task.OrTimeout();

                    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await connection.SendAsync(new byte[0], MessageType.Binary));

                    Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
                }
                finally
                {
                    await connection.DisposeAsync();
                }
            }
        }

        private byte[] FormatMessageToArray(Message message, MessageFormat binary, int bufferSize = 1024)
        {
            var output = new ArrayOutput(bufferSize);
            output.Append('B', TextEncoder.Utf8);
            Assert.True(MessageFormatter.TryWriteMessage(message, output, binary));
            return output.ToArray();
        }
    }
}
