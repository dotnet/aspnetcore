// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Client.Tests
{
    public class LongPollingTransportTests
    {
        [Fact]
        public async Task LongPollingTransportStopsPollAndSendLoopsWhenTransportStopped()
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
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());

                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<Message>();
                    var channelConnection = new ChannelConnection<SendMessage, Message>(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection);

                    transportActiveTask = longPollingTransport.Running;

                    Assert.False(transportActiveTask.IsCompleted);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }

                await transportActiveTask.OrTimeout();
            }
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
            {

                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<Message>();
                    var channelConnection = new ChannelConnection<SendMessage, Message>(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection);

                    await longPollingTransport.Running.OrTimeout();
                    Assert.True(transportToConnection.In.Completion.IsCompleted);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
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
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<Message>();
                    var channelConnection = new ChannelConnection<SendMessage, Message>(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection);

                    var exception =
                        await Assert.ThrowsAsync<HttpRequestException>(async () => await transportToConnection.In.Completion.OrTimeout());
                    Assert.Contains(" 500 ", exception.Message);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
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
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<Message>();
                    var channelConnection = new ChannelConnection<SendMessage, Message>(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection);

                    await connectionToTransport.Out.WriteAsync(new SendMessage());

                    await Assert.ThrowsAsync<HttpRequestException>(async () => await longPollingTransport.Running.OrTimeout());

                    // The channel needs to be drained for the Completion task to be completed
                    while (transportToConnection.In.TryRead(out Message message))
                    {
                    }

                    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await transportToConnection.In.Completion);
                    Assert.Contains(" 500 ", exception.Message);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
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
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<Message>();
                    var channelConnection = new ChannelConnection<SendMessage, Message>(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection);

                    connectionToTransport.Out.Complete();

                    await longPollingTransport.Running.OrTimeout();

                    await longPollingTransport.Running.OrTimeout();
                    await connectionToTransport.In.Completion.OrTimeout();
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Fact]
        public async Task LongPollingTransportSendsAvailableMessagesWhenTheyArrive()
        {
            var sentRequests = new List<byte[]>();

            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    if (request.RequestUri.LocalPath.EndsWith("send"))
                    {
                        // Build a new request object, but convert the entire payload to string
                        sentRequests.Add(await request.Content.ReadAsByteArrayAsync());
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<Message>();
                    var channelConnection = new ChannelConnection<SendMessage, Message>(connectionToTransport, transportToConnection);

                    var tcs1 = new TaskCompletionSource<object>();
                    var tcs2 = new TaskCompletionSource<object>();

                    // Pre-queue some messages
                    await connectionToTransport.Out.WriteAsync(new SendMessage(Encoding.UTF8.GetBytes("Hello"), MessageType.Text, tcs1)).OrTimeout();
                    await connectionToTransport.Out.WriteAsync(new SendMessage(Encoding.UTF8.GetBytes("World"), MessageType.Binary, tcs2)).OrTimeout();

                    // Start the transport
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection);

                    connectionToTransport.Out.Complete();

                    await longPollingTransport.Running.OrTimeout();
                    await connectionToTransport.In.Completion.OrTimeout();

                    Assert.Equal(1, sentRequests.Count);
                    Assert.Equal(new byte[] {
                        (byte)'B',
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o',
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x01, (byte)'W', (byte)'o', (byte)'r', (byte)'l', (byte)'d'
                    }, sentRequests[0]);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }
    }
}
