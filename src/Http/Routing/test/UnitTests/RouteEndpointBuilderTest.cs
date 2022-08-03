// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing;

public class RouteEndpointBuilderTest
{
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
        Assert.Equal(metadata, Assert.Single(endpoint.Metadata));
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

    private class TestCorsMetadata : ICorsMetadata
    { }
}
