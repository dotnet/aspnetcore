// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.EndpointConstraints
{
    public class EndpointSelectorTests
    {
        [Fact]
        public void SelectBestCandidate_MultipleEndpoints_BestMatchSelected()
        {
            // Arrange
            var defaultEndpoint = new TestEndpoint(
                EndpointMetadataCollection.Empty,
                "No constraint endpoint");

            var postEndpoint = new TestEndpoint(
                new EndpointMetadataCollection(new object[] { new HttpMethodEndpointConstraint(new[] { "POST" }) }),
                "POST constraint endpoint");

            var endpoints = new Endpoint[]
                {
                    defaultEndpoint,
                    postEndpoint
                };

            var endpointSelector = CreateSelector(endpoints);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "POST";

            // Act
            var bestCandidateEndpoint = endpointSelector.SelectBestCandidate(httpContext, endpoints);

            // Assert
            Assert.NotNull(postEndpoint);
        }

        [Fact]
        public void SelectBestCandidate_MultipleEndpoints_AmbiguousMatchExceptionThrown()
        {
            // Arrange
            var expectedMessage =
                "The request matched multiple endpoints. Matches: " + Environment.NewLine +
                Environment.NewLine +
                "Ambiguous1" + Environment.NewLine +
                "Ambiguous2";

            var defaultEndpoint1 = new TestEndpoint(
                EndpointMetadataCollection.Empty,
                "Ambiguous1");

            var defaultEndpoint2 = new TestEndpoint(
                EndpointMetadataCollection.Empty,
                "Ambiguous2");

            var endpoints = new Endpoint[]
                {
                    defaultEndpoint1,
                    defaultEndpoint2
                };

            var endpointSelector = CreateSelector(endpoints);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "POST";

            // Act
            var ex = Assert.ThrowsAny<AmbiguousMatchException>(() =>
            {
                endpointSelector.SelectBestCandidate(httpContext, endpoints);
            });

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void SelectBestCandidate_AmbiguousEndpoints_LogIsCorrect()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var actions = new Endpoint[]
            {
                new TestEndpoint(EndpointMetadataCollection.Empty, "A1"),
                new TestEndpoint(EndpointMetadataCollection.Empty, "A2"),
            };
            var selector = CreateSelector(actions, loggerFactory);

            var httpContext = CreateHttpContext("POST");
            var actionNames = string.Join(", ", actions.Select(action => action.DisplayName));
            var expectedMessage = $"Request matched multiple endpoints for request path '/test'. Matching endpoints: {actionNames}";

            // Act
            Assert.Throws<AmbiguousMatchException>(() => { selector.SelectBestCandidate(httpContext, actions); });

            // Assert
            Assert.Empty(sink.Scopes);
            var write = Assert.Single(sink.Writes);
            Assert.Equal(expectedMessage, write.State?.ToString());
        }

        [Fact]
        public void SelectBestCandidate_PrefersEndpointWithConstraints()
        {
            // Arrange
            var actionWithConstraints = new TestEndpoint(
                new EndpointMetadataCollection(new[] { new HttpMethodEndpointConstraint(new string[] { "POST" }) }),
                "Has constraint");

            var actionWithoutConstraints = new TestEndpoint(
                EndpointMetadataCollection.Empty,
                "No constraint");

            var actions = new Endpoint[] { actionWithConstraints, actionWithoutConstraints };

            var selector = CreateSelector(actions);
            var context = CreateHttpContext("POST");

            // Act
            var action = selector.SelectBestCandidate(context, actions);

            // Assert
            Assert.Same(action, actionWithConstraints);
        }

        [Fact]
        public void SelectBestCandidate_ConstraintsRejectAll()
        {
            // Arrange
            var action1 = new TestEndpoint(
                new EndpointMetadataCollection(new[] { new BooleanConstraint() { Pass = false, } }),
                "action1");

            var action2 = new TestEndpoint(
                new EndpointMetadataCollection(new[] { new BooleanConstraint() { Pass = false, } }),
                "action2");

            var actions = new Endpoint[] { action1, action2 };

            var selector = CreateSelector(actions);
            var context = CreateHttpContext("POST");

            // Act
            var action = selector.SelectBestCandidate(context, actions);

            // Assert
            Assert.Null(action);
        }

        [Fact]
        public void SelectBestCandidate_ConstraintsRejectAll_DifferentStages()
        {
            // Arrange
            var action1 = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new BooleanConstraint() { Pass = false, Order = 0 },
                new BooleanConstraint() { Pass = true, Order = 1 },
            }),
            "action1");

            var action2 = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new BooleanConstraint() { Pass = true, Order = 0 },
                new BooleanConstraint() { Pass = false, Order = 1 },
            }),
            "action2");

            var actions = new Endpoint[] { action1, action2 };

            var selector = CreateSelector(actions);
            var context = CreateHttpContext("POST");

            // Act
            var action = selector.SelectBestCandidate(context, actions);

            // Assert
            Assert.Null(action);
        }

        [Fact]
        public void SelectBestCandidate_EndpointConstraintFactory()
        {
            // Arrange
            var actionWithConstraints = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new ConstraintFactory()
                {
                    Constraint = new BooleanConstraint() { Pass = true },
                },
            }),
            "actionWithConstraints");

            var actionWithoutConstraints = new TestEndpoint(
                EndpointMetadataCollection.Empty,
                "actionWithoutConstraints");

            var actions = new Endpoint[] { actionWithConstraints, actionWithoutConstraints };

            var selector = CreateSelector(actions);
            var context = CreateHttpContext("POST");

            // Act
            var action = selector.SelectBestCandidate(context, actions);

            // Assert
            Assert.Same(action, actionWithConstraints);
        }

        [Fact]
        public void SelectBestCandidate_MultipleCallsNoConstraint_ReturnsEndpoint()
        {
            // Arrange
            var noConstraint = new TestEndpoint(EndpointMetadataCollection.Empty, "noConstraint");

            var actions = new Endpoint[] { noConstraint };

            var selector = CreateSelector(actions);
            var context = CreateHttpContext("POST");

            // Act
            var action1 = selector.SelectBestCandidate(context, actions);
            var action2 = selector.SelectBestCandidate(context, actions);

            // Assert
            Assert.Same(action1, noConstraint);
            Assert.Same(action2, noConstraint);
        }

        [Fact]
        public void SelectBestCandidate_MultipleCallsNonConstraintMetadata_ReturnsEndpoint()
        {
            // Arrange
            var noConstraint = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new object(),
            }),
            "noConstraint");

            var actions = new Endpoint[] { noConstraint };

            var selector = CreateSelector(actions);
            var context = CreateHttpContext("POST");

            // Act
            var action1 = selector.SelectBestCandidate(context, actions);
            var action2 = selector.SelectBestCandidate(context, actions);

            // Assert
            Assert.Same(action1, noConstraint);
            Assert.Same(action2, noConstraint);
        }

        [Fact]
        public void SelectBestCandidate_EndpointConstraintFactory_ReturnsNull()
        {
            // Arrange
            var nullConstraint = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new ConstraintFactory(),
            }),
            "nullConstraint");

            var actions = new Endpoint[] { nullConstraint };

            var selector = CreateSelector(actions);
            var context = CreateHttpContext("POST");

            // Act
            var action1 = selector.SelectBestCandidate(context, actions);
            var action2 = selector.SelectBestCandidate(context, actions);

            // Assert
            Assert.Same(action1, nullConstraint);
            Assert.Same(action2, nullConstraint);
        }

        // There's a custom constraint provider registered that only understands BooleanConstraintMarker
        [Fact]
        public void SelectBestCandidate_CustomProvider()
        {
            // Arrange
            var actionWithConstraints = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new BooleanConstraintMarker() { Pass = true },
            }),
            "actionWithConstraints");

            var actionWithoutConstraints = new TestEndpoint(
                EndpointMetadataCollection.Empty,
                "actionWithoutConstraints");

            var actions = new Endpoint[] { actionWithConstraints, actionWithoutConstraints, };

            var selector = CreateSelector(actions);
            var context = CreateHttpContext("POST");

            // Act
            var action = selector.SelectBestCandidate(context, actions);

            // Assert
            Assert.Same(action, actionWithConstraints);
        }

        // Due to ordering of stages, the first action will be better.
        [Fact]
        public void SelectBestCandidate_ConstraintsInOrder()
        {
            // Arrange
            var best = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new BooleanConstraint() { Pass = true, Order = 0, },
            }),
            "best");

            var worst = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new BooleanConstraint() { Pass = true, Order = 1, },
            }),
            "worst");

            var actions = new Endpoint[] { best, worst };

            var selector = CreateSelector(actions);
            var context = CreateHttpContext("POST");

            // Act
            var action = selector.SelectBestCandidate(context, actions);

            // Assert
            Assert.Same(action, best);
        }

        // Due to ordering of stages, the first action will be better.
        [Fact]
        public void SelectBestCandidate_ConstraintsInOrder_MultipleStages()
        {
            // Arrange
            var best = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new BooleanConstraint() { Pass = true, Order = 0, },
                new BooleanConstraint() { Pass = true, Order = 1, },
                new BooleanConstraint() { Pass = true, Order = 2, },
            }),
            "best");

            var worst = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new BooleanConstraint() { Pass = true, Order = 0, },
                new BooleanConstraint() { Pass = true, Order = 1, },
                new BooleanConstraint() { Pass = true, Order = 3, },
            }),
            "worst");

            var actions = new Endpoint[] { best, worst };

            var selector = CreateSelector(actions);
            var context = CreateHttpContext("POST");

            // Act
            var action = selector.SelectBestCandidate(context, actions);

            // Assert
            Assert.Same(action, best);
        }

        [Fact]
        public void SelectBestCandidate_Fallback_ToEndpointWithoutConstraints()
        {
            // Arrange
            var nomatch1 = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new BooleanConstraint() { Pass = true, Order = 0, },
                new BooleanConstraint() { Pass = true, Order = 1, },
                new BooleanConstraint() { Pass = false, Order = 2, },
            }),
            "nomatch1");

            var nomatch2 = new TestEndpoint(new EndpointMetadataCollection(new[]
            {
                new BooleanConstraint() { Pass = true, Order = 0, },
                new BooleanConstraint() { Pass = true, Order = 1, },
                new BooleanConstraint() { Pass = false, Order = 3, },
            }),
            "nomatch2");

            var best = new TestEndpoint(EndpointMetadataCollection.Empty, "best");

            var actions = new Endpoint[] { best, nomatch1, nomatch2 };

            var selector = CreateSelector(actions);
            var context = CreateHttpContext("POST");

            // Act
            var action = selector.SelectBestCandidate(context, actions);

            // Assert
            Assert.Same(action, best);
        }

        private static EndpointSelector CreateSelector(IReadOnlyList<Endpoint> actions, ILoggerFactory loggerFactory = null)
        {
            loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

            var endpointDataSource = new CompositeEndpointDataSource(new[] { new DefaultEndpointDataSource(actions) });

            var actionConstraintProviders = new IEndpointConstraintProvider[] {
                    new DefaultEndpointConstraintProvider(),
                    new BooleanConstraintProvider(),
                };

            return new EndpointSelector(
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
