// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Formatters
{
    public class OutputFormatterTests
    {
        public static IEnumerable<object[]> SelectResponseCharacterEncodingData
        {
            get
            {
                // string acceptEncodings, string requestEncoding, string[] supportedEncodings, string expectedEncoding
                yield return new object[] { "", new string[] { "utf-8", "utf-16" }, "utf-8" };

                yield return new object[] { "utf-8", new string[] { "utf-8", "utf-16" }, "utf-8" };
                yield return new object[] { "utf-16", new string[] { "utf-8", "utf-16" }, "utf-16" };
                yield return new object[] { "utf-16; q=0.5", new string[] { "utf-8", "utf-16" }, "utf-16" };

                yield return new object[] { "utf-8; q=0.0", new string[] { "utf-8", "utf-16" }, "utf-8" };
                yield return new object[] { "utf-8; q=0.0, utf-16; q=0.0", new string[] { "utf-8", "utf-16" }, "utf-8" };

                yield return new object[] { "*; q=0.0", new string[] { "utf-8", "utf-16" }, "utf-8" };
            }
        }

        [Theory]
        [MemberData(nameof(SelectResponseCharacterEncodingData))]
        public void SelectResponseCharacterEncoding_SelectsEncoding(
            string acceptCharsetHeaders,
            string[] supportedEncodings,
            string expectedEncoding)
        {
            // Arrange
            var httpContext = new Mock<HttpContext>();
            var httpRequest = new DefaultHttpContext().Request;
            httpRequest.Headers[HeaderNames.AcceptCharset] = acceptCharsetHeaders;
            httpRequest.Headers[HeaderNames.Accept] = "application/acceptCharset";
            httpContext.SetupGet(o => o.Request).Returns(httpRequest);

            var formatter = new TestOutputFormatter();
            foreach (string supportedEncoding in supportedEncodings)
            {
                formatter.SupportedEncodings.Add(Encoding.GetEncoding(supportedEncoding));
            }

            var context = new OutputFormatterWriteContext(
                httpContext.Object,
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                typeof(string),
                "someValue")
            {
                ContentType = MediaTypeHeaderValue.Parse(httpRequest.Headers[HeaderNames.Accept]),
            };

            // Act
            var actualEncoding = formatter.SelectCharacterEncoding(context);

            // Assert
            Assert.Equal(Encoding.GetEncoding(expectedEncoding), actualEncoding);
        }

        [Fact]
        public void WriteResponseContentHeaders_NoSupportedEncodings_NoEncodingIsSet()
        {
            // Arrange
            var formatter = new TestOutputFormatter();

            var testContentType = MediaTypeHeaderValue.Parse("text/json");

            formatter.SupportedEncodings.Clear();
            formatter.SupportedMediaTypes.Clear();
            formatter.SupportedMediaTypes.Add(testContentType);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null)
            {
                ContentType = testContentType,
            };

            // Act
            formatter.WriteResponseHeaders(context);

            // Assert
            Assert.Null(context.ContentType.Encoding);
            Assert.Equal(testContentType, context.ContentType);

            // If we had set an encoding, it would be part of the content type header
            Assert.Equal(testContentType, context.HttpContext.Response.GetTypedHeaders().ContentType);
        }

        [Fact]
        public async Task WriteResponseHeaders_ClonesMediaType()
        {
            // Arrange
            var formatter = new PngImageFormatter();
            formatter.SupportedMediaTypes.Clear();
            var mediaType = new MediaTypeHeaderValue("image/png");
            formatter.SupportedMediaTypes.Add(mediaType);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            // Act
            await formatter.WriteAsync(context);

            // Assert
            Assert.NotSame(mediaType, context.ContentType);
            Assert.Null(mediaType.Charset);
            Assert.Equal("image/png; charset=utf-8", context.ContentType.ToString());
        }

        [Fact]
        public void CanWriteResult_ForNullContentType_UsesFirstEntryInSupportedContentTypes()
        {
            // Arrange
            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            var formatter = new TestOutputFormatter();

            // Act
            var result = formatter.CanWriteResult(context);

            // Assert
            Assert.True(result);
            Assert.Equal(formatter.SupportedMediaTypes[0].ToString(), context.ContentType.ToString());
        }

        [Fact]
        public void GetSupportedContentTypes_ReturnsNull_ForUnsupportedType()
        {
            // Arrange
            var formatter = new TypeSpecificFormatter();
            formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            formatter.SupportedTypes.Add(typeof(int));

            // Act
            var contentTypes = formatter.GetSupportedContentTypes(contentType: null, objectType: typeof(string));

            // Assert
            Assert.Null(contentTypes);
        }

        [Fact]
        public void CanWrite_ReturnsFalse_ForUnsupportedType()
        {
            // Arrange
            var formatter = new TypeSpecificFormatter();
            formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            formatter.SupportedTypes.Add(typeof(int));

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                typeof(string),
                "Hello, world!")
            {
                ContentType = formatter.SupportedMediaTypes[0],
            };

            // Act
            var result = formatter.CanWriteResult(context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetSupportedContentTypes_ReturnsAllContentTypes_WithContentTypeNull()
        {
            // Arrange
            var formatter = new TestOutputFormatter();
            formatter.SupportedMediaTypes.Clear();
            formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));

            // Act
            var contentTypes = formatter.GetSupportedContentTypes(contentType: null, objectType: typeof(string));

            // Assert
            Assert.Equal(2, contentTypes.Count);
            Assert.Single(contentTypes, ct => ct.ToString() == "application/json");
            Assert.Single(contentTypes, ct => ct.ToString() == "application/xml");
        }

        [Fact]
        public void GetSupportedContentTypes_ReturnsMatchingContentTypes_WithContentType()
        {
            // Arrange
            var formatter = new TestOutputFormatter();

            formatter.SupportedMediaTypes.Clear();
            formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

            // Act
            var contentTypes = formatter.GetSupportedContentTypes(
                MediaTypeHeaderValue.Parse("application/*"),
                typeof(int));

            // Assert
            var contentType = Assert.Single(contentTypes);
            Assert.Equal("application/json", contentType.ToString());
        }

        [Fact]
        public void GetSupportedContentTypes_ReturnsMatchingContentTypes_NoMatches()
        {
            // Arrange
            var formatter = new TestOutputFormatter();

            formatter.SupportedMediaTypes.Clear();
            formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

            // Act
            var contentTypes = formatter.GetSupportedContentTypes(
                MediaTypeHeaderValue.Parse("application/xml"),
                typeof(int));

            // Assert
            Assert.Null(contentTypes);
        }

        [Fact]
        public void GetSupportedContentTypes_ReturnsAllContentTypes_ReturnsNullWithNoSupportedContentTypes()
        {
            // Arrange
            var formatter = new TestOutputFormatter();

            // Intentionally empty
            formatter.SupportedMediaTypes.Clear();

            // Act
            var contentTypes = formatter.GetSupportedContentTypes(contentType: null, objectType: typeof(int));

            // Assert
            Assert.Null(contentTypes);
        }

        private class TypeSpecificFormatter : OutputFormatter
        {
            public List<Type> SupportedTypes { get; } = new List<Type>();

            protected override bool CanWriteType(Type type)
            {
                return SupportedTypes.Contains(type);
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class TestOutputFormatter : OutputFormatter
        {
            public TestOutputFormatter()
            {
                SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/acceptCharset"));
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
            {
                return Task.FromResult(true);
            }
        }

        private class DoesNotSetContext : OutputFormatter
        {
            public DoesNotSetContext()
            {
                SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/doesNotSetContext"));
                SupportedEncodings.Add(Encoding.Unicode);
            }

            public override bool CanWriteResult(OutputFormatterCanWriteContext context)
            {
                // Do not set the selected media Type.
                // The WriteResponseHeaders should do it for you.
                return true;
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
            {
                return Task.FromResult(true);
            }
        }

        private class PngImageFormatter : OutputFormatter
        {
            public PngImageFormatter()
            {
                SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("image/png"));
                SupportedEncodings.Add(Encoding.UTF8);
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
            {
                return Task.FromResult(true);
            }
        }
    }
}
