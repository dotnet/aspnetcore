// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.HttpResults;

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class ConflictResultTests
{
    [Fact]
    public void ConflictObjectResult_SetsStatusCode()
    {
        // Arrange & Act
        var conflictObjectResult = new Conflict();

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, conflictObjectResult.StatusCode);
    }

    [Fact]
    public async Task ConflictObjectResult_ExecuteAsync_SetsStatusCode()
    {
        // Arrange
        var result = new Conflict();
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
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        Conflict MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0);

        // Act
        PopulateMetadata<Conflict>(((Delegate)MyApi).GetMethodInfo(), builder);

        // Assert
        var producesResponseTypeMetadata = builder.Metadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status409Conflict, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(void), producesResponseTypeMetadata.Type);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new Conflict();
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("method", () => PopulateMetadata<Conflict>(null, new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0)));
        Assert.Throws<ArgumentNullException>("builder", () => PopulateMetadata<Conflict>(((Delegate)PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull).GetMethodInfo(), null));
    }

    [Fact]
    public void ConflictObjectResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new Conflict());
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
    }

    private static void PopulateMetadata<TResult>(MethodInfo method, EndpointBuilder builder)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(method, builder);

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        return services.BuildServiceProvider();
    }
}
