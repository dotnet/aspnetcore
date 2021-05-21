// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class BaseRedirectResultTest
    {
        public static async Task Execute_ReturnsContentPath_WhenItDoesNotStartWithTilde(
            string appRoot,
            string contentPath,
            string expectedPath,
            string action,
            Func<RedirectResult, object, Task> function)
        {
            // Arrange
            var httpContext = GetHttpContext(appRoot);
            var actionContext = GetActionContext(httpContext);
            var result = new RedirectResult(contentPath);

            // Act
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            // Verifying if Redirect was called with the specific Url and parameter flag.
            Assert.Equal(expectedPath, httpContext.Response.Headers.Location.ToString());
            Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        }

        public static async Task Execute_ReturnsAppRelativePath_WhenItStartsWithTilde(
            string appRoot,
            string contentPath,
            string expectedPath,
            string action,
            Func<RedirectResult, object, Task> function)
        {
            // Arrange
            var httpContext = GetHttpContext(appRoot);
            var actionContext = GetActionContext(httpContext);
            var result = new RedirectResult(contentPath);

            // Act
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            // Verifying if Redirect was called with the specific Url and parameter flag.
            Assert.Equal(expectedPath, httpContext.Response.Headers.Location.ToString());
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
