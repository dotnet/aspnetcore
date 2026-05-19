// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Tests;

public class RouteOptionsTests
{
    [Fact]
    public void ConfigureRouting_ConfiguresOptionsProperly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();

        // Act
        services.AddRouting(options => options.ConstraintMap.Add("foo", typeof(TestRouteConstraint)));
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var accessor = serviceProvider.GetRequiredService<IOptions<RouteOptions>>();
        Assert.Equal("TestRouteConstraint", accessor.Value.ConstraintMap["foo"].Name);
    }

    [Fact]
    public void EndpointDataSources_WithDependencyInjection_AddedDataSourcesAddedToEndpointDataSource()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRouting();
        var serviceProvider = services.BuildServiceProvider();

        var endpoint1 = new Endpoint((c) => Task.CompletedTask, EndpointMetadataCollection.Empty, string.Empty);
        var endpoint2 = new Endpoint((c) => Task.CompletedTask, EndpointMetadataCollection.Empty, string.Empty);

        var options = serviceProvider.GetRequiredService<IOptions<RouteOptions>>().Value;
        var endpointDataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

        // Act 1
        options.EndpointDataSources.Add(new DefaultEndpointDataSource(endpoint1));

        // Assert 1
        var result = Assert.Single(endpointDataSource.Endpoints);
        Assert.Same(endpoint1, result);

        // Act 2
        options.EndpointDataSources.Add(new DefaultEndpointDataSource(endpoint2));

        // Assert 2
        Assert.Collection(endpointDataSource.Endpoints,
            ep => Assert.Same(endpoint1, ep),
            ep => Assert.Same(endpoint2, ep));
    }

    private class TestRouteConstraint : IRouteConstraint
    {
        public TestRouteConstraint(string pattern)
        {
            Pattern = pattern;
        }

        public string Pattern { get; private set; }
        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            throw new NotImplementedException();
        }
    }
}
