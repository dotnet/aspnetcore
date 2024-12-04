// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http.Json;
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
    public async Task JsonResult_ExecuteAsync_JsonSerializesBody_WithOptions()
    {
        // Arrange
        var jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
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

        var result = new JsonHttpResult<ProblemDetails>(details, jsonSerializerOptions: JsonSerializerOptions.Web);
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
    public async Task ExecuteAsync_UsesDefaults_ForValidationProblemDetails()
    {
        // Arrange
        var details = new HttpValidationProblemDetails();

        var result = new JsonHttpResult<HttpValidationProblemDetails>(details, jsonSerializerOptions: JsonSerializerOptions.Web);
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
    public async Task ExecuteAsync_UsesDefaults_HttpStatusCodesWithoutTypes()
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
    public async Task ExecuteAsync_SetsProblemDetailsStatus_ForValidationProblemDetails()
    {
        // Arrange
        var details = new HttpValidationProblemDetails();

        var result = new JsonHttpResult<HttpValidationProblemDetails>(details, jsonSerializerOptions: null, StatusCodes.Status422UnprocessableEntity);
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

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new JsonHttpResult<object>(null, jsonSerializerOptions: null, null, null);
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void JsonResult_Implements_IContentTypeHttpResult_Correctly()
    {
        // Arrange
        var contentType = "application/json+custom";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IContentTypeHttpResult>(new JsonHttpResult<string>(null, jsonSerializerOptions: null, StatusCodes.Status200OK, contentType));
        Assert.Equal(contentType, result.ContentType);
    }

    [Fact]
    public void JsonResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Arrange
        var contentType = "application/json+custom";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new JsonHttpResult<string>(null, jsonSerializerOptions: null, StatusCodes.Status202Accepted, contentType));
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
    }

    [Fact]
    public void JsonResult_Implements_IStatusCodeHttpResult_Correctly_WithNullStatus()
    {
        // Arrange
        var contentType = "application/json+custom";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new JsonHttpResult<string>(null, jsonSerializerOptions: null, statusCode: null, contentType));
        Assert.Null(result.StatusCode);
    }

    [Fact]
    public void JsonResult_Implements_IValueHttpResult_Correctly()
    {
        // Arrange & Act
        var value = "Foo";
        var contentType = "application/json+custom";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult>(new JsonHttpResult<string>(value, jsonSerializerOptions: null, statusCode: null, contentType));
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void JsonResult_Implements_IValueHttpResultOfT_Correctly()
    {
        // Arrange
        var value = "Foo";
        var contentType = "application/json+custom";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult<string>>(new JsonHttpResult<string>(value, jsonSerializerOptions: null, statusCode: null, contentType));
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
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
