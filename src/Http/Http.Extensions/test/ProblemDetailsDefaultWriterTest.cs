// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public partial class DefaultProblemDetailsWriterTest
{
    private static readonly JsonSerializerOptions SerializerOptions = JsonOptions.DefaultSerializerOptions;

    [Fact]
    public async Task WriteAsync_Works()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream);

        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new ProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
        };
        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedProblem.Status, problemDetails.Status);
        Assert.Equal(expectedProblem.Type, problemDetails.Type);
        Assert.Equal(expectedProblem.Title, problemDetails.Title);
        Assert.Equal(expectedProblem.Detail, problemDetails.Detail);
        Assert.Equal(expectedProblem.Instance, problemDetails.Instance);
        Assert.Equal(expectedTraceId, problemDetails.Extensions["traceId"].ToString());
    }

    [Fact]
    public async Task WriteAsync_Works_ProperCasing()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new ProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
            Extensions = new Dictionary<string, object>() { { "extensionKey", 1 } }
        };
        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var result = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(stream, JsonSerializerOptions.Default);
        Assert.Equal(result.Keys, new(new() { { "type", 0 }, { "title", 1 }, { "status", 2 }, { "detail", 3 }, { "instance", 4 }, { "extensionKey", 5 }, { "traceId", expectedTraceId } }));
    }

    [Fact]
    public async Task WriteAsync_Works_ProperCasing_ValidationProblemDetails()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new ValidationProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
            Errors = new Dictionary<string, string[]>() { { "name", ["Name is invalid."] } }
        };
        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var result = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(stream, JsonSerializerOptions.Default);
        Assert.Equal(result.Keys, new(new() { { "type", 0 }, { "title", 1 }, { "status", 2 }, { "detail", 3 }, { "instance", 4 }, { "errors", 5 }, { "traceId", expectedTraceId } }));
    }

    [Fact]
    public async Task WriteAsync_Works_WhenReplacingProblemDetailsUsingSetter()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var originalProblemDetails = new ProblemDetails();

        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new ProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
        };
        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = originalProblemDetails
        };

        problemDetailsContext.ProblemDetails = expectedProblem;

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedProblem.Status, problemDetails.Status);
        Assert.Equal(expectedProblem.Type, problemDetails.Type);
        Assert.Equal(expectedProblem.Title, problemDetails.Title);
        Assert.Equal(expectedProblem.Detail, problemDetails.Detail);
        Assert.Equal(expectedProblem.Instance, problemDetails.Instance);
        Assert.Equal(expectedTraceId, problemDetails.Extensions["traceId"].ToString());
    }

    [Fact]
    public async Task WriteAsync_Works_WithJsonContext()
    {
        // Arrange
        var options = new JsonOptions();
        options.SerializerOptions.TypeInfoResolver = ProblemDetailsJsonContext.Default;

        var writer = GetWriter(jsonOptions: options);
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new ProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
        };
        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedProblem.Status, problemDetails.Status);
        Assert.Equal(expectedProblem.Type, problemDetails.Type);
        Assert.Equal(expectedProblem.Title, problemDetails.Title);
        Assert.Equal(expectedProblem.Detail, problemDetails.Detail);
        Assert.Equal(expectedProblem.Instance, problemDetails.Instance);
        Assert.Equal(expectedTraceId, problemDetails.Extensions["traceId"].ToString());
    }

    [Fact]
    public async Task WriteAsync_Works_WithMultipleJsonContext()
    {
        // Arrange
        var options = new JsonOptions();
        options.SerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(CustomProblemDetailsContext.Default, CustomProblemDetailsContext2.Default, ProblemDetailsJsonContext.Default);

        var writer = GetWriter(jsonOptions: options);
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new ProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
        };
        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedProblem.Status, problemDetails.Status);
        Assert.Equal(expectedProblem.Type, problemDetails.Type);
        Assert.Equal(expectedProblem.Title, problemDetails.Title);
        Assert.Equal(expectedProblem.Detail, problemDetails.Detail);
        Assert.Equal(expectedProblem.Instance, problemDetails.Instance);
        Assert.Equal(expectedTraceId, problemDetails.Extensions["traceId"].ToString());
    }

    [Fact]
    public async Task WriteAsync_Works_WithHttpValidationProblemDetails()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new HttpValidationProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
        };
        expectedProblem.Errors.Add("sample", new string[] { "error-message" });

        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<HttpValidationProblemDetails>(stream, SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedProblem.Status, problemDetails.Status);
        Assert.Equal(expectedProblem.Type, problemDetails.Type);
        Assert.Equal(expectedProblem.Title, problemDetails.Title);
        Assert.Equal(expectedProblem.Detail, problemDetails.Detail);
        Assert.Equal(expectedProblem.Instance, problemDetails.Instance);
        Assert.Equal(expectedProblem.Errors, problemDetails.Errors);
        Assert.Equal(expectedTraceId, problemDetails.Extensions["traceId"].ToString());
    }

    [Fact]
    public async Task WriteAsync_Works_WithHttpValidationProblemDetails_AndJsonContext()
    {
        // Arrange
        var options = new JsonOptions();
        options.SerializerOptions.TypeInfoResolver = ProblemDetailsJsonContext.Default;

        var writer = GetWriter(jsonOptions: options);
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new HttpValidationProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
        };
        expectedProblem.Errors.Add("sample", new string[] { "error-message" });

        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<HttpValidationProblemDetails>(stream, SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedProblem.Status, problemDetails.Status);
        Assert.Equal(expectedProblem.Type, problemDetails.Type);
        Assert.Equal(expectedProblem.Title, problemDetails.Title);
        Assert.Equal(expectedProblem.Detail, problemDetails.Detail);
        Assert.Equal(expectedProblem.Instance, problemDetails.Instance);
        Assert.Equal(expectedProblem.Errors, problemDetails.Errors);
        Assert.Equal(expectedTraceId, problemDetails.Extensions["traceId"].ToString());
    }

    [Fact]
    public async Task WriteAsync_Works_WithCustomDerivedProblemDetails()
    {
        // Arrange
        var options = new JsonOptions();
        options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

        var writer = GetWriter(jsonOptions: options);
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new CustomProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
            ExtraProperty = "My Extra property"
        };

        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<CustomProblemDetails>(stream, options.SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedProblem.Status, problemDetails.Status);
        Assert.Equal(expectedProblem.Type, problemDetails.Type);
        Assert.Equal(expectedProblem.Title, problemDetails.Title);
        Assert.Equal(expectedProblem.Detail, problemDetails.Detail);
        Assert.Equal(expectedProblem.Instance, problemDetails.Instance);
        Assert.Equal(expectedProblem.ExtraProperty, problemDetails.ExtraProperty);
        Assert.Equal(expectedTraceId, problemDetails.Extensions["traceId"].ToString());
    }

    [Fact]
    public async Task WriteAsync_Works_WithCustomDerivedProblemDetails_AndJsonContext()
    {
        // Arrange
        var options = new JsonOptions();
        options.SerializerOptions.TypeInfoResolver = CustomProblemDetailsContext.Default;

        var writer = GetWriter(jsonOptions: options);
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new CustomProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
            ExtraProperty = "My Extra property"
        };

        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<CustomProblemDetails>(stream, options.SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedProblem.Status, problemDetails.Status);
        Assert.Equal(expectedProblem.Type, problemDetails.Type);
        Assert.Equal(expectedProblem.Title, problemDetails.Title);
        Assert.Equal(expectedProblem.Detail, problemDetails.Detail);
        Assert.Equal(expectedProblem.Instance, problemDetails.Instance);
        Assert.Equal(expectedProblem.ExtraProperty, problemDetails.ExtraProperty);
        Assert.Equal(expectedTraceId, problemDetails.Extensions["traceId"].ToString());
    }

    [Fact]
    public async Task WriteAsync_Works_WithCustomDerivedProblemDetails_AndMultipleJsonContext()
    {
        // Arrange
        var options = new JsonOptions();
        options.SerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(CustomProblemDetailsContext.Default, ProblemDetailsJsonContext.Default);

        var writer = GetWriter(jsonOptions: options);
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new CustomProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
            ExtraProperty = "My Extra property"
        };

        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<CustomProblemDetails>(stream, options.SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedProblem.Status, problemDetails.Status);
        Assert.Equal(expectedProblem.Type, problemDetails.Type);
        Assert.Equal(expectedProblem.Title, problemDetails.Title);
        Assert.Equal(expectedProblem.Detail, problemDetails.Detail);
        Assert.Equal(expectedProblem.Instance, problemDetails.Instance);
        Assert.Equal(expectedProblem.ExtraProperty, problemDetails.ExtraProperty);
        Assert.Equal(expectedTraceId, problemDetails.Extensions["traceId"].ToString());
    }

    [Fact]
    public async Task WriteAsync_AddExtensions()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedProblem = new ProblemDetails();
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        expectedProblem.Extensions["Extension1"] = "Extension1-Value";
        expectedProblem.Extensions["Extension2"] = "Extension2-Value";

        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Collection(problemDetails.Extensions,
            (extension) =>
            {
                Assert.Equal("Extension1", extension.Key);
                Assert.Equal("Extension1-Value", extension.Value.ToString());
            },
            (extension) =>
            {
                Assert.Equal("Extension2", extension.Key);
                Assert.Equal("Extension2-Value", extension.Value.ToString());
            },
            (extension) =>
            {
                Assert.Equal("traceId", extension.Key);
                Assert.Equal(expectedTraceId, extension.Value.ToString());
            });
    }

    [Fact]
    public async Task WriteAsync_AddExtensions_WithJsonContext()
    {
        // Arrange
        var options = new JsonOptions();
        options.SerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(CustomProblemDetailsContext.Default, ProblemDetailsJsonContext.Default);

        var writer = GetWriter(jsonOptions: options);
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedProblem = new ProblemDetails();
        var customExtensionData = new CustomExtensionData("test");
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        expectedProblem.Extensions["Extension"] = customExtensionData;

        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;

        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, options.SerializerOptions);
        Assert.NotNull(problemDetails);

        Assert.Collection(problemDetails.Extensions,
            (extension) =>
            {
                Assert.Equal("Extension", extension.Key);
                var expectedExtension = JsonSerializer.SerializeToElement(customExtensionData, options.SerializerOptions);
                var value = Assert.IsType<JsonElement>(extension.Value);

                Assert.Equal(expectedExtension.GetProperty("data").GetString(), value.GetProperty("data").GetString());
            },
            (extension) =>
            {
                Assert.Equal("traceId", extension.Key);
                Assert.Equal(expectedTraceId, extension.Value.ToString());
            });
    }

    [Fact]
    public async Task WriteAsync_Applies_Defaults()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream, StatusCodes.Status500InternalServerError);
        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;

        //Act
        await writer.WriteAsync(new ProblemDetailsContext() { HttpContext = context });

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.6.1", problemDetails.Type);
        Assert.Equal("An error occurred while processing your request.", problemDetails.Title);
        Assert.Equal(expectedTraceId, problemDetails.Extensions["traceId"].ToString());
    }

    [Fact]
    public async Task WriteAsync_Applies_CustomConfiguration()
    {
        // Arrange
        const string expectedTraceId = "new-traceId-Value";
        var options = new ProblemDetailsOptions()
        {
            CustomizeProblemDetails = (context) =>
            {
                context.ProblemDetails.Status = StatusCodes.Status406NotAcceptable;
                context.ProblemDetails.Title = "Custom Title";
                context.ProblemDetails.Extensions["new-extension"] = new { TraceId = Guid.NewGuid() };
                context.ProblemDetails.Extensions["traceId"] = expectedTraceId;
            }
        };
        var writer = GetWriter(options);
        var stream = new MemoryStream();
        var context = CreateContext(stream, StatusCodes.Status500InternalServerError);

        //Act
        await writer.WriteAsync(new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = { Status = StatusCodes.Status400BadRequest }
        });

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status406NotAcceptable, problemDetails.Status);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", problemDetails.Type);
        Assert.Equal("Custom Title", problemDetails.Title);
        Assert.Contains("new-extension", problemDetails.Extensions);
        Assert.Equal(expectedTraceId, problemDetails.Extensions["traceId"].ToString());
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest, "Bad Request", "https://tools.ietf.org/html/rfc9110#section-15.5.1")]
    [InlineData(StatusCodes.Status418ImATeapot, "I'm a teapot", null)]
    [InlineData(498, null, null)]
    public async Task WriteAsync_UsesStatusCode_FromProblemDetails_WhenSpecified(
        int statusCode,
        string title,
        string type)
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream, StatusCodes.Status500InternalServerError);

        //Act
        await writer.WriteAsync(new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = { Status = statusCode }
        });

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, SerializerOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(statusCode, problemDetails.Status);
        Assert.Equal(type, problemDetails.Type);
        Assert.Equal(title, problemDetails.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("*/*")]
    [InlineData("application/*")]
    [InlineData("application/json")]
    [InlineData("application/problem+json")]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("application/json; v=1.0")]
    public void CanWrite_ReturnsTrue_WhenJsonAccepted(string contentType)
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream, contentType: contentType);

        //Act
        var result = writer.CanWrite(new() { HttpContext = context });

        //Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("application/xml")]
    [InlineData("application/problem+xml")]
    public void CanWrite_ReturnsFalse_WhenJsonNotAccepted(string contentType)
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream, contentType: contentType);

        //Act
        var result = writer.CanWrite(new() { HttpContext = context });

        //Assert
        Assert.False(result);
    }

    private static HttpContext CreateContext(
        Stream body,
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/json")
    {
        var context = new DefaultHttpContext()
        {
            Response = { Body = body, StatusCode = statusCode },
            RequestServices = CreateServices()
        };

        if (!string.IsNullOrEmpty(contentType))
        {
            context.Request.Headers.Accept = contentType;
        }

        return context;
    }

    [Theory]
    [InlineData("SnakeCaseLower", "trace_id")]
    [InlineData("CamelCase", "traceId")]
    [InlineData("KebabCaseLower", "trace-id")]
    [InlineData("KebabCaseUpper", "TRACE-ID")]
    [InlineData("SnakeCaseUpper", "TRACE_ID")]
    public async Task TestPropertyNamingPolicyChanges(string caseSelection, string extensionVariableName)
    {
        // Arrange
        JsonNamingPolicy propertyNamingPolicy = caseSelection switch
        {
            "CamelCase" => JsonNamingPolicy.CamelCase,
            "KebabCaseLower" => JsonNamingPolicy.KebabCaseLower,
            "KebabCaseUpper" => JsonNamingPolicy.KebabCaseUpper,
            "SnakeCaseLower" => JsonNamingPolicy.SnakeCaseLower,
            "SnakeCaseUpper" => JsonNamingPolicy.SnakeCaseUpper,
            _ => JsonNamingPolicy.KebabCaseLower
        };

        var options = new JsonOptions();
        options.SerializerOptions.PropertyNamingPolicy = propertyNamingPolicy;

        var writer = GetWriter(jsonOptions: options);
        var stream = new MemoryStream();
        var context = CreateContext(stream);

        var expectedTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var expectedProblem = new ProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
        };
        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        //Assert
        Assert.Contains($"\"{extensionVariableName}\":\"{expectedTraceId}\"", json);
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        return services.BuildServiceProvider();
    }

    private static DefaultProblemDetailsWriter GetWriter(ProblemDetailsOptions options = null, JsonOptions jsonOptions = null)
    {
        options ??= new ProblemDetailsOptions();
        jsonOptions ??= new JsonOptions();

        return new DefaultProblemDetailsWriter(Options.Create(options), Options.Create(jsonOptions));
    }

    internal class CustomProblemDetails : ProblemDetails
    {
        public string ExtraProperty { get; set; }
    }

    [JsonSerializable(typeof(CustomProblemDetails))]
    [JsonSerializable(typeof(CustomExtensionData))]
    internal partial class CustomProblemDetailsContext : JsonSerializerContext
    { }

    [JsonSerializable(typeof(CustomProblemDetails))]
    internal partial class CustomProblemDetailsContext2 : JsonSerializerContext
    { }

    internal record CustomExtensionData(string Data);
}
