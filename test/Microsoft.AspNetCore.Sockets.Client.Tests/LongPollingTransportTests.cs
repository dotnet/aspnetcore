// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Microsoft.AspNetCore.Sockets.Internal;

namespace Microsoft.AspNetCore.Sockets.Client.Tests
{
    public class LongPollingTransportTests
    {
        [Fact]
        public async Task LongPollingTransportStopsPollAndSendLoopsWhenTransportDisposed()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                    {
                        await Task.Yield();
                        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                    });

            Task transportActiveTask;

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            {
                var connectionToTransport = Channel.CreateUnbounded<Message>();
                var transportToConnection = Channel.CreateUnbounded<Message>();
                var channelConnection = new ChannelConnection<Message>(connectionToTransport, transportToConnection);
                await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection);

                transportActiveTask = longPollingTransport.Running;

                Assert.False(transportActiveTask.IsCompleted);
            }

            Assert.Equal(transportActiveTask, await Task.WhenAny(Task.Delay(1000), transportActiveTask));
        }

        [Fact]
        public async Task LongPollingTransportStopsWhenPollReceives204()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return new HttpResponseMessage(HttpStatusCode.NoContent) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            {
                var connectionToTransport = Channel.CreateUnbounded<Message>();
                var transportToConnection = Channel.CreateUnbounded<Message>();
                var channelConnection = new ChannelConnection<Message>(connectionToTransport, transportToConnection);
                await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection);

                Assert.Equal(longPollingTransport.Running, await Task.WhenAny(Task.Delay(1000), longPollingTransport.Running));
                Assert.True(transportToConnection.In.Completion.IsCompleted);
            }
        }

        [Fact]
        public async Task LongPollingTransportStopsWhenPollRequestFails()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            {
                var connectionToTransport = Channel.CreateUnbounded<Message>();
                var transportToConnection = Channel.CreateUnbounded<Message>();
                var channelConnection = new ChannelConnection<Message>(connectionToTransport, transportToConnection);
                await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection);

                Assert.Equal(longPollingTransport.Running, await Task.WhenAny(Task.Delay(1000), longPollingTransport.Running));
                var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await transportToConnection.In.Completion);
                Assert.Contains(" 500 ", exception.Message);
            }
        }

        [Fact]
        public async Task LongPollingTransportStopsWhenSendRequestFails()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    var statusCode = request.RequestUri.AbsolutePath.EndsWith("send")
                        ? HttpStatusCode.InternalServerError
                        : HttpStatusCode.OK;
                    return new HttpResponseMessage(statusCode) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            {
                var connectionToTransport = Channel.CreateUnbounded<Message>();
                var transportToConnection = Channel.CreateUnbounded<Message>();
                var channelConnection = new ChannelConnection<Message>(connectionToTransport, transportToConnection);
                await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection);

                await connectionToTransport.Out.WriteAsync(new Message());

                Assert.Equal(longPollingTransport.Running, await Task.WhenAny(Task.Delay(1000), longPollingTransport.Running));

                await Assert.ThrowsAsync<HttpRequestException>(async () => await longPollingTransport.Running);

                // The channel needs to be drained for the Completion task to be completed
                Message message;
                while (transportToConnection.In.TryRead(out message))
                {
                    message.Dispose();
                }

                var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await transportToConnection.In.Completion);
                Assert.Contains(" 500 ", exception.Message);
            }
        }

        [Fact]
        public async Task LongPollingTransportShutsDownWhenChannelIsClosed()
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
                var connectionToTransport = Channel.CreateUnbounded<Message>();
                var transportToConnection = Channel.CreateUnbounded<Message>();
                var channelConnection = new ChannelConnection<Message>(connectionToTransport, transportToConnection);
                await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection);

                connectionToTransport.Out.Complete();

                Assert.Equal(longPollingTransport.Running, await Task.WhenAny(Task.Delay(1000), longPollingTransport.Running));
                Assert.Equal(connectionToTransport.In.Completion, await Task.WhenAny(Task.Delay(1000), connectionToTransport.In.Completion));
            }
        }
    }
}
