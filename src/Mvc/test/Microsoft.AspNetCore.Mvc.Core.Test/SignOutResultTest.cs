// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class SignOutResultTest
    {
        [Fact]
        public async Task ExecuteResultAsync_NoArgsInvokesDefaultSignOut()
        {
            // Arrange
            var httpContext = new Mock<HttpContext>();
            var auth = new Mock<IAuthenticationService>();
            auth
                .Setup(c => c.SignOutAsync(httpContext.Object, null, null))
                .Returns(Task.CompletedTask)
                .Verifiable();
            httpContext.Setup(c => c.RequestServices).Returns(CreateServices(auth.Object));
            var result = new SignOutResult();
            var routeData = new RouteData();

            var actionContext = new ActionContext(
                httpContext.Object,
                routeData,
                new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            auth.Verify();
        }

        [Fact]
        public async Task ExecuteResultAsync_InvokesSignOutAsyncOnAuthenticationManager()
        {
            // Arrange
            var httpContext = new Mock<HttpContext>();
            var auth = new Mock<IAuthenticationService>();
            auth
                .Setup(c => c.SignOutAsync(httpContext.Object, "", null))
                .Returns(Task.CompletedTask)
                .Verifiable();
            httpContext.Setup(c => c.RequestServices).Returns(CreateServices(auth.Object));
            var result = new SignOutResult("", null);
            var routeData = new RouteData();

            var actionContext = new ActionContext(
                httpContext.Object,
                routeData,
                new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            auth.Verify();
        }

        [Fact]
        public async Task ExecuteResultAsync_InvokesSignOutAsyncOnAllConfiguredSchemes()
        {
            // Arrange
            var authProperties = new AuthenticationProperties();
            var httpContext = new Mock<HttpContext>();
            var auth = new Mock<IAuthenticationService>();
            auth
                .Setup(c => c.SignOutAsync(httpContext.Object, "Scheme1", authProperties))
                .Returns(Task.CompletedTask)
                .Verifiable();
            auth
                .Setup(c => c.SignOutAsync(httpContext.Object, "Scheme2", authProperties))
                .Returns(Task.CompletedTask)
                .Verifiable();
            httpContext.Setup(c => c.RequestServices).Returns(CreateServices(auth.Object));
            var result = new SignOutResult(new[] { "Scheme1", "Scheme2" }, authProperties);
            var routeData = new RouteData();

            var actionContext = new ActionContext(
                httpContext.Object,
                routeData,
                new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            auth.Verify();
        }

        private static IServiceProvider CreateServices(IAuthenticationService auth)
        {
            return new ServiceCollection()
                .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
                .AddSingleton(auth)
                .BuildServiceProvider();
        }
    }
}