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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
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
                    await sseTransport.StartAsync(
                        new Uri("http://fakeuri.org"), TransferFormat.Text).OrTimeout();

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
                        .Returns<Stream, int, CancellationToken>(async (stream, bufferSize, t) =>
                        {
                            var buffer = Encoding.ASCII.GetBytes("data: 3:abc\r\n\r\n");
                            while (!t.IsCancellationRequested)
                            {
                                await stream.WriteAsync(buffer, 0, buffer.Length).OrTimeout();
                                await Task.Delay(100);
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
                    await sseTransport.StartAsync(
                        new Uri("http://fakeuri.org"), TransferFormat.Text).OrTimeout();

                    transportActiveTask = sseTransport.Running;
                    Assert.False(transportActiveTask.IsCompleted);
                    var message = await sseTransport.Input.ReadSingleAsync().OrTimeout();
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

                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), TransferFormat.Text).OrTimeout();

                var exception = await Assert.ThrowsAsync<FormatException>(() => sseTransport.Input.ReadAllAsync());

                await sseTransport.Running.OrTimeout();

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

                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), TransferFormat.Text).OrTimeout();
                await eventStreamTcs.Task;

                await sseTransport.Output.WriteAsync(new byte[] { 0x42 });

                var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sseTransport.Input.ReadAllAsync().OrTimeout());
                Assert.Contains("500", exception.Message);

                // Errors are only communicated through the pipe
                await sseTransport.Running.OrTimeout();
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

                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), TransferFormat.Text).OrTimeout();
                await eventStreamTcs.Task.OrTimeout();

                sseTransport.Output.Complete();

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

                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), TransferFormat.Text).OrTimeout();

                var message = await sseTransport.Input.ReadSingleAsync().OrTimeout();
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

                var ex = await Assert.ThrowsAsync<ArgumentException>(() => sseTransport.StartAsync(new Uri("http://fakeuri.org"), TransferFormat.Binary).OrTimeout());
                Assert.Equal($"The 'Binary' transfer format is not supported by this transport.{Environment.NewLine}Parameter name: transferFormat", ex.Message);
            }
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
                    sseTransport.StartAsync(new Uri("http://fakeuri.org"), transferFormat));

                Assert.Contains($"The '{transferFormat}' transfer format is not supported by this transport.", exception.Message);
                Assert.Equal("transferFormat", exception.ParamName);
            }
        }
    }
}
