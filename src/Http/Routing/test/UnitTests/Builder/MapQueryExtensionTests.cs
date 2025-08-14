// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Builder;

public class MapQueryExtensionTests
{
    private EndpointDataSource GetBuilderEndpointDataSource(IEndpointRouteBuilder endpointRouteBuilder) =>
        Assert.Single(endpointRouteBuilder.DataSources);

    [Fact]
    public void MapQuery_Delegate_CreatesEndpointWithCorrectMethod()
    {
        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        Func<string> handler = () => "Hello World";

        // Act
        var app = builder.MapQuery("/", handler);

        // Assert
        var dataSource = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
        Assert.Equal("/", routeEndpoint.RoutePattern.RawText);

        var httpMethodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(httpMethodMetadata);
        Assert.Contains("QUERY", httpMethodMetadata.HttpMethods);
    }

    [Fact]
    public void MapQuery_RequestDelegate_CreatesEndpointWithCorrectMethod()
    {
        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        RequestDelegate handler = context => Task.CompletedTask;

        // Act
        var app = builder.MapQuery("/", handler);

        // Assert
        var dataSource = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
        Assert.Equal("/", routeEndpoint.RoutePattern.RawText);

        var httpMethodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(httpMethodMetadata);
        Assert.Contains("QUERY", httpMethodMetadata.HttpMethods);
    }

    // Helper class
    private class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object GetService(Type serviceType) => null;
    }
}