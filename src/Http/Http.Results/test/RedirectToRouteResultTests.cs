// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class RedirectToRouteResultTests
{
    [Fact]
    public async Task RedirectToRoute_Execute_ThrowsOnNullUrl()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = CreateServices(null).BuildServiceProvider();

        var result = new RedirectToRouteHttpResult(null, new Dictionary<string, object>());

        // Act & Assert
        await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                await result.ExecuteAsync(httpContext);
            },
            "No route matches the supplied values.");
    }

    [Fact]
    public async Task ExecuteResultAsync_UsesRouteName_ToGenerateLocationHeader()
    {
        // Arrange
        var routeName = "orders_api";
        var locationUrl = "/api/orders/10";

        var httpContext = GetHttpContext(locationUrl);

        var result = new RedirectToRouteHttpResult(routeName, new { id = 10 });

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.True(httpContext.Response.Headers.ContainsKey("Location"), "Location header not found");
        Assert.Equal(locationUrl, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task ExecuteResultAsync_WithFragment_PassesCorrectValuesToRedirect()
    {
        // Arrange
        var expectedUrl = "/SampleAction#test";
        var expectedStatusCode = StatusCodes.Status301MovedPermanently;
        var httpContext = GetHttpContext(expectedUrl);

        var result = new RedirectToRouteHttpResult("Sample", null, true, "test");

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task ExecuteResultAsync_WithFragment_PassesCorrectValuesToRedirect_WithPreserveMethod()
    {
        // Arrange
        var expectedUrl = "/SampleAction#test";
        var expectedStatusCode = StatusCodes.Status308PermanentRedirect;

        var httpContext = GetHttpContext(expectedUrl);
        var result = new RedirectToRouteHttpResult("Sample", null, true, true, "test");

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new RedirectToRouteHttpResult(null);
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    private static HttpContext GetHttpContext(string path)
    {
        var services = CreateServices(path);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }

    private static IServiceCollection CreateServices(string path)
    {
        var services = new ServiceCollection();
        services.AddSingleton<LinkGenerator>(new TestLinkGenerator { Url = path });

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }
}
