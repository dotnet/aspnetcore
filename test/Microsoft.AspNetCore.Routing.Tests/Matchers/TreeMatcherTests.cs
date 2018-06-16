// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class TreeMatcherTests
    {
        private MatcherEndpoint CreateEndpoint(string template, int order, object values = null, EndpointMetadataCollection metadata = null)
        {
            return new MatcherEndpoint((next) => null, template, values, order, metadata ?? EndpointMetadataCollection.Empty, template, address: null);
        }

        private TreeMatcher CreateTreeMatcher(EndpointDataSource endpointDataSource)
        {
            var compositeDataSource = new CompositeEndpointDataSource(new[] { endpointDataSource });
            var defaultInlineConstraintResolver = new DefaultInlineConstraintResolver(Options.Create(new RouteOptions()));
            var endpointSelector = new EndpointSelector(
                compositeDataSource,
                new EndpointConstraintCache(compositeDataSource, new IEndpointConstraintProvider[] { new DefaultEndpointConstraintProvider() }),
                NullLoggerFactory.Instance);

            return new TreeMatcher(defaultInlineConstraintResolver, NullLogger.Instance, endpointDataSource, endpointSelector);
        }

        [Fact]
        public async Task MatchAsync_DuplicateTemplatesAndDifferentOrder_LowerOrderEndpointMatched()
        {
            // Arrange
            var higherOrderEndpoint = CreateEndpoint("/Teams", 1);
            var lowerOrderEndpoint = CreateEndpoint("/Teams", 0);

            var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                higherOrderEndpoint,
                lowerOrderEndpoint
            });

            var treeMatcher = CreateTreeMatcher(endpointDataSource);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/Teams";

            var endpointFeature = new EndpointFeature();

            // Act
            await treeMatcher.MatchAsync(httpContext, endpointFeature);

            // Assert
            Assert.Equal(lowerOrderEndpoint, endpointFeature.Endpoint);
        }

        [Fact]
        public async Task MatchAsync_MultipleMatches_EndpointSelectorCalled()
        {
            // Arrange
            var endpointWithoutConstraint = CreateEndpoint("/Teams", 0);
            var endpointWithConstraint = CreateEndpoint(
                "/Teams",
                0,
                metadata: new EndpointMetadataCollection(new object[] { new HttpMethodEndpointConstraint(new[] { "POST" }) }));

            var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpointWithoutConstraint,
                endpointWithConstraint
            });

            var treeMatcher = CreateTreeMatcher(endpointDataSource);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "POST";
            httpContext.Request.Path = "/Teams";

            var endpointFeature = new EndpointFeature();

            // Act
            await treeMatcher.MatchAsync(httpContext, endpointFeature);

            // Assert
            Assert.Equal(endpointWithConstraint, endpointFeature.Endpoint);
        }
    }
}
