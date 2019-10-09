// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class MatcherPolicyTest
    {
        [Fact]
        public void ContainsDynamicEndpoint_FindsDynamicEndpoint()
        {
            // Arrange
            var endpoints = new Endpoint[]
            {
                CreateEndpoint("1"),
                CreateEndpoint("2"),
                CreateEndpoint("3", new DynamicEndpointMetadata(isDynamic: true)),
            };

            // Act
            var result = TestMatcherPolicy.ContainsDynamicEndpoints(endpoints);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ContainsDynamicEndpoint_DoesNotFindDynamicEndpoint()
        {
            // Arrange
            var endpoints = new Endpoint[]
            {
                CreateEndpoint("1"),
                CreateEndpoint("2"),
                CreateEndpoint("3", new DynamicEndpointMetadata(isDynamic: false)),
            };

            // Act
            var result = TestMatcherPolicy.ContainsDynamicEndpoints(endpoints);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ContainsDynamicEndpoint_DoesNotFindDynamicEndpoint_Empty()
        {
            // Arrange
            var endpoints = new Endpoint[]{ };

            // Act
            var result = TestMatcherPolicy.ContainsDynamicEndpoints(endpoints);

            // Assert
            Assert.False(result);
        }

        private RouteEndpoint CreateEndpoint(string template, params object[] metadata)
        {
            return new RouteEndpoint(
                TestConstants.EmptyRequestDelegate,
                RoutePatternFactory.Parse(template),
                0,
                new EndpointMetadataCollection(metadata),
                "test");
        }

        private class DynamicEndpointMetadata : IDynamicEndpointMetadata
        {
            public DynamicEndpointMetadata(bool isDynamic)
            {
                IsDynamic = isDynamic;
            }

            public bool IsDynamic { get; }
        }

        private class TestMatcherPolicy : MatcherPolicy
        {
            public override int Order => throw new System.NotImplementedException();

            public new static bool ContainsDynamicEndpoints(IReadOnlyList<Endpoint> endpoints)
            {
                return MatcherPolicy.ContainsDynamicEndpoints(endpoints);
            }
        }
    }
}
