// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.HttpResults;

using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class NoContentResultTests
{
    [Fact]
    public void NoContentResultTests_InitializesStatusCode()
    {
        // Arrange & act
        var result = new NoContent();

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, result.StatusCode);
    }

    [Fact]
    public void NoContentResultTests_ExecuteResultSetsResponseStatusCode()
    {
        // Arrange
        var result = new NoContent();

        var httpContext = GetHttpContext();

        // Act
        result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, httpContext.Response.StatusCode);
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        NoContent MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var context = new EndpointMetadataContext(((Delegate)MyApi).GetMethodInfo(), metadata, null);

        // Act
        PopulateMetadata<NoContent>(context);

        // Assert
        var producesResponseTypeMetadata = context.EndpointMetadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status204NoContent, producesResponseTypeMetadata.StatusCode);
    }

    private static void PopulateMetadata<TResult>(EndpointMetadataContext context)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(context);

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
