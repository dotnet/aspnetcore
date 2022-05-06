// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class NotFoundResultTests
{
    [Fact]
    public void NotFoundObjectResult_InitializesStatusCode()
    {
        // Arrange & act
        var notFound = new NotFound();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task NotFoundObjectResult_ExecuteSuccessful()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var result = new NotFound();

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        NotFound MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var context = new EndpointMetadataContext(((Delegate)MyApi).GetMethodInfo(), metadata, null);

        // Act
        PopulateMetadata<NotFound>(context);

        // Assert
        var producesResponseTypeMetadata = context.EndpointMetadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status404NotFound, producesResponseTypeMetadata.StatusCode);
    }

    [Fact]
    public void ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new NotFound();
        HttpContext httpContext = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenContextIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("context", () => PopulateMetadata<NotFound>(null));
    }

    private static void PopulateMetadata<TResult>(EndpointMetadataContext context)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(context);

    private static HttpContext GetHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = new PathString("");
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = CreateServices();
        return httpContext;
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        return services.BuildServiceProvider();
    }
}
