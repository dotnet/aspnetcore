// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Result;

public class EmptyResultTest
{
    [Fact]
    public async Task EmptyResult_DoesNothing()
    {
        // Arrange
        var emptyResult = EmptyHttpResult.Instance;

        // Act
        var httpContext = GetHttpContext();
        var memoryStream = httpContext.Response.Body as MemoryStream;
        await emptyResult.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal(0, memoryStream.Length);
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
        return new ServiceCollection().BuildServiceProvider();
    }
}
