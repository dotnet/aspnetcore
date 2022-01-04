// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

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

    [Fact]
    public async Task ExecuteAsync_NoArgsInvokesDefaultSignOut()
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

        // Act
        await ((IResult)result).ExecuteAsync(httpContext.Object);

        // Assert
        auth.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_InvokesSignOutAsyncOnAuthenticationManager()
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

        // Act
        await ((IResult)result).ExecuteAsync(httpContext.Object);

        // Assert
        auth.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_InvokesSignOutAsyncOnAllConfiguredSchemes()
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

        // Act
        await ((IResult)result).ExecuteAsync(httpContext.Object);

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
