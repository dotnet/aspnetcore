// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Result;

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class UnprocessableEntityObjectResultTests
{
    [Fact]
    public void NotFoundObjectResult_ProblemDetails_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new HttpValidationProblemDetails();
        var result = new UnprocessableEntityObjectHttpResult(obj);

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, obj.Status);
        Assert.Equal(obj, result.Value);
    }

    [Fact]
    public void UnprocessableEntityObjectResult_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new object();
        var result = new UnprocessableEntityObjectHttpResult(obj);

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
        Assert.Equal(obj, result.Value);
    }

    [Fact]
    public async Task UnprocessableEntityObjectResult_ExecuteAsync_SetsStatusCode()
    {
        // Arrange
        var result = new UnprocessableEntityObjectHttpResult("Hello");
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task UnprocessableEntityObjectResult_ExecuteResultAsync_FormatsData()
    {
        // Arrange
        var result = new UnprocessableEntityObjectHttpResult("Hello");
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
