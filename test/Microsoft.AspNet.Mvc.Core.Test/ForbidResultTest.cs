// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ForbidResultTest
    {
        [Fact]
        public async Task ExecuteResultAsync_InvokesForbiddenAsyncOnAuthenticationManager()
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
        public async Task ExecuteResultAsync_InvokesForbiddenAsyncOnAllConfiguredSchemes()
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

        public static TheoryData ExecuteResultAsync_InvokesForbiddenAsyncWithAuthPropertiesData =>
            new TheoryData<AuthenticationProperties>
            {
                null,
                new AuthenticationProperties()
            };

        [Theory]
        [MemberData(nameof(ExecuteResultAsync_InvokesForbiddenAsyncWithAuthPropertiesData))]
        public async Task ExecuteResultAsync_InvokesForbiddenAsyncWithAuthProperties(AuthenticationProperties expected)
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
        [MemberData(nameof(ExecuteResultAsync_InvokesForbiddenAsyncWithAuthPropertiesData))]
        public async Task ExecuteResultAsync_InvokesForbiddenAsyncWithAuthProperties_WhenAuthenticationSchemesIsEmpty(
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