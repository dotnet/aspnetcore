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
using Newtonsoft.Json.Linq;

public class BadRequestOfTResultTests
{
    [Fact]
    public void BadRequestObjectResult_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new object();
        var badRequestObjectResult = new BadRequest<object>(obj);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestObjectResult.StatusCode);
        Assert.Equal(obj, badRequestObjectResult.Value);
    }

    [Fact]
    public void BadRequestObjectResult_ProblemDetails_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new HttpValidationProblemDetails();
        var result = new BadRequest<HttpValidationProblemDetails>(obj);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, obj.Status);
        Assert.Equal(obj, result.Value);
    }

    [Fact]
    public async Task BadRequestObjectResult_ExecuteAsync_SetsStatusCode()
    {
        // Arrange
        var result = new BadRequest<string>("Hello");
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task BadRequestObjectResult_ExecuteResultAsync_FormatsData()
    {
        // Arrange
        var result = new BadRequest<string>("Hello");
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
    public async Task BadRequestObjectResult_ExecuteResultAsync_UsesStatusCodeFromResultTypeForProblemDetails()
    {
        // Arrange
        var details = new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, };
        var result = new BadRequest<ProblemDetails>(details);

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, details.Status.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        BadRequest<Todo> MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0);

        // Act
        PopulateMetadata<BadRequest<Todo>>(((Delegate)MyApi).GetMethodInfo(), builder);

        // Assert
        var producesResponseTypeMetadata = builder.Metadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status400BadRequest, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(Todo), producesResponseTypeMetadata.Type);
        Assert.Single(producesResponseTypeMetadata.ContentTypes, "application/json");

        Assert.Contains(builder.Metadata, m => m is IDisableCookieRedirectMetadata);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new BadRequest<object>(null);
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("method", () => PopulateMetadata<BadRequest<object>>(null, new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0)));
        Assert.Throws<ArgumentNullException>("builder", () => PopulateMetadata<BadRequest<object>>(((Delegate)PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull).GetMethodInfo(), null));
    }

    [Fact]
    public void BadRequestObjectResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new BadRequest<string>(null));
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public void BadRequestObjectResult_Implements_IValueHttpResult_Correctly()
    {
        // Arrange
        var value = "Foo";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult>(new BadRequest<string>(value));
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void BadRequestObjectResult_Implements_IValueHttpResultOfT_Correctly()
    {
        // Arrange & Act
        var value = "Foo";

        // Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult<string>>(new BadRequest<string>(value));
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public async Task BadRequestObjectResult_WithProblemDetails_UsesDefaultsFromProblemDetailsService()
    {
        // Arrange
        var details = new ProblemDetails();
        var result = new BadRequest<ProblemDetails>(details);
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
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        stream.Position = 0;
        var responseDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(stream, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(responseDetails);
        Assert.Equal(StatusCodes.Status400BadRequest, responseDetails.Status);
        Assert.True(responseDetails.Extensions.ContainsKey("customProperty"));
        Assert.Equal("customValue", responseDetails.Extensions["customProperty"]?.ToString());
    }

    [Fact]
    public async Task BadRequestObjectResult_WithProblemDetails_AppliesTraceIdFromService()
    {
        // Arrange
        var details = new ProblemDetails();
        var result = new BadRequest<ProblemDetails>(details);
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
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        stream.Position = 0;
        var responseDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(stream, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(responseDetails);
        Assert.True(responseDetails.Extensions.ContainsKey("traceId"));
        Assert.NotNull(responseDetails.Extensions["traceId"]);
    }

    [Fact]
    public async Task BadRequestObjectResult_WithProblemDetails_FallsBackWhenServiceNotRegistered()
    {
        // Arrange
        var details = new ProblemDetails { Title = "Test Error" };
        var result = new BadRequest<ProblemDetails>(details);
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
        var responseDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(stream, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(responseDetails);
        Assert.Equal("Test Error", responseDetails.Title);
        Assert.Equal(StatusCodes.Status400BadRequest, responseDetails.Status);
    }

    [Fact]
    public async Task BadRequestObjectResult_WithHttpValidationProblemDetails_UsesDefaultsFromProblemDetailsService()
    {
        // Arrange
        var details = new HttpValidationProblemDetails();
        var result = new BadRequest<HttpValidationProblemDetails>(details);
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
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        stream.Position = 0;
        var responseDetails = System.Text.Json.JsonSerializer.Deserialize<HttpValidationProblemDetails>(stream, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(responseDetails);
        Assert.True(responseDetails.Extensions.ContainsKey("customValidation"));
        Assert.Equal("applied", responseDetails.Extensions["customValidation"]?.ToString());
    }

    [Fact]
    public async Task BadRequestObjectResult_WithNonProblemDetails_DoesNotUseProblemDetailsService()
    {
        // Arrange
        var details = new { error = "test error" };
        var result = new BadRequest<object>(details);
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
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
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
