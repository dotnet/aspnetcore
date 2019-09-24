// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class RedirectResultTest
    {
        [Fact]
        public void RedirectResult_Constructor_WithParameterUrl_SetsResultUrlAndNotPermanentOrPreserveMethod()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new RedirectResult(url);

            // Assert
            Assert.False(result.PreserveMethod);
            Assert.False(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Fact]
        public void RedirectResult_Constructor_WithParameterUrlAndPermanent_SetsResultUrlAndPermanentNotPreserveMethod()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new RedirectResult(url, permanent: true);

            // Assert
            Assert.False(result.PreserveMethod);
            Assert.True(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Fact]
        public void RedirectResult_Constructor_WithParameterUrlPermanentAndPreservesMethod_SetsResultUrlPermanentAndPreservesMethod()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new RedirectResult(url, permanent: true, preserveMethod: true);

            // Assert
            Assert.True(result.PreserveMethod);
            Assert.True(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Theory]
        [InlineData("", "/Home/About", "/Home/About")]
        [InlineData("/myapproot", "/test", "/test")]
        public async Task Execute_ReturnsContentPath_WhenItDoesNotStartWithTilde(
            string appRoot,
            string contentPath,
            string expectedPath)
        {
            // Arrange
            var httpContext = GetHttpContext(appRoot);
            var actionContext = GetActionContext(httpContext);
            var result = new RedirectResult(contentPath);

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            // Verifying if Redirect was called with the specific Url and parameter flag.
            Assert.Equal(expectedPath, httpContext.Response.Headers[HeaderNames.Location].ToString());
            Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        }

        [Theory]
        [InlineData(null, "~/Home/About", "/Home/About")]
        [InlineData("/", "~/Home/About", "/Home/About")]
        [InlineData("/", "~/", "/")]
        [InlineData("", "~/Home/About", "/Home/About")]
        [InlineData("/myapproot", "~/", "/myapproot/")]
        public async Task Execute_ReturnsAppRelativePath_WhenItStartsWithTilde(
            string appRoot,
            string contentPath,
            string expectedPath)
        {
            // Arrange
            var httpContext = GetHttpContext(appRoot);
            var actionContext = GetActionContext(httpContext);
            var result = new RedirectResult(contentPath);

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            // Verifying if Redirect was called with the specific Url and parameter flag.
            Assert.Equal(expectedPath, httpContext.Response.Headers[HeaderNames.Location].ToString());
            Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        }

        private static ActionContext GetActionContext(HttpContext httpContext)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(new Mock<IRouter>().Object);

            return new ActionContext(httpContext,
                                    routeData,
                                    new ActionDescriptor());
        }

        private static IServiceProvider GetServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IActionResultExecutor<RedirectResult>, RedirectResultExecutor>();
            serviceCollection.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            serviceCollection.AddTransient<ILoggerFactory, LoggerFactory>();
            return serviceCollection.BuildServiceProvider();
        }

        private static HttpContext GetHttpContext(
            string appRoot)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = GetServiceProvider();
            httpContext.Request.PathBase = new PathString(appRoot);
            return httpContext;
        }
    }
}