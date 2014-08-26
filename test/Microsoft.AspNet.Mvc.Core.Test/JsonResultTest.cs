// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;


namespace Microsoft.AspNet.Mvc
{
    public class JsonResultTest
    {
        private static readonly byte[] _abcdUTF8Bytes 
            = new byte[] { 123, 34, 102, 111, 111, 34, 58, 34, 97, 98, 99, 100, 34, 125 };
        private JsonOutputFormatter _jsonformatter = new JsonOutputFormatter(
                                                            JsonOutputFormatter.CreateDefaultSettings(),
                                                            indent: false);
        [Fact]
        public async Task ExecuteResult_GeneratesResultsWithoutBOMByDefault()
        {
            // Arrange
            var expected = _abcdUTF8Bytes;
            var memoryStream = new MemoryStream();
            var response = new Mock<HttpResponse>();
            response.SetupGet(r => r.Body)
                   .Returns(memoryStream);
            var context = GetHttpContext(response.Object);
            var actionContext = new ActionContext(context,
                                                  new RouteData(),
                                                  new ActionDescriptor());
            var result = new JsonResult(new { foo = "abcd" });

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expected, memoryStream.ToArray());
        }

        [Fact]
        public async Task ExecuteResult_IfNoMatchFoundUsesPassedInFormatter()
        {
            // Arrange
            var expected = Enumerable.Concat(Encoding.UTF8.GetPreamble(), _abcdUTF8Bytes);
            var memoryStream = new MemoryStream();
            var response = new Mock<HttpResponse>();
            response.SetupGet(r => r.Body)
                   .Returns(memoryStream);
            var context = GetHttpContext(response.Object, registerDefaultFormatter: false);
            var actionContext = new ActionContext(context,
                                                  new RouteData(),
                                                  new ActionDescriptor());
            var testFormatter = new TestJsonFormatter()
                                {
                                    Encoding = Encoding.UTF8
                                };

            var result = new JsonResult(new { foo = "abcd" }, testFormatter);

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expected, memoryStream.ToArray());
        }

        public async Task ExecuteResult_UsesDefaultFormatter_IfNoneIsRegistered_AndNoneIsPassed()
        {
            // Arrange
            var expected = _abcdUTF8Bytes;
            var memoryStream = new MemoryStream();
            var response = new Mock<HttpResponse>();
            response.SetupGet(r => r.Body)
                   .Returns(memoryStream);
            var context = GetHttpContext(response.Object, registerDefaultFormatter: false);
            var actionContext = new ActionContext(context,
                                                  new RouteData(),
                                                  new ActionDescriptor());
            var result = new JsonResult(new { foo = "abcd" });

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expected, memoryStream.ToArray());
        }

        private HttpContext GetHttpContext(HttpResponse response, bool registerDefaultFormatter = true)
        {
            var defaultFormatters = registerDefaultFormatter ? new List<IOutputFormatter>() { _jsonformatter } : 
                                                               new List<IOutputFormatter>();
            var httpContext = new Mock<HttpContext>();
            var mockFormattersProvider = new Mock<IOutputFormattersProvider>();
            mockFormattersProvider.SetupGet(o => o.OutputFormatters)
                                  .Returns(defaultFormatters);
            httpContext.Setup(o => o.RequestServices.GetService(typeof(IOutputFormattersProvider)))
                      .Returns(mockFormattersProvider.Object);
            httpContext.SetupGet(o => o.Request.Accept)
                       .Returns("");
            httpContext.SetupGet(c => c.Response).Returns(response);
            return httpContext.Object;
        }

        private class TestJsonFormatter : IOutputFormatter
        {
            public Encoding Encoding { get; set; }

            public IList<Encoding> SupportedEncodings
            {
                get
                {
                    return null;
                }
            }

            public IList<MediaTypeHeaderValue> SupportedMediaTypes
            {
                get
                {
                    return null;
                }
            }

            public bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
            {
                return true;
            }

            public async Task WriteAsync(OutputFormatterContext context)
            {
                // Override using the selected encoding.
                context.SelectedEncoding = Encoding;
                var jsonFormatter = new JsonOutputFormatter(JsonOutputFormatter.CreateDefaultSettings(),
                                                            indent: false);
                await jsonFormatter.WriteResponseBodyAsync(context);
            }
        }
    }
}