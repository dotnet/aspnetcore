// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

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

                Assert.Equal(longPollingTransport.Running, await Task.WhenAny(Task.Delay(1000), longPollingTransport.Running));
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

                Assert.Equal(longPollingTransport.Running, await Task.WhenAny(Task.Delay(1000), longPollingTransport.Running));
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
                Assert.False(connection.Input.Completion.IsCompleted);

                var data = new byte[] { 1, 1, 2, 3, 5, 8 };
                connection.Output.TryWrite(
                    new Message(ReadableBuffer.Create(data).Preserve(), Format.Binary));

                Assert.Equal(sendTcs.Task, await Task.WhenAny(Task.Delay(1000), sendTcs.Task));
                Assert.Equal(data, sendTcs.Task.Result);
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
                Assert.False(connection.Input.Completion.IsCompleted);

                await connection.Input.WaitToReadAsync();
                Message message;
                connection.Input.TryRead(out message);
                using (message)
                {
                    Assert.Equal("42", Encoding.UTF8.GetString(message.Payload.Buffer.ToArray(), 0, message.Payload.Buffer.Length));
                }
            }
        }

        [Fact]
        public async Task CanCloseConnection()
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
                Assert.False(connection.Input.Completion.IsCompleted);
                connection.Output.TryComplete();

                var whenAnyTask = Task.WhenAny(Task.Delay(1000), connection.Input.Completion);

                // The channel needs to be drained for the Completion task to be completed
                Message message;
                while (!whenAnyTask.IsCompleted)
                {
                    connection.Input.TryRead(out message);
                    message.Dispose();
                }

                Assert.Equal(connection.Input.Completion, await whenAnyTask);
            }
        }
    }
}
