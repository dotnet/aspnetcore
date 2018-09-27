// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Patterns;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class DefaultEndpointSelectorTest
    {
        [Fact]
        public async Task SelectAsync_NoCandidates_DoesNothing()
        {
            // Arrange
            var endpoints = new RouteEndpoint[] { };
            var scores = new int[] { };
            var candidateSet = CreateCandidateSet(endpoints, scores);

            var (httpContext, context) = CreateContext();
            var selector = CreateSelector();

            // Act
            await selector.SelectAsync(httpContext, context, candidateSet);

            // Assert
            Assert.Null(context.Endpoint);
        }

        [Fact]
        public async Task SelectAsync_NoValidCandidates_DoesNothing()
        {
            // Arrange
            var endpoints = new RouteEndpoint[] { CreateEndpoint("/test"), };
            var scores = new int[] { 0, };
            var candidateSet = CreateCandidateSet(endpoints, scores);

            candidateSet[0].Values = new RouteValueDictionary();
            candidateSet[0].IsValidCandidate = false;

            var (httpContext, context) = CreateContext();
            var selector = CreateSelector();

            // Act
            await selector.SelectAsync(httpContext, context, candidateSet);

            // Assert
            Assert.Null(context.Endpoint);
        }

        [Fact]
        public async Task SelectAsync_SingleCandidate_ChoosesCandidate()
        {
            // Arrange
            var endpoints = new RouteEndpoint[] { CreateEndpoint("/test"), };
            var scores = new int[] { 0, };
            var candidateSet = CreateCandidateSet(endpoints, scores);

            candidateSet[0].Values = new RouteValueDictionary();
            candidateSet[0].IsValidCandidate = true;

            var (httpContext, context) = CreateContext();
            var selector = CreateSelector();

            // Act
            await selector.SelectAsync(httpContext, context, candidateSet);

            // Assert
            Assert.Same(endpoints[0], context.Endpoint);
        }

        [Fact]
        public async Task SelectAsync_SingleValidCandidate_ChoosesCandidate()
        {
            // Arrange
            var endpoints = new RouteEndpoint[] { CreateEndpoint("/test1"), CreateEndpoint("/test2"), };
            var scores = new int[] { 0, 0 };
            var candidateSet = CreateCandidateSet(endpoints, scores);

            candidateSet[0].IsValidCandidate = false;
            candidateSet[1].IsValidCandidate = true;

            var (httpContext, context) = CreateContext();
            var selector = CreateSelector();

            // Act
            await selector.SelectAsync(httpContext, context, candidateSet);

            // Assert
            Assert.Same(endpoints[1], context.Endpoint);
        }

        [Fact]
        public async Task SelectAsync_SingleValidCandidateInGroup_ChoosesCandidate()
        {
            // Arrange
            var endpoints = new RouteEndpoint[] { CreateEndpoint("/test1"), CreateEndpoint("/test2"), CreateEndpoint("/test3"), };
            var scores = new int[] { 0, 0, 1 };
            var candidateSet = CreateCandidateSet(endpoints, scores);

            candidateSet[0].IsValidCandidate = false;
            candidateSet[1].IsValidCandidate = true;
            candidateSet[2].IsValidCandidate = true;

            var (httpContext, context) = CreateContext();
            var selector = CreateSelector();

            // Act
            await selector.SelectAsync(httpContext, context, candidateSet);

            // Assert
            Assert.Same(endpoints[1], context.Endpoint);
        }

        [Fact]
        public async Task SelectAsync_ManyGroupsLastCandidate_ChoosesCandidate()
        {
            // Arrange
            var endpoints = new RouteEndpoint[]
            {
                CreateEndpoint("/test1"),
                CreateEndpoint("/test2"),
                CreateEndpoint("/test3"),
                CreateEndpoint("/test4"),
                CreateEndpoint("/test5"),
            };
            var scores = new int[] { 0, 1, 2, 3, 4 };
            var candidateSet = CreateCandidateSet(endpoints, scores);

            candidateSet[0].IsValidCandidate = false;
            candidateSet[1].IsValidCandidate = false;
            candidateSet[2].IsValidCandidate = false;
            candidateSet[3].IsValidCandidate = false;
            candidateSet[4].IsValidCandidate = true;

            var (httpContext, context) = CreateContext();
            var selector = CreateSelector();

            // Act
            await selector.SelectAsync(httpContext, context, candidateSet);

            // Assert
            Assert.Same(endpoints[4], context.Endpoint);
        }

        [Fact]
        public async Task SelectAsync_MultipleValidCandidatesInGroup_ReportsAmbiguity()
        {
            // Arrange
            var endpoints = new RouteEndpoint[] { CreateEndpoint("/test1"), CreateEndpoint("/test2"), CreateEndpoint("/test3"), };
            var scores = new int[] { 0, 1, 1 };
            var candidateSet = CreateCandidateSet(endpoints, scores);

            candidateSet[0].IsValidCandidate = false;
            candidateSet[1].IsValidCandidate = true;
            candidateSet[2].IsValidCandidate = true;

            var (httpContext, context) = CreateContext();
            var selector = CreateSelector();

            // Act
            var ex = await Assert.ThrowsAsync<AmbiguousMatchException>(() => selector.SelectAsync(httpContext, context, candidateSet));

            // Assert
            Assert.Equal(
@"The request matched multiple endpoints. Matches: 

test: /test2
test: /test3", ex.Message);
            Assert.Null(context.Endpoint);
        }

        [Fact]
        public async Task SelectAsync_RunsEndpointSelectorPolicies()
        {
            // Arrange
            var endpoints = new RouteEndpoint[] { CreateEndpoint("/test1"), CreateEndpoint("/test2"), CreateEndpoint("/test3"), };
            var scores = new int[] { 0, 0, 1 };
            var candidateSet = CreateCandidateSet(endpoints, scores);

            var policy = new Mock<MatcherPolicy>();
            policy
                .As<IEndpointSelectorPolicy>()
                .Setup(p => p.ApplyAsync(It.IsAny<HttpContext>(), It.IsAny<EndpointSelectorContext>(), It.IsAny<CandidateSet>()))
                .Returns<HttpContext, EndpointSelectorContext, CandidateSet>((c, f, cs) =>
                {
                    cs[1].IsValidCandidate = false;
                    return Task.CompletedTask;
                });

            candidateSet[0].IsValidCandidate = false;
            candidateSet[1].IsValidCandidate = true;
            candidateSet[2].IsValidCandidate = true;

            var (httpContext, context) = CreateContext();
            var selector = CreateSelector(policy.Object);

            // Act
            await selector.SelectAsync(httpContext, context, candidateSet);

            // Assert
            Assert.Same(endpoints[2], context.Endpoint);
        }

        [Fact]
        public async Task SelectAsync_RunsEndpointSelectorPolicies_CanShortCircuit()
        {
            // Arrange
            var endpoints = new RouteEndpoint[] { CreateEndpoint("/test1"), CreateEndpoint("/test2"), CreateEndpoint("/test3"), };
            var scores = new int[] { 0, 0, 1 };
            var candidateSet = CreateCandidateSet(endpoints, scores);

            var policy1 = new Mock<MatcherPolicy>();
            policy1
                .As<IEndpointSelectorPolicy>()
                .Setup(p => p.ApplyAsync(It.IsAny<HttpContext>(), It.IsAny<EndpointSelectorContext>(), It.IsAny<CandidateSet>()))
                .Returns<HttpContext, EndpointSelectorContext, CandidateSet>((c, f, cs) =>
                {
                    f.Endpoint = cs[0].Endpoint;
                    return Task.CompletedTask;
                });

            // This should never run, it's after policy1 which short circuits
            var policy2 = new Mock<MatcherPolicy>();
            policy2
                .SetupGet(p => p.Order)
                .Returns(1000);
            policy2
                .As<IEndpointSelectorPolicy>()
                .Setup(p => p.ApplyAsync(It.IsAny<HttpContext>(), It.IsAny<EndpointSelectorContext>(), It.IsAny<CandidateSet>()))
                .Throws(new InvalidOperationException());

            candidateSet[0].IsValidCandidate = false;
            candidateSet[1].IsValidCandidate = true;
            candidateSet[2].IsValidCandidate = true;

            var (httpContext, context) = CreateContext();
            var selector = CreateSelector(policy1.Object, policy2.Object);

            // Act
            await selector.SelectAsync(httpContext, context, candidateSet);

            // Assert
            Assert.Same(endpoints[0], context.Endpoint);
        }

        private static (HttpContext httpContext, EndpointSelectorContext context) CreateContext()
        {
            var context = new EndpointSelectorContext();
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IEndpointFeature>(context);
            httpContext.Features.Set<IRouteValuesFeature>(context);

            return (httpContext, context);
        }

        private static RouteEndpoint CreateEndpoint(string template)
        {
            return new RouteEndpoint(
                TestConstants.EmptyRequestDelegate,
                RoutePatternFactory.Parse(template),
                0,
                EndpointMetadataCollection.Empty,
                $"test: {template}");
        }

        private static CandidateSet CreateCandidateSet(RouteEndpoint[] endpoints, int[] scores)
        {
            return new CandidateSet(endpoints, scores);
        }

        private static DefaultEndpointSelector CreateSelector(params MatcherPolicy[] policies)
        {
            return new DefaultEndpointSelector(policies);
        }
    }
}