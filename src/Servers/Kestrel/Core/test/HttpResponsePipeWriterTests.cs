// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HttpResponsePipeWriterTests
    {
        [Fact]
        public void AdvanceAfterStopAcceptingWritesThrowsObjectDisposedException()
        {
            var pipeWriter = CreateHttpResponsePipeWriter();
            pipeWriter.StartAcceptingWrites();
            pipeWriter.StopAcceptingWritesAsync();
            var ex = Assert.Throws<ObjectDisposedException>(() => { pipeWriter.Advance(1); });
            Assert.Contains(CoreStrings.WritingToResponseBodyAfterResponseCompleted, ex.Message);
        }

        [Fact]
        public void GetMemoryAfterStopAcceptingWritesThrowsObjectDisposedException()
        {
            var pipeWriter = CreateHttpResponsePipeWriter();
            pipeWriter.StartAcceptingWrites();
            pipeWriter.StopAcceptingWritesAsync();
            var ex = Assert.Throws<ObjectDisposedException>(() => { pipeWriter.GetMemory(); });
            Assert.Contains(CoreStrings.WritingToResponseBodyAfterResponseCompleted, ex.Message);
        }

        [Fact]
        public void GetSpanAfterStopAcceptingWritesThrowsObjectDisposedException()
        {
            var pipeWriter = CreateHttpResponsePipeWriter();
            pipeWriter.StartAcceptingWrites();
            pipeWriter.StopAcceptingWritesAsync();
            var ex = Assert.Throws<ObjectDisposedException>(() => { pipeWriter.GetSpan(); });
            Assert.Contains(CoreStrings.WritingToResponseBodyAfterResponseCompleted, ex.Message);
        }

        [Fact]
        public void CompleteAfterStopAcceptingWritesThrowsObjectDisposedException()
        {
            var pipeWriter = CreateHttpResponsePipeWriter();
            pipeWriter.StartAcceptingWrites();
            pipeWriter.StopAcceptingWritesAsync();
            var ex = Assert.Throws<ObjectDisposedException>(() => { pipeWriter.Complete(); });
            Assert.Contains(CoreStrings.WritingToResponseBodyAfterResponseCompleted, ex.Message);
        }

        [Fact]
        public void FlushAsyncAfterStopAcceptingWritesThrowsObjectDisposedException()
        {
            var pipeWriter = CreateHttpResponsePipeWriter();
            pipeWriter.StartAcceptingWrites();
            pipeWriter.StopAcceptingWritesAsync();
            var ex = Assert.Throws<ObjectDisposedException>(() => { pipeWriter.FlushAsync(); });
            Assert.Contains(CoreStrings.WritingToResponseBodyAfterResponseCompleted, ex.Message);
        }

        [Fact]
        public void WriteAsyncAfterStopAcceptingWritesThrowsObjectDisposedException()
        {
            var pipeWriter = CreateHttpResponsePipeWriter();
            pipeWriter.StartAcceptingWrites();
            pipeWriter.StopAcceptingWritesAsync();
            var ex = Assert.Throws<ObjectDisposedException>(() => { pipeWriter.WriteAsync(new Memory<byte>()); });
            Assert.Contains(CoreStrings.WritingToResponseBodyAfterResponseCompleted, ex.Message);
        }

        private static HttpResponsePipeWriter CreateHttpResponsePipeWriter()
        {
            return new HttpResponsePipeWriter(Mock.Of<IHttpResponseControl>());
        }
    }
}
