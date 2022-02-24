// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.Result;

public class NotFoundObjectResultTest
{
    [Fact]
    public void NotFoundObjectResult_InitializesStatusCode()
    {
        // Arrange & act
        var notFound = new NotFoundObjectResult(null);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public void NotFoundObjectResult_InitializesStatusCodeAndResponseContent()
    {
        // Arrange & act
        var notFound = new NotFoundObjectResult("Test Content");

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        Assert.Equal("Test Content", notFound.Value);
    }

    [Fact]
    public async Task NotFoundObjectResult_ExecuteSuccessful()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var result = new NotFoundObjectResult("Test Content");

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    private static HttpContext GetHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = new PathString("");
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = CreateServices();
        return httpContext;
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        return services.BuildServiceProvider();
    }
}
