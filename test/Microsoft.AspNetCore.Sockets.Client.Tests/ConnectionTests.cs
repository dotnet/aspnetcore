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
        public void CanDisposeNotStartedConnection()
        {
            using (new Connection(new Uri("http://fakeuri.org"))) { }
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                await connection.StartAsync(longPollingTransport, httpClient);
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await connection.StartAsync(longPollingTransport));
                Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);

                await connection.StopAsync();
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                await connection.StartAsync(longPollingTransport, httpClient);
                await connection.StopAsync();
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                connection.Dispose();
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await connection.StartAsync(longPollingTransport));

                Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);
            }
        }

        [Fact]
        public async Task SendReturnsFalseIfConnectionIsNotStarted()
        {
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                Assert.False(await connection.SendAsync(new byte[0], MessageType.Binary));
            }
        }

        [Fact]
        public async Task SendReturnsFalseIfConnectionIsStopped()
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                await connection.StartAsync(longPollingTransport, httpClient);
                await connection.StopAsync();

                Assert.False(await connection.SendAsync(new byte[0], MessageType.Binary));
            }
        }

        [Fact]
        public async Task SendReturnsFalseIfConnectionIsDisposed()
        {
            var connection = new Connection(new Uri("http://fakeuri.org/"));
            connection.Dispose();
            Assert.False(await connection.SendAsync(new byte[0], MessageType.Binary));
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                var connectedEventRaised = false;
                connection.Connected += () => connectedEventRaised = true;

                await connection.StartAsync(longPollingTransport, httpClient);

                Assert.True(connectedEventRaised);
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
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                var connectedEventRaised = false;
                connection.Connected += () => connectedEventRaised = true;

                await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await connection.StartAsync(mockTransport.Object, httpClient));

                Assert.False(connectedEventRaised);
            }
        }

        [Fact(Skip = "Need draining to make it work. Receive event may fix that.")]
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                var closedEventTcs = new TaskCompletionSource<Exception>();
                connection.Closed += e => closedEventTcs.SetResult(e);

                await connection.StartAsync(longPollingTransport, httpClient);
                await connection.StopAsync();

                
                Assert.Equal(closedEventTcs.Task, await Task.WhenAny(Task.Delay(1000), closedEventTcs.Task));
                // in case of clean disconnect error should be null
                Assert.Null(await closedEventTcs.Task);
            }
        }

        [Fact(Skip = "Need draining to make it work. Receive event may fix that.")]
        public async Task ClosedEventRaisedWhenTheClientIsDisposed()
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            {
                var connection = new Connection(new Uri("http://fakeuri.org/"));
                var closedEventTcs = new TaskCompletionSource<Exception>();
                connection.Closed += e => closedEventTcs.TrySetResult(e);

                using (connection)
                {
                    await connection.StartAsync(longPollingTransport, httpClient);
                }

                Assert.Equal(closedEventTcs.Task, await Task.WhenAny(Task.Delay(1000), closedEventTcs.Task));
                Assert.Null(await closedEventTcs.Task);
            }
        }

        [Fact]
        public async Task ClosedEventRaisedWhenConnectionToServerLost()
        {
            var allowPollTcs = new TaskCompletionSource<object>();
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    if (request.RequestUri.AbsolutePath.EndsWith("/poll"))
                    {
                        await allowPollTcs.Task;
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(string.Empty) };
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                var closedEventTcs = new TaskCompletionSource<Exception>();
                connection.Closed += e => closedEventTcs.TrySetResult(e);
                await connection.StartAsync(longPollingTransport, httpClient);

                var receiveTask = connection.ReceiveAsync(new ReceiveData());
                allowPollTcs.TrySetResult(null);
                await Assert.ThrowsAsync<HttpRequestException>(async () => await receiveTask);

                Assert.Equal(closedEventTcs.Task, await Task.WhenAny(Task.Delay(1000), closedEventTcs.Task));
                Assert.IsType<HttpRequestException>(await closedEventTcs.Task);
            }
        }

        [Fact]
        public async Task ClosedEventNotRaisedWhenTheClientIsStoppedButWasNeverStarted()
        {
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                bool closedEventRaised = false;
                connection.Closed += e => closedEventRaised = true;

                await connection.StopAsync();
                Assert.False(closedEventRaised);
            }
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                await connection.StartAsync(longPollingTransport, httpClient);

                Assert.False(longPollingTransport.Running.IsCompleted);

                await connection.StopAsync();

                await longPollingTransport.Running.OrTimeout();
            }
        }

        [Fact]
        public async Task TransportIsClosedWhenConnectionIsDisposed()
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            {
                using (var connection = new Connection(new Uri("http://fakeuri.org/")))
                {
                    await connection.StartAsync(longPollingTransport, httpClient);

                    Assert.False(longPollingTransport.Running.IsCompleted);
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                await connection.StartAsync(longPollingTransport, httpClient);

                var data = new byte[] { 1, 1, 2, 3, 5, 8 };
                await connection.SendAsync(data, MessageType.Binary);

                Assert.Equal(data, await sendTcs.Task.OrTimeout());

                await connection.StopAsync();
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
                        content = "42";
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                await connection.StartAsync(longPollingTransport, httpClient);

                var receiveData = new ReceiveData();
                Assert.True(await connection.ReceiveAsync(receiveData));
                Assert.Equal("42", Encoding.UTF8.GetString(receiveData.Data));

                await connection.StopAsync();
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                await connection.StartAsync(longPollingTransport, httpClient);
                await connection.StopAsync();
                Assert.False(await connection.SendAsync(new byte[] { 1, 1, 3, 5, 8 }, MessageType.Binary));
            }
        }

        [Fact]
        public async Task CannotReceiveAfterConnectionIsStopped()
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                await connection.StartAsync(longPollingTransport, httpClient);

                await connection.StopAsync();
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await connection.ReceiveAsync(new ReceiveData()));

                Assert.Equal("Cannot receive messages when the connection is stopped.", exception.Message);
            }
        }

        [Fact]
        public async Task CannotSendAfterReceiveThrewException()
        {
            var allowPollTcs = new TaskCompletionSource<object>();
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    if (request.RequestUri.AbsolutePath.EndsWith("/poll"))
                    {
                        await allowPollTcs.Task;
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(string.Empty) };
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                await connection.StartAsync(longPollingTransport, httpClient);

                var receiveTask = connection.ReceiveAsync(new ReceiveData());
                allowPollTcs.TrySetResult(null);
                await Assert.ThrowsAsync<HttpRequestException>(async () => await receiveTask);

                Assert.False(await connection.SendAsync(new byte[] { 1, 1, 3, 5, 8 }, MessageType.Binary));
            }
        }

        [Fact]
        public async Task CannotReceiveAfterReceiveThrewException()
        {
            var allowPollTcs = new TaskCompletionSource<object>();
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    if (request.RequestUri.AbsolutePath.EndsWith("/poll"))
                    {
                        await allowPollTcs.Task;
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(string.Empty) };
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var connection = new Connection(new Uri("http://fakeuri.org/")))
            {
                await connection.StartAsync(longPollingTransport, httpClient);

                var receiveTask = connection.ReceiveAsync(new ReceiveData());
                allowPollTcs.TrySetResult(null);
                await Assert.ThrowsAsync<HttpRequestException>(async () => await receiveTask);

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await connection.ReceiveAsync(new ReceiveData()));

                Assert.Equal("Cannot receive messages when the connection is stopped.", exception.Message);
            }
        }
    }
}
