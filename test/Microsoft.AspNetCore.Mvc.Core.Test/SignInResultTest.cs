// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
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
    public class SignInResultTest
    {
        [Fact]
        public async Task ExecuteResultAsync_InvokesSignInAsyncOnAuthenticationManager()
        {
            // Arrange
            var principal = new ClaimsPrincipal();
            var authenticationManager = new Mock<AuthenticationManager>();
            authenticationManager
                .Setup(c => c.SignInAsync("", principal, null))
                .Returns(TaskCache.CompletedTask)
                .Verifiable();
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices).Returns(CreateServices());
            httpContext.Setup(c => c.Authentication).Returns(authenticationManager.Object);
            var result = new SignInResult("", principal, null);
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
        public async Task ExecuteResultAsync_InvokesSignInAsyncOnConfiguredScheme()
        {
            // Arrange
            var principal = new ClaimsPrincipal();
            var authProperties = new AuthenticationProperties();
            var authenticationManager = new Mock<AuthenticationManager>();
            authenticationManager
                .Setup(c => c.SignInAsync("Scheme1", principal, authProperties))
                .Returns(TaskCache.CompletedTask)
                .Verifiable();
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices).Returns(CreateServices());
            httpContext.Setup(c => c.Authentication).Returns(authenticationManager.Object);
            var result = new SignInResult("Scheme1", principal, authProperties);
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