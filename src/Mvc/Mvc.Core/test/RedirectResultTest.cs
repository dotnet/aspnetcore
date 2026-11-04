// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc;

public class RedirectResultTest : RedirectResultTestBase
{
    protected override Task ExecuteAsync(HttpContext httpContext, string contentPath)
    {
        httpContext.RequestServices = GetServiceProvider();
        var actionContext = new ActionContext(httpContext, new(), new());

        var redirectResult = new RedirectResult(contentPath);
        return redirectResult.ExecuteResultAsync(actionContext);
    }

    private static IServiceProvider GetServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IActionResultExecutor<RedirectResult>, RedirectResultExecutor>();
        serviceCollection.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
        serviceCollection.AddTransient<ILoggerFactory, NullLoggerFactory>();
        return serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public void RedirectResult_Constructor_WithParameterUrl_SetsResultUrlAndNotPermanentOrPreserveMethod()
    {
        // Arrange
        var url = "/test/url";

        // Act
        var result = new RedirectResult(url);

        // Assert
        Assert.False(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public void RedirectResult_Constructor_WithParameterUrlAndPermanent_SetsResultUrlAndPermanentNotPreserveMethod()
    {
        // Arrange
        var url = "/test/url";

        // Act
        var result = new RedirectResult(url, permanent: true);

        // Assert
        Assert.False(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public void RedirectResult_Constructor_WithParameterUrlPermanentAndPreservesMethod_SetsResultUrlPermanentAndPreservesMethod()
    {
        // Arrange
        var url = "/test/url";

        // Act
        var result = new RedirectResult(url, permanent: true, preserveMethod: true);

        // Assert
        Assert.True(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Same(url, result.Url);
    }
}
