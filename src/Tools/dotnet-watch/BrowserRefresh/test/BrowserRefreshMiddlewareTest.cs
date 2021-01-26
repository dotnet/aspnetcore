// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh
{
    public class BrowserRefreshMiddlewareTest
    {
        [Theory]
        [InlineData("DELETE")]
        [InlineData("head")]
        [InlineData("Put")]
        public void IsBrowserRequest_ReturnsFalse_ForNonGetOrPostRequests(string method)
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                Request =
                {
                    Method = method,
                    Headers =
                    {
                        ["Accept"] = "application/html",
                    },
                },
            };

            // Act
            var result = BrowserRefreshMiddleware.IsBrowserRequest(context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsBrowserRequest_ReturnsFalse_IsRequestDoesNotAcceptHtml()
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                Request =
                {
                    Method = "GET",
                    Headers =
                    {
                        ["Accept"] = "application/xml",
                    },
                },
            };

            // Act
            var result = BrowserRefreshMiddleware.IsBrowserRequest(context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsBrowserRequest_ReturnsTrue_ForGetRequestsThatAcceptHtml()
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                Request =
                {
                    Method = "GET",
                    Headers =
                    {
                        ["Accept"] = "application/json,text/html;q=0.9",
                    },
                },
            };

            // Act
            var result = BrowserRefreshMiddleware.IsBrowserRequest(context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsBrowserRequest_ReturnsTrue_ForRequestsThatAcceptAnyHtml()
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                Request =
                {
                    Method = "Post",
                    Headers =
                    {
                        ["Accept"] = "application/json,text/*+html;q=0.9",
                    },
                },
            };

            // Act
            var result = BrowserRefreshMiddleware.IsBrowserRequest(context);

            // Assert
            Assert.True(result);
        }
    }
}
