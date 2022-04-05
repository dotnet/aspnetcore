// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Result;

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class ConflictObjectResultTest
{
    [Fact]
    public void ConflictObjectResult_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new object();
        var conflictObjectResult = new ConflictObjectHttpResult(obj);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, conflictObjectResult.StatusCode);
        Assert.Equal(obj, conflictObjectResult.Value);
    }

    [Fact]
    public void ConflictObjectResult_ProblemDetails_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new ProblemDetails();
        var conflictObjectResult = new ConflictObjectHttpResult(obj);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, conflictObjectResult.StatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, obj.Status);
        Assert.Equal(obj, conflictObjectResult.Value);
    }

    [Fact]
    public async Task ConflictObjectResult_ExecuteAsync_SetsStatusCode()
    {
        // Arrange
        var result = new ConflictObjectHttpResult("Hello");
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ConflictObjectResult_ExecuteResultAsync_FormatsData()
    {
        // Arrange
        var result = new ConflictObjectHttpResult("Hello");
        var stream = new MemoryStream();
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
            Response =
                {
                    Body = stream,
                },
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("\"Hello\"", Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        return services.BuildServiceProvider();
    }
}
