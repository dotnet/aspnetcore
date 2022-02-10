// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.TestObjects;

namespace Microsoft.AspNetCore.Routing;

public class EndpointNameAddressSchemeTest
{
    [Fact]
    public void AddressScheme_Match_ReturnsMatchingEndpoint()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint(
            "/a",
            metadata: new object[] { new EndpointNameMetadata("name1"), });

        var endpoint2 = EndpointFactory.CreateRouteEndpoint(
            "/b",
            metadata: new object[] { new EndpointNameMetadata("name2"), });

        var addressScheme = CreateAddressScheme(endpoint1, endpoint2);

        // Act
        var endpoints = addressScheme.FindEndpoints("name2");

        // Assert
        Assert.Collection(
            endpoints,
            e => Assert.Same(endpoint2, e));
    }

    [Fact]
    public void AddressScheme_NoMatch_ReturnsEmptyCollection()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "/a",
            metadata: new object[] { new EndpointNameMetadata("name1"), new SuppressLinkGenerationMetadata(), });

        var addressScheme = CreateAddressScheme(endpoint);

        // Act
        var endpoints = addressScheme.FindEndpoints("name2");

        // Assert
        Assert.Empty(endpoints);
    }

    [Fact]
    public void AddressScheme_NoMatch_CaseSensitive()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "/a",
            metadata: new object[] { new EndpointNameMetadata("name1"), new SuppressLinkGenerationMetadata(), });

        var addressScheme = CreateAddressScheme(endpoint);

        // Act
        var endpoints = addressScheme.FindEndpoints("NAME1");

        // Assert
        Assert.Empty(endpoints);
    }

    [Fact]
    public void AddressScheme_UpdatesWhenDataSourceChanges()
    {
        var endpoint1 = EndpointFactory.CreateRouteEndpoint(
            "/a",
            metadata: new object[] { new EndpointNameMetadata("name1"), });
        var dynamicDataSource = new DynamicEndpointDataSource(new[] { endpoint1 });

        // Act 1
        var addressScheme = CreateAddressScheme(dynamicDataSource);

        // Assert 1
        var match = Assert.Single(addressScheme.Entries);
        Assert.Same(endpoint1, match.Value.Single());

        // Arrange 2
        var endpoint2 = EndpointFactory.CreateRouteEndpoint(
            "/b",
            metadata: new object[] { new EndpointNameMetadata("name2"), });

        // Act 2
        // Trigger change
        dynamicDataSource.AddEndpoint(endpoint2);

        // Assert 2
        Assert.Collection(
            addressScheme.Entries.OrderBy(kvp => kvp.Key),
            (m) =>
            {
                Assert.Same(endpoint1, m.Value.Single());
            },
            (m) =>
            {
                Assert.Same(endpoint2, m.Value.Single());
            });
    }

    [Fact]
    public void AddressScheme_IgnoresEndpointsWithSuppressLinkGeneration()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "/a",
            metadata: new object[] { new EndpointNameMetadata("name1"), new SuppressLinkGenerationMetadata(), });

        // Act
        var addressScheme = CreateAddressScheme(endpoint);

        // Assert
        Assert.Empty(addressScheme.Entries);
    }

    [Fact]
    public void AddressScheme_UnsuppressedEndpoint_IsUsed()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "/a",
            metadata: new object[] { new EndpointNameMetadata("name1"), new SuppressLinkGenerationMetadata(), new EncourageLinkGenerationMetadata(), });

        // Act
        var addressScheme = CreateAddressScheme(endpoint);

        // Assert
        Assert.Same(endpoint, Assert.Single(Assert.Single(addressScheme.Entries).Value));
    }

    [Fact]
    public void AddressScheme_IgnoresEndpointsWithoutEndpointName()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "/a",
            metadata: new object[] { });

        // Act
        var addressScheme = CreateAddressScheme(endpoint);

        // Assert
        Assert.Empty(addressScheme.Entries);
    }

    [Fact]
    public void AddressScheme_ThrowsExceptionForDuplicateEndpoints()
    {
        // Arrange
        var endpoints = new Endpoint[]
        {
                EndpointFactory.CreateRouteEndpoint("/a", displayName: "a", metadata: new object[] { new EndpointNameMetadata("name1"), }),
                EndpointFactory.CreateRouteEndpoint("/b", displayName: "b", metadata: new object[] { new EndpointNameMetadata("name1"), }),
                EndpointFactory.CreateRouteEndpoint("/c", displayName: "c", metadata: new object[] { new EndpointNameMetadata("name1"), }),

                //// Not a duplicate
                EndpointFactory.CreateRouteEndpoint("/d", displayName: "d", metadata: new object[] { new EndpointNameMetadata("NAME1"), }),

                EndpointFactory.CreateRouteEndpoint("/e", displayName: "e", metadata: new object[] { new EndpointNameMetadata("name2"), }),
                EndpointFactory.CreateRouteEndpoint("/f", displayName: "f", metadata: new object[] { new EndpointNameMetadata("name2"), }),
        };

        var addressScheme = CreateAddressScheme(endpoints);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => addressScheme.FindEndpoints("any name"));

        // Assert
        Assert.Equal(String.Join(Environment.NewLine, @"The following endpoints with a duplicate endpoint name were found.",
"",
"Endpoints with endpoint name 'name1':",
"a",
"b",
"c",
"",
"Endpoints with endpoint name 'name2':",
"e",
"f",
""), ex.Message);
    }

    private EndpointNameAddressScheme CreateAddressScheme(params Endpoint[] endpoints)
    {
        return CreateAddressScheme(new DefaultEndpointDataSource(endpoints));
    }

    private EndpointNameAddressScheme CreateAddressScheme(params EndpointDataSource[] dataSources)
    {
        return new EndpointNameAddressScheme(new CompositeEndpointDataSource(dataSources));
    }

    private class EncourageLinkGenerationMetadata : ISuppressLinkGenerationMetadata
    {
        public bool SuppressLinkGeneration => false;
    }
}
