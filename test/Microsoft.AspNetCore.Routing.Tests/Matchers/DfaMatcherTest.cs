// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // Many of these are integration tests that exercise the system end to end,
    // so we're reusing the services here.
    public class DfaMatcherTest
    {
        private MatcherEndpoint CreateEndpoint(string template, int order, object defaults = null, EndpointMetadataCollection metadata = null)
        {
            return new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                RoutePatternFactory.Parse(template, defaults, constraints: null),
                new RouteValueDictionary(),
                order,
                metadata ?? EndpointMetadataCollection.Empty,
                template);
        }

        private Matcher CreateDfaMatcher(EndpointDataSource dataSource)
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddOptions()
                .AddRouting()
                .BuildServiceProvider();

            var factory = services.GetRequiredService<MatcherFactory>();
            return Assert.IsType<DataSourceDependentMatcher>(factory.CreateMatcher(dataSource));
        }

        [Fact]
        public async Task MatchAsync_ValidRouteConstraint_EndpointMatched()
        {
            // Arrange
            var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/{p:int}", 0)
            });

            var matcher = CreateDfaMatcher(endpointDataSource);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/1";

            var endpointFeature = new EndpointFeature();

            // Act
            await matcher.MatchAsync(httpContext, endpointFeature);

            // Assert
            Assert.NotNull(endpointFeature.Endpoint);
        }

        [Fact]
        public async Task MatchAsync_InvalidRouteConstraint_NoEndpointMatched()
        {
            // Arrange
            var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/{p:int}", 0)
            });

            var matcher = CreateDfaMatcher(endpointDataSource);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/One";

            var endpointFeature = new EndpointFeature();

            // Act
            await matcher.MatchAsync(httpContext, endpointFeature);

            // Assert
            Assert.Null(endpointFeature.Endpoint);
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

            var matcher = CreateDfaMatcher(endpointDataSource);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/Teams";

            var endpointFeature = new EndpointFeature();

            // Act
            await matcher.MatchAsync(httpContext, endpointFeature);

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

            var matcher = CreateDfaMatcher(endpointDataSource);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "POST";
            httpContext.Request.Path = "/Teams";

            var endpointFeature = new EndpointFeature();

            // Act
            await matcher.MatchAsync(httpContext, endpointFeature);

            // Assert
            Assert.Equal(endpointWithConstraint, endpointFeature.Endpoint);
        }
    }
}
