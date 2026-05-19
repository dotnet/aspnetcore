// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Routing;

public class RouteDataTest
{
    [Fact]
    public void RouteData_DefaultPropertyValues()
    {
        // Arrange & Act
        var routeData = new RouteData();

        // Assert
        Assert.Empty(routeData.DataTokens);
        Assert.Empty(routeData.Routers);
        Assert.Empty(routeData.Values);
    }

    [Fact]
    public void RouteData_CopyConstructor()
    {
        // Arrange & Act
        var original = new RouteData();

        original.DataTokens.Add("data", "token");
        original.Routers.Add(Mock.Of<IRouter>());
        original.Values.Add("route", "value");

        var routeData = new RouteData(original);

        // Assert
        Assert.NotSame(routeData.DataTokens, original.DataTokens);
        Assert.Equal(routeData.DataTokens, original.DataTokens);
        Assert.NotSame(routeData.Routers, original.Routers);
        Assert.Equal(routeData.Routers, original.Routers);
        Assert.NotSame(routeData.Values, original.Values);
        Assert.Equal(routeData.Values, original.Values);
    }

    [Fact]
    public void RouteData_PushStateAndRestore_NullValues()
    {
        // Arrange
        var routeData = new RouteData();

        // Act
        var snapshot = routeData.PushState(null, null, null);
        var copy = new RouteData(routeData);
        snapshot.Restore();

        // Assert
        Assert.Equal(routeData.DataTokens, copy.DataTokens);
        Assert.Equal(routeData.Routers, copy.Routers);
        Assert.Equal(routeData.Values, copy.Values);
    }

    [Fact]
    public void RouteData_PushStateAndRestore_EmptyValues()
    {
        // Arrange
        var routeData = new RouteData();

        // Act
        var snapshot = routeData.PushState(null, new RouteValueDictionary(), new RouteValueDictionary());
        var copy = new RouteData(routeData);
        snapshot.Restore();

        // Assert
        Assert.Equal(routeData.DataTokens, copy.DataTokens);
        Assert.Equal(routeData.Routers, copy.Routers);
        Assert.Equal(routeData.Values, copy.Values);
    }

    // This is an important semantic for catchall parameters. A null route value shouldn't be
    // merged.
    [Fact]
    public void RouteData_PushStateAndRestore_NullRouteValueNotSet()
    {
        // Arrange
        var original = new RouteData();
        original.Values.Add("bleh", "16");

        var routeData = new RouteData(original);

        // Act
        var snapshot = routeData.PushState(
            null,
            new RouteValueDictionary(new { bleh = (string)null }),
            new RouteValueDictionary());
        snapshot.Restore();

        // Assert
        Assert.Equal(routeData.Values, original.Values);
    }

    [Fact]
    public void RouteData_PushStateAndThenModify()
    {
        // Arrange
        var routeData = new RouteData();

        // Act
        var snapshot = routeData.PushState(null, null, null);
        routeData.DataTokens.Add("data", "token");
        routeData.Routers.Add(Mock.Of<IRouter>());
        routeData.Values.Add("route", "value");

        var copy = new RouteData(routeData);
        snapshot.Restore();

        // Assert
        Assert.Empty(routeData.DataTokens);
        Assert.NotEqual(routeData.DataTokens, copy.DataTokens);
        Assert.Empty(routeData.Routers);
        Assert.NotEqual(routeData.Routers, copy.Routers);
        Assert.Empty(routeData.Values);
        Assert.NotEqual(routeData.Values, copy.Values);
    }

    [Fact]
    public void RouteData_PushStateAndThenModify_WithInitialData()
    {
        // Arrange
        var original = new RouteData();
        original.DataTokens.Add("data", "token1");
        original.Routers.Add(Mock.Of<IRouter>());
        original.Values.Add("route", "value1");

        var routeData = new RouteData(original);

        // Act
        var snapshot = routeData.PushState(
            Mock.Of<IRouter>(),
            new RouteValueDictionary(new { route = "value2" }),
            new RouteValueDictionary(new { data = "token2" }));

        routeData.DataTokens.Add("data2", "token");
        routeData.Routers.Add(Mock.Of<IRouter>());
        routeData.Values.Add("route2", "value");

        var copy = new RouteData(routeData);
        snapshot.Restore();

        // Assert
        Assert.Equal(original.DataTokens, routeData.DataTokens);
        Assert.NotEqual(routeData.DataTokens, copy.DataTokens);
        Assert.Equal(original.Routers, routeData.Routers);
        Assert.NotEqual(routeData.Routers, copy.Routers);
        Assert.Equal(original.Values, routeData.Values);
        Assert.NotEqual(routeData.Values, copy.Values);
    }
}
