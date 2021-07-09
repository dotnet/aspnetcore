// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Internal
{
    public abstract class RedirectResultTestBase
    {
        protected abstract Task ExecuteAsync(HttpContext httpContext, string contentPath);

        [Theory]
        [InlineData("", "/Home/About", "/Home/About")]
        [InlineData("/myapproot", "/test", "/test")]
        public async Task Execute_ReturnsContentPath_WhenItDoesNotStartWithTilde<TContext>(
            string appRoot,
            string contentPath,
            string expectedPath)
        {
            // Arrange
            var httpContext = GetHttpContext(appRoot);

            // Act
            await ExecuteAsync(httpContext, contentPath);

            // Assert
            // Verifying if Redirect was called with the specific Url and parameter flag.
            Assert.Equal(expectedPath, httpContext.Response.Headers.Location.ToString());
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

            // Act
            await ExecuteAsync(httpContext, contentPath);

            // Assert
            // Verifying if Redirect was called with the specific Url and parameter flag.
            Assert.Equal(expectedPath, httpContext.Response.Headers.Location.ToString());
            Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        }

        private static IServiceProvider GetServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<ILoggerFactory, NullLoggerFactory>();
            serviceCollection.AddTransient(typeof(ILogger<>), typeof(NullLogger<>));
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
