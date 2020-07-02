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
        private readonly HeaderMatcherPolicyOptions options;
        private readonly HeaderMatcherPolicy sut;

        public HeaderMatcherPolicyTests()
        {
            this.options = new HeaderMatcherPolicyOptions();
            var optionMonitorsMock = new Mock<IOptionsMonitor<HeaderMatcherPolicyOptions>>();
            optionMonitorsMock.SetupGet(o => o.CurrentValue).Returns(this.options);
            this.sut = new HeaderMatcherPolicy(optionMonitorsMock.Object);
        }

        [Fact]
        public void Comparer_DifferentSpecificities()
        {
            // Arrange
            var scenarios = new[]
            {
                Tuple.Create(Endpoint("org-id", new string[0]), Endpoint(null, null)),
                Tuple.Create(Endpoint("org-id", new[] { "abc" }), Endpoint(null, null)),
                Tuple.Create(Endpoint("org-id", new[] { "abc" }), Endpoint(string.Empty, null)),
                Tuple.Create(Endpoint("org-id", new[] { "abc" }), Endpoint("org-id", new string[0])),
            };

            // Act
            for (int i = 0; i < scenarios.Length; i++)
            {
                int result1 = this.sut.Comparer.Compare(scenarios[i].Item1, scenarios[i].Item2);
                int result2 = this.sut.Comparer.Compare(scenarios[i].Item2, scenarios[i].Item1);
                Assert.Equal(-1, result1);
                Assert.Equal(1, result2);
            }
        }

        [Fact]
        public void Comparer_SameSpecificities()
        {
            // Arrange
            var scenarios = new[]
            {
                Tuple.Create(Endpoint(null, null), Endpoint(null, null)),
                Tuple.Create(Endpoint(string.Empty, null), Endpoint(null, null)),
                Tuple.Create(Endpoint("org-id", null), Endpoint("tenant-id", null)),
                Tuple.Create(Endpoint("org-id", null), Endpoint("tenant-id", new string[0])),
                Tuple.Create(Endpoint("org-id", new string[0]), Endpoint("tenant-id", new string[0])),
                Tuple.Create(Endpoint("org-id", new[] { "abc" }), Endpoint("tenant-id", new[] { "abc", "def" })),
            };

            // Act
            for (int i = 0; i < scenarios.Length; i++)
            {
                int result1 = this.sut.Comparer.Compare(scenarios[i].Item1, scenarios[i].Item2);
                int result2 = this.sut.Comparer.Compare(scenarios[i].Item2, scenarios[i].Item1);
                Assert.Equal(0, result1);
                Assert.Equal(0, result2);
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
            var endpointSelectorPolicy = (IEndpointSelectorPolicy)this.sut;

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
            var endpointSelectorPolicy = (IEndpointSelectorPolicy)this.sut;

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

            // Act
            await this.sut.ApplyAsync(context, candidates);

            // Assert
            Assert.Equal(shouldMatch, candidates.IsValidCandidate(0));
        }

        [Theory]
        [InlineData("abc", HeaderValueMatchMode.Exact, StringComparison.Ordinal, null, false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, StringComparison.Ordinal, "", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, StringComparison.Ordinal, "abc", true)]
        [InlineData("abc", HeaderValueMatchMode.Exact, StringComparison.Ordinal, "aBC", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, StringComparison.Ordinal, "abcd", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, StringComparison.Ordinal, "ab", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, "", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, "abc", true)]
        [InlineData("abc", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, "aBC", true)]
        [InlineData("abc", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, "abcd", false)]
        [InlineData("abc", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, "ab", false)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, "", false)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, "abc", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, "aBC", false)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, "abcd", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, "ab", false)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "", false)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "abc", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "aBC", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "abcd", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "aBCd", true)]
        [InlineData("abc", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "ab", false)]
        public async Task ApplyAsync_MatchingScenarios_OneHeaderValue(
            string headerValue,
            HeaderValueMatchMode headerValueMatchMode,
            StringComparison headerValueStringComparison,
            string incomingHeaderValue,
            bool shouldMatch)
        {
            // Arrange
            var context = new DefaultHttpContext();
            if (incomingHeaderValue != null)
            {
                context.Request.Headers.Add("org-id", incomingHeaderValue);
            }

            var endpoint = Endpoint("org-id", new[] { headerValue }, headerValueMatchMode, headerValueStringComparison);
            var candidates = new CandidateSet(new[] { endpoint }, new RouteValueDictionary[1], new int[1]);

            // Act
            await this.sut.ApplyAsync(context, candidates);

            // Assert
            Assert.Equal(shouldMatch, candidates.IsValidCandidate(0));
        }

        [Theory]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.Ordinal, null, false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.Ordinal, "", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.Ordinal, "abc", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.Ordinal, "abcd", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.Ordinal, "def", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.Ordinal, "defg", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, null, false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, "", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, "abc", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, "aBC", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, "aBCd", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, "def", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Exact, StringComparison.OrdinalIgnoreCase, "DEFg", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, null, false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, "", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, "abc", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, "abcd", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, "def", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, "defg", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.Ordinal, "aabc", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, null, false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "", false)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "abc", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "aBC", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "aBCd", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "def", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "DEFg", true)]
        [InlineData("abc", "def", HeaderValueMatchMode.Prefix, StringComparison.OrdinalIgnoreCase, "aabc", false)]
        public async Task ApplyAsync_MatchingScenarios_TwoHeaderValues(
            string header1Value,
            string header2Value,
            HeaderValueMatchMode headerValueMatchMode,
            StringComparison headerValueStringComparison,
            string incomingHeaderValue,
            bool shouldMatch)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add("org-id", incomingHeaderValue);
            var endpoint = Endpoint("org-id", new[] { header1Value, header2Value }, headerValueMatchMode, headerValueStringComparison);

            var candidates = new CandidateSet(new[] { endpoint }, new RouteValueDictionary[1], new int[1]);

            // Act
            await this.sut.ApplyAsync(context, candidates);

            // Assert
            Assert.Equal(shouldMatch, candidates.IsValidCandidate(0));
        }

        [Fact]
        public async Task ApplyAsync_RespectsMaxHeadersToInspect()
        {
            // Arrange
            this.options.MaximumRequestHeaderValuesToInspect = 2;
            var context = new DefaultHttpContext();
            context.Request.Headers.Add("org-id", new[] { "abc1", "abc2", "abc3" });
            var endpoint1 = Endpoint("org-id", new[] { "abc1" });
            var endpoint2 = Endpoint("org-id", new[] { "abc2" });
            var endpoint3 = Endpoint("org-id", new[] { "abc3" });

            var candidates = new CandidateSet(new[] { endpoint1, endpoint2, endpoint3 }, new RouteValueDictionary[3], new int[3]);

            // Act
            await this.sut.ApplyAsync(context, candidates);

            // Assert
            Assert.True(candidates.IsValidCandidate(0));
            Assert.True(candidates.IsValidCandidate(1));
            Assert.False(candidates.IsValidCandidate(2));
        }

        private static Endpoint Endpoint(
            string headerName,
            string[] headerValues,
            HeaderValueMatchMode headerValueMatchMode = HeaderValueMatchMode.Exact,
            StringComparison headerValueStringComparison = StringComparison.Ordinal,
            bool isDynamic = false)
        {
            var builder = new RouteEndpointBuilder(_ => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
            var metadata = new Mock<IHeaderMetadata>();
            metadata.SetupGet(m => m.HeaderName).Returns(headerName);
            metadata.SetupGet(m => m.HeaderValues).Returns(headerValues);
            metadata.SetupGet(m => m.HeaderValueMatchMode).Returns(headerValueMatchMode);
            metadata.SetupGet(m => m.HeaderValueStringComparison).Returns(headerValueStringComparison);

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
