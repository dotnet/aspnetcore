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
        public async Task ConnectionReturnsUrlUsedToStartTheConnection()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            var connectionUrl = new Uri("http://fakeuri.org/");
            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            {
                using (var connection = await Connection.ConnectAsync(connectionUrl, longPollingTransport, httpClient))
                {
                    Assert.Equal(connectionUrl, connection.Url);
                }

                await longPollingTransport.Running.OrTimeout();
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
            using (var connection = await Connection.ConnectAsync(new Uri("http://fakeuri.org/"), longPollingTransport, httpClient))
            {
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
                using (var connection = await Connection.ConnectAsync(new Uri("http://fakeuri.org/"), longPollingTransport, httpClient))
                {
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
            using (var connection = await Connection.ConnectAsync(new Uri("http://fakeuri.org/"), longPollingTransport, httpClient))
            {
                var data = new byte[] { 1, 1, 2, 3, 5, 8 };
                await connection.SendAsync(data, Format.Binary);

                Assert.Equal(data, await sendTcs.Task.OrTimeout());
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
            using (var connection = await Connection.ConnectAsync(new Uri("http://fakeuri.org/"), longPollingTransport, httpClient))
            {
                var receiveData = new ReceiveData();
                Assert.True(await connection.ReceiveAsync(receiveData));
                Assert.Equal("42", Encoding.UTF8.GetString(receiveData.Data));
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
            using (var connection = await Connection.ConnectAsync(new Uri("http://fakeuri.org/"), longPollingTransport, httpClient))
            {
                await connection.StopAsync();
                Assert.False(await connection.SendAsync(new byte[] { 1, 1, 3, 5, 8 }, Format.Binary));
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
            using (var connection = await Connection.ConnectAsync(new Uri("http://fakeuri.org/"), longPollingTransport, httpClient))
            {
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
            using (var connection = await Connection.ConnectAsync(new Uri("http://fakeuri.org/"), longPollingTransport, httpClient))
            {
                var receiveTask = connection.ReceiveAsync(new ReceiveData());
                allowPollTcs.TrySetResult(null);
                await Assert.ThrowsAsync<HttpRequestException>(async () => await receiveTask);

                Assert.False(await connection.SendAsync(new byte[] { 1, 1, 3, 5, 8 }, Format.Binary));
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
            using (var connection = await Connection.ConnectAsync(new Uri("http://fakeuri.org/"), longPollingTransport, httpClient))
            {
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
