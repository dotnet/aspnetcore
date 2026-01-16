// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.HttpResults;

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class ConflictOfTResultTests
{
    [Fact]
    public void ConflictObjectResult_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new object();
        var conflictObjectResult = new Conflict<object>(obj);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, conflictObjectResult.StatusCode);
        Assert.Equal(obj, conflictObjectResult.Value);
    }

    [Fact]
    public void ConflictObjectResult_ProblemDetails_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new ProblemDetails();
        var conflictObjectResult = new Conflict<ProblemDetails>(obj);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, conflictObjectResult.StatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, obj.Status);
        Assert.Equal(obj, conflictObjectResult.Value);
    }

    [Fact]
    public async Task ConflictObjectResult_ExecuteAsync_SetsStatusCode()
    {
        // Arrange
        var result = new Conflict<string>("Hello");
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
        var result = new Conflict<string>("Hello");
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
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        Conflict<Todo> MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0);

        // Act
        PopulateMetadata<Conflict<Todo>>(((Delegate)MyApi).GetMethodInfo(), builder);

        // Assert
        var producesResponseTypeMetadata = builder.Metadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status409Conflict, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(Todo), producesResponseTypeMetadata.Type);
        Assert.Single(producesResponseTypeMetadata.ContentTypes, "application/json");

        Assert.Contains(builder.Metadata, m => m is IDisableCookieRedirectMetadata);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new Conflict<object>(null);
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("method", () => PopulateMetadata<Conflict<object>>(null, new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0)));
        Assert.Throws<ArgumentNullException>("builder", () => PopulateMetadata<Conflict<object>>(((Delegate)PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull).GetMethodInfo(), null));
    }

    [Fact]
    public void ConflictObjectResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new Conflict<string>(null));
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
    }

    [Fact]
    public void ConflictObjectResult_Implements_IValueHttpResult_Correctly()
    {
        // Arrange
        var value = "Foo";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult>(new Conflict<string>(value));
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void ConflictObjectResult_Implements_IValueHttpResultOfT_Correctly()
    {
        // Arrange & Act
        var value = "Foo";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult<string>>(new Conflict<string>(value));
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public async Task ConflictObjectResult_WithProblemDetails_UsesDefaultsFromProblemDetailsService()
    {
        // Arrange
        var details = new ProblemDetails();
        var result = new Conflict<ProblemDetails>(details);
        var stream = new MemoryStream();
        var services = CreateServiceCollection()
            .AddProblemDetails(options => options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["customProperty"] = "customValue")
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
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        stream.Position = 0;
        var responseDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(stream, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(responseDetails);
        Assert.Equal(StatusCodes.Status409Conflict, responseDetails.Status);
        Assert.True(responseDetails.Extensions.ContainsKey("customProperty"));
        Assert.Equal("customValue", responseDetails.Extensions["customProperty"]?.ToString());
    }

    [Fact]
    public async Task ConflictObjectResult_WithProblemDetails_AppliesTraceIdFromService()
    {
        // Arrange
        var details = new ProblemDetails();
        var result = new Conflict<ProblemDetails>(details);
        var stream = new MemoryStream();
        var services = CreateServiceCollection()
            .AddProblemDetails()
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
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        stream.Position = 0;
        var responseDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(stream, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(responseDetails);
        Assert.True(responseDetails.Extensions.ContainsKey("traceId"));
        Assert.NotNull(responseDetails.Extensions["traceId"]);
    }

    [Fact]
    public async Task ConflictObjectResult_WithProblemDetails_FallsBackWhenServiceNotRegistered()
    {
        // Arrange
        var details = new ProblemDetails { Title = "Test Error" };
        var result = new Conflict<ProblemDetails>(details);
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
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        stream.Position = 0;
        var responseDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(stream, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(responseDetails);
        Assert.Equal("Test Error", responseDetails.Title);
        Assert.Equal(StatusCodes.Status409Conflict, responseDetails.Status);
    }

    [Fact]
    public async Task ConflictObjectResult_WithHttpValidationProblemDetails_UsesDefaultsFromProblemDetailsService()
    {
        // Arrange
        var details = new HttpValidationProblemDetails();
        var result = new Conflict<HttpValidationProblemDetails>(details);
        var stream = new MemoryStream();
        var services = CreateServiceCollection()
            .AddProblemDetails(options => options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["customValidation"] = "applied")
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
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        stream.Position = 0;
        var responseDetails = System.Text.Json.JsonSerializer.Deserialize<HttpValidationProblemDetails>(stream, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(responseDetails);
        Assert.True(responseDetails.Extensions.ContainsKey("customValidation"));
        Assert.Equal("applied", responseDetails.Extensions["customValidation"]?.ToString());
    }

    [Fact]
    public async Task ConflictObjectResult_WithNonProblemDetails_DoesNotUseProblemDetailsService()
    {
        // Arrange
        var details = new { error = "test error" };
        var result = new Conflict<object>(details);
        var stream = new MemoryStream();
        var customizationCalled = false;
        var services = CreateServiceCollection()
            .AddProblemDetails(options => options.CustomizeProblemDetails = context => customizationCalled = true)
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
        Assert.False(customizationCalled, "CustomizeProblemDetails should not be called for non-ProblemDetails types");
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
    }

    private static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddTransient(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }

    private static void PopulateMetadata<TResult>(MethodInfo method, EndpointBuilder builder)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(method, builder);

    private record Todo(int Id, string Title);

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        return services.BuildServiceProvider();
    }
}
