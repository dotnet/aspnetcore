// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.EndpointConstraints
{
    public class EndpointConstraintEndpointSelectorTest
    {
        [Fact]
        public async Task SelectBestCandidate_MultipleEndpoints_BestMatchSelected()
        {
            // Arrange
            var defaultEndpoint = CreateEndpoint("No constraint endpoint");

            var postEndpoint = CreateEndpoint(
                "POST constraint endpoint",
                new HttpMethodEndpointConstraint(new[] { "POST" }));

            var endpoints = new[]
            {
                defaultEndpoint,
                postEndpoint
            };

            var selector = CreateSelector(endpoints);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "POST";

            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));

            // Assert
            Assert.Same(postEndpoint, feature.Endpoint);
        }

        [Fact]
        public async Task SelectBestCandidate_MultipleEndpoints_AmbiguousMatchExceptionThrown()
        {
            // Arrange
            var expectedMessage =
                "The request matched multiple endpoints. Matches: " + Environment.NewLine +
                Environment.NewLine +
                "Ambiguous1" + Environment.NewLine +
                "Ambiguous2";

            var defaultEndpoint1 = CreateEndpoint("Ambiguous1");
            var defaultEndpoint2 = CreateEndpoint("Ambiguous2");

            var endpoints = new[]
            {
                defaultEndpoint1,
                defaultEndpoint2
            };

            var selector = CreateSelector(endpoints);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "POST";

            var feature = new EndpointFeature();

            // Act
            var ex = await Assert.ThrowsAnyAsync<AmbiguousMatchException>(() =>
            {
                return selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));
            });

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public async Task SelectBestCandidate_AmbiguousEndpoints_LogIsCorrect()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var endpoints = new[]
            {
                CreateEndpoint("A1"),
                CreateEndpoint("A2"),
            };

            var selector = CreateSelector(endpoints, loggerFactory);

            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            var names = string.Join(", ", endpoints.Select(action => action.DisplayName));
            var expectedMessage =
                $"Request matched multiple endpoints for request path '/test'. " +
                $"Matching endpoints: {names}";

            // Act
            await Assert.ThrowsAsync<AmbiguousMatchException>(() =>
            {
                return selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));
            });

            // Assert
            Assert.Empty(sink.Scopes);
            var write = Assert.Single(sink.Writes);
            Assert.Equal(expectedMessage, write.State?.ToString());
        }

        [Fact]
        public async Task SelectBestCandidate_PrefersEndpointWithConstraints()
        {
            // Arrange
            var endpointWithConstraint = CreateEndpoint(
                "Has constraint",
                new HttpMethodEndpointConstraint(new string[] { "POST" }));

            var endpointWithoutConstraints = CreateEndpoint("No constraint");

            var endpoints = new[] { endpointWithConstraint, endpointWithoutConstraints };

            var selector = CreateSelector(endpoints);
            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));

            // Assert
            Assert.Same(endpointWithConstraint, endpointWithConstraint);
        }

        [Fact]
        public async Task SelectBestCandidate_ConstraintsRejectAll()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(
                "action1",
                new BooleanConstraint() { Pass = false, });

            var endpoint2 = CreateEndpoint(
                "action2",
                new BooleanConstraint() { Pass = false, });

            var endpoints = new[] { endpoint1, endpoint2 };

            var selector = CreateSelector(endpoints);
            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));

            // Assert
            Assert.Null(feature.Endpoint);
        }

        [Fact]
        public async Task SelectBestCandidate_ConstraintsRejectAll_DifferentStages()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(
                "action1",
                new BooleanConstraint() { Pass = false, Order = 0 },
                new BooleanConstraint() { Pass = true, Order = 1 });

            var endpoint2 = CreateEndpoint(
                "action2",
                new BooleanConstraint() { Pass = true, Order = 0 },
                new BooleanConstraint() { Pass = false, Order = 1 });

            var endpoints = new[] { endpoint1, endpoint2 };

            var selector = CreateSelector(endpoints);
            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));

            // Assert
            Assert.Null(feature.Endpoint);
        }

        [Fact]
        public async Task SelectBestCandidate_EndpointConstraintFactory()
        {
            // Arrange
            var endpointWithConstraints = CreateEndpoint(
                "actionWithConstraints",
                new ConstraintFactory()
                {
                    Constraint = new BooleanConstraint() { Pass = true },
                });

            var actionWithoutConstraints = CreateEndpoint("actionWithoutConstraints");

            var endpoints = new[] { endpointWithConstraints, actionWithoutConstraints };

            var selector = CreateSelector(endpoints);
            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));

            // Assert
            Assert.Same(endpointWithConstraints, feature.Endpoint);
        }

        [Fact]
        public async Task SelectBestCandidate_MultipleCallsNoConstraint_ReturnsEndpoint()
        {
            // Arrange
            var noConstraint = CreateEndpoint("noConstraint");

            var endpoints = new[] { noConstraint };

            var selector = CreateSelector(endpoints);
            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));
            var endpoint1 = feature.Endpoint;

            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));
            var endpoint2 = feature.Endpoint;

            // Assert
            Assert.Same(endpoint1, noConstraint);
            Assert.Same(endpoint2, noConstraint);
        }

        [Fact]
        public async Task SelectBestCandidate_MultipleCallsNonConstraintMetadata_ReturnsEndpoint()
        {
            // Arrange
            var noConstraint = CreateEndpoint("noConstraint", new object());

            var endpoints = new[] { noConstraint };

            var selector = CreateSelector(endpoints);
            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));
            var endpoint1 = feature.Endpoint;

            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));
            var endpoint2 = feature.Endpoint;

            // Assert
            Assert.Same(endpoint1, noConstraint);
            Assert.Same(endpoint2, noConstraint);
        }

        [Fact]
        public async Task SelectBestCandidate_EndpointConstraintFactory_ReturnsNull()
        {
            // Arrange
            var nullConstraint = CreateEndpoint("nullConstraint", new ConstraintFactory());

            var endpoints = new[] { nullConstraint };

            var selector = CreateSelector(endpoints);
            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));
            var endpoint1 = feature.Endpoint;

            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));
            var endpoint2 = feature.Endpoint;

            // Assert
            Assert.Same(endpoint1, nullConstraint);
            Assert.Same(endpoint2, nullConstraint);
        }

        // There's a custom constraint provider registered that only understands BooleanConstraintMarker
        [Fact]
        public async Task SelectBestCandidate_CustomProvider()
        {
            // Arrange
            var endpointWithConstraints = CreateEndpoint(
                "actionWithConstraints",
                new BooleanConstraintMarker() { Pass = true });

            var endpointWithoutConstraints = CreateEndpoint("actionWithoutConstraints");

            var endpoints = new[] { endpointWithConstraints, endpointWithoutConstraints, };

            var selector = CreateSelector(endpoints);
            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));

            // Assert
            Assert.Same(endpointWithConstraints, feature.Endpoint);
        }

        // Due to ordering of stages, the first action will be better.
        [Fact]
        public async Task SelectBestCandidate_ConstraintsInOrder()
        {
            // Arrange
            var best = CreateEndpoint("best", new BooleanConstraint() { Pass = true, Order = 0, });

            var worst = CreateEndpoint("worst", new BooleanConstraint() { Pass = true, Order = 1, });

            var endpoints = new[] { best, worst };

            var selector = CreateSelector(endpoints);
            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));

            // Assert
            Assert.Same(best, feature.Endpoint);
        }

        // Due to ordering of stages, the first action will be better.
        [Fact]
        public async Task SelectBestCandidate_ConstraintsInOrder_MultipleStages()
        {
            // Arrange
            var best = CreateEndpoint(
                "best",
                new BooleanConstraint() { Pass = true, Order = 0, },
                new BooleanConstraint() { Pass = true, Order = 1, },
                new BooleanConstraint() { Pass = true, Order = 2, });

            var worst = CreateEndpoint(
                "worst",
                new BooleanConstraint() { Pass = true, Order = 0, },
                new BooleanConstraint() { Pass = true, Order = 1, },
                new BooleanConstraint() { Pass = true, Order = 3, });

            var endpoints = new[] { best, worst };

            var selector = CreateSelector(endpoints);
            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));

            // Assert
            Assert.Same(best, feature.Endpoint);
        }

        [Fact]
        public async Task SelectBestCandidate_Fallback_ToEndpointWithoutConstraints()
        {
            // Arrange
            var nomatch1 = CreateEndpoint(
                "nomatch1",
                new BooleanConstraint() { Pass = true, Order = 0, },
                new BooleanConstraint() { Pass = true, Order = 1, },
                new BooleanConstraint() { Pass = false, Order = 2, });

            var nomatch2 = CreateEndpoint(
                "nomatch2",
                new BooleanConstraint() { Pass = true, Order = 0, },
                new BooleanConstraint() { Pass = true, Order = 1, },
                new BooleanConstraint() { Pass = false, Order = 3, });

            var best = CreateEndpoint("best");

            var endpoints = new[] { best, nomatch1, nomatch2 };

            var selector = CreateSelector(endpoints);
            var httpContext = CreateHttpContext("POST");
            var feature = new EndpointFeature();

            // Act
            await selector.SelectAsync(httpContext, feature, CreateCandidateSet(endpoints));

            // Assert
            Assert.Same(best, feature.Endpoint);
        }

        private static MatcherEndpoint CreateEndpoint(string displayName, params object[] metadata)
        {
            return new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                RoutePatternFactory.Parse("/"),
                new RouteValueDictionary(),
                0,
                new EndpointMetadataCollection(metadata),
                displayName);
        }

        private static CandidateSet CreateCandidateSet(MatcherEndpoint[] endpoints)
        {
            var scores = new int[endpoints.Length];
            return new CandidateSet(endpoints, scores);
        }

        private static EndpointSelector CreateSelector(IReadOnlyList<Endpoint> actions, ILoggerFactory loggerFactory = null)
        {
            loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

            var endpointDataSource = new CompositeEndpointDataSource(new[] { new DefaultEndpointDataSource(actions) });

            var actionConstraintProviders = new IEndpointConstraintProvider[] {
                    new DefaultEndpointConstraintProvider(),
                    new BooleanConstraintProvider(),
                };

            return new EndpointConstraintEndpointSelector(
                endpointDataSource,
                GetEndpointConstraintCache(actionConstraintProviders),
                loggerFactory);
        }

        private static HttpContext CreateHttpContext(string httpMethod)
        {
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

            var request = new Mock<HttpRequest>(MockBehavior.Strict);
            request.SetupGet(r => r.Method).Returns(httpMethod);
            request.SetupGet(r => r.Path).Returns(new PathString("/test"));
            request.SetupGet(r => r.Headers).Returns(new HeaderDictionary());
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.RequestServices).Returns(serviceProvider);

            return httpContext.Object;
        }

        private static EndpointConstraintCache GetEndpointConstraintCache(IEndpointConstraintProvider[] actionConstraintProviders = null)
        {
            return new EndpointConstraintCache(
                new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>()),
                actionConstraintProviders.AsEnumerable() ?? new List<IEndpointConstraintProvider>());
        }

        private class BooleanConstraint : IEndpointConstraint
        {
            public bool Pass { get; set; }

            public int Order { get; set; }

            public bool Accept(EndpointConstraintContext context)
            {
                return Pass;
            }
        }

        private class ConstraintFactory : IEndpointConstraintFactory
        {
            public IEndpointConstraint Constraint { get; set; }

            public bool IsReusable => true;

            public IEndpointConstraint CreateInstance(IServiceProvider services)
            {
                return Constraint;
            }
        }

        private class BooleanConstraintMarker : IEndpointConstraintMetadata
        {
            public bool Pass { get; set; }
        }

        private class BooleanConstraintProvider : IEndpointConstraintProvider
        {
            public int Order { get; set; }

            public void OnProvidersExecuting(EndpointConstraintProviderContext context)
            {
                foreach (var item in context.Results)
                {
                    if (item.Metadata is BooleanConstraintMarker marker)
                    {
                        Assert.Null(item.Constraint);
                        item.Constraint = new BooleanConstraint() { Pass = marker.Pass };
                    }
                }
            }

            public void OnProvidersExecuted(EndpointConstraintProviderContext context)
            {
            }
        }
    }
}
