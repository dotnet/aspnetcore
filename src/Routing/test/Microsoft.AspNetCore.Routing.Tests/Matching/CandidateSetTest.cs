// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
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
        [InlineData(31)]
        [InlineData(32)] // this is the break point where we use a BitArray
        [InlineData(33)]
        public void Create_CreatesCandidateSet(int count)
        {
            // Arrange
            var endpoints = new RouteEndpoint[count];
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
                Assert.True(candidateSet.IsValidCandidate(i));
                Assert.Same(endpoints[i], state.Endpoint);
                Assert.Equal(candidates[i].Score, state.Score);
                Assert.Null(state.Values);

                candidateSet.SetValidity(i, false);
                Assert.False(candidateSet.IsValidCandidate(i));
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
        [InlineData(31)]
        [InlineData(32)] // this is the break point where we use a BitArray
        [InlineData(33)]
        public void Create_CreatesCandidateSet_TestConstructor(int count)
        {
            // Arrange
            var endpoints = new RouteEndpoint[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] = CreateEndpoint($"/{i}");
            }

            var values = new RouteValueDictionary[count];
            for (var i = 0; i < endpoints.Length; i++)
            {
                values[i] = new RouteValueDictionary()
                {
                    { "i", i }
                };
            }

            // Act
            var candidateSet = new CandidateSet(endpoints, values, Enumerable.Range(0, count).ToArray());

            // Assert
            for (var i = 0; i < candidateSet.Count; i++)
            {
                ref var state = ref candidateSet[i];
                Assert.True(candidateSet.IsValidCandidate(i));
                Assert.Same(endpoints[i], state.Endpoint);
                Assert.Equal(i, state.Score);
                Assert.NotNull(state.Values);
                Assert.Equal(i, state.Values["i"]);

                candidateSet.SetValidity(i, false);
                Assert.False(candidateSet.IsValidCandidate(i));
            }
        }

        private RouteEndpoint CreateEndpoint(string template)
        {
            return new RouteEndpoint(
                TestConstants.EmptyRequestDelegate,
                RoutePatternFactory.Parse(template),
                0,
                EndpointMetadataCollection.Empty,
                "test");
        }

        private static DfaMatcherBuilder CreateDfaMatcherBuilder(params MatcherPolicy[] policies)
        {
            var dataSource = new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>());
            return new DfaMatcherBuilder(
                NullLoggerFactory.Instance,
                Mock.Of<ParameterPolicyFactory>(),
                Mock.Of<EndpointSelector>(),
                policies);
        }
    }
}
