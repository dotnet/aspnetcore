// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Routing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ContentResultTest
    {
        [Fact]
        public async Task ContentResult_Response_NullEncoding_SetsContentTypeAndDefaultEncoding()
        {
            // Arrange
            var contentResult = new ContentResult
            {
                Content = "Test Content",
                ContentType = new MediaTypeHeaderValue("application/json")
            };
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);

            // Act
            await contentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);
        }

        [Fact]
        public async Task ContentResult_Response_SetsContentTypeAndEncoding()
        {
            // Arrange
            var contentResult = new ContentResult
            {
                Content = "Test Content",
                ContentType = new MediaTypeHeaderValue("text/plain")
                {
                    Encoding = Encoding.ASCII
                }
            };
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);

            // Act
            await contentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal("text/plain; charset=us-ascii", httpContext.Response.ContentType);
        }

        [Fact]
        public async Task ContentResult_Response_NullContent_SetsContentTypeAndEncoding()
        {
            // Arrange
            var contentResult = new ContentResult
            {
                Content = null,
                ContentType = new MediaTypeHeaderValue("text/plain")
                {
                    Encoding = Encoding.UTF7
                }
            };
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);

            // Act
            await contentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal("text/plain; charset=utf-7", httpContext.Response.ContentType);
        }

        public static TheoryData<MediaTypeHeaderValue, string, string, byte[]> ContentResultContentTypeData
        {
            get
            {
                return new TheoryData<MediaTypeHeaderValue, string, string, byte[]>
                {
                    {
                        null,
                        "κόσμε",
                        "text/plain; charset=utf-8",
                        new byte[] { 206, 186, 225, 189, 185, 207, 131, 206, 188, 206, 181 } //utf-8 without BOM
                    },
                    {
                        new MediaTypeHeaderValue("text/foo"),
                        "κόσμε",
                        "text/foo; charset=utf-8",
                        new byte[] { 206, 186, 225, 189, 185, 207, 131, 206, 188, 206, 181 } //utf-8 without BOM
                    },
                    {
                        MediaTypeHeaderValue.Parse("text/foo;p1=p1-value"),
                        "κόσμε",
                        "text/foo; p1=p1-value; charset=utf-8",
                        new byte[] { 206, 186, 225, 189, 185, 207, 131, 206, 188, 206, 181 } //utf-8 without BOM
                    },
                    {
                        new MediaTypeHeaderValue("text/foo") { Encoding = Encoding.ASCII },
                        "abcd",
                        "text/foo; charset=us-ascii",
                        new byte[] { 97, 98, 99, 100 }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ContentResultContentTypeData))]
        public async Task ContentResult_ExecuteResultAsync_SetContentTypeAndEncoding_OnResponse(
            MediaTypeHeaderValue contentType,
            string content,
            string expectedContentType,
            byte[] expectedContentData)
        {
            // Arrange
            var contentResult = new ContentResult
            {
                Content = content,
                ContentType = contentType
            };
            var httpContext = GetHttpContext();
            var memoryStream = new MemoryStream();
            httpContext.Response.Body = memoryStream;
            var actionContext = GetActionContext(httpContext);

            // Act
            await contentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expectedContentType, httpContext.Response.ContentType);
            Assert.Equal(expectedContentData, memoryStream.ToArray());
        }

        private static ActionContext GetActionContext(HttpContext httpContext)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(Mock.Of<IRouter>());

            return new ActionContext(httpContext,
                                    routeData,
                                    new ActionDescriptor());
        }

        private static HttpContext GetHttpContext()
        {
            return new DefaultHttpContext();
        }
    }
}