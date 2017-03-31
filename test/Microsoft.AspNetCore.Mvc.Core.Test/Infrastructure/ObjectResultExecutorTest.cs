// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ObjectResultExecutorTest
    {
        [Fact]
        public void SelectFormatter_WithNoProvidedContentType_DoesConneg()
        {
            // Arrange
            var executor = CreateExecutor();

            var formatters = new List<IOutputFormatter>
            {
                new TestXmlOutputFormatter(),
                new TestJsonOutputFormatter(), // This will be chosen based on the accept header
            };

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "application/json";

            // Act
            var formatter = executor.SelectFormatter(
                context,
                new MediaTypeCollection { "application/json" },
                formatters);

            // Assert
            Assert.Same(formatters[1], formatter);
            MediaTypeAssert.Equal("application/json", context.ContentType);
        }

        // For this test case probably the most common use case is when there is a format mapping based
        // content type selected but the developer had set the content type on the Response.ContentType
        [Fact]
        public async Task ExecuteAsync_ContentTypeProvidedFromResponseAndObjectResult_UsesResponseContentType()
        {
            // Arrange
            var executor = CreateCustomObjectResultExecutor();
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext() { HttpContext = httpContext };
            httpContext.Request.Headers[HeaderNames.Accept] = "application/xml"; // This will not be used
            httpContext.Response.ContentType = "text/plain";
            var result = new ObjectResult("input");
            result.Formatters.Add(new TestXmlOutputFormatter());
            result.Formatters.Add(new TestJsonOutputFormatter());
            result.Formatters.Add(new TestStringOutputFormatter()); // This will be chosen based on the content type

            // Act
            await executor.ExecuteAsync(actionContext, result);

            // Assert
            Assert.IsType<TestStringOutputFormatter>(executor.SelectedOutputFormatter);
            MediaTypeAssert.Equal("text/plain; charset=utf-8", httpContext.Response.ContentType);
        }

        [Fact]
        public void SelectFormatter_WithOneProvidedContentType_IgnoresAcceptHeader()
        {
            // Arrange
            var executor = CreateExecutor();

            var formatters = new List<IOutputFormatter>
            {
                new TestXmlOutputFormatter(),
                new TestJsonOutputFormatter(), // This will be chosen based on the content type
            };

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "application/xml"; // This will not be used

            // Act
            var formatter = executor.SelectFormatter(
                context,
                new MediaTypeCollection { "application/json" },
                formatters);

            // Assert
            Assert.Same(formatters[1], formatter);
            Assert.Equal(new StringSegment("application/json"), context.ContentType);
        }

        [Fact]
        public async Task ExecuteAsync_WithOneProvidedContentType_FromResponseContentType_IgnoresAcceptHeader()
        {
            // Arrange
            var executor = CreateCustomObjectResultExecutor();
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext() { HttpContext = httpContext };
            httpContext.Request.Headers[HeaderNames.Accept] = "application/xml"; // This will not be used
            httpContext.Response.ContentType = "application/json";
            var result = new ObjectResult("input");
            result.Formatters.Add(new TestXmlOutputFormatter());
            result.Formatters.Add(new TestJsonOutputFormatter()); // This will be chosen based on the content type

            // Act
            await executor.ExecuteAsync(actionContext, result);

            // Assert
            Assert.IsType<TestJsonOutputFormatter>(executor.SelectedOutputFormatter);
            Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);
        }

        [Fact]
        public void SelectFormatter_WithOneProvidedContentType_NoFallback()
        {
            // Arrange
            var executor = CreateExecutor();

            var formatters = new List<IOutputFormatter>
            {
                new TestXmlOutputFormatter(),
            };

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "application/xml"; // This will not be used

            // Act
            var formatter = executor.SelectFormatter(
                context,
                new MediaTypeCollection { "application/json" },
                formatters);

            // Assert
            Assert.Null(formatter);
        }

        [Fact]
        public async Task ExecuteAsync_WithOneProvidedContentType_FromResponseContentType_NoFallback()
        {
            // Arrange
            var executor = CreateCustomObjectResultExecutor();
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext() { HttpContext = httpContext };
            httpContext.Request.Headers[HeaderNames.Accept] = "application/xml"; // This will not be used
            httpContext.Response.ContentType = "application/json";
            var result = new ObjectResult("input");
            result.Formatters.Add(new TestXmlOutputFormatter());

            // Act
            await executor.ExecuteAsync(actionContext, result);

            // Assert
            Assert.Null(executor.SelectedOutputFormatter);
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
            var executor = CreateExecutor();

            var formatters = new List<IOutputFormatter>
            {
                new CannotWriteFormatter(),
                new TestJsonOutputFormatter(),
            };

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = acceptHeader;

            // Act
            var formatter = executor.SelectFormatter(
                context,
                contentTypes,
                formatters);

            // Assert
            Assert.Same(formatters[1], formatter);
            Assert.Equal(new StringSegment(expectedContentType), context.ContentType);
        }

        [Fact]
        public void SelectFormatter_NoProvidedContentTypesAndNoAcceptHeader_ChoosesFirstFormatterThatCanWrite()
        {
            // Arrange
            var executor = CreateExecutor();

            var formatters = new List<IOutputFormatter>
            {
                new CannotWriteFormatter(),
                new TestJsonOutputFormatter(),
                new TestXmlOutputFormatter(),
            };

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            // Act
            var formatter = executor.SelectFormatter(
                context,
                new MediaTypeCollection(),
                formatters);

            // Assert
            Assert.Same(formatters[1], formatter);
            Assert.Equal(new StringSegment("application/json"), context.ContentType);
        }

        [Fact]
        public void SelectFormatter_WithAcceptHeader_UsesFallback()
        {
            // Arrange
            var executor = CreateExecutor();

            var formatters = new List<IOutputFormatter>
            {
                new TestXmlOutputFormatter(),
                new TestJsonOutputFormatter(),
            };

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "text/custom,application/custom";

            // Act
            var formatter = executor.SelectFormatter(
                context,
                new MediaTypeCollection { },
                formatters);

            // Assert
            Assert.Same(formatters[0], formatter);
            Assert.Equal(new StringSegment("application/xml"), context.ContentType);
        }

        [Fact]
        public void SelectFormatter_WithAcceptHeaderAndReturnHttpNotAcceptable_DoesNotUseFallback()
        {
            // Arrange
            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ReturnHttpNotAcceptable = true;

            var executor = CreateExecutor(options);

            var formatters = new List<IOutputFormatter>
            {
                new TestXmlOutputFormatter(),
                new TestJsonOutputFormatter(),
            };

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "text/custom,application/custom";

            // Act
            var formatter = executor.SelectFormatter(
                context,
                new MediaTypeCollection { },
                formatters);

            // Assert
            Assert.Null(formatter);
        }

        [Fact]
        public void SelectFormatter_WithAcceptHeaderOnly_SetsContentTypeIsServerDefinedToFalse()
        {
            // Arrange
            var executor = CreateExecutor();

            var formatters = new List<IOutputFormatter>
            {
                new ServerContentTypeOnlyFormatter()
            };

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "text/custom";

            // Act
            var formatter = executor.SelectFormatter(
                context,
                new MediaTypeCollection { },
                formatters);

            // Assert
            Assert.Null(formatter);
        }

        [Fact]
        public void SelectFormatter_WithAcceptHeaderAndContentTypes_SetsContentTypeIsServerDefinedWhenExpected()
        {
            // Arrange
            var executor = CreateExecutor();

            var formatters = new List<IOutputFormatter>
            {
                new ServerContentTypeOnlyFormatter()
            };
            
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
            var formatter = executor.SelectFormatter(
                context,
                serverDefinedContentTypes,
                formatters);

            // Assert
            Assert.Same(formatters[0], formatter);
            Assert.Equal(new StringSegment("text/custom2"), context.ContentType);
        }

        [Fact]
        public void SelectFormatter_WithContentTypesOnly_SetsContentTypeIsServerDefinedToTrue()
        {
            // Arrange
            var executor = CreateExecutor();

            var formatters = new List<IOutputFormatter>
            {
                new ServerContentTypeOnlyFormatter()
            };

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: null,
                @object: null);

            var serverDefinedContentTypes = new MediaTypeCollection();
            serverDefinedContentTypes.Add("text/custom");

            // Act
            var formatter = executor.SelectFormatter(
                context,
                serverDefinedContentTypes,
                formatters);

            // Assert
            Assert.Same(formatters[0], formatter);
            Assert.Equal(new StringSegment("text/custom"), context.ContentType);
        }

        [Fact]
        public async Task ExecuteAsync_NoFormatterFound_Returns406()
        {
            // Arrange
            var executor = CreateExecutor();

            var actionContext = new ActionContext()
            {
                HttpContext = new DefaultHttpContext(),
            };

            var result = new ObjectResult("input");

            // This formatter won't write anything
            result.Formatters = new FormatterCollection<IOutputFormatter>
            {
                new CannotWriteFormatter(),
            };

            // Act
            await executor.ExecuteAsync(actionContext, result);

            // Assert
            Assert.Equal(StatusCodes.Status406NotAcceptable, actionContext.HttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task ExecuteAsync_FallsBackOnFormattersInOptions()
        {
            // Arrange
            var options = new TestOptionsManager<MvcOptions>();
            options.Value.OutputFormatters.Add(new TestJsonOutputFormatter());

            var executor = CreateExecutor(options: options);

            var actionContext = new ActionContext()
            {
                HttpContext = GetHttpContext(),
            };

            var result = new ObjectResult("someValue");

            // Act
            await executor.ExecuteAsync(actionContext, result);

            // Assert
            Assert.Equal(
                "application/json; charset=utf-8",
                actionContext.HttpContext.Response.Headers[HeaderNames.ContentType]);
        }

        [Fact]
        public async Task ExecuteAsync_ThrowsWithNoFormatters()
        {
            // Arrange
            var expected = $"'{typeof(MvcOptions).FullName}.{nameof(MvcOptions.OutputFormatters)}' must not be " +
                $"empty. At least one '{typeof(IOutputFormatter).FullName}' is required to format a response.";
            var executor = CreateExecutor();
            var actionContext = new ActionContext
            {
                HttpContext = GetHttpContext(),
            };
            var result = new ObjectResult("some value");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => executor.ExecuteAsync(actionContext, result));
            Assert.Equal(expected, exception.Message);
        }

        [Theory]
        [InlineData(new[] { "application/*" }, "application/*")]
        [InlineData(new[] { "application/xml", "application/*", "application/json" }, "application/*")]
        [InlineData(new[] { "application/*", "application/json" }, "application/*")]
        [InlineData(new[] { "*/*" }, "*/*")]
        [InlineData(new[] { "application/xml", "*/*", "application/json" }, "*/*")]
        [InlineData(new[] { "*/*", "application/json" }, "*/*")]
        [InlineData(new[] { "application/json", "application/*+json" }, "application/*+json")]
        [InlineData(new[] { "application/entiy+json;*", "application/json" }, "application/entiy+json;*")]
        public async Task ExecuteAsync_MatchAllContentType_Throws(string[] contentTypes, string invalidContentType)
        {
            // Arrange
            var result = new ObjectResult("input");

            var mediaTypes = new MediaTypeCollection();
            foreach (var contentType in contentTypes)
            {
                mediaTypes.Add(contentType);
            }

            result.ContentTypes = mediaTypes;

            var executor = CreateExecutor();

            var actionContext = new ActionContext() { HttpContext = new DefaultHttpContext() };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => executor.ExecuteAsync(actionContext, result));

            var expectedMessage = string.Format("The content-type '{0}' added in the 'ContentTypes' property is " +
              "invalid. Media types which match all types or match all subtypes are not supported.",
              invalidContentType);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Theory]
        // Chrome & Opera
        [InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8", "application/json; charset=utf-8")]
        // IE
        [InlineData("text/html,application/xhtml+xml,*/*", "application/json; charset=utf-8")]
        // Firefox & Safari
        [InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8", "application/json; charset=utf-8")]
        // Misc
        [InlineData("*/*", @"application/json; charset=utf-8")]
        [InlineData("text/html,*/*;q=0.8,application/xml;q=0.9", "application/json; charset=utf-8")]
        public async Task ExecuteAsync_SelectDefaultFormatter_OnAllMediaRangeAcceptHeaderMediaType(
            string acceptHeader,
            string expectedContentType)
        {
            // Arrange
            var options = new TestOptionsManager<MvcOptions>();
            options.Value.RespectBrowserAcceptHeader = false;

            var executor = CreateExecutor(options: options);

            var result = new ObjectResult("input");
            result.Formatters.Add(new TestJsonOutputFormatter());
            result.Formatters.Add(new TestXmlOutputFormatter());

            var actionContext = new ActionContext()
            {
                HttpContext = GetHttpContext(),
            };
            actionContext.HttpContext.Request.Headers[HeaderNames.Accept] = acceptHeader;

            // Act
            await executor.ExecuteAsync(actionContext, result);

            // Assert
            Assert.Equal(expectedContentType, actionContext.HttpContext.Response.Headers[HeaderNames.ContentType]);
        }

        [Theory]
        // Chrome & Opera
        [InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8", "application/xml; charset=utf-8")]
        // IE
        [InlineData("text/html,application/xhtml+xml,*/*", "application/json; charset=utf-8")]
        // Firefox & Safari
        [InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8", "application/xml; charset=utf-8")]
        // Misc
        [InlineData("*/*", @"application/json; charset=utf-8")]
        [InlineData("text/html,*/*;q=0.8,application/xml;q=0.9", "application/xml; charset=utf-8")]
        public async Task ObjectResult_PerformsContentNegotiation_OnAllMediaRangeAcceptHeaderMediaType(
            string acceptHeader,
            string expectedContentType)
        {
            // Arrange
            var options = new TestOptionsManager<MvcOptions>();
            options.Value.RespectBrowserAcceptHeader = true;

            var executor = CreateExecutor(options: options);

            var result = new ObjectResult("input");
            result.Formatters.Add(new TestJsonOutputFormatter());
            result.Formatters.Add(new TestXmlOutputFormatter());

            var actionContext = new ActionContext()
            {
                HttpContext = GetHttpContext(),
            };
            actionContext.HttpContext.Request.Headers[HeaderNames.Accept] = acceptHeader;

            // Act
            await executor.ExecuteAsync(actionContext, result);

            // Assert
            var responseContentType = actionContext.HttpContext.Response.Headers[HeaderNames.ContentType];
            MediaTypeAssert.Equal(expectedContentType, responseContentType);
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

        private static TestObjectResultExecutor CreateExecutor(IOptions<MvcOptions> options = null)
        {
            return new TestObjectResultExecutor(
                options ?? new TestOptionsManager<MvcOptions>(),
                new TestHttpResponseStreamWriterFactory(),
                NullLoggerFactory.Instance);
        }

        private static CustomObjectResultExecutor CreateCustomObjectResultExecutor()
        {
            return new CustomObjectResultExecutor(
                new TestOptionsManager<MvcOptions>(),
                new TestHttpResponseStreamWriterFactory(),
                NullLoggerFactory.Instance);
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

        private class TestObjectResultExecutor : ObjectResultExecutor
        {
            public TestObjectResultExecutor(
                IOptions<MvcOptions> options,
                IHttpResponseStreamWriterFactory writerFactory,
                ILoggerFactory loggerFactory)
                : base(options, writerFactory, loggerFactory)
            {
            }

            new public IOutputFormatter SelectFormatter(
                OutputFormatterWriteContext formatterContext,
                MediaTypeCollection contentTypes,
                IList<IOutputFormatter> formatters)
            {
                return base.SelectFormatter(formatterContext, contentTypes, formatters);
            }
        }

        private class CustomObjectResultExecutor : ObjectResultExecutor
        {
            public CustomObjectResultExecutor(
                IOptions<MvcOptions> options,
                IHttpResponseStreamWriterFactory writerFactory,
                ILoggerFactory loggerFactory)
                : base(options, writerFactory, loggerFactory)
            {
            }

            public IOutputFormatter SelectedOutputFormatter { get; private set; }

            protected override IOutputFormatter SelectFormatter(
                OutputFormatterWriteContext formatterContext,
                MediaTypeCollection contentTypes,
                IList<IOutputFormatter> formatters)
            {
                SelectedOutputFormatter = base.SelectFormatter(formatterContext, contentTypes, formatters);
                return SelectedOutputFormatter;
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
