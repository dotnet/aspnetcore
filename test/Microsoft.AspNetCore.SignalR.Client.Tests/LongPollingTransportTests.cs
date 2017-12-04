// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Internal;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.Client.Tests
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
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                    });

            Task transportActiveTask;

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);

                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, TransferMode.Binary, connectionId: string.Empty);

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
                    return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {

                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = ChannelConnection.Create(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, TransferMode.Binary, connectionId: string.Empty);

                    await longPollingTransport.Running.OrTimeout();
                    Assert.True(transportToConnection.Reader.Completion.IsCompleted);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Fact]
        public async Task LongPollingTransportResponseWithNoContentDoesNotStopPoll()
        {
            int requests = 0;
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    if (requests == 0)
                    {
                        requests++;
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK, "Hello");
                    }
                    else if (requests == 1)
                    {
                        requests++;
                        // Time out
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                    }
                    else if (requests == 2)
                    {
                        requests++;
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK, "World");
                    }

                    // Done
                    return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {

                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, TransferMode.Binary, connectionId: string.Empty);

                    var data = await transportToConnection.Reader.ReadAllAsync().OrTimeout();
                    await longPollingTransport.Running.OrTimeout();
                    Assert.True(transportToConnection.Reader.Completion.IsCompleted);
                    Assert.Equal(2, data.Count);
                    Assert.Equal(Encoding.UTF8.GetBytes("Hello"), data[0]);
                    Assert.Equal(Encoding.UTF8.GetBytes("World"), data[1]);
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
                    return ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, TransferMode.Binary, connectionId: string.Empty);

                    var exception =
                        await Assert.ThrowsAsync<HttpRequestException>(async () => await transportToConnection.Reader.Completion.OrTimeout());
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
                    var statusCode = request.Method == HttpMethod.Post
                        ? HttpStatusCode.InternalServerError
                        : HttpStatusCode.OK;
                    return ResponseUtils.CreateResponse(statusCode);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, TransferMode.Binary, connectionId: string.Empty);

                    await connectionToTransport.Writer.WriteAsync(new SendMessage());

                    await Assert.ThrowsAsync<HttpRequestException>(async () => await longPollingTransport.Running.OrTimeout());

                    // The channel needs to be drained for the Completion task to be completed
                    while (transportToConnection.Reader.TryRead(out var message))
                    {
                    }

                    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await transportToConnection.Reader.Completion);
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
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, TransferMode.Binary, connectionId: string.Empty);

                    connectionToTransport.Writer.Complete();

                    await longPollingTransport.Running.OrTimeout();

                    await longPollingTransport.Running.OrTimeout();
                    await connectionToTransport.Reader.Completion.OrTimeout();
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Fact]
        public async Task LongPollingTransportDispatchesMessagesReceivedFromPoll()
        {
            var message1Payload = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };

            var firstCall = true;
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            var sentRequests = new List<HttpRequestMessage>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    sentRequests.Add(request);

                    await Task.Yield();

                    if (firstCall)
                    {
                        firstCall = false;
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK, message1Payload);
                    }

                    return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);

                    // Start the transport
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, TransferMode.Binary, connectionId: string.Empty);

                    // Wait for the transport to finish
                    await longPollingTransport.Running.OrTimeout();

                    // Pull Messages out of the channel
                    var messages = new List<byte[]>();
                    while (await transportToConnection.Reader.WaitToReadAsync())
                    {
                        while (transportToConnection.Reader.TryRead(out var message))
                        {
                            messages.Add(message);
                        }
                    }

                    // Check the provided request
                    Assert.Equal(2, sentRequests.Count);

                    // Check the messages received
                    Assert.Single(messages);
                    Assert.Equal(message1Payload, messages[0]);
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
                    if (request.Method == HttpMethod.Post)
                    {
                        // Build a new request object, but convert the entire payload to string
                        sentRequests.Add(await request.Content.ReadAsByteArrayAsync());
                    }
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);

                    var tcs1 = new TaskCompletionSource<object>();
                    var tcs2 = new TaskCompletionSource<object>();

                    // Pre-queue some messages
                    await connectionToTransport.Writer.WriteAsync(new SendMessage(Encoding.UTF8.GetBytes("Hello"), tcs1)).OrTimeout();
                    await connectionToTransport.Writer.WriteAsync(new SendMessage(Encoding.UTF8.GetBytes("World"), tcs2)).OrTimeout();

                    // Start the transport
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, TransferMode.Binary, connectionId: string.Empty);

                    connectionToTransport.Writer.Complete();

                    await longPollingTransport.Running.OrTimeout();
                    await connectionToTransport.Reader.Completion.OrTimeout();

                    Assert.Single(sentRequests);
                    Assert.Equal(new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)'W', (byte)'o', (byte)'r', (byte)'l', (byte)'d'
                    }, sentRequests[0]);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Theory]
        [InlineData(TransferMode.Binary)]
        [InlineData(TransferMode.Text)]
        public async Task LongPollingTransportSetsTransferMode(TransferMode transferMode)
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
                var longPollingTransport = new LongPollingTransport(httpClient);

                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);

                    Assert.Null(longPollingTransport.Mode);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, transferMode, connectionId: string.Empty);
                    Assert.Equal(transferMode, longPollingTransport.Mode);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Fact]
        public async Task LongPollingTransportThrowsForInvalidTransferMode()
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
                var longPollingTransport = new LongPollingTransport(httpClient);
                var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                    longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), null, TransferMode.Text | TransferMode.Binary, connectionId: string.Empty));

                Assert.Contains("Invalid transfer mode.", exception.Message);
                Assert.Equal("requestedTransferMode", exception.ParamName);
            }
        }

        [Fact]
        public async Task LongPollingTransportRePollsIfRequestCancelled()
        {
            var numPolls = 0;
            var completionTcs = new TaskCompletionSource<object>();

            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    if (Interlocked.Increment(ref numPolls) < 3)
                    {
                        throw new OperationCanceledException();
                    }

                    completionTcs.SetResult(null);
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);

                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, TransferMode.Binary, connectionId: string.Empty);

                    var completedTask = await Task.WhenAny(completionTcs.Task, longPollingTransport.Running).OrTimeout();
                    Assert.Equal(completionTcs.Task, completedTask);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }
    }
}
