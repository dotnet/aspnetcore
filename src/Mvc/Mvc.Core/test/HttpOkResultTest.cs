// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc;

public class HttpOkResultTest
{
    [Fact]
    public void HttpOkResult_InitializesStatusCode()
    {
        // Arrange & Act
        var result = new OkResult();

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Fact]
    public async Task HttpOkResult_SetsStatusCode()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = CreateServices().BuildServiceProvider();

        var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var result = new OkResult();

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.HttpContext.Response.StatusCode);
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }
}
