// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
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

                Assert.Equal(5, await readTask.TimeoutAfter(TestConstants.DefaultTimeout));
                Assert.Equal(0, await stream.ReadAsync(buffer, 0, buffer.Length));

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

                await body.StopAsync();
            }
        }

        public static IEnumerable<object[]> StreamData => new[]
        {
            new object[] { new ThrowOnWriteSynchronousStream() },
            new object[] { new ThrowOnWriteAsynchronousStream() },
        };

        public static IEnumerable<object[]> RequestData => new[]
        {
            // Content-Length
            new object[] { new HttpRequestHeaders { HeaderContentLength = "12" }, new[] { "Hello ", "World!" } },
            // Chunked
            new object[] { new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, new[] { "6\r\nHello \r\n", "6\r\nWorld!\r\n0\r\n\r\n" } },
        };

        public static IEnumerable<object[]> CombinedData =>
            from stream in StreamData
            from request in RequestData
            select new[] { stream[0], request[0], request[1] };

        [Theory]
        [MemberData(nameof(RequestData))]
        public async Task CopyToAsyncDoesNotCopyBlocks(HttpRequestHeaders headers, string[] data)
        {
            var writeCount = 0;
            var writeTcs = new TaskCompletionSource<byte[]>();
            var mockDestination = new Mock<Stream>() { CallBase = true };

            mockDestination
                .Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), CancellationToken.None))
                .Callback((byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                {
                    writeTcs.SetResult(buffer);
                    writeCount++;
                })
                .Returns(Task.CompletedTask);

            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http11, headers, input.Http1Connection);

                var copyToAsyncTask = body.CopyToAsync(mockDestination.Object);

                // The block returned by IncomingStart always has at least 2048 available bytes,
                // so no need to bounds check in this test.
                var bytes = Encoding.ASCII.GetBytes(data[0]);
                var buffer = input.Application.Output.GetMemory(2028);
                ArraySegment<byte> block;
                Assert.True(MemoryMarshal.TryGetArray(buffer, out block));
                Buffer.BlockCopy(bytes, 0, block.Array, block.Offset, bytes.Length);
                input.Application.Output.Advance(bytes.Length);
                await input.Application.Output.FlushAsync();

                // Verify the block passed to WriteAsync is the same one incoming data was written into.
                Assert.Same(block.Array, await writeTcs.Task);

                writeTcs = new TaskCompletionSource<byte[]>();
                bytes = Encoding.ASCII.GetBytes(data[1]);
                buffer = input.Application.Output.GetMemory(2048);
                Assert.True(MemoryMarshal.TryGetArray(buffer, out block));
                Buffer.BlockCopy(bytes, 0, block.Array, block.Offset, bytes.Length);
                input.Application.Output.Advance(bytes.Length);
                await input.Application.Output.FlushAsync();

                Assert.Same(block.Array, await writeTcs.Task);

                if (headers.HeaderConnection == "close")
                {
                    input.Application.Output.Complete();
                }

                await copyToAsyncTask;

                Assert.Equal(2, writeCount);

                await body.StopAsync();
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

                await body.StopAsync();
            }
        }

        [Fact]
        public async Task StopAsyncPreventsFurtherDataConsumption()
        {
            using (var input = new TestInput())
            {
                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "2" }, input.Http1Connection);
                var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
                stream.StartAcceptingReads(body);

                // Add some input and consume it to ensure PumpAsync is running
                input.Add("a");
                Assert.Equal(1, await stream.ReadAsync(new byte[1], 0, 1));

                await body.StopAsync();

                // Add some more data. Checking for cancelation and exiting the loop
                // should take priority over reading this data.
                input.Add("b");

                // There shouldn't be any additional data available
                Assert.Equal(0, await stream.ReadAsync(new byte[1], 0, 1));
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

                await body.StopAsync();
            }
        }

        [Fact]
        public async Task ConsumeAsyncThrowsOnTimeout()
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

                var exception = await Assert.ThrowsAsync<BadHttpRequestException>(() => body.ConsumeAsync());
                Assert.Equal(StatusCodes.Status408RequestTimeout, exception.StatusCode);

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

                await body.StopAsync();
            }
        }

        [Fact]
        public async Task LogsWhenStopsReadingRequestBody()
        {
            using (var input = new TestInput())
            {
                var logEvent = new ManualResetEventSlim();
                var mockLogger = new Mock<IKestrelTrace>();
                mockLogger
                    .Setup(logger => logger.RequestBodyDone("ConnectionId", "RequestId"))
                    .Callback(() => logEvent.Set());
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

                Assert.True(logEvent.Wait(TestConstants.DefaultTimeout));

                await body.StopAsync();
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
                input.Add("hello,");
                Assert.Equal(6, await body.ReadAsync(new ArraySegment<byte>(new byte[6])));

                input.Add(" world");
                Assert.Equal(6, await body.ReadAsync(new ArraySegment<byte>(new byte[6])));

                // Due to the limits set on HttpProtocol.RequestBodyPipe, backpressure should be triggered on every write to that pipe.
                mockTimeoutControl.Verify(timeoutControl => timeoutControl.PauseTimingReads(), Times.Exactly(2));
                mockTimeoutControl.Verify(timeoutControl => timeoutControl.ResumeTimingReads(), Times.Exactly(2));
            }
        }

        [Fact]
        public async Task OnlyEnforcesRequestBodyTimeoutAfterSending100Continue()
        {
            using (var input = new TestInput())
            {
                var produceContinueCalled = false;
                var startTimingReadsCalledAfterProduceContinue = false;

                var mockHttpResponseControl = new Mock<IHttpResponseControl>();
                mockHttpResponseControl
                    .Setup(httpResponseControl => httpResponseControl.ProduceContinue())
                    .Callback(() => produceContinueCalled = true);
                input.Http1Connection.HttpResponseControl = mockHttpResponseControl.Object;

                var mockTimeoutControl = new Mock<ITimeoutControl>();
                mockTimeoutControl
                    .Setup(timeoutControl => timeoutControl.StartTimingReads())
                    .Callback(() => startTimingReadsCalledAfterProduceContinue = produceContinueCalled);

                input.Http1ConnectionContext.TimeoutControl = mockTimeoutControl.Object;

                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);

                // Add some input and read it to start PumpAsync
                var readTask = body.ReadAsync(new ArraySegment<byte>(new byte[1]));

                Assert.True(startTimingReadsCalledAfterProduceContinue);

                input.Add("a");
                await readTask;

                await body.StopAsync();
            }
        }

        [Fact]
        public async Task DoesNotEnforceRequestBodyTimeoutOnUpgradeRequests()
        {
            using (var input = new TestInput())
            {
                var mockTimeoutControl = new Mock<ITimeoutControl>();
                input.Http1ConnectionContext.TimeoutControl = mockTimeoutControl.Object;

                var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderConnection = "upgrade" }, input.Http1Connection);

                // Add some input and read it to start PumpAsync
                input.Add("a");
                Assert.Equal(1, await body.ReadAsync(new ArraySegment<byte>(new byte[1])));

                input.Fin();

                await Assert.ThrowsAsync<BadHttpRequestException>(async () => await body.ReadAsync(new ArraySegment<byte>(new byte[1])));

                mockTimeoutControl.Verify(timeoutControl => timeoutControl.StartTimingReads(), Times.Never);
                mockTimeoutControl.Verify(timeoutControl => timeoutControl.StopTimingReads(), Times.Never);

                // Due to the limits set on HttpProtocol.RequestBodyPipe, backpressure should be triggered on every
                // write to that pipe. Verify that read timing pause and resume are not called on upgrade
                // requests.
                mockTimeoutControl.Verify(timeoutControl => timeoutControl.PauseTimingReads(), Times.Never);
                mockTimeoutControl.Verify(timeoutControl => timeoutControl.ResumeTimingReads(), Times.Never);

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
