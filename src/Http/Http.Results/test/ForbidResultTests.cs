// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class ForbidResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_InvokesForbidAsyncOnAuthenticationService()
    {
        // Arrange
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.ForbidAsync(It.IsAny<HttpContext>(), "", null))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var httpContext = GetHttpContext(auth.Object);
        var result = new ForbidHttpResult("", null);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify();
    }

    [Fact]
    public async Task ExecuteResultAsync_InvokesForbidAsyncOnAllConfiguredSchemes()
    {
        // Arrange
        var authProperties = new AuthenticationProperties();
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.ForbidAsync(It.IsAny<HttpContext>(), "Scheme1", authProperties))
            .Returns(Task.CompletedTask)
            .Verifiable();
        auth
            .Setup(c => c.ForbidAsync(It.IsAny<HttpContext>(), "Scheme2", authProperties))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var httpContext = GetHttpContext(auth.Object);
        var result = new ForbidHttpResult(new[] { "Scheme1", "Scheme2" }, authProperties);
        var routeData = new RouteData();

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify();
    }

    public static TheoryData<AuthenticationProperties> ExecuteResultAsync_InvokesForbidAsyncWithAuthPropertiesData =>
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
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.ForbidAsync(It.IsAny<HttpContext>(), null, expected))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var result = new ForbidHttpResult(expected);
        var httpContext = GetHttpContext(auth.Object);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify();
    }

    [Theory]
    [MemberData(nameof(ExecuteResultAsync_InvokesForbidAsyncWithAuthPropertiesData))]
    public async Task ExecuteResultAsync_InvokesForbidAsyncWithAuthProperties_WhenAuthenticationSchemesIsEmpty(
        AuthenticationProperties expected)
    {
        // Arrange
        var auth = new Mock<IAuthenticationService>();
        auth
            .Setup(c => c.ForbidAsync(It.IsAny<HttpContext>(), null, expected))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var httpContext = GetHttpContext(auth.Object);
        var result = new ForbidHttpResult(expected)
        {
            AuthenticationSchemes = new string[0]
        };
        var routeData = new RouteData();

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new ForbidHttpResult();
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
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }
}
