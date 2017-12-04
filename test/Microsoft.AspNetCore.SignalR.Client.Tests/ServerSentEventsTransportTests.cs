// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Internal;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class ServerSentEventsTransportTests
    {
        [Fact]
        public async Task CanStartStopSSETransport()
        {
            var eventStreamTcs = new TaskCompletionSource<object>();
            var copyToAsyncTcs = new TaskCompletionSource<int>();

            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    // Receive loop started - allow stopping the transport
                    eventStreamTcs.SetResult(null);

                    // returns unfinished task to block pipelines
                    var mockStream = new Mock<Stream>();
                    mockStream
                        .Setup(s => s.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Returns(copyToAsyncTcs.Task);
                    mockStream.Setup(s => s.CanRead).Returns(true);
                    return new HttpResponseMessage { Content = new StreamContent(mockStream.Object) };
                });

            try
            {
                using (var httpClient = new HttpClient(mockHttpHandler.Object))
                {
                    var sseTransport = new ServerSentEventsTransport(httpClient);
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                    await sseTransport.StartAsync(
                        new Uri("http://fakeuri.org"), channelConnection, TransferMode.Text, connectionId: string.Empty).OrTimeout();

                    await eventStreamTcs.Task.OrTimeout();
                    await sseTransport.StopAsync().OrTimeout();
                    await sseTransport.Running.OrTimeout();
                }
            }
            finally
            {
                copyToAsyncTcs.SetResult(0);
            }
        }

        [Fact]
        public async Task SSETransportStopsSendAndReceiveLoopsWhenTransportStopped()
        {
            var eventStreamCts = new CancellationTokenSource();
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    var mockStream = new Mock<Stream>();
                    mockStream
                        .Setup(s => s.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Returns<Stream, int, CancellationToken>(async (stream, bufferSize, t) =>
                            {
                                await Task.Yield();
                                var buffer = Encoding.ASCII.GetBytes("data: 3:abc\r\n\r\n");
                                while (!eventStreamCts.IsCancellationRequested)
                                {
                                    await stream.WriteAsync(buffer, 0, buffer.Length);
                                }
                            });
                    mockStream.Setup(s => s.CanRead).Returns(true);

                    return new HttpResponseMessage { Content = new StreamContent(mockStream.Object) };
                });

            Task transportActiveTask;

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var sseTransport = new ServerSentEventsTransport(httpClient);

                try
                {
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                    await sseTransport.StartAsync(
                        new Uri("http://fakeuri.org"), channelConnection, TransferMode.Text, connectionId: string.Empty).OrTimeout();

                    transportActiveTask = sseTransport.Running;
                    Assert.False(transportActiveTask.IsCompleted);
                    var message = await transportToConnection.Reader.ReadAsync().AsTask().OrTimeout();
                    Assert.Equal("3:abc", Encoding.ASCII.GetString(message));
                }
                finally
                {
                    await sseTransport.StopAsync().OrTimeout();
                }

                await transportActiveTask.OrTimeout();
                eventStreamCts.Cancel();
            }
        }

        [Fact]
        public async Task SSETransportStopsWithErrorIfServerSendsIncompleteResults()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    var mockStream = new Mock<Stream>();
                    mockStream
                        .Setup(s => s.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Returns<Stream, int, CancellationToken>(async (stream, bufferSize, t) =>
                        {
                            var buffer = Encoding.ASCII.GetBytes("data: 3:a");
                            await stream.WriteAsync(buffer, 0, buffer.Length);
                        });
                    mockStream.Setup(s => s.CanRead).Returns(true);

                    return new HttpResponseMessage { Content = new StreamContent(mockStream.Object) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var sseTransport = new ServerSentEventsTransport(httpClient);

                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), channelConnection, TransferMode.Text, connectionId: string.Empty).OrTimeout();

                var exception = await Assert.ThrowsAsync<FormatException>(() => sseTransport.Running.OrTimeout());
                Assert.Equal("Incomplete message.", exception.Message);
            }
        }

        [Fact]
        public async Task SSETransportStopsWithErrorIfSendingMessageFails()
        {
            var eventStreamTcs = new TaskCompletionSource<object>();
            var copyToAsyncTcs = new TaskCompletionSource<int>();

            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    if (request.Headers.Accept?.Contains(new MediaTypeWithQualityHeaderValue("text/event-stream")) == true)
                    {
                        // Receive loop started - allow stopping the transport
                        eventStreamTcs.SetResult(null);

                        // returns unfinished task to block pipelines
                        var mockStream = new Mock<Stream>();
                        mockStream
                            .Setup(s => s.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                            .Returns(copyToAsyncTcs.Task);
                        mockStream.Setup(s => s.CanRead).Returns(true);
                        return new HttpResponseMessage { Content = new StreamContent(mockStream.Object) };
                    }

                    return ResponseUtils.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var sseTransport = new ServerSentEventsTransport(httpClient);

                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);

                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), channelConnection, TransferMode.Text, connectionId: string.Empty).OrTimeout();
                await eventStreamTcs.Task;

                var sendTcs = new TaskCompletionSource<object>();
                Assert.True(connectionToTransport.Writer.TryWrite(new SendMessage(new byte[] { 0x42 }, sendTcs)));

                var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sendTcs.Task.OrTimeout());
                Assert.Contains("500", exception.Message);

                Assert.Same(exception, await Assert.ThrowsAsync<HttpRequestException>(() => sseTransport.Running.OrTimeout()));
            }
        }

        [Fact]
        public async Task SSETransportStopsIfChannelClosed()
        {
            var eventStreamTcs = new TaskCompletionSource<object>();
            var copyToAsyncTcs = new TaskCompletionSource<int>();

            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    // Receive loop started - allow stopping the transport
                    eventStreamTcs.SetResult(null);

                    // returns unfinished task to block pipelines
                    var mockStream = new Mock<Stream>();
                    mockStream
                        .Setup(s => s.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Returns(copyToAsyncTcs.Task);
                    mockStream.Setup(s => s.CanRead).Returns(true);
                    return new HttpResponseMessage { Content = new StreamContent(mockStream.Object) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var sseTransport = new ServerSentEventsTransport(httpClient);

                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);

                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), channelConnection, TransferMode.Text, connectionId: string.Empty).OrTimeout();
                await eventStreamTcs.Task.OrTimeout();

                connectionToTransport.Writer.TryComplete(null);

                await sseTransport.Running.OrTimeout();
            }
        }

        [Fact]
        public async Task SSETransportStopsIfTheServerClosesTheStream()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return new HttpResponseMessage { Content = new StringContent("data: 3:abc\r\n\r\n") };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var sseTransport = new ServerSentEventsTransport(httpClient);

                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), channelConnection, TransferMode.Text, connectionId: string.Empty).OrTimeout();

                var message = await transportToConnection.Reader.ReadAsync().AsTask().OrTimeout();
                Assert.Equal("3:abc", Encoding.ASCII.GetString(message));

                await sseTransport.Running.OrTimeout();
            }
        }

        [Theory]
        [InlineData(TransferMode.Text)]
        [InlineData(TransferMode.Binary)]
        public async Task SSETransportSetsTransferMode(TransferMode transferMode)
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return new HttpResponseMessage { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var sseTransport = new ServerSentEventsTransport(httpClient);
                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                Assert.Null(sseTransport.Mode);
                await sseTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, transferMode, connectionId: string.Empty).OrTimeout();
                Assert.Equal(TransferMode.Text, sseTransport.Mode);
                await sseTransport.StopAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task SSETransportThrowsForInvalidTransferMode()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return new HttpResponseMessage { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var sseTransport = new ServerSentEventsTransport(httpClient);
                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                    sseTransport.StartAsync(new Uri("http://fakeuri.org"), null, TransferMode.Text | TransferMode.Binary, connectionId: string.Empty));

                Assert.Contains("Invalid transfer mode.", exception.Message);
                Assert.Equal("requestedTransferMode", exception.ParamName);
            }
        }
    }
}
