// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Builder;

public class MvcAreaRouteBuilderExtensionsTest
{
    [Fact]
    public void MapAreaRoute_Simple()
    {
        // Arrange
        var builder = CreateRouteBuilder();

        // Act
        builder.MapAreaRoute(name: null, areaName: "admin", template: "site/Admin/");

        // Assert
        var route = Assert.IsType<Route>((Assert.Single(builder.Routes)));

        Assert.Null(route.Name);
        Assert.Equal("site/Admin/", route.RouteTemplate);
        Assert.Collection(
            route.Constraints.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.IsType<StringRouteConstraint>(kvp.Value);
            });
        Assert.Empty(route.DataTokens);
        Assert.Collection(
            route.Defaults.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.Equal("admin", kvp.Value);
            });
    }

    [Fact]
    public void MapAreaRoute_Defaults()
    {
        // Arrange
        var builder = CreateRouteBuilder();

        // Act
        builder.MapAreaRoute(
            name: "admin_area",
            areaName: "admin",
            template: "site/Admin/",
            defaults: new { action = "Home" });

        // Assert
        var route = Assert.IsType<Route>((Assert.Single(builder.Routes)));

        Assert.Equal("admin_area", route.Name);
        Assert.Equal("site/Admin/", route.RouteTemplate);
        Assert.Collection(
            route.Constraints.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.IsType<StringRouteConstraint>(kvp.Value);
            });
        Assert.Empty(route.DataTokens);
        Assert.Collection(
            route.Defaults.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("action", kvp.Key);
                Assert.Equal("Home", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.Equal("admin", kvp.Value);
            });
    }

    [Fact]
    public void MapAreaRoute_DefaultsAndConstraints()
    {
        // Arrange
        var builder = CreateRouteBuilder();

        // Act
        builder.MapAreaRoute(
            name: "admin_area",
            areaName: "admin",
            template: "site/Admin/",
            defaults: new { action = "Home" },
            constraints: new { id = new IntRouteConstraint() });

        // Assert
        var route = Assert.IsType<Route>((Assert.Single(builder.Routes)));

        Assert.Equal("admin_area", route.Name);
        Assert.Equal("site/Admin/", route.RouteTemplate);
        Assert.Collection(
            route.Constraints.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.IsType<StringRouteConstraint>(kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("id", kvp.Key);
                Assert.IsType<IntRouteConstraint>(kvp.Value);
            });
        Assert.Empty(route.DataTokens);
        Assert.Collection(
            route.Defaults.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("action", kvp.Key);
                Assert.Equal("Home", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.Equal("admin", kvp.Value);
            });
    }

    [Fact]
    public void MapAreaRoute_DefaultsConstraintsAndDataTokens()
    {
        // Arrange
        var builder = CreateRouteBuilder();

        // Act
        builder.MapAreaRoute(
            name: "admin_area",
            areaName: "admin",
            template: "site/Admin/",
            defaults: new { action = "Home" },
            constraints: new { id = new IntRouteConstraint() },
            dataTokens: new { some_token = "hello" });

        // Assert
        var route = Assert.IsType<Route>((Assert.Single(builder.Routes)));

        Assert.Equal("admin_area", route.Name);
        Assert.Equal("site/Admin/", route.RouteTemplate);
        Assert.Collection(
            route.Constraints.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.IsType<StringRouteConstraint>(kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("id", kvp.Key);
                Assert.IsType<IntRouteConstraint>(kvp.Value);
            });
        Assert.Collection(
            route.DataTokens.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("some_token", kvp.Key);
                Assert.Equal("hello", kvp.Value);
            });
        Assert.Collection(
            route.Defaults.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("action", kvp.Key);
                Assert.Equal("Home", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.Equal("admin", kvp.Value);
            });
    }

    [Fact]
    public void MapAreaRoute_DoesNotReplaceValuesForAreaIfAlreadyPresentInConstraintsOrDefaults()
    {
        // Arrange
        var builder = CreateRouteBuilder();

        // Act
        builder.MapAreaRoute(
            name: "admin_area",
            areaName: "admin",
            template: "site/Admin/",
            defaults: new { area = "Home" },
            constraints: new { area = new IntRouteConstraint() },
            dataTokens: new { some_token = "hello" });

        // Assert
        var route = Assert.IsType<Route>((Assert.Single(builder.Routes)));

        Assert.Equal("admin_area", route.Name);
        Assert.Equal("site/Admin/", route.RouteTemplate);
        Assert.Collection(
            route.Constraints.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.IsType<IntRouteConstraint>(kvp.Value);
            });
        Assert.Collection(
            route.DataTokens.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("some_token", kvp.Key);
                Assert.Equal("hello", kvp.Value);
            });
        Assert.Collection(
            route.Defaults.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.Equal("Home", kvp.Value);
            });
    }

    [Fact]
    public void MapAreaRoute_UsesPassedInAreaNameAsIs()
    {
        // Arrange
        var builder = CreateRouteBuilder();
        var areaName = "user.admin";

        // Act
        builder.MapAreaRoute(name: null, areaName: areaName, template: "site/Admin/");

        // Assert
        var route = Assert.IsType<Route>((Assert.Single(builder.Routes)));

        Assert.Null(route.Name);
        Assert.Equal("site/Admin/", route.RouteTemplate);
        Assert.Collection(
            route.Constraints.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.IsType<StringRouteConstraint>(kvp.Value);

                var values = new RouteValueDictionary(new { area = areaName });
                var match = kvp.Value.Match(
                    new DefaultHttpContext(),
                    route: new Mock<IRouter>().Object,
                    routeKey: kvp.Key,
                    values: values,
                    routeDirection: RouteDirection.UrlGeneration);

                Assert.True(match);
            });
        Assert.Empty(route.DataTokens);
        Assert.Collection(
            route.Defaults.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.Equal(kvp.Value, areaName);
            });
    }

    private IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRouting();
        return services.BuildServiceProvider();
    }

    private IRouteBuilder CreateRouteBuilder()
    {
        var builder = new Mock<IRouteBuilder>();
        builder
            .SetupGet(b => b.ServiceProvider)
            .Returns(CreateServices());
        builder
            .SetupGet(b => b.Routes)
            .Returns(new List<IRouter>());
        builder
            .SetupGet(b => b.DefaultHandler)
            .Returns(Mock.Of<IRouter>());

        return builder.Object;
    }
}
