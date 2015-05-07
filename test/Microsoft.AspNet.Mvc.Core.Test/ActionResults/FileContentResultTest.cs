// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Routing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class FileContentResultTest
    {
        [Fact]
        public void Constructor_SetsFileContents()
        {
            // Arrange
            var fileContents = new byte[0];

            // Act
            var result = new FileContentResult(fileContents, "text/plain");

            // Assert
            Assert.Same(fileContents, result.FileContents);
        }

        [Fact]
        public async Task WriteFileAsync_CopiesBuffer_ToOutputStream()
        {
            // Arrange
            var buffer = new byte[] { 1, 2, 3, 4, 5 };

            var httpContext = new DefaultHttpContext();

            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new FileContentResult(buffer, "text/plain");

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(buffer, outStream.ToArray());
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding()
        {
            // Arrange
            var expectedContentType = "text/foo; charset=us-ascii";
            var buffer = new byte[] { 1, 2, 3, 4, 5 };

            var httpContext = new DefaultHttpContext();

            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new FileContentResult(buffer, MediaTypeHeaderValue.Parse(expectedContentType));

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(buffer, outStream.ToArray());
            Assert.Equal(expectedContentType, httpContext.Response.ContentType);
        }
    }
}