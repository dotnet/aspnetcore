// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Mvc.Infrastructure
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
                new[] { new MediaTypeHeaderValue("application/json") },
                formatters);

            // Assert
            Assert.Same(formatters[1], formatter);
            Assert.Equal(new MediaTypeHeaderValue("application/json"), context.ContentType);
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
                new[] { new MediaTypeHeaderValue("application/json") },
                formatters);

            // Assert
            Assert.Same(formatters[1], formatter);
            Assert.Equal(new MediaTypeHeaderValue("application/json"), context.ContentType);
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
                new[] { new MediaTypeHeaderValue("application/json") },
                formatters);

            // Assert
            Assert.Null(formatter);
        }

        // ObjectResult.ContentTypes, Accept header, expected content type
        public static TheoryData<string[], string, string> ContentTypes
        {
            get
            {
                var contentTypes = new string[]
                {
                    "text/plain",
                    "text/xml",
                    "application/json",
                };

                return new TheoryData<string[], string, string>()
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
            IEnumerable<string> contentTypes,
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
                contentTypes.Select(contentType => MediaTypeHeaderValue.Parse(contentType)).ToList(),
                formatters);

            // Assert
            Assert.Same(formatters[1], formatter);
            Assert.Equal(new MediaTypeHeaderValue(expectedContentType), context.ContentType);
        }

        [Fact]
        public void SelectFormatter_NoProvidedContentTypesAndNoAcceptHeader_ChoosesFirstFormattterThatCanWrite()
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
                new List<MediaTypeHeaderValue>(),
                formatters);

            // Assert
            Assert.Same(formatters[1], formatter);
            Assert.Equal(new MediaTypeHeaderValue("application/json"), context.ContentType);
            Assert.Null(context.FailedContentNegotiation);
        }

        [Fact]
        public void SelectFormatter_WithAcceptHeader_ConnegFails()
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

            context.HttpContext.Request.Headers[HeaderNames.Accept] = "text/custom, application/custom";

            // Act
            var formatter = executor.SelectFormatter(
                context,
                new MediaTypeHeaderValue[] { },
                formatters);

            // Assert
            Assert.Same(formatters[0], formatter);
            Assert.Equal(new MediaTypeHeaderValue("application/xml"), context.ContentType);
            Assert.True(context.FailedContentNegotiation);
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

        [Theory]
        [InlineData(new[] { "application/*" }, "application/*")]
        [InlineData(new[] { "application/xml", "application/*", "application/json" }, "application/*")]
        [InlineData(new[] { "application/*", "application/json" }, "application/*")]
        [InlineData(new[] { "*/*" }, "*/*")]
        [InlineData(new[] { "application/xml", "*/*", "application/json" }, "*/*")]
        [InlineData(new[] { "*/*", "application/json" }, "*/*")]
        public async Task ExecuteAsync_MatchAllContentType_Throws(string[] contentTypes, string invalidContentType)
        {
            // Arrange
            var result = new ObjectResult("input");
            result.ContentTypes = contentTypes
                .Select(contentType => MediaTypeHeaderValue.Parse(contentType))
                .ToList();

            var executor = CreateExecutor();

            var actionContext = new ActionContext();

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
        [InlineData("text/html, application/xhtml+xml, */*", "application/json; charset=utf-8")] 
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
        [InlineData("text/html, application/xhtml+xml, */*", "application/json; charset=utf-8")]
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
            Assert.Equal(expectedContentType, actionContext.HttpContext.Response.Headers[HeaderNames.ContentType]);
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

        private class TestJsonOutputFormatter : OutputFormatter
        {
            public TestJsonOutputFormatter()
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));

                SupportedEncodings.Add(Encoding.UTF8);
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
            {
                return Task.FromResult(0);
            }
        }

        private class TestXmlOutputFormatter : OutputFormatter
        {
            public TestXmlOutputFormatter()
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml"));
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xml"));

                SupportedEncodings.Add(Encoding.UTF8);
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
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

            public new IOutputFormatter SelectFormatter(
                OutputFormatterWriteContext formatterContext,
                IList<MediaTypeHeaderValue> contentTypes,
                IList<IOutputFormatter> formatters)
            {
                return base.SelectFormatter(formatterContext, contentTypes, formatters);
            }
        }
    }
}
