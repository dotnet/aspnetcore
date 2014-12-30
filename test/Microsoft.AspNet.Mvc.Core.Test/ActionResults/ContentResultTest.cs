// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Routing;
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
                ContentType = "application/json",
                ContentEncoding = null
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
                ContentType = "text/plain",
                ContentEncoding = Encoding.ASCII
            };
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);

            // Act
            await contentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal("text/plain; charset=us-ascii", httpContext.Response.ContentType);
        }

        [Fact]
        public async Task ContentResult_Response_NullContentType_SetsEncodingAndDefaultContentType()
        {
            // Arrange
            var contentResult = new ContentResult
            {
                Content = "Test Content",
                ContentType = null,
                ContentEncoding = Encoding.UTF7
            };
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);

            // Act
            await contentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal("text/plain; charset=utf-7", httpContext.Response.ContentType);
        }

        [Fact]
        public async Task ContentResult_Response_NullContent_SetsContentTypeAndEncoding()
        {
            // Arrange
            var contentResult = new ContentResult
            {
                Content = null,
                ContentType = "application/json",
                ContentEncoding = Encoding.UTF8
            };
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);

            // Act
            await contentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);
        }

        [Fact]
        public async Task ContentResult_Response_BadContentType_ThrowsFormatException()
        {
            // Arrange
            var contentResult = new ContentResult
            {
                Content = "Test Content",
                ContentType = "some-type",
                ContentEncoding = null
            };
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);

            // Act
            var exception = await Assert.ThrowsAsync<FormatException>(
                        async () => await contentResult.ExecuteResultAsync(actionContext));

            // Assert
            Assert.Equal("Invalid media type 'some-type'.", exception.Message);
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