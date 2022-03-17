// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Http.Result;

public class SignOutResultTest
{
    [Fact]
    public async Task ExecuteAsync_NoArgsInvokesDefaultSignOut()
    {
        // Arrange
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.SignOutAsync(It.IsAny<HttpContext>(), null, null))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var httpContext = GetHttpContext(auth.Object);
        var result = new SignOutResult();

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_InvokesSignOutAsyncOnAuthenticationManager()
    {
        // Arrange
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.SignOutAsync(It.IsAny<HttpContext>(), "", null))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var httpContext = GetHttpContext(auth.Object);
        var result = new SignOutResult("", null);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_InvokesSignOutAsyncOnAllConfiguredSchemes()
    {
        // Arrange
        var authProperties = new AuthenticationProperties();
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.SignOutAsync(It.IsAny<HttpContext>(), "Scheme1", authProperties))
            .Returns(Task.CompletedTask)
            .Verifiable();
        auth
            .Setup(c => c.SignOutAsync(It.IsAny<HttpContext>(), "Scheme2", authProperties))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var httpContext = GetHttpContext(auth.Object);
        var result = new SignOutResult(new[] { "Scheme1", "Scheme2" }, authProperties);

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
