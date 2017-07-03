// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class FrameRequestStreamTests
    {
        [Fact]
        public void CanReadReturnsTrue()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void CanSeekReturnsFalse()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.False(stream.CanSeek);
        }

        [Fact]
        public void CanWriteReturnsFalse()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public void SeekThrows()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void LengthThrows()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.Length);
        }

        [Fact]
        public void SetLengthThrows()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
        }

        [Fact]
        public void PositionThrows()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.Position);
            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        }

        [Fact]
        public void WriteThrows()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
        }

        [Fact]
        public void WriteByteThrows()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.WriteByte(0));
        }

        [Fact]
        public async Task WriteAsyncThrows()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.WriteAsync(new byte[1], 0, 1));
        }

#if NET461
        [Fact]
        public void BeginWriteThrows()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.BeginWrite(new byte[1], 0, 1, null, null));
        }
#elif NETCOREAPP2_0
#else
#error Target framework needs to be updated
#endif

        [Fact]
        public void FlushThrows()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.Flush());
        }

        [Fact]
        public async Task FlushAsyncThrows()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.FlushAsync());
        }

        [Fact]
        public async Task SynchronousReadsThrowIfDisallowedByIHttpBodyControlFeature()
        {
            var allowSynchronousIO = false;
            var mockBodyControl = new Mock<IHttpBodyControlFeature>();
            mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(() => allowSynchronousIO);
            var mockMessageBody = new Mock<MessageBody>((Frame)null);
            mockMessageBody.Setup(m => m.ReadAsync(It.IsAny<ArraySegment<byte>>(), CancellationToken.None)).ReturnsAsync(0);

            var stream = new FrameRequestStream(mockBodyControl.Object);
            stream.StartAcceptingReads(mockMessageBody.Object);

            Assert.Equal(0, await stream.ReadAsync(new byte[1], 0, 1));

            var ioEx = Assert.Throws<InvalidOperationException>(() => stream.Read(new byte[1], 0, 1));
            Assert.Equal("Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead.", ioEx.Message);

            var ioEx2 = Assert.Throws<InvalidOperationException>(() => stream.CopyTo(Stream.Null));
            Assert.Equal("Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead.", ioEx2.Message);

            allowSynchronousIO = true;
            Assert.Equal(0, stream.Read(new byte[1], 0, 1));
        }

        [Fact]
        public void AbortCausesReadToCancel()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            stream.Abort();
            var task = stream.ReadAsync(new byte[1], 0, 1);
            Assert.True(task.IsCanceled);
        }

        [Fact]
        public void AbortWithErrorCausesReadToCancel()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            var error = new Exception();
            stream.Abort(error);
            var task = stream.ReadAsync(new byte[1], 0, 1);
            Assert.True(task.IsFaulted);
            Assert.Same(error, task.Exception.InnerException);
        }

        [Fact]
        public void StopAcceptingReadsCausesReadToThrowObjectDisposedException()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            stream.StopAcceptingReads();
            Assert.Throws<ObjectDisposedException>(() => { stream.ReadAsync(new byte[1], 0, 1); });
        }

        [Fact]
        public void AbortCausesCopyToAsyncToCancel()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            stream.Abort();
            var task = stream.CopyToAsync(Mock.Of<Stream>());
            Assert.True(task.IsCanceled);
        }

        [Fact]
        public void AbortWithErrorCausesCopyToAsyncToCancel()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            var error = new Exception();
            stream.Abort(error);
            var task = stream.CopyToAsync(Mock.Of<Stream>());
            Assert.True(task.IsFaulted);
            Assert.Same(error, task.Exception.InnerException);
        }

        [Fact]
        public void StopAcceptingReadsCausesCopyToAsyncToThrowObjectDisposedException()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            stream.StopAcceptingReads();
            Assert.Throws<ObjectDisposedException>(() => { stream.CopyToAsync(Mock.Of<Stream>()); });
        }

        [Fact]
        public void NullDestinationCausesCopyToAsyncToThrowArgumentNullException()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            Assert.Throws<ArgumentNullException>(() => { stream.CopyToAsync(null); });
        }

        [Fact]
        public void ZeroBufferSizeCausesCopyToAsyncToThrowArgumentException()
        {
            var stream = new FrameRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            Assert.Throws<ArgumentException>(() => { stream.CopyToAsync(Mock.Of<Stream>(), 0); });
        }
    }
}
