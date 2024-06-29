// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class ValidationProblemResultTests
{
    [Fact]
    public async Task ExecuteAsync_UsesDefaults_ForProblemDetails()
    {
        // Arrange
        var details = new HttpValidationProblemDetails();
        var result = new ValidationProblem(details);
        var stream = new MemoryStream();
        var httpContext = new DefaultHttpContext
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
        Assert.Equal(details, result.ProblemDetails);
        stream.Position = 0;
        var responseDetails = JsonSerializer.Deserialize<ProblemDetails>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", responseDetails.Type);
        Assert.Equal("One or more validation errors occurred.", responseDetails.Title);
        Assert.Equal(StatusCodes.Status400BadRequest, responseDetails.Status);
    }

    [Fact]
    public async Task ExecuteAsync_UsesDefaultsFromProblemDetailsService_ForProblemDetails()
    {
        // Arrange
        var details = new HttpValidationProblemDetails();

        var result = new ValidationProblem(details);
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
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        stream.Position = 0;
        var responseDetails = JsonSerializer.Deserialize<ProblemDetails>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Null(responseDetails.Type);
        Assert.Equal("One or more validation errors occurred.", responseDetails.Title);
        Assert.Equal(StatusCodes.Status400BadRequest, responseDetails.Status);
    }

    [Fact]
    public void ExecuteAsync_ThrowsArgumentNullException_ForNullProblemDetails()
    {
        Assert.Throws<ArgumentNullException>("problemDetails", () => new ValidationProblem(null));
    }

    [Fact]
    public void ExecuteAsync_ThrowsArgumentException_ForNon400StatusCodeFromProblemDetails()
    {
        Assert.Throws<ArgumentException>("problemDetails", () => new ValidationProblem(
            new HttpValidationProblemDetails { Status = StatusCodes.Status413RequestEntityTooLarge, }));
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        ValidationProblem MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0);

        // Act
        PopulateMetadata<ValidationProblem>(((Delegate)MyApi).GetMethodInfo(), builder);

        // Assert
        var producesResponseTypeMetadata = builder.Metadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status400BadRequest, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(HttpValidationProblemDetails), producesResponseTypeMetadata.Type);
        Assert.Single(producesResponseTypeMetadata.ContentTypes, "application/problem+json");
    }

    [Fact]
    public void ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new ValidationProblem(new());
        HttpContext httpContext = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("method", () => PopulateMetadata<ValidationProblem>(null, new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0)));
        Assert.Throws<ArgumentNullException>("builder", () => PopulateMetadata<ValidationProblem>(((Delegate)PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull).GetMethodInfo(), null));
    }

    [Fact]
    public void ValidationProblemResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new ValidationProblem(new HttpValidationProblemDetails()));
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public void ValidationProblemResult_Implements_IValueHttpResult_Correctly()
    {
        // Arrange
        var value = new HttpValidationProblemDetails();

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult>(new ValidationProblem(value));
        Assert.IsType<HttpValidationProblemDetails>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void ValidationProblemResult_Implements_IValueHttpResultOfT_Correctly()
    {
        // Arrange
        var value = new HttpValidationProblemDetails();

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult<HttpValidationProblemDetails>>(new ValidationProblem(value));
        Assert.IsType<HttpValidationProblemDetails>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void ValidationProblemResult_Implements_IContentTypeHttpResult_Correctly()
    {
        // Arrange
        var contentType = "application/problem+json";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IContentTypeHttpResult>(new ValidationProblem(new()));
        Assert.Equal(contentType, result.ContentType);
    }

    private static void PopulateMetadata<TResult>(MethodInfo method, EndpointBuilder builder)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(method, builder);

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
