// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class JsonResultTests
{
    [Fact]
    public async Task JsonResult_ExecuteAsync_WithNullValue_Works()
    {
        // Arrange
        var result = new JsonHttpResult<object>(value: null, statusCode: 411, jsonSerializerOptions: null);

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(411, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task JsonResult_ExecuteAsync_SetsStatusCode()
    {
        // Arrange
        var result = new JsonHttpResult<object>(value: null, statusCode: 407, jsonSerializerOptions: null);

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(407, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task JsonResult_ExecuteAsync_JsonSerializesBody()
    {
        // Arrange
        var result = new JsonHttpResult<string>(value: "Hello", statusCode: 407, jsonSerializerOptions: null);
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
    public async Task JsonResult_ExecuteAsync_JsonSerializesBody_WitOptions()
    {
        // Arrange
        var jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        var value = new Todo(10, "MyName") { Description = null };
        var result = new JsonHttpResult<object>(value, jsonSerializerOptions: jsonOptions);
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
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);

        stream.Position = 0;
        var responseDetails = JsonSerializer.Deserialize<Todo>(stream);
        Assert.Equal(value.Id, responseDetails.Id);
        Assert.Equal(value.Title, responseDetails.Title);
        Assert.Equal(value.Description, responseDetails.Description);

        stream.Position = 0;
        Assert.Equal(JsonSerializer.Serialize(value, options: jsonOptions), Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task ExecuteAsync_UsesDefaults_ForProblemDetails()
    {
        // Arrange
        var details = new ProblemDetails();

        var result = new JsonHttpResult<ProblemDetails>(details, jsonSerializerOptions: null);
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

        var result = new JsonHttpResult<HttpValidationProblemDetails>(details, jsonSerializerOptions: null);
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
    public async Task ExecuteAsync_SetsProblemDetailsStatus_ForValidationProblemDetails()
    {
        // Arrange
        var details = new HttpValidationProblemDetails();

        var result = new JsonHttpResult<HttpValidationProblemDetails>(details, StatusCodes.Status422UnprocessableEntity, jsonSerializerOptions: null);
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, details.Status.Value);
    }

    [Fact]
    public async Task ExecuteAsync_GetsStatusCodeFromProblemDetails()
    {
        // Arrange
        var details = new ProblemDetails { Status = StatusCodes.Status413RequestEntityTooLarge, };

        var result = new JsonHttpResult<ProblemDetails>(details, jsonSerializerOptions: null);

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
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        return services.BuildServiceProvider();
    }

    private record Todo(int Id, string Title)
    {
        public string Description { get; init; }
    }
}
