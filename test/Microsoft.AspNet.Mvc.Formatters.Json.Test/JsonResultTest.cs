// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class JsonResultTest
    {
        private static readonly byte[] _abcdUTF8Bytes
            = new byte[] { 123, 34, 102, 111, 111, 34, 58, 34, 97, 98, 99, 100, 34, 125 };

        [Fact]
        public async Task ExecuteResultAsync_UsesDefaultContentType_IfNoContentTypeSpecified()
        {
            // Arrange
            var expected = _abcdUTF8Bytes;

            var context = GetHttpContext();
            var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());

            var result = new JsonResult(new { foo = "abcd" });

            // Act
            await result.ExecuteResultAsync(actionContext);
            var written = GetWrittenBytes(context);

            // Assert
            Assert.Equal(expected, written);
            Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        }

        [Fact]
        public async Task ExecuteResultAsync_NullEncoding_DoesNotSetCharsetOnContentType()
        {
            // Arrange
            var expected = _abcdUTF8Bytes;

            var context = GetHttpContext();
            var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());

            var result = new JsonResult(new { foo = "abcd" });
            result.ContentType = new MediaTypeHeaderValue("text/json");

            // Act
            await result.ExecuteResultAsync(actionContext);
            var written = GetWrittenBytes(context);

            // Assert
            Assert.Equal(expected, written);
            Assert.Equal("text/json", context.Response.ContentType);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsContentTypeAndEncoding()
        {
            // Arrange
            var expected = _abcdUTF8Bytes;

            var context = GetHttpContext();
            var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());

            var result = new JsonResult(new { foo = "abcd" });
            result.ContentType = new MediaTypeHeaderValue("text/json")
            {
                Encoding = Encoding.ASCII
            };

            // Act
            await result.ExecuteResultAsync(actionContext);
            var written = GetWrittenBytes(context);

            // Assert
            Assert.Equal(expected, written);
            Assert.Equal("text/json; charset=us-ascii", context.Response.ContentType);
        }

        [Fact]
        public async Task NoResultContentTypeSet_UsesResponseContentType_AndSuppliedEncoding()
        {
            // Arrange
            var expectedData = Encoding.ASCII.GetBytes("{\"foo\":\"abcd\"}");
            var expectedContentType = "text/foo; p1=p1-value; charset=us-ascii";
            var context = GetHttpContext();
            context.Response.ContentType = expectedContentType;
            var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());

            var result = new JsonResult(new { foo = "abcd" });

            // Act
            await result.ExecuteResultAsync(actionContext);
            var written = GetWrittenBytes(context);

            // Assert
            Assert.Equal(expectedData, written);
            Assert.Equal(expectedContentType, context.Response.ContentType);
        }

        [Theory]
        [InlineData("text/foo", "text/foo")]
        [InlineData("text/foo; p1=p1-value", "text/foo; p1=p1-value")]
        public async Task NoResultContentTypeSet_UsesResponseContentTypeAndDefaultEncoding_DoesNotSetCharset(
            string responseContentType,
            string expectedContentType)
        {
            // Arrange
            var expected = _abcdUTF8Bytes;

            var context = GetHttpContext();
            context.Response.ContentType = responseContentType;
            var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());

            var result = new JsonResult(new { foo = "abcd" });

            // Act
            await result.ExecuteResultAsync(actionContext);
            var written = GetWrittenBytes(context);

            // Assert
            Assert.Equal(expected, written);
            Assert.Equal(expectedContentType, context.Response.ContentType);
        }

        private static List<byte> AbcdIndentedUTF8Bytes
        {
            get
            {
                var bytes = new List<byte>();
                bytes.Add(123);
                bytes.AddRange(Encoding.UTF8.GetBytes(Environment.NewLine));
                bytes.AddRange(new byte[] { 32, 32, 34, 102, 111, 111, 34, 58, 32, 34, 97, 98, 99, 100, 34 });
                bytes.AddRange(Encoding.UTF8.GetBytes(Environment.NewLine));
                bytes.Add(125);
                return bytes;
            }
        }

        [Fact]
        public async Task ExecuteResultAsync_UsesPassedInSerializerSettings()
        {
            // Arrange
            var expected = AbcdIndentedUTF8Bytes;

            var context = GetHttpContext();
            var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());

            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.Formatting = Formatting.Indented;

            var result = new JsonResult(new { foo = "abcd" }, serializerSettings);

            // Act
            await result.ExecuteResultAsync(actionContext);
            var written = GetWrittenBytes(context);

            // Assert
            Assert.Equal(expected, written);
            Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        }

        private static HttpContext GetHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            var services = new ServiceCollection();
            services.AddOptions();
            services.AddInstance<ILoggerFactory>(NullLoggerFactory.Instance);
            httpContext.RequestServices = services.BuildServiceProvider();

            return httpContext;
        }

        private static byte[] GetWrittenBytes(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            return Assert.IsType<MemoryStream>(context.Response.Body).ToArray();
        }
    }
}