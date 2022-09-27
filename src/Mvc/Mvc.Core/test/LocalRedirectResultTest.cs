// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class LocalRedirectResultTest
{
    [Fact]
    public void Constructor_WithParameterUrl_SetsResultUrlAndNotPermanentOrPreserveMethod()
    {
        // Arrange
        var url = "/test/url";

        // Act
        var result = new LocalRedirectResult(url);

        // Assert
        Assert.False(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public void Constructor_WithParameterUrlAndPermanent_SetsResultUrlAndPermanentNotPreserveMethod()
    {
        // Arrange
        var url = "/test/url";

        // Act
        var result = new LocalRedirectResult(url, permanent: true);

        // Assert
        Assert.False(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public void Constructor_WithParameterUrlAndPermanent_SetsResultUrlPermanentAndPreserveMethod()
    {
        // Arrange
        var url = "/test/url";

        // Act
        var result = new LocalRedirectResult(url, permanent: true, preserveMethod: true);

        // Assert
        Assert.True(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public async Task Execute_ReturnsExpectedValues()
    {
        // Arrange
        var appRoot = "/";
        var contentPath = "~/Home/About";
        var expectedPath = "/Home/About";

        var httpContext = GetHttpContext(appRoot);
        var actionContext = GetActionContext(httpContext);
        var result = new LocalRedirectResult(contentPath);

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(expectedPath, httpContext.Response.Headers.Location.ToString());
        Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
    }

    [Theory]
    [InlineData("", "//")]
    [InlineData("", "/\\")]
    [InlineData("", "//foo")]
    [InlineData("", "/\\foo")]
    [InlineData("", "Home/About")]
    [InlineData("/myapproot", "http://www.example.com")]
    public async Task Execute_Throws_ForNonLocalUrl(
        string appRoot,
        string contentPath)
    {
        // Arrange
        var httpContext = GetHttpContext(appRoot);
        var actionContext = GetActionContext(httpContext);
        var result = new LocalRedirectResult(contentPath);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => result.ExecuteResultAsync(actionContext));
        Assert.Equal(
            "The supplied URL is not local. A URL with an absolute path is considered local if it does not " +
            "have a host/authority part. URLs using virtual paths ('~/') are also local.",
            exception.Message);
    }

    [Theory]
    [InlineData("", "~//")]
    [InlineData("", "~/\\")]
    [InlineData("", "~//foo")]
    [InlineData("", "~/\\foo")]
    public async Task Execute_Throws_ForNonLocalUrlTilde(
        string appRoot,
        string contentPath)
    {
        // Arrange
        var httpContext = GetHttpContext(appRoot);
        var actionContext = GetActionContext(httpContext);
        var result = new LocalRedirectResult(contentPath);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => result.ExecuteResultAsync(actionContext));
        Assert.Equal(
            "The supplied URL is not local. A URL with an absolute path is considered local if it does not " +
            "have a host/authority part. URLs using virtual paths ('~/') are also local.",
            exception.Message);
    }

    private static ActionContext GetActionContext(HttpContext httpContext)
    {
        var routeData = new RouteData();
        routeData.Routers.Add(new Mock<IRouter>().Object);

        return new ActionContext(httpContext, routeData, new ActionDescriptor());
    }

    private static IServiceProvider GetServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IActionResultExecutor<LocalRedirectResult>, LocalRedirectResultExecutor>();
        serviceCollection.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
        serviceCollection.AddTransient<ILoggerFactory, LoggerFactory>();
        return serviceCollection.BuildServiceProvider();
    }

    private static HttpContext GetHttpContext(
        string appRoot)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = GetServiceProvider();
        httpContext.Request.PathBase = new PathString(appRoot);
        return httpContext;
    }
}
