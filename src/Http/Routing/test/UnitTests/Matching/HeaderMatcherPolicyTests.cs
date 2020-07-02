// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class HeaderMatcherPolicyTests
    {
        [Fact]
        public void Comparer_SortOrder()
        {
            // Arrange
            var endpoints = new[]
            {
                (0, Endpoint("header", new[] { "abc" }, HeaderValueMatchMode.Exact)),
                (0, Endpoint("header", new[] { "abc", "def" }, HeaderValueMatchMode.Exact)),
                (0, Endpoint("header2", new[] { "abc", "def" }, HeaderValueMatchMode.Exact)),

                (1, Endpoint("header", new[] { "abc" }, HeaderValueMatchMode.Exact, maxValuesToInspect: 2)),

                (2, Endpoint("header", new[] { "abc" }, HeaderValueMatchMode.Exact, maxValuesToInspect: 3)),

                (3, Endpoint("header", new[] { "abc" }, HeaderValueMatchMode.Exact, valueIgnoresCase: true)),

                (4, Endpoint("header", new[] { "abc" }, HeaderValueMatchMode.Exact, valueIgnoresCase: true, maxValuesToInspect: 2)),

                (5, Endpoint("header", new[] { "abc" }, HeaderValueMatchMode.Prefix)),
                (5, Endpoint("header", new[] { "abc", "def" }, HeaderValueMatchMode.Prefix)),
                (5, Endpoint("header2", new[] { "abc", "def" }, HeaderValueMatchMode.Prefix)),

                (6, Endpoint("header", new[] { "abc" }, HeaderValueMatchMode.Prefix, maxValuesToInspect: 2)),

                (7, Endpoint("header", new[] { "abc" }, HeaderValueMatchMode.Prefix, valueIgnoresCase: true)),

                (8, Endpoint("header", new[] { "abc" }, HeaderValueMatchMode.Prefix, valueIgnoresCase: true, maxValuesToInspect: 2)),

                (9, Endpoint("header", new string[0], HeaderValueMatchMode.Exact)),
                (9, Endpoint("header", new string[0], HeaderValueMatchMode.Exact, valueIgnoresCase: true)),
                (9, Endpoint("header", new string[0], HeaderValueMatchMode.Prefix)),
                (9, Endpoint("header", new string[0], HeaderValueMatchMode.Prefix, valueIgnoresCase: true)),
                (9, Endpoint("header", new string[0], maxValuesToInspect: 2)),

                (10, Endpoint(string.Empty, null)),
                (10, Endpoint(null, null)),
            };
            var sut = new HeaderMatcherPolicy();

            // Act
            for (int i = 0; i < endpoints.Length; i++)
            {
                for (int j = 0; j < endpoints.Length; j++)
                {
                    var a = endpoints[i];
                    var b = endpoints[j];

                    var actual = sut.Comparer.Compare(a.Item2, b.Item2);
                    var expected =
                        a.Item1 < b.Item1 ? -1 :
                        a.Item1 > b.Item1 ? 1 : 0;
                    if (actual != expected)
                    {
                        Assert.True(false, $"Error comparing [{i}] to [{j}], expected {expected}, found {actual}.");
                    }
                }
            }
        }

        [Fact]
        public void AppliesToEndpoints_AppliesScenarios()
        {
            // Arrange
            var scenarios = new[]
            {
                Endpoint("org-id", new string[0]),
                Endpoint("org-id", new[] { "abc" }),
                Endpoint("org-id", new[] { "abc", "def" }),
                Endpoint(null, null, isDynamic: true),
                Endpoint(string.Empty, null, isDynamic: true),
                Endpoint("org-id", new string[0], isDynamic: true),
                Endpoint("org-id", new[] { "abc" }, isDynamic: true),
                Endpoint(null, null, isDynamic: true),
            };
            var sut = new HeaderMatcherPolicy();
            var endpointSelectorPolicy = (IEndpointSelectorPolicy)sut;

            // Act
            for (int i = 0; i < scenarios.Length; i++)
            {
                bool result = endpointSelectorPolicy.AppliesToEndpoints(new[] { scenarios[i] });
                Assert.True(result, $"scenario {i}");
            }
        }

        [Fact]
        public void AppliesToEndpoints_DoesNotApplyScenarios()
        {
            // Arrange
            var scenarios = new[]
            {
                Endpoint(null, null),
                Endpoint(string.Empty, null),
                Endpoint(string.Empty, new string[0]),
                Endpoint(string.Empty, new[] { "abc" }),
            };
            var sut = new HeaderMatcherPolicy();
            var endpointSelectorPolicy = (IEndpointSelectorPolicy)sut;

            // Act
            for (int i = 0; i < scenarios.Length; i++)
            {
                bool result = endpointSelectorPolicy.AppliesToEndpoints(new[] { scenarios[i] });
                Assert.False(result, $"scenario {i}");
            }
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", true)]
        [InlineData("abc", true)]
        public async Task ApplyAsync_MatchingScenarios_AnyHeaderValue(string incomingHeaderValue, bool shouldMatch)
        {
            // Arrange
            var context = new DefaultHttpContext();
            if (incomingHeaderValue != null)
            {
                context.Request.Headers.Add("org-id", incomingHeaderValue);
            }

            var endpoint = Endpoint("org-id", new string[0]);
            var candidates = new CandidateSet(new[] { endpoint }, new RouteValueDictionary[1], new int[1]);
            var sut = new HeaderMatcherPolicy();

            // Act
            await sut.ApplyAsync(context, candidates);

            // Assert
            Assert.Equal(shouldMatch, candidates.IsValidCandidate(0));
        }

        [Theory]
        [InlineData("abc", HeaderValueMatchMode.Exact, false, null, false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, false, "", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, false, "abc", true)]
        [InlineData("abc", HeaderValueMatchMode.Exact, false, "aBC", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, false, "abcd", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, false, "ab", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, true, "", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, true, "abc", true)]
        [InlineData("abc", HeaderValueMatchMode.Exact, true, "aBC", true)]
        [InlineData("abc", HeaderValueMatchMode.Exact, true, "abcd", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, true, "ab", false)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, false, "", false)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, false, "abc", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, false, "aBC", false)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, false, "abcd", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, false, "ab", false)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, true, "", false)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, true, "abc", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, true, "aBC", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, true, "abcd", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, true, "aBCd", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, true, "ab", false)]
        public async Task ApplyAsync_MatchingScenarios_OneHeaderValue(
            string headerValue,
            HeaderValueMatchMode headerValueMatchMode,
            bool valueIgnoresCase,
            string incomingHeaderValue,
            bool shouldMatch)
        {
            // Arrange
            var context = new DefaultHttpContext();
            if (incomingHeaderValue != null)
            {
                context.Request.Headers.Add("org-id", incomingHeaderValue);
            }

            var endpoint = Endpoint("org-id", new[] { headerValue }, headerValueMatchMode, valueIgnoresCase);
            var candidates = new CandidateSet(new[] { endpoint }, new RouteValueDictionary[1], new int[1]);
            var sut = new HeaderMatcherPolicy();

            // Act
            await sut.ApplyAsync(context, candidates);

            // Assert
            Assert.Equal(shouldMatch, candidates.IsValidCandidate(0));
        }

        [Theory]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, false, null, false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, false, "", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, false, "abc", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, false, "abcd", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, false, "def", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, false, "defg", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, true, null, false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, true, "", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, true, "abc", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, true, "aBC", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, true, "aBCd", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, true, "def", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, true, "DEFg", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, false, null, false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, false, "", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, false, "abc", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, false, "abcd", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, false, "def", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, false, "defg", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, false, "aabc", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, true, null, false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, true, "", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, true, "abc", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, true, "aBC", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, true, "aBCd", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, true, "def", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, true, "DEFg", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, true, "aabc", false)]
        public async Task ApplyAsync_MatchingScenarios_TwoHeaderValues(
            string header1Value,
            string header2Value,
            HeaderValueMatchMode headerValueMatchMode,
            bool valueIgnoresCase,
            string incomingHeaderValue,
            bool shouldMatch)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add("org-id", incomingHeaderValue);
            var endpoint = Endpoint("org-id", new[] { header1Value, header2Value }, headerValueMatchMode, valueIgnoresCase);

            var candidates = new CandidateSet(new[] { endpoint }, new RouteValueDictionary[1], new int[1]);
            var sut = new HeaderMatcherPolicy();

            // Act
            await sut.ApplyAsync(context, candidates);

            // Assert
            Assert.Equal(shouldMatch, candidates.IsValidCandidate(0));
        }

        [Fact]
        public async Task ApplyAsync_RespectsMaxHeadersToInspect()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add("org-id", new[] { "abc1", "abc2", "abc3" });
            var endpoint1 = Endpoint("org-id", new[] { "abc1" }, maxValuesToInspect: 2);
            var endpoint2 = Endpoint("org-id", new[] { "abc2" }, maxValuesToInspect: 2);
            var endpoint3 = Endpoint("org-id", new[] { "abc3" }, maxValuesToInspect: 2);
            var endpoint4 = Endpoint("org-id", new[] { "abc3" }, maxValuesToInspect: 3);

            var candidates = new CandidateSet(new[] { endpoint1, endpoint2, endpoint3, endpoint4 }, new RouteValueDictionary[4], new int[4]);
            var sut = new HeaderMatcherPolicy();

            // Act
            await sut.ApplyAsync(context, candidates);

            // Assert
            Assert.True(candidates.IsValidCandidate(0));
            Assert.True(candidates.IsValidCandidate(1));
            Assert.False(candidates.IsValidCandidate(2));
            Assert.True(candidates.IsValidCandidate(3));
        }

        private static Endpoint Endpoint(
            string headerName,
            string[] headerValues,
            HeaderValueMatchMode headerValueMatchMode = HeaderValueMatchMode.Exact,
            bool valueIgnoresCase = false,
            int maxValuesToInspect = 1,
            bool isDynamic = false)
        {
            var builder = new RouteEndpointBuilder(_ => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
            var metadata = new Mock<IHeaderMetadata>();
            metadata.SetupGet(m => m.HeaderName).Returns(headerName);
            metadata.SetupGet(m => m.HeaderValues).Returns(headerValues);
            metadata.SetupGet(m => m.ValueMatchMode).Returns(headerValueMatchMode);
            metadata.SetupGet(m => m.ValueIgnoresCase).Returns(valueIgnoresCase);
            metadata.SetupGet(m => m.MaximumValuesToInspect).Returns(maxValuesToInspect);

            builder.Metadata.Add(metadata.Object);
            if (isDynamic)
            {
                builder.Metadata.Add(new DynamicEndpointMetadata());
            }

            return builder.Build();
        }

        private class DynamicEndpointMetadata : IDynamicEndpointMetadata
        {
            public bool IsDynamic => true;
        }
    }
}
