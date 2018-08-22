// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Routing.Patterns;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class CandidateSetTest
    {
        // We special case low numbers of candidates, so we want to verify that it works correctly for a variety
        // of input sizes.
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)] // this is the break-point where we start to use a list.
        [InlineData(6)]
        public void Create_CreatesCandidateSet(int count)
        {
            // Arrange
            var endpoints = new MatcherEndpoint[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] = CreateEndpoint($"/{i}");
            }

            var builder = CreateDfaMatcherBuilder();
            var candidates = builder.CreateCandidates(endpoints);

            // Act
            var candidateSet = new CandidateSet(candidates);

            // Assert
            for (var i = 0; i < candidateSet.Count; i++)
            {
                ref var state = ref candidateSet[i];
                Assert.True(state.IsValidCandidate);
                Assert.Same(endpoints[i], state.Endpoint);
                Assert.Equal(candidates[i].Score, state.Score);
                Assert.Null(state.Values);
            }
        }

        // We special case low numbers of candidates, so we want to verify that it works correctly for a variety
        // of input sizes.
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)] // this is the break-point where we start to use a list.
        [InlineData(6)]
        public void Create_CreatesCandidateSet_TestConstructor(int count)
        {
            // Arrange
            var endpoints = new MatcherEndpoint[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] = CreateEndpoint($"/{i}");
            }

            // Act
            var candidateSet = new CandidateSet(endpoints, Enumerable.Range(0, count).ToArray());

            // Assert
            for (var i = 0; i < candidateSet.Count; i++)
            {
                ref var state = ref candidateSet[i];
                Assert.True(state.IsValidCandidate);
                Assert.Same(endpoints[i], state.Endpoint);
                Assert.Equal(i, state.Score);
                Assert.Null(state.Values);
            }
        }

        private MatcherEndpoint CreateEndpoint(string template)
        {
            return new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                RoutePatternFactory.Parse(template),
                0,
                EndpointMetadataCollection.Empty,
                "test");
        }

        private static DfaMatcherBuilder CreateDfaMatcherBuilder(params MatcherPolicy[] policies)
        {
            var dataSource = new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>());
            return new DfaMatcherBuilder(
                Mock.Of<ParameterPolicyFactory>(),
                Mock.Of<EndpointSelector>(),
                policies);
        }
    }
}
