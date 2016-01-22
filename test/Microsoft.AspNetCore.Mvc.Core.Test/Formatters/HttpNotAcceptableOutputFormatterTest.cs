// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.Formatters
{
    public class HttpNotAcceptableOutputFormatterTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(null)]
        public void CanWriteResult_ReturnsFalse_WhenConnegHasntFailed(bool? failedContentNegotiation)
        {
            // Arrange
            var formatter = new HttpNotAcceptableOutputFormatter();

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null)
            {
                FailedContentNegotiation = failedContentNegotiation,
            };

            // Act
            var result = formatter.CanWriteResult(context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanWriteResult_ReturnsTrue_WhenConnegHasFailed()
        {
            // Arrange
            var formatter = new HttpNotAcceptableOutputFormatter();

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null)
            {
                FailedContentNegotiation = true,
            };

            // Act
            var result = formatter.CanWriteResult(context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task WriteAsync_Sets406NotAcceptable()
        {
            // Arrange
            var formatter = new HttpNotAcceptableOutputFormatter();

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            // Act
             await formatter.WriteAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status406NotAcceptable, context.HttpContext.Response.StatusCode);
        }
    }
}
