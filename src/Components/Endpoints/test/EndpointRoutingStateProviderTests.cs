// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints.DependencyInjection;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class EndpointRoutingStateProviderTests
{
    [Fact]
    public void RouteData_ReturnsSetValue_WhenCacheNotInvalidated()
    {
        // Arrange
        var provider = new EndpointRoutingStateProvider();
        var routeData = new RouteData(typeof(object), new Dictionary<string, object>());

        // Act
        provider.RouteData = routeData;

        // Assert
        Assert.Same(routeData, provider.RouteData);
    }

    [Fact]
    public void RouteData_CanBeSetMultipleTimes()
    {
        // Arrange
        var provider = new EndpointRoutingStateProvider();
        var routeData1 = new RouteData(typeof(object), new Dictionary<string, object>());
        var routeData2 = new RouteData(typeof(string), new Dictionary<string, object>());

        // Act & Assert
        provider.RouteData = routeData1;
        Assert.Same(routeData1, provider.RouteData);

        provider.RouteData = routeData2;
        Assert.Same(routeData2, provider.RouteData);

        provider.RouteData = null;
        Assert.Null(provider.RouteData);
    }
}