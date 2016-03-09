// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class SignOutResultTest
    {
        [Fact]
        public async Task ExecuteResultAsync_InvokesSignOutAsyncOnAuthenticationManager()
        {
            // Arrange
            var authenticationManager = new Mock<AuthenticationManager>();
            authenticationManager
                .Setup(c => c.SignOutAsync("", null))
                .Returns(TaskCache.CompletedTask)
                .Verifiable();
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices).Returns(CreateServices());
            httpContext.Setup(c => c.Authentication).Returns(authenticationManager.Object);
            var result = new SignOutResult("", null);
            var routeData = new RouteData();

            var actionContext = new ActionContext(
                httpContext.Object,
                routeData,
                new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            authenticationManager.Verify();
        }

        [Fact]
        public async Task ExecuteResultAsync_InvokesSignOutAsyncOnAllConfiguredSchemes()
        {
            // Arrange
            var authProperties = new AuthenticationProperties();
            var authenticationManager = new Mock<AuthenticationManager>();
            authenticationManager
                .Setup(c => c.SignOutAsync("Scheme1", authProperties))
                .Returns(TaskCache.CompletedTask)
                .Verifiable();
            authenticationManager
                .Setup(c => c.SignOutAsync("Scheme2", authProperties))
                .Returns(TaskCache.CompletedTask)
                .Verifiable();
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices).Returns(CreateServices());
            httpContext.Setup(c => c.Authentication).Returns(authenticationManager.Object);
            var result = new SignOutResult(new[] { "Scheme1", "Scheme2" }, authProperties);
            var routeData = new RouteData();

            var actionContext = new ActionContext(
                httpContext.Object,
                routeData,
                new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            authenticationManager.Verify();
        }

        private static IServiceProvider CreateServices()
        {
            return new ServiceCollection()
                .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
                .BuildServiceProvider();
        }
    }
}