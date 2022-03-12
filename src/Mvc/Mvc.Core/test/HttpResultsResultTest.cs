// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc;

public class HttpResultsResultTest
{
    [Fact]
    public void HttpResultsResult_InitializesWithResultsStaticMethods()
    {
        // Arrange & Act
        var result = new HttpResultsActionResult(Results.Ok());

        // Assert
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task HttpResultsResult_SetsStatusCode()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            RequestServices = CreateServices().BuildServiceProvider()
        };

        var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var result = new HttpResultsActionResult(Results.StatusCode(StatusCodes.Status400BadRequest));

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.HttpContext.Response.StatusCode);
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }
}
