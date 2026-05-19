// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.HttpResults;

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
        var responseDetails = JsonSerializer.Deserialize<ProblemDetails>(stream, JsonSerializerOptions.Web);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.6.1", responseDetails.Type);
        Assert.Equal("An error occurred while processing your request.", responseDetails.Title);
        Assert.Equal(StatusCodes.Status500InternalServerError, responseDetails.Status);
    }

    [Fact]
    public async Task ExecuteAsync_UsesDefaultsFromProblemDetailsService_ForProblemDetails()
    {
        // Arrange
        var details = new ProblemDetails();

        var result = new ProblemHttpResult(details);
        var stream = new MemoryStream();
        var services = CreateServiceCollection()
            .AddProblemDetails(options => options.CustomizeProblemDetails = x => x.ProblemDetails.Type = null)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = services,
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
        var responseDetails = JsonSerializer.Deserialize<ProblemDetails>(stream, JsonSerializerOptions.Web);
        Assert.Null(responseDetails.Type);
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
        var responseDetails = JsonSerializer.Deserialize<HttpValidationProblemDetails>(stream, JsonSerializerOptions.Web);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", responseDetails.Type);
        Assert.Equal("One or more validation errors occurred.", responseDetails.Title);
        Assert.Equal(StatusCodes.Status400BadRequest, responseDetails.Status);
    }

    [Fact]
    public async Task ExecuteAsync_SetsTitleFromReasonPhrases_WhenNotInDefaults()
    {
        // Arrange
        var details = new ProblemDetails()
        {
            Status = StatusCodes.Status418ImATeapot,
        };

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
        Assert.Equal(StatusCodes.Status418ImATeapot, httpContext.Response.StatusCode);
        stream.Position = 0;
        var responseDetails = JsonSerializer.Deserialize<HttpValidationProblemDetails>(stream, JsonSerializerOptions.Web);
        Assert.Null(responseDetails.Type);
        Assert.Equal("I'm a teapot", responseDetails.Title);
        Assert.Equal(StatusCodes.Status418ImATeapot, responseDetails.Status);
    }

    [Fact]
    public async Task ExecuteAsync_IncludeErrors_ForValidationProblemDetails()
    {
        // Arrange
        var details = new HttpValidationProblemDetails(new Dictionary<string, string[]>
        {
            { "testError", new string[] { "message" } }
        });

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
        var responseDetails = JsonSerializer.Deserialize<HttpValidationProblemDetails>(stream, JsonSerializerOptions.Web);
        Assert.Equal(StatusCodes.Status400BadRequest, responseDetails.Status);
        var error = Assert.Single(responseDetails.Errors);
        Assert.Equal("testError", error.Key);
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

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new ProblemHttpResult(new());
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void ProblemResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new ProblemHttpResult(new() { Status = StatusCodes.Status416RangeNotSatisfiable }));
        Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, result.StatusCode);
    }

    [Fact]
    public void ProblemResult_Implements_IStatusCodeHttpResult_Correctly_WithDefaultStatusCode()
    {
        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new ProblemHttpResult(new()));
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    [Fact]
    public void ProblemResult_Implements_IValueHttpResult_Correctly()
    {
        // Arrange
        var value = new ProblemDetails();

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult>(new ProblemHttpResult(value));
        Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void ProblemResult_Implements_IValueHttpResultOfT_Correctly()
    {
        // Arrange
        var value = new ProblemDetails();

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult<ProblemDetails>>(new ProblemHttpResult(value));
        Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void ProblemResult_Implements_IContentTypeHttpResult_Correctly()
    {
        // Act & Assert
        var result = Assert.IsAssignableFrom<IContentTypeHttpResult>(new ProblemHttpResult(new()));
        Assert.Equal("application/problem+json", result.ContentType);
    }

    private static IServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }

    private static IServiceProvider CreateServices()
    {
        var services = CreateServiceCollection();

        return services.BuildServiceProvider();
    }
}
