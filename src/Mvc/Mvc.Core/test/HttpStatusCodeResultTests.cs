// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc;

public class HttpStatusCodeResultTests
{
    [Fact]
    public void HttpStatusCodeResult_ExecuteResultSetsResponseStatusCode()
    {
        // Arrange
        var result = new StatusCodeResult(StatusCodes.Status404NotFound);

        var httpContext = GetHttpContext();
        var routeData = new RouteData();
        var actionDescriptor = new ActionDescriptor();

        var context = new ActionContext(httpContext, routeData, actionDescriptor);

        // Act
        result.ExecuteResult(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    [Fact]
    public void HttpStatusCodeResult_ReturnsCorrectStatusCodeAsIStatusCodeActionResult()
    {
        // Arrange
        var result = new StatusCodeResult(StatusCodes.Status404NotFound);

        // Act
        var statusResult = result as IStatusCodeActionResult;

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, statusResult?.StatusCode);
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }

    private static HttpContext GetHttpContext()
    {
        var services = CreateServices();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }
}
