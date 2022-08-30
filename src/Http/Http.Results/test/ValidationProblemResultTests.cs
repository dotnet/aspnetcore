// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
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
        var responseDetails = JsonSerializer.Deserialize<ProblemDetails>(stream);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", responseDetails.Type);
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
        var context = new EndpointMetadataContext(((Delegate)MyApi).GetMethodInfo(), metadata, EmptyServiceProvider.Instance);

        // Act
        PopulateMetadata<ValidationProblem>(context);

        // Assert
        var producesResponseTypeMetadata = context.EndpointMetadata.OfType<ProducesResponseTypeMetadata>().Last();
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
    public void PopulateMetadata_ThrowsArgumentNullException_WhenContextIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("context", () => PopulateMetadata<ValidationProblem>(null));
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

    private static void PopulateMetadata<TResult>(EndpointMetadataContext context)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(context);

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        return services.BuildServiceProvider();
    }
}
