// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Mvc.Formatters
{
    public class StreamOutputFormatterTest
    {
        [Theory]
        [InlineData(typeof(Stream), "text/plain")]
        [InlineData(typeof(Stream), null)]
        public void CanWriteResult_ReturnsTrue_ForStreams(Type type, string contentType)
        {
            // Arrange
            var formatter = new StreamOutputFormatter();
            var contentTypeHeader = contentType == null ? null : new MediaTypeHeaderValue(contentType);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                type,
                new MemoryStream())
            {
                ContentType = contentTypeHeader,
            };

            // Act
            var canWrite = formatter.CanWriteResult(context);

            // Assert
            Assert.True(canWrite);
        }

        [Theory]
        [InlineData(typeof(SimplePOCO), "text/plain")]
        [InlineData(typeof(SimplePOCO), null)]
        public void CanWriteResult_OnlyActsOnStreams_IgnoringContentType(Type type, string contentType)
        {
            // Arrange
            var formatter = new StreamOutputFormatter();
            var contentTypeHeader = contentType == null ? null : new MediaTypeHeaderValue(contentType);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                type,
                new SimplePOCO())
            {
                ContentType = contentTypeHeader,
            };

            // Act
            var canWrite = formatter.CanWriteResult(context);

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
            var @object = type != null ? Activator.CreateInstance(type) : null;

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                type,
                @object);

            // Act
            var result = formatter.CanWriteResult(context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DisablesResponseBuffering_IfBufferingFeatureAvailable()
        {
            // Arrange
            var formatter = new StreamOutputFormatter();

            var expected = Encoding.UTF8.GetBytes("Test data");

            var httpContext = new DefaultHttpContext();
            var body = new MemoryStream();
            httpContext.Response.Body = body;

            var bufferingFeature = new TestBufferingFeature();
            httpContext.Features.Set<IHttpBufferingFeature>(bufferingFeature);

            var context = new OutputFormatterWriteContext(
                httpContext,
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                typeof(Stream),
                new MemoryStream(expected));

            // Act
            await formatter.WriteAsync(context);

            // Assert
            Assert.Equal(expected, body.ToArray());
            Assert.True(bufferingFeature.DisableResponseBufferingInvoked);
        }

        private class SimplePOCO
        {
            public int Id { get; set; }
        }
    }
}