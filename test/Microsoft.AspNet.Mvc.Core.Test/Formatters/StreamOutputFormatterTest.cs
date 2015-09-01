// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ActionResults;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Mvc.Formatters
{
    public class StreamOutputFormatterTest
    {
        [Theory]
        [InlineData(typeof(Stream), "text/plain")]
        [InlineData(typeof(Stream), null)]
        [InlineData(typeof(object), "text/plain")]
        [InlineData(typeof(object), null)]
        [InlineData(typeof(IActionResult), "text/plain")]
        [InlineData(typeof(IActionResult), null)]
        public void CanWriteResult_ReturnsTrue_ForStreams(Type declaredType, string contentType)
        {
            // Arrange
            var formatter = new StreamOutputFormatter();
            var contentTypeHeader = contentType == null ? null : new MediaTypeHeaderValue(contentType);
            var formatterContext = new OutputFormatterContext()
            {
                DeclaredType = declaredType,
                Object = new MemoryStream()
            };

            // Act
            var canWrite = formatter.CanWriteResult(formatterContext, contentTypeHeader);

            // Assert
            Assert.True(canWrite);
        }

        [Theory]
        [InlineData(typeof(object), "text/plain")]
        [InlineData(typeof(object), null)]
        [InlineData(typeof(SimplePOCO), "text/plain")]
        [InlineData(typeof(SimplePOCO), null)]
        public void CanWriteResult_OnlyActsOnStreams_IgnoringContentType(Type declaredType, string contentType)
        {
            // Arrange
            var formatter = new StreamOutputFormatter();
            var contentTypeHeader = contentType == null ? null : new MediaTypeHeaderValue(contentType);
            var formatterContext = new OutputFormatterContext()
            {
                DeclaredType = declaredType,
                Object = new SimplePOCO()
            };

            // Act
            var canWrite = formatter.CanWriteResult(formatterContext, contentTypeHeader);

            // Assert
            Assert.False(canWrite);
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(SimplePOCO))]
        [InlineData(null)]
        public void CanWriteResult_OnlyActsOnStreams(Type type)
        {
            // Arrange
            var formatter = new StreamOutputFormatter();
            var context = new OutputFormatterContext();
            var contentType = new MediaTypeHeaderValue("text/plain");

            context.Object = type != null ? Activator.CreateInstance(type) : null;

            // Act
            var result = formatter.CanWriteResult(context, contentType);

            // Assert
            Assert.False(result);
            Assert.Null(context.SelectedContentType);
        }

        [Fact]
        public void CanWriteResult_SetsContentType()
        {
            // Arrange
            var formatter = new StreamOutputFormatter();
            var contentType = new MediaTypeHeaderValue("text/plain");
            var context = new OutputFormatterContext();
            context.Object = new MemoryStream();

            // Act
            var result = formatter.CanWriteResult(context, contentType);

            // Assert
            Assert.True(result);
            Assert.Same(contentType, context.SelectedContentType);
        }

        [Fact]
        public async Task DisablesResponseBuffering_IfBufferingFeatureAvailable()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes("Test data");
            var formatter = new StreamOutputFormatter();
            var httpContext = new DefaultHttpContext();
            var ms = new MemoryStream();
            httpContext.Response.Body = ms;
            var bufferingFeature = new TestBufferingFeature();
            httpContext.Features.Set<IHttpBufferingFeature>(bufferingFeature);
            var context = new OutputFormatterContext();
            context.Object = new MemoryStream(expected);
            context.HttpContext = httpContext;

            // Act
            await formatter.WriteAsync(context);

            // Assert
            Assert.Equal(expected, ms.ToArray());
            Assert.True(bufferingFeature.DisableResponseBufferingInvoked);
        }

        private class SimplePOCO
        {
            public int Id { get; set; }
        }
    }
}