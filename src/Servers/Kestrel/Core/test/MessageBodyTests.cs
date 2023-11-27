// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class MessageBodyTests : LoggedTest
{
    [Theory]
    [InlineData((int)HttpVersion.Http10)]
    [InlineData((int)HttpVersion.Http11)]
    public async Task CanReadFromContentLength(int intHttpVersion)
    {
        var httpVersion = (HttpVersion)intHttpVersion;
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
            var mockBodyControl = new Mock<IHttpBodyControlFeature>();
            mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(true);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(mockBodyControl.Object, reader);
            reader.StartAcceptingReads(body);

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
    [InlineData((int)HttpVersion.Http10)]
    [InlineData((int)HttpVersion.Http11)]
    public async Task CanReadFromContentLengthPipeApis(int intHttpVersion)
    {
        var httpVersion = (HttpVersion)intHttpVersion;
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("Hello");

            var readResult = await reader.ReadAsync();

            Assert.Equal(5, readResult.Buffer.Length);
            AssertASCII("Hello", readResult.Buffer);
            reader.AdvanceTo(readResult.Buffer.End);

            readResult = await reader.ReadAsync();
            Assert.True(readResult.IsCompleted);
            reader.AdvanceTo(readResult.Buffer.End);

            await body.StopAsync();
        }
    }

    [Theory]
    [InlineData((int)HttpVersion.Http10)]
    [InlineData((int)HttpVersion.Http11)]
    public async Task CanTryReadFromContentLengthPipeApis(int intHttpVersion)
    {
        var httpVersion = (HttpVersion)intHttpVersion;
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("Hello");
            Assert.True(reader.TryRead(out var readResult));

            Assert.Equal(5, readResult.Buffer.Length);
            AssertASCII("Hello", readResult.Buffer);
            reader.AdvanceTo(readResult.Buffer.End);

            reader.TryRead(out readResult);
            Assert.True(readResult.IsCompleted);
            reader.AdvanceTo(readResult.Buffer.End);

            await body.StopAsync();
        }
    }

    [Theory]
    [InlineData((int)HttpVersion.Http10)]
    [InlineData((int)HttpVersion.Http11)]
    public async Task ReadAsyncWithoutAdvanceFromContentLengthThrows(int intHttpVersion)
    {
        var httpVersion = (HttpVersion)intHttpVersion;
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("Hello");
            var readResult = await reader.ReadAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await reader.ReadAsync());

            await body.StopAsync();
        }
    }

    [Theory]
    [InlineData((int)HttpVersion.Http10)]
    [InlineData((int)HttpVersion.Http11)]
    public async Task TryReadWithoutAdvanceFromContentLengthThrows(int intHttpVersion)
    {
        var httpVersion = (HttpVersion)intHttpVersion;
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("Hello");
            Assert.True(reader.TryRead(out var readResult));

            Assert.Throws<InvalidOperationException>(() => reader.TryRead(out readResult));

            await body.StopAsync();
        }
    }

    [Theory]
    [InlineData((int)HttpVersion.Http10)]
    [InlineData((int)HttpVersion.Http11)]
    public async Task CanReadAsyncFromContentLength(int intHttpVersion)
    {
        var httpVersion = (HttpVersion)intHttpVersion;
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);

            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

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
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(mockBodyControl.Object, reader);
            reader.StartAcceptingReads(body);

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
    public async Task BadChunkPrefixThrowsBadRequestException()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
            var mockBodyControl = new Mock<IHttpBodyControlFeature>();
            mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(true);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(mockBodyControl.Object, reader);
            reader.StartAcceptingReads(body);
            var buffer = new byte[1024];
            var task = stream.ReadAsync(buffer, 0, buffer.Length);

            input.Add("g");
            input.Add("g");

#pragma warning disable CS0618 // Type or member is obsolete
            await Assert.ThrowsAsync<BadHttpRequestException>(() => task);
#pragma warning restore CS0618 // Type or member is obsolete

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task WritingChunkOverMaxChunkSizeThrowsBadRequest()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
            var mockBodyControl = new Mock<IHttpBodyControlFeature>();
            mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(true);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(mockBodyControl.Object, reader);
            reader.StartAcceptingReads(body);
            var buffer = new byte[1024];
            var task = stream.ReadAsync(buffer, 0, buffer.Length);

            // Max is 10 bytes
            for (int i = 0; i < 11; i++)
            {
                input.Add(i.ToString(CultureInfo.InvariantCulture));
            }

#pragma warning disable CS0618 // Type or member is obsolete
            await Assert.ThrowsAsync<BadHttpRequestException>(() => task);
#pragma warning restore CS0618 // Type or member is obsolete

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task InvalidChunkSuffixThrowsBadRequest()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
            var mockBodyControl = new Mock<IHttpBodyControlFeature>();
            mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(true);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(mockBodyControl.Object, reader);
            reader.StartAcceptingReads(body);
            var buffer = new byte[1024];

            async Task ReadAsync()
            {
                while (true)
                {
                    await stream.ReadAsync(buffer, 0, buffer.Length);
                }
            }

            var task = ReadAsync();

            input.Add("1");
            input.Add("\r");
            input.Add("\n");
            input.Add("h");
            input.Add("0");
            input.Add("\r");
            input.Add("\n");
            input.Add("\r");
            input.Add("n");

#pragma warning disable CS0618 // Type or member is obsolete
            await Assert.ThrowsAsync<BadHttpRequestException>(() => task);
#pragma warning restore CS0618 // Type or member is obsolete

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task CanReadAsyncFromChunkedEncoding()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

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
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

            input.Add("5;\r\0");

            var buffer = new byte[1024];
            var readTask = stream.ReadAsync(buffer, 0, buffer.Length);

            Assert.False(readTask.IsCompleted);

            input.Add("\r\r\r\nHello\r\n0\r\n\r\n");

            Assert.Equal(5, await readTask.DefaultTimeout());
            try
            {
                var res = await stream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(0, res);
            }
            catch (Exception)
            {
                throw;
            }

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task ReadThrowsGivenChunkPrefixGreaterThanMaxInt()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

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
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

            input.Add("012345678\r");

            var buffer = new byte[1024];
#pragma warning disable CS0618 // Type or member is obsolete
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
#pragma warning restore CS0618 // Type or member is obsolete
                    await stream.ReadAsync(buffer, 0, buffer.Length));

            Assert.Equal(CoreStrings.BadRequest_BadChunkSizeData, ex.Message);

            await body.StopAsync();
        }
    }

    [Theory]
    [InlineData((int)HttpVersion.Http10)]
    [InlineData((int)HttpVersion.Http11)]
    public async Task CanReadFromRemainingData(int intHttpVersion)
    {
        var httpVersion = (HttpVersion)intHttpVersion;
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderConnection = "upgrade" }, input.Http1Connection);
            var mockBodyControl = new Mock<IHttpBodyControlFeature>();
            mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(true);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(mockBodyControl.Object, reader);
            reader.StartAcceptingReads(body);

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
    [InlineData((int)HttpVersion.Http10)]
    [InlineData((int)HttpVersion.Http11)]
    public async Task CanReadAsyncFromRemainingData(int intHttpVersion)
    {
        var httpVersion = (HttpVersion)intHttpVersion;
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders { HeaderConnection = "upgrade" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

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
    [InlineData((int)HttpVersion.Http10)]
    [InlineData((int)HttpVersion.Http11)]
    public async Task ReadFromNoContentLengthReturnsZero(int intHttpVersion)
    {
        var httpVersion = (HttpVersion)intHttpVersion;
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders(), input.Http1Connection);
            var mockBodyControl = new Mock<IHttpBodyControlFeature>();
            mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(true);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(mockBodyControl.Object, reader);
            reader.StartAcceptingReads(body);

            input.Add("Hello");

            var buffer = new byte[1024];
            Assert.Equal(0, stream.Read(buffer, 0, buffer.Length));

            await body.StopAsync();
        }
    }

    [Theory]
    [InlineData((int)HttpVersion.Http10)]
    [InlineData((int)HttpVersion.Http11)]
    public async Task ReadAsyncFromNoContentLengthReturnsZero(int intHttpVersion)
    {
        var httpVersion = (HttpVersion)intHttpVersion;
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(httpVersion, new HttpRequestHeaders(), input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

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
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

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
#pragma warning disable CS0618 // Type or member is obsolete
            var ex = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
                    Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked, not-chunked" }, input.Http1Connection));

            Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
            Assert.Equal(CoreStrings.FormatBadRequest_FinalTransferCodingNotChunked("chunked, not-chunked"), ex.Message);
        }
    }

    [Theory]
    [InlineData((int)HttpMethod.Post)]
    [InlineData((int)HttpMethod.Put)]
    public void ForReturnsZeroLengthWhenNoContentLengthOrTransferEncodingIsSetHttp11(int intMethod)
    {
        var method = (HttpMethod)intMethod;
        using (var input = new TestInput())
        {
            input.Http1Connection.Method = method;
            var result = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders(), input.Http1Connection);

            Assert.Same(MessageBody.ZeroContentLengthKeepAlive, result);
        }
    }

    [Theory]
    [InlineData((int)HttpMethod.Post)]
    [InlineData((int)HttpMethod.Put)]
    public void ForThrowsWhenMethodRequiresLengthButNoContentLengthSetHttp10(int intMethod)
    {
        var method = (HttpMethod)intMethod;
        using (var input = new TestInput())
        {
            input.Http1Connection.Method = method;
#pragma warning disable CS0618 // Type or member is obsolete
            var ex = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
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
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

            input.Add("Hello");

            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
            }

            Assert.Equal(0, await stream.ReadAsync(new ArraySegment<byte>(new byte[1])));

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

            Assert.True((await body.ReadAsync()).IsCompleted);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task ConsumeAsyncAbortsConnectionInputAfterStartingTryReadWithoutAdvance()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http10, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);

            input.Add("Hello");

            body.TryRead(out var readResult);

            await body.ConsumeAsync();

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
            // note the http1connection request body pipe reader should be the same.
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderConnection = headerConnection }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

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
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

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
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

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
    public async Task ReadAsyncThrowsOnTimeout()
    {
        using (var input = new TestInput())
        {
            var mockTimeoutControl = new Mock<ITimeoutControl>();

            input.Http1ConnectionContext.TimeoutControl = mockTimeoutControl.Object;

            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);

            // Add some input and read it to start PumpAsync
            input.Add("a");
            var readResult = await body.ReadAsync();
            Assert.Equal(1, readResult.Buffer.Length);
            body.AdvanceTo(readResult.Buffer.End);

            // Time out on the next read
            input.Http1Connection.SendTimeoutResponse();

#pragma warning disable CS0618 // Type or member is obsolete
            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await body.ReadAsync());
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.Equal(StatusCodes.Status408RequestTimeout, exception.StatusCode);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task ConsumeAsyncCompletesAndDoesNotThrowOnTimeout()
    {
        var mockTimeoutControl = new Mock<ITimeoutControl>();

        using (var input = new TestInput(log: new KestrelTrace(LoggerFactory), timeoutControl: mockTimeoutControl.Object))
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);

            // Add some input and read it to start PumpAsync
            input.Add("a");
            var readResult = await body.ReadAsync();
            Assert.Equal(1, readResult.Buffer.Length);

            // need to advance to make PipeReader in ReadCompleted state
            body.AdvanceTo(readResult.Buffer.End);

            // Time out on the next read
            input.Http1Connection.SendTimeoutResponse();

            await body.ConsumeAsync();

#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Contains(TestSink.Writes,
                m => m.EventId.Name == "ConnectionBadRequest" &&
                    m.Exception is BadHttpRequestException ex &&
                    ex.Reason == RequestRejectionReason.RequestBodyTimeout);
#pragma warning restore CS0618 // Type or member is obsolete

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
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

            // Add some input and read it to start PumpAsync
            input.Add("a");

            // Time out on the next read
            input.Http1Connection.SendTimeoutResponse();

            using (var ms = new MemoryStream())
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var exception = await Assert.ThrowsAsync<BadHttpRequestException>(() => stream.CopyToAsync(ms));
#pragma warning restore CS0618 // Type or member is obsolete
                Assert.Equal(StatusCodes.Status408RequestTimeout, exception.StatusCode);
            }

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task LogsWhenStartsReadingRequestBody()
    {
        using (var input = new TestInput(log: new KestrelTrace(LoggerFactory)))
        {
            input.Http1Connection.ConnectionIdFeature = "ConnectionId";
            input.Http1Connection.TraceIdentifier = "RequestId";

            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "2" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

            // Add some input and consume it to ensure PumpAsync is running
            input.Add("a");
            Assert.Equal(1, await stream.ReadAsync(new byte[1], 0, 1));

            Assert.Contains(TestSink.Writes,
                m => m.EventId.Name == "RequestBodyStart" &&
                    m.Message.Contains(@"Connection id ""ConnectionId"", Request id ""RequestId"": started reading request body."));

            input.Fin();

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task LogsWhenStopsReadingRequestBody()
    {
        var logEvent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TestSink.MessageLogged += context =>
        {
            if (context.EventId.Name == "RequestBodyDone")
            {
                logEvent.SetResult();
            }
        };

        using (var input = new TestInput(log: new KestrelTrace(LoggerFactory)))
        {
            input.Http1Connection.ConnectionIdFeature = "ConnectionId";
            input.Http1Connection.TraceIdentifier = "RequestId";

            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "2" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>(), reader);
            reader.StartAcceptingReads(body);

            // Add some input and consume it to ensure PumpAsync is running
            input.Add("a");
            Assert.Equal(1, await stream.ReadAsync(new byte[1], 0, 1));

            input.Fin();

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
            var readTask1 = body.ReadAsync();
            input.Add("hello,");
            var readResult = await readTask1;
            Assert.Equal(6, readResult.Buffer.Length);
            body.AdvanceTo(readResult.Buffer.End);

            var readTask2 = body.ReadAsync();
            input.Add(" world");
            readResult = await readTask2;
            Assert.Equal(6, readResult.Buffer.Length);
            body.AdvanceTo(readResult.Buffer.End);

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
            var readTask = body.ReadAsync();

            Assert.True(startRequestBodyCalled);

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
            var minReadRate = input.Http1Connection.MinRequestBodyDataRate;
            var mockTimeoutControl = new Mock<ITimeoutControl>();
            input.Http1ConnectionContext.TimeoutControl = mockTimeoutControl.Object;

            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderConnection = "upgrade" }, input.Http1Connection);

            // Add some input and read it to start PumpAsync
            input.Add("a");
            var readResult = await body.ReadAsync();
            Assert.Equal(1, readResult.Buffer.Length);

            // need to advance to make PipeReader in ReadCompleted state
            body.AdvanceTo(readResult.Buffer.End);

            input.Fin();

            Assert.True((await body.ReadAsync()).IsCompleted);

            mockTimeoutControl.Verify(timeoutControl => timeoutControl.StartRequestBody(minReadRate), Times.Never);
            mockTimeoutControl.Verify(timeoutControl => timeoutControl.StopRequestBody(), Times.Never);

            // Due to the limits set on HttpProtocol.RequestBodyPipe, backpressure should be triggered on every
            // write to that pipe. Verify that read timing pause and resume are not called on upgrade
            // requests.
            mockTimeoutControl.Verify(timeoutControl => timeoutControl.StopTimingRead(), Times.Never);
            mockTimeoutControl.Verify(timeoutControl => timeoutControl.StartTimingRead(), Times.Never);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task CancelPendingReadContentLengthWorks()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            var readResultTask = reader.ReadAsync();

            reader.CancelPendingRead();

            var readResult = await readResultTask;

            Assert.True(readResult.IsCanceled);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task CancelPendingReadChunkedWorks()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            var readResultTask = reader.ReadAsync();

            reader.CancelPendingRead();

            var readResult = await readResultTask;

            Assert.True(readResult.IsCanceled);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task CancelPendingReadUpgradeWorks()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderConnection = "upgrade" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            var readResultTask = reader.ReadAsync();

            reader.CancelPendingRead();

            var readResult = await readResultTask;

            Assert.True(readResult.IsCanceled);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task CancelPendingReadForZeroContentLengthCannotBeCanceled()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders(), input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            var readResultTask = reader.ReadAsync();

            Assert.True(readResultTask.IsCompleted);

            reader.CancelPendingRead();

            var readResult = await readResultTask;

            Assert.False(readResult.IsCanceled);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task TryReadReturnsCompletedResultAfterReadingEntireContentLength()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("Hello");

            Assert.True(reader.TryRead(out var readResult));

            Assert.True(readResult.IsCompleted);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task TryReadReturnsCompletedResultAfterReadingEntireChunk()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("5\r\nHello\r\n");

            Assert.True(reader.TryRead(out var readResult));
            Assert.False(readResult.IsCompleted);
            AssertASCII("Hello", readResult.Buffer);

            reader.AdvanceTo(readResult.Buffer.End);

            input.Add("0\r\n\r\n");
            Assert.True(reader.TryRead(out readResult));

            Assert.True(readResult.IsCompleted);
            reader.AdvanceTo(readResult.Buffer.End);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task TryReadDoesNotReturnCompletedReadResultFromUpgradeStreamUntilCompleted()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderConnection = "upgrade" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("Hello");

            Assert.True(reader.TryRead(out var readResult));
            Assert.False(readResult.IsCompleted);
            AssertASCII("Hello", readResult.Buffer);

            reader.AdvanceTo(readResult.Buffer.End);

            input.Fin();

            reader.TryRead(out readResult);
            Assert.True(readResult.IsCompleted);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task TryReadDoesReturnsCompletedReadResultForZeroContentLength()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders(), input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("Hello");

            Assert.True(reader.TryRead(out var readResult));
            Assert.True(readResult.IsCompleted);

            reader.AdvanceTo(readResult.Buffer.End);

            reader.TryRead(out readResult);
            Assert.True(readResult.IsCompleted);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task CompleteForContentLengthAllowsConsumeToWork()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("a");

            Assert.True(reader.TryRead(out var readResult));

            Assert.False(readResult.IsCompleted);

            input.Add("asdf");

            reader.AdvanceTo(readResult.Buffer.End);
            reader.Complete();

            await body.ConsumeAsync();
        }
    }

    [Fact]
    public async Task CompleteForContentLengthDoesNotCompleteConnectionPipeMakesReadReturnThrow()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("a");

            Assert.True(reader.TryRead(out var readResult));

            Assert.False(readResult.IsCompleted);

            input.Add("asdf");

            reader.Complete();
            reader.AdvanceTo(readResult.Buffer.End);

            Assert.Throws<InvalidOperationException>(() => reader.TryRead(out readResult));

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task UnexpectedEndOfRequestContentIsRepeatedlyThrownForContentLengthBody()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Application.Output.Complete();

#pragma warning disable CS0618 // Type or member is obsolete
            var ex0 = Assert.Throws<BadHttpRequestException>(() => reader.TryRead(out var readResult));
            var ex1 = Assert.Throws<BadHttpRequestException>(() => reader.TryRead(out var readResult));
            var ex2 = await Assert.ThrowsAsync<BadHttpRequestException>(() => reader.ReadAsync().AsTask());
            var ex3 = await Assert.ThrowsAsync<BadHttpRequestException>(() => reader.ReadAsync().AsTask());
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex0.Reason);
            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex1.Reason);
            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex2.Reason);
            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex3.Reason);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task UnexpectedEndOfRequestContentIsRepeatedlyThrownForContentLengthBodyAfterExaminingButNotConsumingBytes()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderContentLength = "5" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            await input.Application.Output.WriteAsync(new byte[] { 0 });

            var readResult = await reader.ReadAsync();

            reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            input.Application.Output.Complete();

#pragma warning disable CS0618 // Type or member is obsolete
            var ex0 = Assert.Throws<BadHttpRequestException>(() => reader.TryRead(out var readResult));
            var ex1 = Assert.Throws<BadHttpRequestException>(() => reader.TryRead(out var readResult));
            var ex2 = await Assert.ThrowsAsync<BadHttpRequestException>(() => reader.ReadAsync().AsTask());
            var ex3 = await Assert.ThrowsAsync<BadHttpRequestException>(() => reader.ReadAsync().AsTask());
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex0.Reason);
            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex1.Reason);
            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex2.Reason);
            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex3.Reason);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task UnexpectedEndOfRequestContentIsRepeatedlyThrownForChunkedBody()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Application.Output.Complete();

#pragma warning disable CS0618 // Type or member is obsolete
            var ex0 = Assert.Throws<BadHttpRequestException>(() => reader.TryRead(out var readResult));
            var ex1 = Assert.Throws<BadHttpRequestException>(() => reader.TryRead(out var readResult));
            var ex2 = await Assert.ThrowsAsync<BadHttpRequestException>(() => reader.ReadAsync().AsTask());
            var ex3 = await Assert.ThrowsAsync<BadHttpRequestException>(() => reader.ReadAsync().AsTask());
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex0.Reason);
            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex1.Reason);
            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex2.Reason);
            Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, ex3.Reason);

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task CompleteForChunkedAllowsConsumeToWork()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("5\r\nHello\r\n");

            Assert.True(reader.TryRead(out var readResult));

            Assert.False(readResult.IsCompleted);
            reader.AdvanceTo(readResult.Buffer.End);

            input.Add("1\r\nH\r\n0\r\n\r\n");

            reader.Complete();

            await body.ConsumeAsync();
        }
    }

    [Fact]
    public async Task CompleteForChunkedDoesNotCompleteConnectionPipeMakesReadThrow()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderTransferEncoding = "chunked" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("5\r\nHello\r\n");

            Assert.True(reader.TryRead(out var readResult));

            Assert.False(readResult.IsCompleted);
            reader.AdvanceTo(readResult.Buffer.End);

            input.Add("1\r\nH\r\n");

            reader.Complete();

            Assert.Throws<InvalidOperationException>(() => reader.TryRead(out readResult));

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task CompleteForUpgradeDoesNotCompleteConnectionPipeMakesReadThrow()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders { HeaderConnection = "upgrade" }, input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("asdf");

            Assert.True(reader.TryRead(out var readResult));

            Assert.False(readResult.IsCompleted);
            reader.AdvanceTo(readResult.Buffer.End);

            input.Add("asdf");

            reader.Complete();

            Assert.Throws<InvalidOperationException>(() => reader.TryRead(out readResult));

            await body.StopAsync();
        }
    }

    [Fact]
    public async Task CompleteForZeroByteBodyDoesNotCompleteConnectionPipeNoopsReads()
    {
        using (var input = new TestInput())
        {
            var body = Http1MessageBody.For(HttpVersion.Http11, new HttpRequestHeaders(), input.Http1Connection);
            var reader = new HttpRequestPipeReader();
            reader.StartAcceptingReads(body);

            input.Add("asdf");

            Assert.True(reader.TryRead(out var readResult));

            Assert.True(readResult.IsCompleted);
            reader.AdvanceTo(readResult.Buffer.End);

            input.Add("asdf");

            reader.Complete();

            // TODO should this noop or throw? I think we should keep parity with normal pipe behavior.
            // So maybe this should throw
            reader.TryRead(out readResult);

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

    private void AssertASCII(string expected, ReadOnlySequence<byte> actual)
    {
        var arr = actual.ToArray();
        var encoding = Encoding.ASCII;
        var bytes = encoding.GetBytes(expected);
        Assert.Equal(bytes.Length, actual.Length);
        for (var index = 0; index < bytes.Length; index++)
        {
            Assert.Equal(bytes[index], arr[index]);
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
