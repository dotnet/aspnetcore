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
    public class BaseLocalRedirectResultTest
    {
        public static async Task Execute_ReturnsExpectedValues<TContext>(
            Func<LocalRedirectResult, TContext, Task> function)
        {
            // Arrange
            var appRoot = "/";
            var contentPath = "~/Home/About";
            var expectedPath = "/Home/About";

            var httpContext = GetHttpContext(appRoot);
            var actionContext = GetActionContext(httpContext);
            var result = new LocalRedirectResult(contentPath);

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            Assert.Equal(expectedPath, httpContext.Response.Headers.Location.ToString());
            Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        }

        public static async Task Execute_Throws_ForNonLocalUrl<TContext>(
            string appRoot,
            string contentPath,
            Func<LocalRedirectResult, TContext, Task> function)
        {
            // Arrange
            var httpContext = GetHttpContext(appRoot);
            var actionContext = GetActionContext(httpContext);
            var result = new LocalRedirectResult(contentPath);

            // Act & Assert
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => function(result, (TContext)context));
            Assert.Equal(
                "The supplied URL is not local. A URL with an absolute path is considered local if it does not " +
                "have a host/authority part. URLs using virtual paths ('~/') are also local.",
                exception.Message);
        }

        public static async Task Execute_Throws_ForNonLocalUrlTilde<TContext>(
            string appRoot,
            string contentPath,
            Func<LocalRedirectResult, TContext, Task> function)
        {
            // Arrange
            var httpContext = GetHttpContext(appRoot);
            var actionContext = GetActionContext(httpContext);
            var result = new LocalRedirectResult(contentPath);

            // Act & Assert
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => function(result, (TContext)context));
            Assert.Equal(
                "The supplied URL is not local. A URL with an absolute path is considered local if it does not " +
                "have a host/authority part. URLs using virtual paths ('~/') are also local.",
                exception.Message);
        }

        private static ActionContext GetActionContext(HttpContext httpContext)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(new Mock<IRouter>().Object);

            return new ActionContext(httpContext, routeData, new ActionDescriptor());
        }

        private static IServiceProvider GetServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IActionResultExecutor<LocalRedirectResult>, LocalRedirectResultExecutor>();
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
