// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Moq.Protected;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public class ServerSentEventsTransportTests : VerifiableLoggedTest
{
    [Fact]
    public async Task CanStartStopSSETransport()
    {
        var eventStreamTcs = new TaskCompletionSource();
        var copyToAsyncTcs = new TaskCompletionSource();

        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();
                // Receive loop started - allow stopping the transport
                eventStreamTcs.SetResult();

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
            using (StartVerifiableLog())
            {
                var sseTransport = new ServerSentEventsTransport(httpClient, loggerFactory: LoggerFactory);
                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), TransferFormat.Text).DefaultTimeout();

                await eventStreamTcs.Task.DefaultTimeout();
                await sseTransport.StopAsync().DefaultTimeout();
                await sseTransport.Running.DefaultTimeout();
            }
        }
        finally
        {
            copyToAsyncTcs.SetResult();
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
                    .Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                    .Returns<Memory<byte>, CancellationToken>(async (data, t) =>
                    {
                        if (t.IsCancellationRequested)
                        {
                            return 0;
                        }

                        int count = Encoding.ASCII.GetBytes("data: 3:abc\r\n\r\n", data.Span);
                        await Task.Delay(100);
                        return count;
                    });
                mockStream.Setup(s => s.CanRead).Returns(true);

                return Task.FromResult(new HttpResponseMessage { Content = new StreamContent(mockStream.Object) });
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        using (StartVerifiableLog())
        {
            var sseTransport = new ServerSentEventsTransport(httpClient, loggerFactory: LoggerFactory);

            Task transportActiveTask;
            try
            {
                await sseTransport.StartAsync(
                    new Uri("http://fakeuri.org"), TransferFormat.Text).DefaultTimeout();

                transportActiveTask = sseTransport.Running;
                Assert.False(transportActiveTask.IsCompleted);
                var message = await sseTransport.Input.ReadSingleAsync().DefaultTimeout();
                Assert.StartsWith("3:abc", Encoding.ASCII.GetString(message));
            }
            finally
            {
                await sseTransport.StopAsync().DefaultTimeout();
            }

            await transportActiveTask.DefaultTimeout();
        }
    }

    [Fact]
    public async Task SSETransportStopIfServerSendsIncompleteResults()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var calls = 0;
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();

                var mockStream = new Mock<Stream>();
                mockStream
                    .Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                    .Returns<Memory<byte>, CancellationToken>((data, t) =>
                    {
                        if (calls == 0)
                        {
                            calls++;
                            return new ValueTask<int>(Encoding.ASCII.GetBytes("data: 3:a", data.Span));
                        }
                        return new ValueTask<int>(0);
                    });
                mockStream.Setup(s => s.CanRead).Returns(true);

                return new HttpResponseMessage { Content = new StreamContent(mockStream.Object) };
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        using (StartVerifiableLog())
        {
            var sseTransport = new ServerSentEventsTransport(httpClient, loggerFactory: LoggerFactory);

            await sseTransport.StartAsync(
                new Uri("http://fakeuri.org"), TransferFormat.Text).DefaultTimeout();

            await sseTransport.Input.ReadAllAsync().DefaultTimeout();

            await sseTransport.Running.DefaultTimeout();
        }
    }

    [Fact]
    public async Task SSETransportStopsWithErrorIfSendingMessageFails()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == typeof(ServerSentEventsTransport).FullName &&
                   writeContext.EventId.Name == "ErrorSending";
        }

        var eventStreamTcs = new TaskCompletionSource();
        var readTcs = new TaskCompletionSource<int>();

        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();

                if (request.Headers.Accept?.Contains(new MediaTypeWithQualityHeaderValue("text/event-stream")) == true)
                {
                    // Receive loop started - allow stopping the transport
                    eventStreamTcs.SetResult();

                    // returns unfinished task to block pipelines
                    var mockStream = new Mock<Stream>();
                    mockStream
                        .Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                        .Returns<Memory<byte>, CancellationToken>(async (data, ct) =>
                        {
                            using (ct.Register(() => readTcs.TrySetCanceled()))
                            {
                                return await readTcs.Task;
                            }
                        });
                    mockStream.Setup(s => s.CanRead).Returns(true);
                    return new HttpResponseMessage { Content = new StreamContent(mockStream.Object) };
                }

                return ResponseUtils.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
        {
            var sseTransport = new ServerSentEventsTransport(httpClient, loggerFactory: LoggerFactory);

            await sseTransport.StartAsync(
                new Uri("http://fakeuri.org"), TransferFormat.Text).DefaultTimeout();
            await eventStreamTcs.Task;

            await sseTransport.Output.WriteAsync(new byte[] { 0x42 });

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sseTransport.Input.ReadAllAsync().DefaultTimeout());
            Assert.Contains("500", exception.Message);

            // Errors are only communicated through the pipe
            await sseTransport.Running.DefaultTimeout();
        }
    }

    [Fact]
    public async Task SSETransportStopsIfChannelClosed()
    {
        var eventStreamTcs = new TaskCompletionSource();
        var readTcs = new TaskCompletionSource<int>();

        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();

                // Receive loop started - allow stopping the transport
                eventStreamTcs.SetResult();

                // returns unfinished task to block pipelines
                var mockStream = new Mock<Stream>();
                mockStream
                        .Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                        .Returns<Memory<byte>, CancellationToken>(async (data, ct) =>
                        {
                            using (ct.Register(() => readTcs.TrySetCanceled()))
                            {
                                return await readTcs.Task;
                            }
                        });
                mockStream.Setup(s => s.CanRead).Returns(true);
                return new HttpResponseMessage { Content = new StreamContent(mockStream.Object) };
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        using (StartVerifiableLog())
        {
            var sseTransport = new ServerSentEventsTransport(httpClient, loggerFactory: LoggerFactory);

            await sseTransport.StartAsync(
                new Uri("http://fakeuri.org"), TransferFormat.Text).DefaultTimeout();
            await eventStreamTcs.Task.DefaultTimeout();

            sseTransport.Output.Complete();

            await sseTransport.Running.DefaultTimeout();
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
        using (StartVerifiableLog())
        {
            var sseTransport = new ServerSentEventsTransport(httpClient, loggerFactory: LoggerFactory);

            await sseTransport.StartAsync(
                new Uri("http://fakeuri.org"), TransferFormat.Text).DefaultTimeout();

            var message = await sseTransport.Input.ReadSingleAsync().DefaultTimeout();
            Assert.Equal("3:abc", Encoding.ASCII.GetString(message));

            await sseTransport.Running.DefaultTimeout();
        }
    }

    [Fact]
    public async Task SSETransportCancelsSendOnStop()
    {
        var eventStreamTcs = new TaskCompletionSource();
        var readTcs = new TaskCompletionSource();
        var sendSyncPoint = new SyncPoint();

        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();

                if (request.Headers.Accept?.Contains(new MediaTypeWithQualityHeaderValue("text/event-stream")) == true)
                {
                    // Receive loop started - allow stopping the transport
                    eventStreamTcs.SetResult();

                    // returns unfinished task to block pipelines
                    var mockStream = new Mock<Stream>();
                    mockStream
                        .Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                        .Returns(async () =>
                        {
                            await readTcs.Task;

                            throw new TaskCanceledException();
                        });
                    mockStream.Setup(s => s.CanRead).Returns(true);
                    return new HttpResponseMessage { Content = new StreamContent(mockStream.Object) };
                }

                // Throw TaskCanceledException from SSE send's SendAsync on stop
                cancellationToken.Register(s => ((SyncPoint)s).Continue(), sendSyncPoint);
                await sendSyncPoint.WaitToContinue();
                throw new TaskCanceledException();
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        using (StartVerifiableLog())
        {
            var sseTransport = new ServerSentEventsTransport(httpClient, loggerFactory: LoggerFactory);

            await sseTransport.StartAsync(
                new Uri("http://fakeuri.org"), TransferFormat.Text).DefaultTimeout();
            await eventStreamTcs.Task;

            await sseTransport.Output.WriteAsync(new byte[] { 0x42 });

            // For send request to be in progress
            await sendSyncPoint.WaitForSyncPoint();

            var stopTask = sseTransport.StopAsync();

            readTcs.SetResult();
            sendSyncPoint.Continue();

            await stopTask;
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
        using (StartVerifiableLog())
        {
            var sseTransport = new ServerSentEventsTransport(httpClient, loggerFactory: LoggerFactory);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sseTransport.StartAsync(new Uri("http://fakeuri.org"), TransferFormat.Binary).DefaultTimeout());

            Assert.Equal("transferFormat", ex.ParamName);
            Assert.Equal($"The 'Binary' transfer format is not supported by this transport.", ex.GetLocalizationSafeMessage());
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
        using (StartVerifiableLog())
        {
            var sseTransport = new ServerSentEventsTransport(httpClient, loggerFactory: LoggerFactory);
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                sseTransport.StartAsync(new Uri("http://fakeuri.org"), transferFormat));

            Assert.Contains($"The '{transferFormat}' transfer format is not supported by this transport.", exception.Message);
            Assert.Equal("transferFormat", exception.ParamName);
        }
    }

    [Fact]
    public async Task StartAsyncSetsCorrectAcceptHeaderForSSE()
    {
        var testHttpHandler = new TestHttpMessageHandler();
        var responseTaskCompletionSource = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Setting up the handler to check for 'text/event-stream' Accept header
        testHttpHandler.OnRequest((request, next, cancellationToken) =>
        {
            if (request.Headers.Accept?.Contains(new MediaTypeWithQualityHeaderValue("text/event-stream")) == true)
            {
                responseTaskCompletionSource.SetResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
            else
            {
                responseTaskCompletionSource.SetResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }
            return responseTaskCompletionSource.Task;
        });

        using (var httpClient = new HttpClient(testHttpHandler))
        {
            var sseTransport = new ServerSentEventsTransport(httpClient, loggerFactory: LoggerFactory);

            // Starting the SSE transport and verifying the outcome
            await sseTransport.StartAsync(new Uri("http://fakeuri.org"), TransferFormat.Text).DefaultTimeout();
            await sseTransport.StopAsync().DefaultTimeout();

            Assert.True(responseTaskCompletionSource.Task.IsCompleted);
            var response = await responseTaskCompletionSource.Task.DefaultTimeout();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Theory]
    [InlineData(new[] { "\r\n" }, "")]
    [InlineData(new[] { "\r\n:\r\n" }, "")]
    [InlineData(new[] { "\r\n:comment\r\n" }, "")]
    [InlineData(new[] { "data: \r\r\n\n" }, "")]
    [InlineData(new[] { ":comment\r\ndata: \r\r\n\r\n" }, "")]
    [InlineData(new[] { "data: A\rB\r\n\r\n" }, "A")]
    [InlineData(new[] { "data: Hello, World\r\n\r\n" }, "Hello, World")]
    [InlineData(new[] { "data: Hello, World\r\n\r\ndata: " }, "Hello, World")]
    [InlineData(new[] { "data: Hello, World\r\n\r\n:comment\r\ndata: " }, "Hello, World")]
    [InlineData(new[] { "data: Hello, World\r\n\r\n:comment" }, "Hello, World")]
    [InlineData(new[] { "data: Hello, World\r\n\r\n:comment\r\n" }, "Hello, World")]
    [InlineData(new[] { "data: Hello, World\r\n:comment\r\n\r\n" }, "Hello, World")]
    [InlineData(new[] { "data: SGVsbG8sIFdvcmxk\r\n\r\n" }, "SGVsbG8sIFdvcmxk")]
    [InlineData(new[] { "d", "ata: Hello, World\r\n\r\n" }, "Hello, World")]
    [InlineData(new[] { "da", "ta: Hello, World\r\n\r\n" }, "Hello, World")]
    [InlineData(new[] { "dat", "a: Hello, World\r\n\r\n" }, "Hello, World")]
    [InlineData(new[] { "data", ": Hello, World\r\n\r\n" }, "Hello, World")]
    [InlineData(new[] { "data:", " Hello, World\r\n\r\n" }, "Hello, World")]
    [InlineData(new[] { "data: Hello, World", "\r\n\r\n" }, "Hello, World")]
    [InlineData(new[] { "data: Hello, World\r\n", "\r\n" }, "Hello, World")]
    [InlineData(new[] { "data: ", "Hello, World\r\n\r\n" }, "Hello, World")]
    [InlineData(new[] { "data: ", "Hello, World\n\n" }, "Hello, World")]
    [InlineData(new[] { "data: ", "Hello, World\r\n\n" }, "Hello, World")]
    [InlineData(new[] { ":", "comment", "\r\n", "d", "ata: Hello, World\r\n\r\n" }, "Hello, World")]
    [InlineData(new[] { ":comment", "\r\n", "data: Hello, World", "\r\n\r\n" }, "Hello, World")]
    [InlineData(new[] { "data: Hello, World\r\n", ":comment\r\n", "\r\n" }, "Hello, World")]
    [InlineData(new[] { "data: Hello \r\n", "data: World\r\n\r\n" }, "Hello \nWorld")]
    public async Task CanProcessMessagesSuccessfully(string[] messageParts, string expectedMessage)
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();
                return new HttpResponseMessage { Content = new StreamContent(new OneAtATimeStream(messageParts)) };
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        using (StartVerifiableLog())
        {
            var sseTransport = new ServerSentEventsTransport(httpClient, loggerFactory: LoggerFactory);

            await sseTransport.StartAsync(
                new Uri("http://fakeuri.org"), TransferFormat.Text).DefaultTimeout();

            var message = await sseTransport.Input.ReadAllAsync().DefaultTimeout();
            Assert.Equal(expectedMessage, Encoding.ASCII.GetString(message));

            await sseTransport.Running.DefaultTimeout();
        }
    }

    public sealed class OneAtATimeStream : MemoryStream
    {
        private readonly string[] _contents;
        private int _index;

        public OneAtATimeStream(string[] contents)
        {
            _contents = contents;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_index == _contents.Length)
            {
                return new(0);
            }

            Debug.Assert(buffer.Length > _contents[_index].Length);

            return new(Encoding.UTF8.GetBytes(_contents[_index++], buffer.Span));
        }
    }
}
