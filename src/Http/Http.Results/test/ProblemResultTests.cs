// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.Result;

public class ProblemResultTests
{
    [Fact]
    public async Task ExecuteAsync_UsesDefaults_ForProblemDetails()
    {
        // Arrange
        var details = new ProblemDetails();

        var result = new ProblemHttpResult(details);
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
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        stream.Position = 0;
        var responseDetails = JsonSerializer.Deserialize<ProblemDetails>(stream);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.1", responseDetails.Type);
        Assert.Equal("An error occurred while processing your request.", responseDetails.Title);
        Assert.Equal(StatusCodes.Status500InternalServerError, responseDetails.Status);
    }

    [Fact]
    public async Task ExecuteAsync_UsesDefaults_ForValidationProblemDetails()
    {
        // Arrange
        var details = new HttpValidationProblemDetails();

        var result = new ProblemHttpResult(details);
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
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        stream.Position = 0;
        var responseDetails = JsonSerializer.Deserialize<HttpValidationProblemDetails>(stream);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", responseDetails.Type);
        Assert.Equal("One or more validation errors occurred.", responseDetails.Title);
        Assert.Equal(StatusCodes.Status400BadRequest, responseDetails.Status);
    }

    [Fact]
    public async Task ExecuteAsync_GetsStatusCodeFromProblemDetails()
    {
        // Arrange
        var details = new ProblemDetails { Status = StatusCodes.Status413RequestEntityTooLarge, };

        var result = new ProblemHttpResult(details);

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status413RequestEntityTooLarge, details.Status.Value);
        Assert.Equal(StatusCodes.Status413RequestEntityTooLarge, result.StatusCode);
        Assert.Equal(StatusCodes.Status413RequestEntityTooLarge, httpContext.Response.StatusCode);
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        return services.BuildServiceProvider();
    }
}
