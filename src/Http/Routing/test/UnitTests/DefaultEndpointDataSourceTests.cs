// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

public class DefaultEndpointDataSourceTests
{
    [Fact]
    public void Constructor_Params_EndpointsInitialized()
    {
        // Arrange & Act
        var dataSource = new DefaultEndpointDataSource(
            new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "1"),
            new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "2")
            );

        // Assert
        Assert.Collection(dataSource.Endpoints,
            endpoint => Assert.Equal("1", endpoint.DisplayName),
            endpoint => Assert.Equal("2", endpoint.DisplayName));
    }

    [Fact]
    public void Constructor_Params_ShouldMakeCopyOfEndpoints()
    {
        // Arrange
        var endpoint1 = new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "1");
        var endpoint2 = new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "2");
        var endpoints = new[] { endpoint1, endpoint2 };

        // Act
        var dataSource = new DefaultEndpointDataSource(endpoints);
        Array.Resize(ref endpoints, 1);
        endpoints[0] = null;

        // Assert
        Assert.Equal(2, dataSource.Endpoints.Count);
        Assert.Contains(endpoint1, dataSource.Endpoints);
        Assert.Contains(endpoint2, dataSource.Endpoints);
    }

    [Fact]
    public void Constructor_Params_ShouldThrowArgumentNullExceptionWhenEndpointsIsNull()
    {
        Endpoint[] endpoints = null;

        var actual = Assert.Throws<ArgumentNullException>(() => new DefaultEndpointDataSource(endpoints));
        Assert.Equal("endpoints", actual.ParamName);
    }

    [Fact]
    public void Constructor_Enumerable_EndpointsInitialized()
    {
        // Arrange & Act
        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "1"),
                new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "2")
            });

        // Assert
        Assert.Collection(dataSource.Endpoints,
            endpoint => Assert.Equal("1", endpoint.DisplayName),
            endpoint => Assert.Equal("2", endpoint.DisplayName));
    }

    [Fact]
    public void Constructor_Enumerable_ShouldMakeCopyOfEndpoints()
    {
        // Arrange
        var endpoint1 = new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "1");
        var endpoint2 = new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "2");
        var endpoints = new List<Endpoint> { endpoint1, endpoint2 };

        // Act
        var dataSource = new DefaultEndpointDataSource((IEnumerable<Endpoint>)endpoints);
        endpoints.RemoveAt(0);
        endpoints[0] = null;

        // Assert
        Assert.Equal(2, dataSource.Endpoints.Count);
        Assert.Contains(endpoint1, dataSource.Endpoints);
        Assert.Contains(endpoint2, dataSource.Endpoints);
    }

    [Fact]
    public void Constructor_Enumerable_ShouldThrowArgumentNullExceptionWhenEndpointsIsNull()
    {
        IEnumerable<Endpoint> endpoints = null;

        var actual = Assert.Throws<ArgumentNullException>(() => new DefaultEndpointDataSource(endpoints));
        Assert.Equal("endpoints", actual.ParamName);
    }
}
