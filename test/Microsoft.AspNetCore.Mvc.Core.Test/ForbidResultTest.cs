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
    public class ForbidResultTest
    {
        [Fact]
        public async Task ExecuteResultAsync_InvokesForbidAsyncOnAuthenticationManager()
        {
            // Arrange
            var authenticationManager = new Mock<AuthenticationManager>();
            authenticationManager
                .Setup(c => c.ForbidAsync("", null))
                .Returns(TaskCache.CompletedTask)
                .Verifiable();
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices).Returns(CreateServices());
            httpContext.Setup(c => c.Authentication).Returns(authenticationManager.Object);
            var result = new ForbidResult("", null);
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
        public async Task ExecuteResultAsync_InvokesForbidAsyncOnAllConfiguredSchemes()
        {
            // Arrange
            var authProperties = new AuthenticationProperties();
            var authenticationManager = new Mock<AuthenticationManager>();
            authenticationManager
                .Setup(c => c.ForbidAsync("Scheme1", authProperties))
                .Returns(TaskCache.CompletedTask)
                .Verifiable();
            authenticationManager
                .Setup(c => c.ForbidAsync("Scheme2", authProperties))
                .Returns(TaskCache.CompletedTask)
                .Verifiable();
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices).Returns(CreateServices());
            httpContext.Setup(c => c.Authentication).Returns(authenticationManager.Object);
            var result = new ForbidResult(new[] { "Scheme1", "Scheme2" }, authProperties);
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

        public static TheoryData ExecuteResultAsync_InvokesForbidAsyncWithAuthPropertiesData =>
            new TheoryData<AuthenticationProperties>
            {
                null,
                new AuthenticationProperties()
            };

        [Theory]
        [MemberData(nameof(ExecuteResultAsync_InvokesForbidAsyncWithAuthPropertiesData))]
        public async Task ExecuteResultAsync_InvokesForbidAsyncWithAuthProperties(AuthenticationProperties expected)
        {
            // Arrange
            var authenticationManager = new Mock<AuthenticationManager>();
            authenticationManager
                .Setup(c => c.ForbidAsync(expected))
                .Returns(TaskCache.CompletedTask)
                .Verifiable();
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices).Returns(CreateServices());
            httpContext.Setup(c => c.Authentication).Returns(authenticationManager.Object);
            var result = new ForbidResult(expected);
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

        [Theory]
        [MemberData(nameof(ExecuteResultAsync_InvokesForbidAsyncWithAuthPropertiesData))]
        public async Task ExecuteResultAsync_InvokesForbidAsyncWithAuthProperties_WhenAuthenticationSchemesIsEmpty(
            AuthenticationProperties expected)
        {
            // Arrange
            var authenticationManager = new Mock<AuthenticationManager>();
            authenticationManager
                .Setup(c => c.ForbidAsync(expected))
                .Returns(TaskCache.CompletedTask)
                .Verifiable();
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices).Returns(CreateServices());
            httpContext.Setup(c => c.Authentication).Returns(authenticationManager.Object);
            var result = new ForbidResult(expected)
            {
                AuthenticationSchemes = new string[0]
            };
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