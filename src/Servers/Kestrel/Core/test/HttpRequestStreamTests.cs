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
    public class HttpRequestStreamTests
    {
        [Fact]
        public void CanReadReturnsTrue()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void CanSeekReturnsFalse()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.False(stream.CanSeek);
        }

        [Fact]
        public void CanWriteReturnsFalse()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public void SeekThrows()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void LengthThrows()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.Length);
        }

        [Fact]
        public void SetLengthThrows()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
        }

        [Fact]
        public void PositionThrows()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.Position);
            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        }

        [Fact]
        public void WriteThrows()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
        }

        [Fact]
        public void WriteByteThrows()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            Assert.Throws<NotSupportedException>(() => stream.WriteByte(0));
        }

        [Fact]
        public async Task WriteAsyncThrows()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.WriteAsync(new byte[1], 0, 1));
        }

        [Fact]
        // Read-only streams should support Flush according to https://github.com/dotnet/corefx/pull/27327#pullrequestreview-98384813
        public void FlushDoesNotThrow()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.Flush();
        }

        [Fact]
        public async Task FlushAsyncDoesNotThrow()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            await stream.FlushAsync();
        }

        [Fact]
        public async Task SynchronousReadsThrowIfDisallowedByIHttpBodyControlFeature()
        {
            var allowSynchronousIO = false;
            var mockBodyControl = new Mock<IHttpBodyControlFeature>();
            mockBodyControl.Setup(m => m.AllowSynchronousIO).Returns(() => allowSynchronousIO);
            var mockMessageBody = new Mock<MessageBody>(null, null);
            mockMessageBody.Setup(m => m.ReadAsync(It.IsAny<Memory<byte>>(), CancellationToken.None)).Returns(new ValueTask<int>(0));

            var stream = new HttpRequestStream(mockBodyControl.Object);
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
        public async Task AbortCausesReadToCancel()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            stream.Abort();
            await Assert.ThrowsAsync<TaskCanceledException>(() => stream.ReadAsync(new byte[1], 0, 1));
        }

        [Fact]
        public async Task AbortWithErrorCausesReadToCancel()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            var error = new Exception();
            stream.Abort(error);
            var exception = await Assert.ThrowsAsync<Exception>(() => stream.ReadAsync(new byte[1], 0, 1));
            Assert.Same(error, exception);
        }

        [Fact]
        public void StopAcceptingReadsCausesReadToThrowObjectDisposedException()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            stream.StopAcceptingReads();
            Assert.Throws<ObjectDisposedException>(() => { stream.ReadAsync(new byte[1], 0, 1); });
        }

        [Fact]
        public async Task AbortCausesCopyToAsyncToCancel()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            stream.Abort();
            await Assert.ThrowsAsync<TaskCanceledException>(() => stream.CopyToAsync(Mock.Of<Stream>()));
        }

        [Fact]
        public async Task AbortWithErrorCausesCopyToAsyncToCancel()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            var error = new Exception();
            stream.Abort(error);
            var exception = await Assert.ThrowsAsync<Exception>(() => stream.CopyToAsync(Mock.Of<Stream>()));
            Assert.Same(error, exception);
        }

        [Fact]
        public void StopAcceptingReadsCausesCopyToAsyncToThrowObjectDisposedException()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            stream.StopAcceptingReads();
            Assert.Throws<ObjectDisposedException>(() => { stream.CopyToAsync(Mock.Of<Stream>()); });
        }

        [Fact]
        public void NullDestinationCausesCopyToAsyncToThrowArgumentNullException()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            Assert.Throws<ArgumentNullException>(() => { stream.CopyToAsync(null); });
        }

        [Fact]
        public void ZeroBufferSizeCausesCopyToAsyncToThrowArgumentException()
        {
            var stream = new HttpRequestStream(Mock.Of<IHttpBodyControlFeature>());
            stream.StartAcceptingReads(null);
            Assert.Throws<ArgumentException>(() => { stream.CopyToAsync(Mock.Of<Stream>(), 0); });
        }
    }
}
