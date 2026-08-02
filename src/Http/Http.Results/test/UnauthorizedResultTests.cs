// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class UnauthorizedResultTests
{
    [Fact]
    public void UnauthorizedResult_InitializesStatusCode()
    {
        // Arrange & act
        var result = new UnauthorizedHttpResult();

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public void UnauthorizedResult_ExecuteResultSetsResponseStatusCode()
    {
        // Arrange
        var result = new UnauthorizedHttpResult();

        var httpContext = GetHttpContext();

        // Act
        result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new UnauthorizedHttpResult();
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void UnauthorizedResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new UnauthorizedHttpResult());
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        static UnauthorizedHttpResult MyApi() { throw new NotImplementedException(); }
        var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0);

        // Act
        PopulateMetadata<UnauthorizedHttpResult>(((Delegate)MyApi).GetMethodInfo(), builder);

        // Assert
        var producesResponseTypeMetadata = builder.Metadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status401Unauthorized, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(void), producesResponseTypeMetadata.Type);
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("method", () => PopulateMetadata<UnauthorizedHttpResult>(null, new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0)));
        Assert.Throws<ArgumentNullException>("builder", () => PopulateMetadata<UnauthorizedHttpResult>(((Delegate)PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull).GetMethodInfo(), null));
    }

    private static void PopulateMetadata<TResult>(MethodInfo method, EndpointBuilder builder)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(method, builder);

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services;
    }

    private static HttpContext GetHttpContext()
    {
        var services = CreateServices();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }
}
