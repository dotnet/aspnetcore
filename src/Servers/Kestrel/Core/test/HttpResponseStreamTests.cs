// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HttpResponseStreamTests
    {
        [Fact]
        public void CanReadReturnsFalse()
        {
            var stream = CreateHttpResponseStream();
            Assert.False(stream.CanRead);
        }

        [Fact]
        public void CanSeekReturnsFalse()
        {
            var stream = CreateHttpResponseStream();
            Assert.False(stream.CanSeek);
        }

        [Fact]
        public void CanWriteReturnsTrue()
        {
            var stream = CreateHttpResponseStream();
            Assert.True(stream.CanWrite);
        }

        [Fact]
        public void ReadThrows()
        {
            var stream = CreateHttpResponseStream();
            Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
        }

        [Fact]
        public void ReadByteThrows()
        {
            var stream = CreateHttpResponseStream();
            Assert.Throws<NotSupportedException>(() => stream.ReadByte());
        }

        [Fact]
        public async Task ReadAsyncThrows()
        {
            var stream = CreateHttpResponseStream();
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.ReadAsync(new byte[1], 0, 1));
        }

        [Fact]
        public void BeginReadThrows()
        {
            var stream = CreateHttpResponseStream();
            Assert.Throws<NotSupportedException>(() => stream.BeginRead(new byte[1], 0, 1, null, null));
        }

        [Fact]
        public void SeekThrows()
        {
            var stream = CreateHttpResponseStream();
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void LengthThrows()
        {
            var stream = CreateHttpResponseStream();
            Assert.Throws<NotSupportedException>(() => stream.Length);
        }

        [Fact]
        public void SetLengthThrows()
        {
            var stream = CreateHttpResponseStream();
            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
        }

        [Fact]
        public void PositionThrows()
        {
            var stream = CreateHttpResponseStream();
            Assert.Throws<NotSupportedException>(() => stream.Position);
            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        }

        [Fact]
        public void StopAcceptingWritesCausesWriteToThrowObjectDisposedException()
        {
            var pipeWriter = new HttpResponsePipeWriter(Mock.Of<IHttpResponseControl>());
            var stream = new HttpResponseStream(Mock.Of<IHttpBodyControlFeature>(), pipeWriter);
            pipeWriter.StartAcceptingWrites();
            pipeWriter.StopAcceptingWritesAsync();
            var ex = Assert.Throws<ObjectDisposedException>(() => { stream.WriteAsync(new byte[1], 0, 1); });
            Assert.Contains(CoreStrings.WritingToResponseBodyAfterResponseCompleted, ex.Message);
        }

        [Fact]
        public async Task SynchronousWritesThrowIfDisallowedByIHttpBodyControlFeature()
        {
            var allowSynchronousIO = false;
            var mockBodyControl = new Mock<IHttpBodyControlFeature>();
            mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(() => allowSynchronousIO);
            var mockHttpResponseControl = new Mock<IHttpResponseControl>();
            mockHttpResponseControl.Setup(m => m.WritePipeAsync(It.IsAny<ReadOnlyMemory<byte>>(), CancellationToken.None)).Returns(new ValueTask<FlushResult>(new FlushResult()));

            var pipeWriter = new HttpResponsePipeWriter(mockHttpResponseControl.Object);
            var stream = new HttpResponseStream(mockBodyControl.Object, pipeWriter);
            pipeWriter.StartAcceptingWrites();

            // WriteAsync doesn't throw.
            await stream.WriteAsync(new byte[1], 0, 1);

            var ioEx = Assert.Throws<InvalidOperationException>(() => stream.Write(new byte[1], 0, 1));
            Assert.Equal("Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true instead.", ioEx.Message);

            allowSynchronousIO = true;
            // If IHttpBodyControlFeature.AllowSynchronousIO is true, Write no longer throws.
            stream.Write(new byte[1], 0, 1);
        }

        private static HttpResponseStream CreateHttpResponseStream()
        {
            var pipeWriter = new HttpResponsePipeWriter(Mock.Of<IHttpResponseControl>());
            return new HttpResponseStream(Mock.Of<IHttpBodyControlFeature>(), pipeWriter);
        }
    }
}
