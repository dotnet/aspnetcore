// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class EndpointFeatureTest
    {
        [Fact]
        public void RouteData_CanIntializeDataTokens_WithMetadata()
        {
            // Arrange
            var expected = new RouteValueDictionary(new { foo = 17, bar = "hello", });

            var feature = new EndpointFeature()
            {
                Endpoint = new MatcherEndpoint(
                    MatcherEndpoint.EmptyInvoker,
                    RoutePatternFactory.Parse("/"),
                    0,
                    new EndpointMetadataCollection(new DataTokensMetadata(expected)),
                    "test"),
            };

            // Act
            var routeData = ((IRoutingFeature)feature).RouteData;

            // Assert
            Assert.NotSame(expected, routeData.DataTokens);
            Assert.Equal(expected.OrderBy(kvp => kvp.Key), routeData.DataTokens.OrderBy(kvp => kvp.Key));
        }

        [Fact]
        public void RouteData_DataTokensIsEmpty_WithoutMetadata()
        {
            // Arrange
            var feature = new EndpointFeature()
            {
                Endpoint = new MatcherEndpoint(
                    MatcherEndpoint.EmptyInvoker,
                    RoutePatternFactory.Parse("/"),
                    0,
                    new EndpointMetadataCollection(),
                    "test"),
            };

            // Act
            var routeData = ((IRoutingFeature)feature).RouteData;

            // Assert
            Assert.Empty(routeData.DataTokens);
        }
    }
}
