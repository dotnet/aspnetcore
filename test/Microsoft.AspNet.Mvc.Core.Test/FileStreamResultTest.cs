// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
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

            var result = new FileStreamResult(originalStream, MediaTypeHeaderValue.Parse(expectedContentType));

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            var outBytes = outStream.ToArray();
            Assert.True(originalBytes.SequenceEqual(outBytes));
            Assert.Equal(expectedContentType, httpContext.Response.ContentType);
        }

        [Fact]
        public async Task DisablesResponseBuffering_IfBufferingFeatureAvailable()
        {
            // Arrange
            var expectedContentType = "text/foo; charset=us-ascii";
            var expected = Encoding.ASCII.GetBytes("Test data");

            var originalStream = new MemoryStream(expected);

            var httpContext = GetHttpContext();
            var bufferingFeature = new TestBufferingFeature();
            httpContext.Features.Set<IHttpBufferingFeature>(bufferingFeature);
            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var result = new FileStreamResult(originalStream, MediaTypeHeaderValue.Parse(expectedContentType));

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            var outBytes = outStream.ToArray();
            Assert.Equal(expected, outBytes);
            Assert.Equal(expectedContentType, httpContext.Response.ContentType);
            Assert.True(bufferingFeature.DisableResponseBufferingInvoked);
        }

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
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