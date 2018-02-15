// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
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
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                    await sseTransport.StartAsync(
                        new Uri("http://fakeuri.org"), pair.Application, TransferMode.Text, connection: Mock.Of<IConnection>()).OrTimeout();

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
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
                {
                    var mockStream = new Mock<Stream>();
                    mockStream
                        .Setup(s => s.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Returns<Stream, int, CancellationToken>(async (stream, bufferSize, token) =>
                        {
                            await Task.Yield();
                            var buffer = Encoding.ASCII.GetBytes("data: 3:abc\r\n\r\n");
                            while (!token.IsCancellationRequested)
                            {
                                await stream.WriteAsync(buffer, 0, buffer.Length, token).OrTimeout();
                            }
                        });
                    mockStream.Setup(s => s.CanRead).Returns(true);

                    return Task.FromResult(new HttpResponseMessage { Content = new StreamContent(mockStream.Object) });
                });

            Task transportActiveTask;

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var sseTransport = new ServerSentEventsTransport(httpClient);

                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

                    await sseTransport.StartAsync(
                        new Uri("http://fakeuri.org"), pair.Application, TransferMode.Text, connection: Mock.Of<IConnection>()).OrTimeout();

                    transportActiveTask = sseTransport.Running;
                    Assert.False(transportActiveTask.IsCompleted);
                    var message = await pair.Transport.Input.ReadSingleAsync().OrTimeout();
                    Assert.StartsWith("3:abc", Encoding.ASCII.GetString(message));
                }
                finally
                {
                    await sseTransport.StopAsync().OrTimeout();
                }

                await transportActiveTask.OrTimeout();
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

                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), pair.Application, TransferMode.Text, connection: Mock.Of<IConnection>()).OrTimeout();

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

                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), pair.Application, TransferMode.Text, connection: Mock.Of<IConnection>()).OrTimeout();
                await eventStreamTcs.Task;

                await pair.Transport.Output.WriteAsync(new byte[] { 0x42 });

                var exception = await Assert.ThrowsAsync<HttpRequestException>(() => pair.Transport.Input.ReadAllAsync().OrTimeout());
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

                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), pair.Application, TransferMode.Text, connection: Mock.Of<IConnection>()).OrTimeout();
                await eventStreamTcs.Task.OrTimeout();

                pair.Transport.Output.Complete();

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

                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), pair.Application, TransferMode.Text, connection: Mock.Of<IConnection>()).OrTimeout();

                var message = await pair.Transport.Input.ReadSingleAsync().OrTimeout();
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
                Assert.Null(sseTransport.Mode);

                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                await sseTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, transferMode, connection: Mock.Of<IConnection>()).OrTimeout();
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
                var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                    sseTransport.StartAsync(new Uri("http://fakeuri.org"), null, TransferMode.Text | TransferMode.Binary, connection: Mock.Of<IConnection>()));

                Assert.Contains("Invalid transfer mode.", exception.Message);
                Assert.Equal("requestedTransferMode", exception.ParamName);
            }
        }
    }
}
