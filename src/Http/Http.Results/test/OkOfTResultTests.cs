// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class OkOfTResultTests
{
    [Fact]
    public void OkObjectResult_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var value = "Hello world";
        var result = new Ok<string>(value);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void OkObjectResult_ProblemDetails_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new HttpValidationProblemDetails();
        var result = new Ok<HttpValidationProblemDetails>(obj);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, obj.Status);
        Assert.Equal(obj, result.Value);
    }

    [Fact]
    public async Task OkObjectResult_ExecuteAsync_FormatsData()
    {
        // Arrange
        var result = new Ok<string>("Hello");
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

    [Fact]
    public async Task OkObjectResult_ExecuteAsync_SetsStatusCode()
    {
        // Arrange
        var result = new Ok<string>("Hello");
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices()
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        return services.BuildServiceProvider();
    }
}
