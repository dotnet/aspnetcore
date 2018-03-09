// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCompression.Tests
{
    public class BodyWrapperStreamTests
    {
        [Theory]
        [InlineData(null, "Accept-Encoding")]
        [InlineData("", "Accept-Encoding")]
        [InlineData("AnotherHeader", "AnotherHeader,Accept-Encoding")]
        [InlineData("Accept-Encoding", "Accept-Encoding")]
        [InlineData("accepT-encodinG", "accepT-encodinG")]
        [InlineData("accept-encoding,AnotherHeader", "accept-encoding,AnotherHeader")]
        public void OnWrite_AppendsAcceptEncodingToVaryHeader_IfNotPresent(string providedVaryHeader, string expectedVaryHeader)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Headers[HeaderNames.Vary] = providedVaryHeader;
            var stream = new BodyWrapperStream(httpContext, new MemoryStream(), new MockResponseCompressionProvider(flushable: true), null, null);

            stream.Write(new byte[] { }, 0, 0);


            Assert.Equal(expectedVaryHeader, httpContext.Response.Headers[HeaderNames.Vary]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Write_IsPassedToUnderlyingStream_WhenDisableResponseBuffering(bool flushable)
        {

            var buffer = new byte[] { 1 };
            byte[] written = null;

            var mock = new Mock<Stream>();
            mock.SetupGet(s => s.CanWrite).Returns(true);
            mock.Setup(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback<byte[], int, int>((b, o, c) =>
                {
                    written = new ArraySegment<byte>(b, 0, c).ToArray();
                });

            var stream = new BodyWrapperStream(new DefaultHttpContext(), mock.Object, new MockResponseCompressionProvider(flushable), null, null);

            stream.DisableResponseBuffering();
            stream.Write(buffer, 0, buffer.Length);

            Assert.Equal(buffer, written);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WriteAsync_IsPassedToUnderlyingStream_WhenDisableResponseBuffering(bool flushable)
        {
            var buffer = new byte[] { 1 };

            var memoryStream = new MemoryStream();
            var stream = new BodyWrapperStream(new DefaultHttpContext(), memoryStream, new MockResponseCompressionProvider(flushable), null, null);

            stream.DisableResponseBuffering();
            await stream.WriteAsync(buffer, 0, buffer.Length);

            Assert.Equal(buffer, memoryStream.ToArray());
        }

        [Fact]
        public async Task SendFileAsync_IsPassedToUnderlyingStream_WhenDisableResponseBuffering()
        {
            var memoryStream = new MemoryStream();

            var stream = new BodyWrapperStream(new DefaultHttpContext(), memoryStream, new MockResponseCompressionProvider(true), null, null);

            stream.DisableResponseBuffering();

            var path = "testfile1kb.txt";
            await stream.SendFileAsync(path, 0, null, CancellationToken.None);

            Assert.Equal(File.ReadAllBytes(path), memoryStream.ToArray());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BeginWrite_IsPassedToUnderlyingStream_WhenDisableResponseBuffering(bool flushable)
        {
            var buffer = new byte[] { 1 };

            var memoryStream = new MemoryStream();

            var stream = new BodyWrapperStream(new DefaultHttpContext(), memoryStream, new MockResponseCompressionProvider(flushable), null, null);

            stream.DisableResponseBuffering();
            stream.BeginWrite(buffer, 0, buffer.Length, (o) => {}, null);

            Assert.Equal(buffer, memoryStream.ToArray());
        }

        private class MockResponseCompressionProvider: IResponseCompressionProvider
        {
            private readonly bool _flushable;

            public MockResponseCompressionProvider(bool flushable)
            {
                _flushable = flushable;
            }

            public ICompressionProvider GetCompressionProvider(HttpContext context)
            {
                return new MockCompressionProvider(_flushable);
            }

            public bool ShouldCompressResponse(HttpContext context)
            {
                return true;
            }

            public bool CheckRequestAcceptsCompression(HttpContext context)
            {
                return true;
            }
        }


        private class MockCompressionProvider : ICompressionProvider
        {
            public MockCompressionProvider(bool flushable)
            {
                SupportsFlush = flushable;
            }

            public string EncodingName { get; }

            public bool SupportsFlush { get; }

            public Stream CreateStream(Stream outputStream)
            {
                if (SupportsFlush)
                {
                    return new BufferedStream(outputStream);
                }
                else
                {
                    return new NoFlushBufferedStream(outputStream);
                }

            }
        }

        private class NoFlushBufferedStream : Stream
        {
            private readonly BufferedStream _bufferedStream;

            public NoFlushBufferedStream(Stream outputStream)
            {
                _bufferedStream = new BufferedStream(outputStream);
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count) => _bufferedStream.Read(buffer, offset, count);

            public override long Seek(long offset, SeekOrigin origin) => _bufferedStream.Seek(offset, origin);

            public override void SetLength(long value) => _bufferedStream.SetLength(value);

            public override void Write(byte[] buffer, int offset, int count) => _bufferedStream.Write(buffer, offset, count);

            public override bool CanRead => _bufferedStream.CanRead;

            public override bool CanSeek => _bufferedStream.CanSeek;

            public override bool CanWrite => _bufferedStream.CanWrite;

            public override long Length => _bufferedStream.Length;

            public override long Position
            {
                get { return _bufferedStream.Position; }
                set { _bufferedStream.Position = value; }
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                _bufferedStream.Flush();
            }
        }
    }
}
