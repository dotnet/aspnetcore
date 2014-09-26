// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class FileStreamResultTest
    {
        [Fact]
        public void Constructor_SetsFileName()
        {
            // Arrange
            var stream = Stream.Null;

            // Act
            var result = new FileStreamResult(stream, "text/plain");

            // Assert
            Assert.Equal(stream, result.FileStream);
        }

        [Fact]
        public async Task WriteFileAsync_WritesResponse_InChunksOfFourKilobytes()
        {
            // Arrange
            var mockReadStream = new Mock<Stream>();
            mockReadStream.SetupSequence(s => s.ReadAsync(It.IsAny<byte[]>(), 0, 0x1000, CancellationToken.None))
                .Returns(Task.FromResult(0x1000))
                .Returns(Task.FromResult(0x500))
                .Returns(Task.FromResult(0));

            var mockBodyStream = new Mock<Stream>();
            mockBodyStream
                .Setup(s => s.WriteAsync(It.IsAny<byte[]>(), 0, 0x1000, CancellationToken.None))
                .Returns(Task.FromResult(0));

            mockBodyStream
                .Setup(s => s.WriteAsync(It.IsAny<byte[]>(), 0, 0x500, CancellationToken.None))
                .Returns(Task.FromResult(0));

            var result = new FileStreamResult(mockReadStream.Object, "text/plain");

            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = mockBodyStream.Object;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            mockReadStream.Verify();
            mockBodyStream.Verify();
        }

        [Fact]
        public async Task WriteFileAsync_CopiesProvidedStream_ToOutputStream()
        {
            // Arrange
            // Generate an array of bytes with a predictable pattern
            // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, A, B, C, D, E, F, 10, 11, 12, 13
            var originalBytes = Enumerable.Range(0, 0x1234)
                .Select(b => (byte)(b % 20)).ToArray();

            var originalStream = new MemoryStream(originalBytes);

            var httpContext = new DefaultHttpContext();
            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new FileStreamResult(originalStream, "text/plain");

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            var outBytes = outStream.ToArray();
            Assert.True(originalBytes.SequenceEqual(outBytes));
        }
    }
}