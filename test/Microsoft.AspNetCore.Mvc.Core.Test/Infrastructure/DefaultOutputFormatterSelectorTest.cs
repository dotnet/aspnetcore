// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class DefaultObjectResultExecutorTest
    {
        [Fact]
        public void SelectFormatter_UsesPassedInFormatters_IgnoresOptionsFormatters()
        {
            // Arrange
            var formatters = new List<IOutputFormatter>
            {
                new TestXmlOutputFormatter(),
                new TestJsonOutputFormatter(), // This will be chosen based on the content type
            };
            var selector = CreateSelector(new IOutputFormatter[] { });

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "application/xml"; // This will not be used

            // Act
            var formatter = selector.SelectFormatter(
                context,
                formatters,
                new MediaTypeCollection { "application/json" });

            // Assert
            Assert.Same(formatters[1], formatter);
            Assert.Equal(new StringSegment("application/json"), context.ContentType);
        }

        [Fact]
        public void SelectFormatter_WithOneProvidedContentType_IgnoresAcceptHeader()
        {
            // Arrange
            var formatters = new List<IOutputFormatter>
            {
                new TestXmlOutputFormatter(),
                new TestJsonOutputFormatter(), // This will be chosen based on the content type
            };
            var selector = CreateSelector(formatters);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "application/xml"; // This will not be used

            // Act
            var formatter = selector.SelectFormatter(
                context,
                Array.Empty<IOutputFormatter>(),
                new MediaTypeCollection { "application/json" });

            // Assert
            Assert.Same(formatters[1], formatter);
            Assert.Equal(new StringSegment("application/json"), context.ContentType);
        }

        [Fact]
        public void SelectFormatter_WithOneProvidedContentType_NoFallback()
        {
            // Arrange
            var formatters = new List<IOutputFormatter>
            {
                new TestXmlOutputFormatter(),
            };
            var selector = CreateSelector(formatters);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "application/xml"; // This will not be used

            // Act
            var formatter = selector.SelectFormatter(
                context,
                Array.Empty<IOutputFormatter>(),
                new MediaTypeCollection { "application/json" });

            // Assert
            Assert.Null(formatter);
        }

        // ObjectResult.ContentTypes, Accept header, expected content type
        public static TheoryData<MediaTypeCollection, string, string> ContentTypes
        {
            get
            {
                var contentTypes = new MediaTypeCollection
                {
                    "text/plain",
                    "text/xml",
                    "application/json",
                };

                return new TheoryData<MediaTypeCollection, string, string>()
                {
                    // Empty accept header, should select based on ObjectResult.ContentTypes.
                    { contentTypes, "", "application/json" },

                    // null accept header, should select based on ObjectResult.ContentTypes.
                    { contentTypes, null, "application/json" },

                    // The accept header does not match anything in ObjectResult.ContentTypes.
                    // The first formatter that can write the result gets to choose the content type.
                    { contentTypes, "text/custom", "application/json" },

                    // Accept header matches ObjectResult.ContentTypes, but no formatter supports the accept header.
                    // The first formatter that can write the result gets to choose the content type.
                    { contentTypes, "text/xml", "application/json" },

                    // Filters out Accept headers with 0 quality and selects the one with highest quality.
                    {
                        contentTypes,
                        "text/plain;q=0.3, text/json;q=0, text/cusotm;q=0.0, application/json;q=0.4",
                        "application/json"
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ContentTypes))]
        public void SelectFormatter_WithMultipleProvidedContentTypes_DoesConneg(
            MediaTypeCollection contentTypes,
            string acceptHeader,
            string expectedContentType)
        {
            // Arrange
            var formatters = new List<IOutputFormatter>
            {
                new CannotWriteFormatter(),
                new TestJsonOutputFormatter(),
            };
            var selector = CreateSelector(formatters);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = acceptHeader;

            // Act
            var formatter = selector.SelectFormatter(
                context,
                Array.Empty<IOutputFormatter>(),
                contentTypes);

            // Assert
            Assert.Same(formatters[1], formatter);
            Assert.Equal(new StringSegment(expectedContentType), context.ContentType);
        }

        [Fact]
        public void SelectFormatter_NoProvidedContentTypesAndNoAcceptHeader_ChoosesFirstFormatterThatCanWrite()
        {
            // Arrange
            var formatters = new List<IOutputFormatter>
            {
                new CannotWriteFormatter(),
                new TestJsonOutputFormatter(),
                new TestXmlOutputFormatter(),
            };
            var selector = CreateSelector(formatters);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            // Act
            var formatter = selector.SelectFormatter(
                context,
                Array.Empty<IOutputFormatter>(),
                new MediaTypeCollection());

            // Assert
            Assert.Same(formatters[1], formatter);
            Assert.Equal(new StringSegment("application/json"), context.ContentType);
        }

        [Fact]
        public void SelectFormatter_WithAcceptHeader_UsesFallback()
        {
            // Arrange
            var formatters = new List<IOutputFormatter>
            {
                new TestXmlOutputFormatter(),
                new TestJsonOutputFormatter(),
            };
            var selector = CreateSelector(formatters);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "text/custom,application/custom";

            // Act
            var formatter = selector.SelectFormatter(
                context,
                Array.Empty<IOutputFormatter>(),
                new MediaTypeCollection());

            // Assert
            Assert.Same(formatters[0], formatter);
            Assert.Equal(new StringSegment("application/xml"), context.ContentType);
        }

        [Fact]
        public void SelectFormatter_WithAcceptHeaderAndReturnHttpNotAcceptable_DoesNotUseFallback()
        {
            // Arrange
            var options = new MvcOptions()
            {
                ReturnHttpNotAcceptable = true,
                OutputFormatters =
                {
                    new TestXmlOutputFormatter(),
                    new TestJsonOutputFormatter(),
                },
            };

            var selector = CreateSelector(options);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "text/custom,application/custom";

            // Act
            var formatter = selector.SelectFormatter(
                context,
                Array.Empty<IOutputFormatter>(),
                new MediaTypeCollection());

            // Assert
            Assert.Null(formatter);
        }

        [Fact]
        public void SelectFormatter_WithAcceptHeaderOnly_SetsContentTypeIsServerDefinedToFalse()
        {
            // Arrange
            var formatters = new List<IOutputFormatter>
            {
                new ServerContentTypeOnlyFormatter()
            };

            var selector = CreateSelector(formatters);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "text/custom";

            // Act
            var formatter = selector.SelectFormatter(
                context,
                Array.Empty<IOutputFormatter>(),
                new MediaTypeCollection());

            // Assert
            Assert.Null(formatter);
        }

        [Fact]
        public void SelectFormatter_WithAcceptHeaderAndContentTypes_SetsContentTypeIsServerDefinedWhenExpected()
        {
            // Arrange
            var formatters = new List<IOutputFormatter>
            {
                new ServerContentTypeOnlyFormatter()
            };

            var selector = CreateSelector(formatters);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "text/custom, text/custom2";

            var serverDefinedContentTypes = new MediaTypeCollection();
            serverDefinedContentTypes.Add("text/other");
            serverDefinedContentTypes.Add("text/custom2");

            // Act
            var formatter = selector.SelectFormatter(
                context,
                Array.Empty<IOutputFormatter>(),
                serverDefinedContentTypes);

            // Assert
            Assert.Same(formatters[0], formatter);
            Assert.Equal(new StringSegment("text/custom2"), context.ContentType);
        }

        [Fact]
        public void SelectFormatter_WithContentTypesOnly_SetsContentTypeIsServerDefinedToTrue()
        {
            // Arrange
            var formatters = new List<IOutputFormatter>
            {
                new ServerContentTypeOnlyFormatter()
            };

            var selector = CreateSelector(formatters);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            var serverDefinedContentTypes = new MediaTypeCollection();
            serverDefinedContentTypes.Add("text/custom");

            // Act
            var formatter = selector.SelectFormatter(
                context,
                Array.Empty<IOutputFormatter>(),
                serverDefinedContentTypes);

            // Assert
            Assert.Same(formatters[0], formatter);
            Assert.Equal(new StringSegment("text/custom"), context.ContentType);
        }

        private static DefaultOutputFormatterSelector CreateSelector(IEnumerable<IOutputFormatter> formatters)
        {
            var options = new MvcOptions();
            foreach (var formatter in formatters)
            {
                options.OutputFormatters.Add(formatter);
            }

            return CreateSelector(options);
        }

        private static DefaultOutputFormatterSelector CreateSelector(MvcOptions options)
        {
            return new DefaultOutputFormatterSelector(Options.Create(options), NullLoggerFactory.Instance);
        }

        private class CannotWriteFormatter : IOutputFormatter
        {
            public virtual bool CanWriteResult(OutputFormatterCanWriteContext context)
            {
                return false;
            }

            public virtual Task WriteAsync(OutputFormatterWriteContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class TestJsonOutputFormatter : TextOutputFormatter
        {
            public TestJsonOutputFormatter()
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));

                SupportedEncodings.Add(Encoding.UTF8);
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
            {
                return Task.FromResult(0);
            }
        }

        private class TestXmlOutputFormatter : TextOutputFormatter
        {
            public TestXmlOutputFormatter()
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml"));
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xml"));

                SupportedEncodings.Add(Encoding.UTF8);
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
            {
                return Task.FromResult(0);
            }
        }

        private class TestStringOutputFormatter : TextOutputFormatter
        {
            public TestStringOutputFormatter()
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));

                SupportedEncodings.Add(Encoding.UTF8);
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
            {
                return Task.FromResult(0);
            }
        }

        private class ServerContentTypeOnlyFormatter : OutputFormatter
        {
            public override bool CanWriteResult(OutputFormatterCanWriteContext context)
            {
                // This test formatter matches if and only if the content type is specified
                // as "server defined". This lets tests identify what value the ObjectResultExecutor
                // passed for that flag.
                return context.ContentTypeIsServerDefined;
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
            {
                return Task.FromResult(0);
            }
        }
    }
}
