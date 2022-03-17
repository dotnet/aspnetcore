// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class SignInResultTest
{
    [Fact]
    public async Task ExecuteResultAsync_InvokesSignInAsyncOnAuthenticationManager()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var httpContext = new Mock<HttpContext>();
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.SignInAsync(httpContext.Object, "", principal, null))
            .Returns(Task.CompletedTask)
            .Verifiable();
        httpContext.Setup(c => c.RequestServices).Returns(CreateServices(auth.Object));
        var result = new SignInResult("", principal, null);
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
    public async Task ExecuteResultAsync_InvokesSignInAsyncOnAuthenticationManagerWithDefaultScheme()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var httpContext = new Mock<HttpContext>();
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.SignInAsync(httpContext.Object, null, principal, null))
            .Returns(Task.CompletedTask)
            .Verifiable();
        httpContext.Setup(c => c.RequestServices).Returns(CreateServices(auth.Object));
        var result = new SignInResult(principal);
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
    public async Task ExecuteResultAsync_InvokesSignInAsyncOnConfiguredScheme()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var authProperties = new AuthenticationProperties();
        var httpContext = new Mock<HttpContext>();
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.SignInAsync(httpContext.Object, "Scheme1", principal, authProperties))
            .Returns(Task.CompletedTask)
            .Verifiable();
        httpContext.Setup(c => c.RequestServices).Returns(CreateServices(auth.Object));
        var result = new SignInResult("Scheme1", principal, authProperties);
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
