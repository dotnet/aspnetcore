// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
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
        public void Constructor_SetsContentTypeAndParameters()
        {
            // Arrange
            var stream = Stream.Null;
            var contentType = "text/plain; charset=us-ascii; p1=p1-value";
            var expectedMediaType = contentType;

            // Act
            var result = new FileStreamResult(stream, contentType);

            // Assert
            Assert.Equal(stream, result.FileStream);
            MediaTypeAssert.Equal(expectedMediaType, result.ContentType);
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

            var httpContext = GetHttpContext();
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

            var httpContext = GetHttpContext();
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

        [Fact]
        public async Task SetsSuppliedContentTypeAndEncoding()
        {
            // Arrange
            var expectedContentType = "text/foo; charset=us-ascii";
            // Generate an array of bytes with a predictable pattern
            // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, A, B, C, D, E, F, 10, 11, 12, 13
            var originalBytes = Enumerable.Range(0, 0x1234)
                .Select(b => (byte)(b % 20)).ToArray();

            var originalStream = new MemoryStream(originalBytes);

            var httpContext = GetHttpContext();
            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new FileStreamResult(originalStream, expectedContentType);

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            var outBytes = outStream.ToArray();
            Assert.True(originalBytes.SequenceEqual(outBytes));
            Assert.Equal(expectedContentType, httpContext.Response.ContentType);
        }

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<FileStreamResultExecutor>();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            return services;
        }

        private static HttpContext GetHttpContext()
        {
            var services = CreateServices();

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services.BuildServiceProvider();

            return httpContext;
        }
    }
}