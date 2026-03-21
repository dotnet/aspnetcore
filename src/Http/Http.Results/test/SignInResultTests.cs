// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class SignInResultTests
{
    [Fact]
    public async Task ExecuteAsync_InvokesSignInAsyncOnAuthenticationManager()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.SignInAsync(It.IsAny<HttpContext>(), "", principal, null))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var httpContext = GetHttpContext(auth.Object);
        var result = new SignInHttpResult(principal, "", null);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_InvokesSignInAsyncOnAuthenticationManagerWithDefaultScheme()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.SignInAsync(It.IsAny<HttpContext>(), null, principal, null))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var httpContext = GetHttpContext(auth.Object);
        var result = new SignInHttpResult(principal);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_InvokesSignInAsyncOnConfiguredScheme()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var authProperties = new AuthenticationProperties();
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.SignInAsync(It.IsAny<HttpContext>(), "Scheme1", principal, authProperties))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var httpContext = GetHttpContext(auth.Object);
        var result = new SignInHttpResult(principal, "Scheme1", authProperties);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new SignInHttpResult(new());
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    private static DefaultHttpContext GetHttpContext(IAuthenticationService auth)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = CreateServices()
            .AddSingleton(auth)
            .BuildServiceProvider();
        return httpContext;
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services;
    }
}
