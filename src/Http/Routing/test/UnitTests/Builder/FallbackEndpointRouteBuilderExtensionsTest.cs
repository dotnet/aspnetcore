// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

public class FallbackEndpointRouteBuilderExtensionsTest
{
    private EndpointDataSource GetBuilderEndpointDataSource(IEndpointRouteBuilder endpointRouteBuilder) =>
        Assert.Single(endpointRouteBuilder.DataSources);

    [Fact]
    public void MapFallback_AddFallbackMetadata()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        RequestDelegate initialRequestDelegate = static (context) => Task.CompletedTask;

        builder.MapFallback(initialRequestDelegate);

        var dataSource = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        Assert.Contains(FallbackMetadata.Instance, endpoint.Metadata);
        Assert.Equal(int.MaxValue, ((RouteEndpoint)endpoint).Order);
    }

    [Fact]
    public void MapFallback_Pattern_AddFallbackMetadata()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        RequestDelegate initialRequestDelegate = static (context) => Task.CompletedTask;

        builder.MapFallback("/", initialRequestDelegate);

        var dataSource = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        Assert.Contains(FallbackMetadata.Instance, endpoint.Metadata);
        Assert.Equal(int.MaxValue, ((RouteEndpoint)endpoint).Order);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}
