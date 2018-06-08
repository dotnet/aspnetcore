// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class DefaultEndpointFinderTest
    {
        [Fact]
        public void FindEndpoints_IgnoresCase_ForRouteNameLookup()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(new Address("home"));
            var endpoint2 = CreateEndpoint(new Address("admin"));
            var endpointFinder = CreateDefaultEndpointFinder(endpoint1, endpoint2);

            // Act
            var result = endpointFinder.FindEndpoints(new Address("Admin"));

            // Assert
            var endpoint = Assert.Single(result);
            Assert.Same(endpoint2, endpoint);
        }

        [Fact]
        public void FindEndpoints_MultipleEndpointsWithSameName_ReturnsFirstEndpoint_WithMatchingName()
        {
            // Arrange
            var name = "common-tag-for-all-my-section's-routes";
            var endpoint1 = CreateEndpoint(new Address(name));
            var endpoint2 = CreateEndpoint(new Address("admin"));
            var endpoint3 = CreateEndpoint(new Address(name));
            var endpoint4 = CreateEndpoint(new Address("products"));
            var endpointFinder = CreateDefaultEndpointFinder(endpoint1, endpoint2, endpoint3, endpoint4);

            // Act
            var result = endpointFinder.FindEndpoints(new Address(name));

            // Assert
            var endpoint = Assert.Single(result);
            Assert.Same(endpoint, endpoint1);
        }

        [Fact]
        public void FindEndpoints_ReturnsAllEndpoints_WhenNoEndpointsHaveAddress()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(address: null);
            var endpoint2 = CreateEndpoint(address: null);
            var endpointFinder = CreateDefaultEndpointFinder(endpoint1, endpoint2);

            // Act
            var result = endpointFinder.FindEndpoints(new Address("Admin"));

            // Assert
            Assert.Collection(
                result,
                (ep) => Assert.Same(endpoint1, ep),
                (ep) => Assert.Same(endpoint2, ep));
        }

        [Fact]
        public void FindEndpoints_ReturnsAllEndpoints_WhenLookupAddress_IsNull()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(new Address("home"));
            var endpoint2 = CreateEndpoint(new Address("admin"));
            var endpointFinder = CreateDefaultEndpointFinder(endpoint1, endpoint2);

            // Act
            var result = endpointFinder.FindEndpoints(lookupAddress: null);

            // Assert
            Assert.Collection(
                result,
                (ep) => Assert.Same(endpoint1, ep),
                (ep) => Assert.Same(endpoint2, ep));
        }

        [Fact]
        public void FindEndpoints_ReturnsAllEndpoints_WhenNoEndpointsHaveAddress_AndLookupAddress_IsNull()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(address: null);
            var endpoint2 = CreateEndpoint(address: null);
            var endpointFinder = CreateDefaultEndpointFinder(endpoint1, endpoint2);

            // Act
            var result = endpointFinder.FindEndpoints(lookupAddress: null);

            // Assert
            Assert.Collection(
                result,
                (ep) => Assert.Same(endpoint1, ep),
                (ep) => Assert.Same(endpoint2, ep));
        }

        [Fact]
        public void FindEndpoints_ReturnsAllEndpoints_WhenNoInformationGiven_OnLookupAddress()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(new Address("home"));
            var endpoint2 = CreateEndpoint(new Address("admin"));
            var endpointFinder = CreateDefaultEndpointFinder(endpoint1, endpoint2);

            // Act
            var result = endpointFinder.FindEndpoints(new Address(name: null));

            // Assert
            Assert.Collection(
                result,
                (ep) => Assert.Same(endpoint1, ep),
                (ep) => Assert.Same(endpoint2, ep));
        }

        [Fact]
        public void FindEndpoints_ReturnsEmpty_WhenNoEndpointFound_WithLookupAddress_Name()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(new Address("home"));
            var endpoint2 = CreateEndpoint(new Address("admin"));
            var endpointFinder = CreateDefaultEndpointFinder(endpoint1, endpoint2);

            // Act
            var result = endpointFinder.FindEndpoints(new Address("DoesNotExist"));

            // Assert
            Assert.Empty(result);
        }

        private Endpoint CreateEndpoint(Address address)
        {
            return new TestEndpoint(
                EndpointMetadataCollection.Empty,
                displayName: null,
                address: address);
        }

        private DefaultEndpointFinder CreateDefaultEndpointFinder(params Endpoint[] endpoints)
        {
            return new DefaultEndpointFinder(
                new CompositeEndpointDataSource(new[] { new DefaultEndpointDataSource(endpoints) }),
                NullLogger<DefaultEndpointFinder>.Instance);
        }

        private class HomeController
        {
            public void Index() { }
            public void Contact() { }
        }

        private class AdminController
        {
            public void Index() { }
            public void Contact() { }
        }
    }
}
