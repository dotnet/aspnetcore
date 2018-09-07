// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.TestObjects;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class EndpointNameEndpointFinderTest
    {
        [Fact]
        public void EndpointFinder_Match_ReturnsMatchingEndpoint()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint(
                "/a",
                metadata: new object[] { new EndpointNameMetadata("name1"), });

            var endpoint2 = EndpointFactory.CreateRouteEndpoint(
                "/b",
                metadata: new object[] { new EndpointNameMetadata("name2"), });

            var finder = CreateEndpointFinder(endpoint1, endpoint2);

            // Act
            var endpoints = finder.FindEndpoints("name2");

            // Assert
            Assert.Collection(
                endpoints,
                e => Assert.Same(endpoint2, e));
        }

        [Fact]
        public void EndpointFinder_NoMatch_ReturnsEmptyCollection()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateRouteEndpoint(
                "/a",
                metadata: new object[] { new EndpointNameMetadata("name1"), new SuppressLinkGenerationMetadata(), });

            var finder = CreateEndpointFinder(endpoint);

            // Act
            var endpoints = finder.FindEndpoints("name2");

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void EndpointFinder_NoMatch_CaseSensitive()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateRouteEndpoint(
                "/a",
                metadata: new object[] { new EndpointNameMetadata("name1"), new SuppressLinkGenerationMetadata(), });

            var finder = CreateEndpointFinder(endpoint);

            // Act
            var endpoints = finder.FindEndpoints("NAME1");

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void EndpointFinder_UpdatesWhenDataSourceChanges()
        {
            var endpoint1 = EndpointFactory.CreateRouteEndpoint(
                "/a",
                metadata: new object[] { new EndpointNameMetadata("name1"), });
            var dynamicDataSource = new DynamicEndpointDataSource(new[] { endpoint1 });

            // Act 1
            var finder = CreateEndpointFinder(dynamicDataSource);

            // Assert 1
            var match = Assert.Single(finder.Entries);
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
                finder.Entries.OrderBy(kvp => kvp.Key),
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
        public void EndpointFinder_IgnoresEndpointsWithSuppressLinkGeneration()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateRouteEndpoint(
                "/a",
                metadata: new object[] { new EndpointNameMetadata("name1"), new SuppressLinkGenerationMetadata(), });

            // Act
            var finder = CreateEndpointFinder(endpoint);

            // Assert
            Assert.Empty(finder.Entries);
        }

        [Fact]
        public void EndpointFinder_IgnoresEndpointsWithoutEndpointName()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateRouteEndpoint(
                "/a",
                metadata: new object[] { });

            // Act
            var finder = CreateEndpointFinder(endpoint);

            // Assert
            Assert.Empty(finder.Entries);
        }

        [Fact]
        public void EndpointFinder_ThrowsExceptionForDuplicateEndpoints()
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

            var finder = CreateEndpointFinder(endpoints);

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => finder.FindEndpoints("any name"));

            // Assert
            Assert.Equal(@"The following endpoints with a duplicate endpoint name were found.

Endpoints with endpoint name 'name1':
a
b
c

Endpoints with endpoint name 'name2':
e
f
", ex.Message);
        }

        private EndpointNameEndpointFinder CreateEndpointFinder(params Endpoint[] endpoints)
        {
            return CreateEndpointFinder(new DefaultEndpointDataSource(endpoints));
        }

        private EndpointNameEndpointFinder CreateEndpointFinder(params EndpointDataSource[] dataSources)
        {
            return new EndpointNameEndpointFinder(new CompositeEndpointDataSource(dataSources));
        }
    }
}
