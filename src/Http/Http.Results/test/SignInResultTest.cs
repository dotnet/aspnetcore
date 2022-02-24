// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Http.Result;

public class SignInResultTest
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
        var result = new SignInResult("", principal, null);

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
        var result = new SignInResult(principal);

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
        var result = new SignInResult("Scheme1", principal, authProperties);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify();
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
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services;
    }
}
