// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
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
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", responseDetails.Type);
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
        var context = new EndpointMetadataContext(((Delegate)MyApi).GetMethodInfo(), metadata, null);

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
