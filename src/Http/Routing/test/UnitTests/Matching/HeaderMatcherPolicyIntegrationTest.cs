// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // End-to-end tests for the header matching functionality
    public class HeaderMatcherPolicyIntegrationTest
    {
        [Fact]
        public async Task Match_NoMetadata_MatchesAlways()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello");

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Theory]
        [InlineData("")]
        [InlineData("value")]
        public async Task Match_HeaderPresence_MatchesWhenPresent(string headerValue)
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header"));

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", new[] { KeyValuePair.Create("some-header", headerValue) });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HeaderPresence_MatchesWhenPresentCaseInsensitive()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header"));

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", new[] { KeyValuePair.Create("some-HEADER", string.Empty) });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HeaderPresence_DoesNotMatchWhenAbsent()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header"));

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", new[] { KeyValuePair.Create("another", string.Empty) });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Fact]
        public async Task Match_SingleValue_Matches()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header", "abc"));

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", new[] { KeyValuePair.Create("some-header", "abc") });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Theory]
        [InlineData("ABC")]
        [InlineData("abc ")]
        [InlineData("abcdef")]
        public async Task Match_SingleValue_NoMatch(string requestHeaderValue)
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header", "abc"));

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", new[] { KeyValuePair.Create("some-header", requestHeaderValue) });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Fact]
        public async Task Match_SingleValue_IgnoreCase()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header", "abc") { ValueIgnoresCase = true });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", new[] { KeyValuePair.Create("some-header", "aBC") });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("abcdef")]
        public async Task Match_SingleValue_Prefix(string requestHeaderValue)
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header", "abc") { ValueMatchMode = HeaderValueMatchMode.Prefix });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", new[] { KeyValuePair.Create("some-header", requestHeaderValue) });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Theory]
        [InlineData("ab-c")]
        [InlineData("-abc")]
        public async Task Match_SingleValue_Prefix_NoMatch(string requestHeaderValue)
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header", "abc") { ValueMatchMode = HeaderValueMatchMode.Prefix });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", new[] { KeyValuePair.Create("some-header", requestHeaderValue) });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("def")]
        public async Task Match_MultiValues_Matches(string requestHeaderValue)
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header", "abc", "def"));

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", new[] { KeyValuePair.Create("some-header", requestHeaderValue) });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Theory]
        [InlineData("ABC")]
        [InlineData("abc ")]
        [InlineData("abcdef")]
        public async Task Match_MultiValues_NoMatch(string requestHeaderValue)
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header", "abc", "def"));

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", new[] { KeyValuePair.Create("some-header", requestHeaderValue) });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Fact]
        public async Task Match_Specificity_PicksMoreSpecific()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header1"));
            var endpoint2 = CreateEndpoint("/hello", headerAttribute: new HeaderAttribute("some-header1", "abc"));

            var matcher = CreateMatcher(endpoint1, endpoint2);
            var httpContext = CreateContext("/hello", new[] { KeyValuePair.Create("some-header1", "abc") });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint2);
        }

        private static Matcher CreateMatcher(params RouteEndpoint[] endpoints)
        {
            var services = new ServiceCollection()
                .AddOptions()
                .AddLogging()
                .AddRouting()
                .BuildServiceProvider();

            var builder = services.GetRequiredService<DfaMatcherBuilder>();
            for (var i = 0; i < endpoints.Length; i++)
            {
                builder.AddEndpoint(endpoints[i]);
            }

            return builder.Build();
        }

        private static HttpContext CreateContext(
            string path,
            IEnumerable<KeyValuePair<string,string>> headers = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = path;
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    httpContext.Request.Headers.Add(header.Key, header.Value);
                }
            }

            return httpContext;
        }

        private RouteEndpoint CreateEndpoint(
            string template,
            object defaults = null,
            object constraints = null,
            int order = 0,
            HeaderAttribute headerAttribute = null)
        {
            var metadata = new List<object>();
            if (headerAttribute != null)
            {
                metadata.Add(headerAttribute);
            }

            return new RouteEndpoint(
                TestConstants.EmptyRequestDelegate,
                RoutePatternFactory.Parse(template, defaults, constraints),
                order,
                new EndpointMetadataCollection(metadata),
                "test endpoint");
        }
    }
}
