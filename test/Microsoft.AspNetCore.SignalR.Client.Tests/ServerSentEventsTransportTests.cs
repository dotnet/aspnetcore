// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.AspNetCore.Sockets.Client.Internal;
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
                        new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Text, connection: Mock.Of<IConnection>()).OrTimeout();

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

        [Fact(Skip = "Flaky tests keep failing")]
        public async Task SSETransportStopsSendAndReceiveLoopsWhenTransportStopped()
        {
            var eventStreamCts = new CancellationTokenSource();
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
                {
                    var mockStream = new Mock<Stream>();
                    mockStream
                        .Setup(s => s.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Returns<Stream, int, CancellationToken>(async (stream, bufferSize, t) =>
                        {
                            await Task.Yield();
                            var buffer = Encoding.ASCII.GetBytes("data: 3:abc\r\n\r\n");
                            while (!eventStreamCts.IsCancellationRequested)
                            {
                                await stream.WriteAsync(buffer, 0, buffer.Length).OrTimeout();
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
                        new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Text, connection: Mock.Of<IConnection>()).OrTimeout();

                    transportActiveTask = sseTransport.Running;
                    Assert.False(transportActiveTask.IsCompleted);
                    var message = await pair.Transport.Input.ReadSingleAsync().OrTimeout();
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

                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Text, connection: Mock.Of<IConnection>()).OrTimeout();

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
                    new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Text, connection: Mock.Of<IConnection>()).OrTimeout();
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
                    new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Text, connection: Mock.Of<IConnection>()).OrTimeout();
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
                    new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Text, connection: Mock.Of<IConnection>()).OrTimeout();

                var message = await pair.Transport.Input.ReadSingleAsync().OrTimeout();
                Assert.Equal("3:abc", Encoding.ASCII.GetString(message));

                await sseTransport.Running.OrTimeout();
            }
        }

        [Fact]
        public async Task SSETransportDoesNotSupportBinary()
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

                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var ex = await Assert.ThrowsAsync<ArgumentException>(() => sseTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Binary, connection: Mock.Of<IConnection>()).OrTimeout());
                Assert.Equal($"The 'Binary' transfer format is not supported by this transport.{Environment.NewLine}Parameter name: transferFormat", ex.Message);
            }
        }

        [Fact]
        public async Task SSETransportSetsUserAgent()
        {
            HttpHeaderValueCollection<ProductInfoHeaderValue> userAgentHeaderCollection = null;

            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    userAgentHeaderCollection = request.Headers.UserAgent;
                    await Task.Yield();
                    return new HttpResponseMessage { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var sseTransport = new ServerSentEventsTransport(httpClient);

                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                await sseTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Text, connection: Mock.Of<IConnection>()).OrTimeout();
                await sseTransport.StopAsync().OrTimeout();
            }

            Assert.NotNull(userAgentHeaderCollection);
            var userAgentHeader = Assert.Single(userAgentHeaderCollection);
            Assert.Equal("Microsoft.AspNetCore.Sockets.Client.Http", userAgentHeader.Product.Name);

            // user agent version should come from version embedded in assembly metadata
            var assemblyVersion = typeof(Constants)
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            Assert.Equal(assemblyVersion.InformationalVersion, userAgentHeader.Product.Version);
        }

        [Theory]
        [InlineData(TransferFormat.Binary)] // Binary not supported
        [InlineData(TransferFormat.Text | TransferFormat.Binary)] // Multiple values not allowed
        [InlineData((TransferFormat)42)] // Unexpected value
        public async Task SSETransportThrowsForInvalidTransferFormat(TransferFormat transferFormat)
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
                    sseTransport.StartAsync(new Uri("http://fakeuri.org"), null, transferFormat, connection: Mock.Of<IConnection>()));

                Assert.Contains($"The '{transferFormat}' transfer format is not supported by this transport.", exception.Message);
                Assert.Equal("transferFormat", exception.ParamName);
            }
        }
    }
}
