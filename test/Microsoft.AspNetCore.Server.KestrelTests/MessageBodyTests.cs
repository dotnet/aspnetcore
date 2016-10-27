// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Microsoft.Extensions.Internal;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    /// <summary>
    /// Summary description for MessageBodyTests
    /// </summary>
    public class MessageBodyTests
    {
        [Fact]
        public void Http10ConnectionClose()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http10, new FrameRequestHeaders { HeaderContentLength = "5" }, input.FrameContext);
                var stream = new FrameRequestStream();
                stream.StartAcceptingReads(body);

                input.Add("Hello", true);

                var buffer1 = new byte[1024];
                var count1 = stream.Read(buffer1, 0, 1024);
                AssertASCII("Hello", new ArraySegment<byte>(buffer1, 0, 5));

                var buffer2 = new byte[1024];
                var count2 = stream.Read(buffer2, 0, 1024);
                Assert.Equal(0, count2);
            }
        }

        [Fact]
        public async Task Http10ConnectionCloseAsync()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http10, new FrameRequestHeaders { HeaderContentLength = "5" }, input.FrameContext);
                var stream = new FrameRequestStream();
                stream.StartAcceptingReads(body);

                input.Add("Hello", true);

                var buffer1 = new byte[1024];
                var count1 = await stream.ReadAsync(buffer1, 0, 1024);
                AssertASCII("Hello", new ArraySegment<byte>(buffer1, 0, 5));

                var buffer2 = new byte[1024];
                var count2 = await stream.ReadAsync(buffer2, 0, 1024);
                Assert.Equal(0, count2);
            }
        }

        [Fact]
        public void Http10NoContentLength()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http10, new FrameRequestHeaders(), input.FrameContext);
                var stream = new FrameRequestStream();
                stream.StartAcceptingReads(body);

                input.Add("Hello", true);

                var buffer1 = new byte[1024];
                Assert.Equal(0, stream.Read(buffer1, 0, 1024));
            }
        }

        [Fact]
        public async Task Http10NoContentLengthAsync()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http10, new FrameRequestHeaders(), input.FrameContext);
                var stream = new FrameRequestStream();
                stream.StartAcceptingReads(body);

                input.Add("Hello", true);

                var buffer1 = new byte[1024];
                Assert.Equal(0, await stream.ReadAsync(buffer1, 0, 1024));
            }
        }

        [Fact]
        public void Http11NoContentLength()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http11, new FrameRequestHeaders(), input.FrameContext);
                var stream = new FrameRequestStream();
                stream.StartAcceptingReads(body);

                input.Add("Hello", true);

                var buffer1 = new byte[1024];
                Assert.Equal(0, stream.Read(buffer1, 0, 1024));
            }
        }

        [Fact]
        public async Task Http11NoContentLengthAsync()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http11, new FrameRequestHeaders(), input.FrameContext);
                var stream = new FrameRequestStream();
                stream.StartAcceptingReads(body);

                input.Add("Hello", true);

                var buffer1 = new byte[1024];
                Assert.Equal(0, await stream.ReadAsync(buffer1, 0, 1024));
            }
        }

        [Fact]
        public void Http11NoContentLengthConnectionClose()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http11, new FrameRequestHeaders { HeaderConnection = "close" }, input.FrameContext);
                var stream = new FrameRequestStream();
                stream.StartAcceptingReads(body);

                input.Add("Hello", true);

                var buffer1 = new byte[1024];
                Assert.Equal(0, stream.Read(buffer1, 0, 1024));
            }
        }

        [Fact]
        public async Task Http11NoContentLengthConnectionCloseAsync()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http11, new FrameRequestHeaders { HeaderConnection = "close" }, input.FrameContext);
                var stream = new FrameRequestStream();
                stream.StartAcceptingReads(body);

                input.Add("Hello", true);

                var buffer1 = new byte[1024];
                Assert.Equal(0, await stream.ReadAsync(buffer1, 0, 1024));
            }
        }

        [Fact]
        public async Task CanHandleLargeBlocks()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http10, new FrameRequestHeaders { HeaderContentLength = "8197" }, input.FrameContext);
                var stream = new FrameRequestStream();
                stream.StartAcceptingReads(body);

                // Input needs to be greater than 4032 bytes to allocate a block not backed by a slab.
                var largeInput = new string('a', 8192);

                input.Add(largeInput);
                // Add a smaller block to the end so that SocketInput attempts to return the large
                // block to the memory pool.
                input.Add("Hello", fin: true);

                var ms = new MemoryStream();

                await stream.CopyToAsync(ms);
                var requestArray = ms.ToArray();
                Assert.Equal(8197, requestArray.Length);
                AssertASCII(largeInput + "Hello", new ArraySegment<byte>(requestArray, 0, requestArray.Length));

                var count = await stream.ReadAsync(new byte[1], 0, 1);
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public void ForThrowsWhenFinalTransferCodingIsNotChunked()
        {
            using (var input = new TestInput())
            {
                var ex = Assert.Throws<BadHttpRequestException>(() =>
                    MessageBody.For(HttpVersion.Http10, new FrameRequestHeaders { HeaderTransferEncoding = "chunked, not-chunked" }, input.FrameContext));

                Assert.Equal(400, ex.StatusCode);
                Assert.Equal("Final transfer coding is not \"chunked\": \"chunked, not-chunked\"", ex.Message);
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
            new object[] { new FrameRequestHeaders { HeaderContentLength = "12" }, new[] { "Hello ", "World!" } },
            // Chunked
            new object[] { new FrameRequestHeaders { HeaderTransferEncoding = "chunked" }, new[] { "6\r\nHello \r\n", "6\r\nWorld!\r\n0\r\n\r\n" } },
        };

        public static IEnumerable<object[]> CombinedData => 
            from stream in StreamData
            from request in RequestData
            select new[] { stream[0], request[0], request[1] };

        [Theory]
        [MemberData(nameof(RequestData))]
        public async Task CopyToAsyncDoesNotCopyBlocks(FrameRequestHeaders headers, string[] data)
        {
            var writeCount = 0;
            var writeTcs = new TaskCompletionSource<byte[]>();
            var mockDestination = new Mock<Stream>();

            mockDestination
                .Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), CancellationToken.None))
                .Callback((byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                {
                    writeTcs.SetResult(buffer);
                    writeCount++;
                })
                .Returns(TaskCache.CompletedTask);

            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http11, headers, input.FrameContext);

                var copyToAsyncTask = body.CopyToAsync(mockDestination.Object);

                // The block returned by IncomingStart always has at least 2048 available bytes,
                // so no need to bounds check in this test.
                var socketInput = input.FrameContext.SocketInput;
                var bytes = Encoding.ASCII.GetBytes(data[0]);
                var block = socketInput.IncomingStart();
                Buffer.BlockCopy(bytes, 0, block.Array, block.End, bytes.Length);
                socketInput.IncomingComplete(bytes.Length, null);

                // Verify the block passed to WriteAsync is the same one incoming data was written into.
                Assert.Same(block.Array, await writeTcs.Task);

                writeTcs = new TaskCompletionSource<byte[]>();
                bytes = Encoding.ASCII.GetBytes(data[1]);
                block = socketInput.IncomingStart();
                Buffer.BlockCopy(bytes, 0, block.Array, block.End, bytes.Length);
                socketInput.IncomingComplete(bytes.Length, null);

                Assert.Same(block.Array, await writeTcs.Task);

                if (headers.HeaderConnection == "close")
                {
                    socketInput.IncomingFin();
                }

                await copyToAsyncTask;

                Assert.Equal(2, writeCount);
            }
        }

        [Theory]
        [MemberData(nameof(CombinedData))]
        public async Task CopyToAsyncAdvancesRequestStreamWhenDestinationWriteAsyncThrows(Stream writeStream, FrameRequestHeaders headers, string[] data)
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http11, headers, input.FrameContext);

                input.Add(data[0]);

                await Assert.ThrowsAsync<XunitException>(() => body.CopyToAsync(writeStream));

                input.Add(data[1], fin: headers.HeaderConnection == "close");

                // "Hello " should have been consumed
                var readBuffer = new byte[6];
                var count = await body.ReadAsync(new ArraySegment<byte>(readBuffer, 0, readBuffer.Length));
                Assert.Equal(6, count);
                AssertASCII("World!", new ArraySegment<byte>(readBuffer, 0, 6));

                count = await body.ReadAsync(new ArraySegment<byte>(readBuffer, 0, readBuffer.Length));
                Assert.Equal(0, count);
            }
        }

        [Theory]
        [InlineData("keep-alive, upgrade")]
        [InlineData("Keep-Alive, Upgrade")]
        [InlineData("upgrade, keep-alive")]
        [InlineData("Upgrade, Keep-Alive")]
        public void ConnectionUpgradeKeepAlive(string headerConnection)
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For(HttpVersion.Http11, new FrameRequestHeaders { HeaderConnection = headerConnection }, input.FrameContext);
                var stream = new FrameRequestStream();
                stream.StartAcceptingReads(body);

                input.Add("Hello", true);

                var buffer = new byte[1024];
                Assert.Equal(5, stream.Read(buffer, 0, 1024));
                AssertASCII("Hello", new ArraySegment<byte>(buffer, 0, 5));
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