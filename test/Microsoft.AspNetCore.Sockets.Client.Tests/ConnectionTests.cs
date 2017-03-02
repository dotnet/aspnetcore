// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Microsoft.AspNetCore.SignalR.Tests.Common;

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
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                try
                {
                    await connection.StartAsync(longPollingTransport, httpClient);
                    var exception =
                        await Assert.ThrowsAsync<InvalidOperationException>(
                            async () => await connection.StartAsync(longPollingTransport));
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
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                await connection.StartAsync(longPollingTransport, httpClient);
                await connection.DisposeAsync();
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await connection.StartAsync(longPollingTransport));

                Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);
            }
        }

        [Fact]
        public async Task CannotStartDisposedConnection()
        {
            using (var httpClient = new HttpClient())
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                await connection.DisposeAsync();
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await connection.StartAsync(longPollingTransport));

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

                var startTask = connection.StartAsync(transport.Object, httpClient);
                await allowDisposeTcs.Task;
                var disposeTask = connection.DisposeAsync();
                // allow StartAsync to continue once DisposeAsync has started
                releaseNegotiateTcs.SetResult(null);

                // unblock DisposeAsync only after StartAsync completed
                await startTask.OrTimeout();
                releaseDisposeTcs.SetResult(null);
                await disposeTask.OrTimeout();

                transport.Verify(t => t.StartAsync(It.IsAny<Uri>(), It.IsAny<IChannelConnection<Message>>()), Times.Never);
            }
        }

        [Fact]
        public async Task SendReturnsFalseIfConnectionIsNotStarted()
        {
            var connection = new Connection(new Uri("http://fakeuri.org/"));
            Assert.False(await connection.SendAsync(new byte[0], MessageType.Binary));
        }

        [Fact]
        public async Task SendReturnsFalseIfConnectionIsDisposed()
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
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                await connection.StartAsync(longPollingTransport, httpClient);
                await connection.DisposeAsync();

                Assert.False(await connection.SendAsync(new byte[0], MessageType.Binary));
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
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                try
                {
                    var connectedEventRaised = false;
                    connection.Connected += () => connectedEventRaised = true;

                    await connection.StartAsync(longPollingTransport, httpClient);

                    Assert.True(connectedEventRaised);
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
            mockTransport.Setup(t => t.StartAsync(It.IsAny<Uri>(), It.IsAny<IChannelConnection<Message>>()))
                .Returns(Task.FromException(new InvalidOperationException("Transport failed to start")));

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                var connectedEventRaised = false;

                try
                {
                    connection.Connected += () => connectedEventRaised = true;

                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await connection.StartAsync(mockTransport.Object, httpClient));
                }
                finally
                {
                    await connection.DisposeAsync();
                }

                Assert.False(connectedEventRaised);
            }
        }

        [Fact]
        public async Task ClosedEventRaisedWhenTheClientIsStopped()
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
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                var closedEventTcs = new TaskCompletionSource<Exception>();
                connection.Closed += e => closedEventTcs.SetResult(e);

                await connection.StartAsync(longPollingTransport, httpClient);
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
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                var closedEventTcs = new TaskCompletionSource<Exception>();
                connection.Closed += e => closedEventTcs.TrySetResult(e);

                try
                {
                    await connection.StartAsync(longPollingTransport, httpClient);
                    Assert.IsType<HttpRequestException>(await closedEventTcs.Task.OrTimeout());
                }
                finally
                {
                    await connection.DisposeAsync();
                }
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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                try
                {
                    await connection.StartAsync(longPollingTransport, httpClient);

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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {

                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                try
                {
                    await connection.StartAsync(longPollingTransport, httpClient);

                    var data = new byte[] { 1, 1, 2, 3, 5, 8 };
                    await connection.SendAsync(data, MessageType.Binary);

                    Assert.Equal(data, await sendTcs.Task.OrTimeout());
                }
                finally
                {
                    await connection.DisposeAsync();
                }
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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {

                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
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

                    await connection.StartAsync(longPollingTransport, httpClient);

                    Assert.Equal("42", await receiveTcs.Task.OrTimeout());
                }
                finally
                {
                    await connection.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task CannotSendAfterConnectionIsStopped()
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
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));

                await connection.StartAsync(longPollingTransport, httpClient);
                await connection.DisposeAsync();
                Assert.False(await connection.SendAsync(new byte[] { 1, 1, 3, 5, 8 }, MessageType.Binary));
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
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                try
                {
                    var closeTcs = new TaskCompletionSource<Exception>();
                    connection.Closed += e => closeTcs.TrySetResult(e);

                    await connection.StartAsync(longPollingTransport, httpClient);

                    // Exception in send should shutdown the connection
                    await closeTcs.Task.OrTimeout();

                    Assert.False(await connection.SendAsync(new byte[] { 1, 1, 3, 5, 8 }, MessageType.Binary));
                }
                finally
                {
                    await connection.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task CannotReceiveAfterReceiveThrewException()
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
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                try
                {
                    var closeTcs = new TaskCompletionSource<Exception>();
                    connection.Closed += e => closeTcs.TrySetResult(e);

                    await connection.StartAsync(longPollingTransport, httpClient);

                    // Exception in send should shutdown the connection
                    await closeTcs.Task.OrTimeout();

                    Assert.False(await connection.SendAsync(new byte[] { 1, 1, 3, 5, 8 }, MessageType.Binary));
                }
                finally
                {
                    await connection.DisposeAsync();
                }
            }
        }
    }
}
