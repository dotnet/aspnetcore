// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class MessageBodyTests
    {
        [Theory]
        [InlineData(HttpVersion.Http10)]
        [InlineData(HttpVersion.Http11)]
        public async Task CanReadFromContentLength(HttpVersion httpVersion)
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
                var mockBodyControl = new Mock<IHttpBodyControlFeature>();
                mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(true);
                var stream = new HttpRequestStream(mockBodyControl.Object);
                stream.StartAcceptingReads(body);

                input.Add("Hello");

                var buffer = new byte[1024];

                var count = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(5, count);
                AssertASCII("Hello", new ArraySegment<byte>(buffer, 0, count));

                count = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(0, count);

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Theory]
        [InlineData(HttpVersion.Http10)]
        [InlineData(HttpVersion.Http11)]
        public async Task CanReadAsyncFromContentLength(HttpVersion httpVersion)
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                input.Add("Hello");

                var buffer = new byte[1024];

                var count = await stream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(5, count);
                AssertASCII("Hello", new ArraySegment<byte>(buffer, 0, count));

                count = await stream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(0, count);

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task CanReadFromChunkedEncoding()
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
                var mockBodyControl = new Mock<IHttpBodyControlFeature>();
                mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(true);
                var stream = new HttpRequestStream(mockBodyControl.Object);
                stream.StartAcceptingReads(body);

                input.Add("5\r\nHello\r\n");

                var buffer = new byte[1024];

                var count = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(5, count);
                AssertASCII("Hello", new ArraySegment<byte>(buffer, 0, count));

                input.Add("0\r\n\r\n");

                count = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(0, count);

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task CanReadAsyncFromChunkedEncoding()
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                input.Add("5\r\nHello\r\n");

                var buffer = new byte[1024];

                var count = await stream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(5, count);
                AssertASCII("Hello", new ArraySegment<byte>(buffer, 0, count));

                input.Add("0\r\n\r\n");

                count = await stream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(0, count);

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task ReadExitsGivenIncompleteChunkedExtension()
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                input.Add("5;\r\0");

                var buffer = new byte[1024];
                var readTask = stream.ReadAsync(buffer, 0, buffer.Length);

                Assert.False(readTask.IsCompleted);

                input.Add("\r\r\r\nHello\r\n0\r\n\r\n");

                Assert.Equal(5, await readTask.DefaultTimeout());
                Assert.Equal(0, await stream.ReadAsync(buffer, 0, buffer.Length));

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task ReadThrowsGivenChunkPrefixGreaterThanMaxInt()
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                input.Add("80000000\r\n");

                var buffer = new byte[1024];
                var ex = await Assert.ThrowsAsync<IOException>(async () =>
                    await stream.ReadAsync(buffer, 0, buffer.Length));
                Assert.IsType<OverflowException>(ex.InnerException);
                Assert.Equal(CoreStrings.BadRequest_BadChunkSizeData, ex.Message);

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task ReadThrowsGivenChunkPrefixGreaterThan8Bytes()
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                input.Add("012345678\r");

                var buffer = new byte[1024];
                var ex = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
                    await stream.ReadAsync(buffer, 0, buffer.Length));

                Assert.Equal(CoreStrings.BadRequest_BadChunkSizeData, ex.Message);

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Theory]
        [InlineData(HttpVersion.Http10)]
        [InlineData(HttpVersion.Http11)]
        public async Task CanReadFromRemainingData(HttpVersion httpVersion)
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderConnection = "upgrade" }, input.Http1Connection);
                var mockBodyControl = new Mock<IHttpBodyControlFeature>();
                mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(true);
                var stream = new HttpRequestStream(mockBodyControl.Object);
                stream.StartAcceptingReads(body);

                input.Add("Hello");

                var buffer = new byte[1024];

                var count = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(5, count);
                AssertASCII("Hello", new ArraySegment<byte>(buffer, 0, count));

                input.Fin();

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Theory]
        [InlineData(HttpVersion.Http10)]
        [InlineData(HttpVersion.Http11)]
        public async Task CanReadAsyncFromRemainingData(HttpVersion httpVersion)
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderConnection = "upgrade" }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                input.Add("Hello");

                var buffer = new byte[1024];

                var count = await stream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(5, count);
                AssertASCII("Hello", new ArraySegment<byte>(buffer, 0, count));

                input.Fin();

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Theory]
        [InlineData(HttpVersion.Http10)]
        [InlineData(HttpVersion.Http11)]
        public async Task ReadFromNoContentLengthReturnsZero(HttpVersion httpVersion)
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders(), input.Http1Connection);
                var mockBodyControl = new Mock<IHttpBodyControlFeature>();
                mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(true);
                var stream = new HttpRequestStream(mockBodyControl.Object);
                stream.StartAcceptingReads(body);

                input.Add("Hello");

                var buffer = new byte[1024];
                Assert.Equal(0, stream.Read(buffer, 0, buffer.Length));

                await body.StopAsync();
            }
        }

        [Theory]
        [InlineData(HttpVersion.Http10)]
        [InlineData(HttpVersion.Http11)]
        public async Task ReadAsyncFromNoContentLengthReturnsZero(HttpVersion httpVersion)
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders(), input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                input.Add("Hello");

                var buffer = new byte[1024];
                Assert.Equal(0, await stream.ReadAsync(buffer, 0, buffer.Length));

                await body.StopAsync();
            }
        }

        [Fact]
        public async Task CanHandleLargeBlocks()
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http10, new HttpRequestHeaders { HeaderContentLength = "8197" }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                // Input needs to be greater than 4032 bytes to allocate a block not backed by a slab.
                var largeInput = new string('a', 8192);

                input.Add(largeInput);
                // Add a smaller block to the end so that SocketInput attempts to return the large
                // block to the memory pool.
                input.Add("Hello");

                var ms = new MemoryStream();

                await stream.CopyToAsync(ms);
                var requestArray = ms.ToArray();
                Assert.Equal(8197, requestArray.Length);
                AssertASCII(largeInput + "Hello", new ArraySegment<byte>(requestArray, 0, requestArray.Length));

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public void ForThrowsWhenFinalTransferCodingIsNotChunked()
        {
            using (var input = new TestInput())
            {
                var ex = Assert.Throws<BadHttpRequestException>(() =>
                    Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked, not-chunked" }, input.Http1Connection));

                Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
                Assert.Equal(CoreStrings.FormatBadRequest_FinalTransferCodingNotChunked("chunked, not-chunked"), ex.Message);
            }
        }

        [Theory]
        [InlineData(HttpMethod.Post)]
        [InlineData(HttpMethod.Put)]
        public void ForThrowsWhenMethodRequiresLengthButNoContentLengthOrTransferEncodingIsSet(HttpMethod method)
        {
            using (var input = new TestInput())
            {
                input.Http1Connection.Method = method;
                var ex = Assert.Throws<BadHttpRequestException>(() =>
                    Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders(), input.Http1Connection));

                Assert.Equal(StatusCodes.Status411LengthRequired, ex.StatusCode);
                Assert.Equal(CoreStrings.FormatBadRequest_LengthRequired(((IHttpRequestFeature)input.Http1Connection).Method), ex.Message);
            }
        }

        [Theory]
        [InlineData(HttpMethod.Post)]
        [InlineData(HttpMethod.Put)]
        public void ForThrowsWhenMethodRequiresLengthButNoContentLengthSetHttp10(HttpMethod method)
        {
            using (var input = new TestInput())
            {
                input.Http1Connection.Method = method;
                var ex = Assert.Throws<BadHttpRequestException>(() =>
                    Http1MessageBody.For(HttpVersion.Http10, new HttpRequestHeaders(), input.Http1Connection));

                Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
                Assert.Equal(CoreStrings.FormatBadRequest_LengthRequiredHttp10(((IHttpRequestFeature)input.Http1Connection).Method), ex.Message);
            }
        }

        [Fact]
        public async Task CopyToAsyncDoesNotCompletePipeReader()
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http10, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);

                input.Add("Hello");

                using (var ms = new MemoryStream())
                {
                    await body.CopyToAsync(ms);
                }

                Assert.Equal(0, await body.ReadAsync(new ArraySegment<byte>(new byte[1])));

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task ConsumeAsyncConsumesAllRemainingInput()
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http10, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);

                input.Add("Hello");

                await body.ConsumeAsync();

                Assert.Equal(0, await body.ReadAsync(new ArraySegment<byte>(new byte[1])));

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task CopyToAsyncDoesNotCopyBlocks()
        {
            var writeCount = 0;
            var writeTcs = new TaskCompletionSource<(byte[], int, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
            var mockDestination = new Mock<Stream> { CallBase = true };

            mockDestination
                .Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), CancellationToken.None))
                .Callback((byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                {
                    writeTcs.SetResult((buffer, offset, count));
                    writeCount++;
                })
                .Returns(Task.CompletedTask);

            using (var memoryPool = KestrelMemoryPool.Create())
            {
                var options = new PipeOptions(pool: memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
                var pair = DuplexPipe.CreateConnectionPair(options, options);
                var transport = pair.Transport;
                var http1ConnectionContext = new HttpConnectionContext
                {
                    ServiceContext = new TestServiceContext(),
                    ConnectionFeatures = new FeatureCollection(),
                    Transport = transport,
                    MemoryPool = memoryPool,
                    TimeoutControl = Mock.Of<ITimeoutControl>()
                };
                var http1Connection = new Http1Connection(http1ConnectionContext)
                {
                    HasStartedConsumingRequestBody = true
                };

                var headers = new HttpRequestHeaders { HeaderContentLength = "12" };
                var body = Http1MessageBody.For(HttpVersion.Http11, headers, http1Connection);

                var copyToAsyncTask = body.CopyToAsync(mockDestination.Object);

                var bytes = Encoding.ASCII.GetBytes("Hello ");
                var buffer = http1Connection.RequestBodyPipe.Writer.GetMemory(2048);
                Assert.True(MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment));
                Buffer.BlockCopy(bytes, 0, segment.Array, segment.Offset, bytes.Length);
                http1Connection.RequestBodyPipe.Writer.Advance(bytes.Length);
                await http1Connection.RequestBodyPipe.Writer.FlushAsync();

                // Verify the block passed to Stream.WriteAsync() is the same one incoming data was written into.
                Assert.Equal((segment.Array, segment.Offset, bytes.Length), await writeTcs.Task);

                // Verify the again when GetMemory returns the tail space of the same block.
                writeTcs = new TaskCompletionSource<(byte[], int, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
                bytes = Encoding.ASCII.GetBytes("World!");
                buffer = http1Connection.RequestBodyPipe.Writer.GetMemory(2048);
                Assert.True(MemoryMarshal.TryGetArray(buffer, out segment));
                Buffer.BlockCopy(bytes, 0, segment.Array, segment.Offset, bytes.Length);
                http1Connection.RequestBodyPipe.Writer.Advance(bytes.Length);
                await http1Connection.RequestBodyPipe.Writer.FlushAsync();

                Assert.Equal((segment.Array, segment.Offset, bytes.Length), await writeTcs.Task);

                http1Connection.RequestBodyPipe.Writer.Complete();

                await copyToAsyncTask;

                Assert.Equal(2, writeCount);

                // Don't call body.StopAsync() because PumpAsync() was never called.
                http1Connection.RequestBodyPipe.Reader.Complete();
            }
        }

        [Theory]
        [InlineData("keep-alive, upgrade")]
        [InlineData("Keep-Alive, Upgrade")]
        [InlineData("upgrade, keep-alive")]
        [InlineData("Upgrade, Keep-Alive")]
        public async Task ConnectionUpgradeKeepAlive(string headerConnection)
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderConnection = headerConnection }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                input.Add("Hello");

                var buffer = new byte[1024];
                Assert.Equal(5, await stream.ReadAsync(buffer, 0, buffer.Length));
                AssertASCII("Hello", new ArraySegment<byte>(buffer, 0, 5));

                input.Fin();

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task UpgradeConnectionAcceptsContentLengthZero()
        {
            // https://tools.ietf.org/html/rfc7230#section-3.3.2
            // "A user agent SHOULD NOT send a Content-Length header field when the request message does not contain
            // a payload body and the method semantics do not anticipate such a body."
            //  ==> it can actually send that header
            var headerConnection = "Upgrade, Keep-Alive";
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderConnection = headerConnection, ContentLength = 0 }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                input.Add("Hello");

                var buffer = new byte[1024];
                Assert.Equal(5, await stream.ReadAsync(buffer, 0, buffer.Length));
                AssertASCII("Hello", new ArraySegment<byte>(buffer, 0, 5));

                input.Fin();

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task PumpAsyncDoesNotReturnAfterCancelingInput()
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "2" }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                // Add some input and consume it to ensure PumpAsync is running
                input.Add("a");
                Assert.Equal(1, await stream.ReadAsync(new byte[1], 0, 1));

                input.Transport.Input.CancelPendingRead();

                // Add more input and verify is read
                input.Add("b");
                Assert.Equal(1, await stream.ReadAsync(new byte[1], 0, 1));

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task ReadAsyncThrowsOnTimeout()
        {
            using (var input = new TestInput())
            {
                var mockTimeoutControl = new Mock<ITimeoutControl>();

                input.Http1ConnectionContext.TimeoutControl = mockTimeoutControl.Object;

                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);

                // Add some input and read it to start PumpAsync
                input.Add("a");
                Assert.Equal(1, await body.ReadAsync(new ArraySegment<byte>(new byte[1])));

                // Time out on the next read
                input.Http1Connection.SendTimeoutResponse();

                var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await body.ReadAsync(new Memory<byte>(new byte[1])));
                Assert.Equal(StatusCodes.Status408RequestTimeout, exception.StatusCode);

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task ConsumeAsyncCompletesAndDoesNotThrowOnTimeout()
        {
            using (var input = new TestInput())
            {
                var mockTimeoutControl = new Mock<ITimeoutControl>();
                input.Http1ConnectionContext.TimeoutControl = mockTimeoutControl.Object;

                var mockLogger = new Mock<IKestrelTrace>();
                input.Http1Connection.ServiceContext.Log = mockLogger.Object;

                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);

                // Add some input and read it to start PumpAsync
                input.Add("a");
                Assert.Equal(1, await body.ReadAsync(new ArraySegment<byte>(new byte[1])));

                // Time out on the next read
                input.Http1Connection.SendTimeoutResponse();

                await body.ConsumeAsync();

                mockLogger.Verify(logger => logger.ConnectionBadRequest(
                    It.IsAny<string>(),
                    It.Is<BadHttpRequestException>(ex => ex.Reason == RequestRejectionReason.RequestBodyTimeout)));

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task CopyToAsyncThrowsOnTimeout()
        {
            using (var input = new TestInput())
            {
                var mockTimeoutControl = new Mock<ITimeoutControl>();

                input.Http1ConnectionContext.TimeoutControl = mockTimeoutControl.Object;

                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);

                // Add some input and read it to start PumpAsync
                input.Add("a");
                Assert.Equal(1, await body.ReadAsync(new ArraySegment<byte>(new byte[1])));

                // Time out on the next read
                input.Http1Connection.SendTimeoutResponse();

                using (var ms = new MemoryStream())
                {
                    var exception = await Assert.ThrowsAsync<BadHttpRequestException>(() => body.CopyToAsync(ms));
                    Assert.Equal(StatusCodes.Status408RequestTimeout, exception.StatusCode);
                }

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task LogsWhenStartsReadingRequestBody()
        {
            using (var input = new TestInput())
            {
                var mockLogger = new Mock<IKestrelTrace>();
                input.Http1Connection.ServiceContext.Log = mockLogger.Object;
                input.Http1Connection.ConnectionIdFeature = "ConnectionId";
                input.Http1Connection.TraceIdentifier = "RequestId";

                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "2" }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                // Add some input and consume it to ensure PumpAsync is running
                input.Add("a");
                Assert.Equal(1, await stream.ReadAsync(new byte[1], 0, 1));

                mockLogger.Verify(logger => logger.RequestBodyStart("ConnectionId", "RequestId"));

                input.Fin();

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task LogsWhenStopsReadingRequestBody()
        {
            using (var input = new TestInput())
            {
                var logEvent = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var mockLogger = new Mock<IKestrelTrace>();
                mockLogger
                    .Setup(logger => logger.RequestBodyDone("ConnectionId", "RequestId"))
                    .Callback(() => logEvent.SetResult(null));
                input.Http1Connection.ServiceContext.Log = mockLogger.Object;
                input.Http1Connection.ConnectionIdFeature = "ConnectionId";
                input.Http1Connection.TraceIdentifier = "RequestId";

                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "2" }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                // Add some input and consume it to ensure PumpAsync is running
                input.Add("a");
                Assert.Equal(1, await stream.ReadAsync(new byte[1], 0, 1));

                input.Fin();

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();

                await logEvent.Task.DefaultTimeout();
            }
        }

        [Fact]
        public async Task PausesAndResumesRequestBodyTimeoutOnBackpressure()
        {
            using (var input = new TestInput())
            {
                var mockTimeoutControl = new Mock<ITimeoutControl>();
                input.Http1ConnectionContext.TimeoutControl = mockTimeoutControl.Object;

                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "12" }, input.Http1Connection);

                // Add some input and read it to start PumpAsync
                var readTask1 = body.ReadAsync(new ArraySegment<byte>(new byte[6]));
                input.Add("hello,");
                Assert.Equal(6, await readTask1);

                var readTask2 = body.ReadAsync(new ArraySegment<byte>(new byte[6]));
                input.Add(" world");
                Assert.Equal(6, await readTask2);

                // Due to the limits set on HttpProtocol.RequestBodyPipe, backpressure should be triggered on every write to that pipe.
                mockTimeoutControl.Verify(timeoutControl => timeoutControl.StopTimingRead(), Times.Exactly(2));
                mockTimeoutControl.Verify(timeoutControl => timeoutControl.StartTimingRead(), Times.Exactly(2));
            }
        }

        [Fact]
        public async Task OnlyEnforcesRequestBodyTimeoutAfterFirstRead()
        {
            using (var input = new TestInput())
            {
                var startRequestBodyCalled = false;

                var minReadRate = input.Http1Connection.MinRequestBodyDataRate;
                var mockTimeoutControl = new Mock<ITimeoutControl>();
                mockTimeoutControl
                    .Setup(timeoutControl => timeoutControl.StartRequestBody(minReadRate))
                    .Callback(() => startRequestBodyCalled = true);

                input.Http1ConnectionContext.TimeoutControl = mockTimeoutControl.Object;

                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);

                Assert.False(startRequestBodyCalled);

                // Add some input and read it to start PumpAsync
                var readTask = body.ReadAsync(new ArraySegment<byte>(new byte[1]));

                Assert.True(startRequestBodyCalled);

                input.Add("a");
                await readTask;

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        [Fact]
        public async Task DoesNotEnforceRequestBodyTimeoutOnUpgradeRequests()
        {
            using (var input = new TestInput())
            {
                var minReadRate = input.Http1Connection.MinRequestBodyDataRate;
                var mockTimeoutControl = new Mock<ITimeoutControl>();
                input.Http1ConnectionContext.TimeoutControl = mockTimeoutControl.Object;

                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderConnection = "upgrade" }, input.Http1Connection);

                // Add some input and read it to start PumpAsync
                input.Add("a");
                Assert.Equal(1, await body.ReadAsync(new ArraySegment<byte>(new byte[1])));

                input.Fin();

                Assert.Equal(0, await body.ReadAsync(new ArraySegment<byte>(new byte[1])));

                mockTimeoutControl.Verify(timeoutControl => timeoutControl.StartRequestBody(minReadRate), Times.Never);
                mockTimeoutControl.Verify(timeoutControl => timeoutControl.StopRequestBody(), Times.Never);

                // Due to the limits set on HttpProtocol.RequestBodyPipe, backpressure should be triggered on every
                // write to that pipe. Verify that read timing pause and resume are not called on upgrade
                // requests.
                mockTimeoutControl.Verify(timeoutControl => timeoutControl.StopTimingRead(), Times.Never);
                mockTimeoutControl.Verify(timeoutControl => timeoutControl.StartTimingRead(), Times.Never);

                input.Http1Connection.RequestBodyPipe.Reader.Complete();
                await body.StopAsync();
            }
        }

        private void AssertASCII(string expected, ArraySegment<byte> actual)
        {
            var encoding = Encoding.ASCII;
            var bytes = encoding.GetBytes(expected);
            Assert.Equal(bytes.Length, actual.Count);
            for (var index = 0; index < bytes.Length; index++)
            {
                Assert.Equal(bytes[index], actual.Array[actual.Offset + index]);
            }
        }

        private class ThrowOnWriteSynchronousStream : Stream
        {
            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throw new XunitException();
            }

            public override bool CanRead { get; }
            public override bool CanSeek { get; }
            public override bool CanWrite => true;
            public override long Length { get; }
            public override long Position { get; set; }
        }

        private class ThrowOnWriteAsynchronousStream : Stream
        {
            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await Task.Delay(1);
                throw new XunitException();
            }

            public override bool CanRead { get; }
            public override bool CanSeek { get; }
            public override bool CanWrite => true;
            public override long Length { get; }
            public override long Position { get; set; }
        }
    }
}
