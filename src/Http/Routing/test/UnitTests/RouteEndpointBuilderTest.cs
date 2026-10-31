// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing;

public class RouteEndpointBuilderTest
{
    [Fact]
    public void Constructor_AllowsNullRequestDelegate()
    {
        var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0);
        Assert.Null(builder.RequestDelegate);
    }

    [Fact]
    public void Constructor_DoesNotAllowNullRoutePattern()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new RouteEndpointBuilder(context => Task.CompletedTask, routePattern: null, order: 0));
        Assert.Equal("routePattern", ex.ParamName);
    }

    [Fact]
    public void Build_AllValuesSet_EndpointCreated()
    {
        const int defaultOrder = 0;
        var metadata = new object();
        RequestDelegate requestDelegate = (d) => null;

        var builder = new RouteEndpointBuilder(requestDelegate, RoutePatternFactory.Parse("/"), defaultOrder)
        {
            DisplayName = "Display name!",
            Metadata = { metadata }
        };

        var endpoint = Assert.IsType<RouteEndpoint>(builder.Build());
        Assert.Equal("Display name!", endpoint.DisplayName);
        Assert.Equal(defaultOrder, endpoint.Order);
        Assert.Equal(requestDelegate, endpoint.RequestDelegate);
        Assert.Equal("/", endpoint.RoutePattern.RawText);
        Assert.Collection(endpoint.Metadata,
            m => Assert.Same(metadata, m),
            m => Assert.IsAssignableFrom<IRouteDiagnosticsMetadata>(m));
    }

    [Fact]
    public void Build_CustomRouteDiagnosticsMetadata_EndpointCreated()
    {
        const int defaultOrder = 0;
        var metadata = new object();
        RequestDelegate requestDelegate = (d) => null;

        var builder = new RouteEndpointBuilder(requestDelegate, RoutePatternFactory.Parse("/"), defaultOrder)
        {
            Metadata = { new TestRouteDiaganosticsMetadata { Route = "Test" }, metadata }
        };

        var endpoint = Assert.IsType<RouteEndpoint>(builder.Build());
        Assert.Equal("/", endpoint.RoutePattern.RawText);
        Assert.Collection(endpoint.Metadata,
            m =>
            {
                var metadata = Assert.IsAssignableFrom<IRouteDiagnosticsMetadata>(m);
                Assert.Equal("Test", metadata.Route);
            },
            m => Assert.Equal(metadata, m));
    }

    private sealed class TestRouteDiaganosticsMetadata : IRouteDiagnosticsMetadata
    {
        public string Route { get; init; }
    }

    [Fact]
    public void Build_UpdateHttpMethodMetadata_WhenCorsPresent()
    {
        // Arrange
        const int defaultOrder = 0;
        static Task RequestDelegate(HttpContext d) => null;

        var builder = new RouteEndpointBuilder(RequestDelegate, RoutePatternFactory.Parse("/"), defaultOrder)
        {
            DisplayName = "Display name!",
            Metadata = { new TestCorsMetadata(), new HttpMethodMetadata(new[] { HttpMethods.Delete }, acceptCorsPreflight: false) }
        };

        // Act && Assert
        var endpoint = Assert.IsType<RouteEndpoint>(builder.Build());
        var httpMethodMetadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
        Assert.NotNull(httpMethodMetadata);
        Assert.True(httpMethodMetadata.AcceptCorsPreflight);
    }

    [Fact]
    public void Build_UpdateLastHttpMethodMetadata_WhenCorsPresent()
    {
        // Arrange
        const int defaultOrder = 0;
        static Task RequestDelegate(HttpContext d) => null;

        var builder = new RouteEndpointBuilder(RequestDelegate, RoutePatternFactory.Parse("/"), defaultOrder)
        {
            DisplayName = "Display name!",
            Metadata = { new HttpMethodMetadata(new[] { HttpMethods.Get }, acceptCorsPreflight: false), new TestCorsMetadata(), new HttpMethodMetadata(new[] { HttpMethods.Delete }, acceptCorsPreflight: false) }
        };

        // Act && Assert
        var endpoint = Assert.IsType<RouteEndpoint>(builder.Build());

        Assert.Collection(endpoint.Metadata.GetOrderedMetadata<HttpMethodMetadata>(),
            (metadata) => Assert.False(metadata.AcceptCorsPreflight),
            (metadata) => Assert.True(metadata.AcceptCorsPreflight));
    }

    [Fact]
    public void Build_DoesNotChangeHttpMethodMetadata_WhenCorsNotPresent()
    {
        // Arrange
        const int defaultOrder = 0;
        static Task RequestDelegate(HttpContext d) => null;

        var builder = new RouteEndpointBuilder(RequestDelegate, RoutePatternFactory.Parse("/"), defaultOrder)
        {
            DisplayName = "Display name!",
            Metadata = { new HttpMethodMetadata(new[] { HttpMethods.Delete }, acceptCorsPreflight: false) }
        };

        // Act && Assert
        var endpoint = Assert.IsType<RouteEndpoint>(builder.Build());
        var httpMethodMetadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
        Assert.NotNull(httpMethodMetadata);
        Assert.False(httpMethodMetadata.AcceptCorsPreflight);
    }

    [Fact]
    public async Task Build_DoesNot_RunFilters()
    {
        var endpointFilterCallCount = 0;
        var invocationFilterCallCount = 0;
        var invocationCallCount = 0;

        const int defaultOrder = 0;
        RequestDelegate requestDelegate = (d) =>
        {
            invocationCallCount++;
            return Task.CompletedTask;
        };

        var builder = new RouteEndpointBuilder(requestDelegate, RoutePatternFactory.Parse("/"), defaultOrder);

        builder.FilterFactories.Add((endopintContext, next) =>
        {
            endpointFilterCallCount++;

            return invocationContext =>
            {
                invocationFilterCallCount++;

                return next(invocationContext);
            };
        });

        var endpoint = Assert.IsType<RouteEndpoint>(builder.Build());

        await endpoint.RequestDelegate(new DefaultHttpContext());

        Assert.Equal(0, endpointFilterCallCount);
        Assert.Equal(0, invocationFilterCallCount);
        Assert.Equal(1, invocationCallCount);
    }

    private class TestCorsMetadata : ICorsMetadata
    { }
}
