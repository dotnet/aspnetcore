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
    // End-to-end tests for the host matching functionality
    public abstract class HostMatcherPolicyIntegrationTestBase
    {
        protected abstract bool HasDynamicMetadata { get; }

        [Fact]
        public async Task Match_Host()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "contoso.com", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HostWithPort()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "contoso.com:8080", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com:8080");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_Host_Unicode()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "æon.contoso.com", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "æon.contoso.com");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HostWithPort_IncorrectPort()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "contoso.com:8080", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com:1111");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Fact]
        public async Task Match_HostWithPort_IncorrectHost()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "contoso.com:8080", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "www.contoso.com:8080");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Fact]
        public async Task Match_HostWithWildcard_Unicode()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "*.contoso.com:8080", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "æon.contoso.com:8080");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HostWithWildcard_NoSubdomain()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "*.contoso.com:8080", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com:8080");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Fact]
        public async Task Match_HostWithWildcard_Subdomain()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "*.contoso.com:8080", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "www.contoso.com:8080");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HostWithWildcard_MultipleSubdomains()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "*.contoso.com:8080", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "www.blog.contoso.com:8080");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HostWithWildcard_PrefixNotInSubdomain()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "*.contoso.com:8080", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "mycontoso.com:8080");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Fact]
        public async Task Match_HostAndHostWithWildcard_NoSubdomain()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "contoso.com:8080", "*.contoso.com:8080", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com:8080");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_Host_CaseInsensitive()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "Contoso.COM", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HostWithPort_InferHttpPort()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "contoso.com:80", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com", "http");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HostWithPort_InferHttpsPort()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "contoso.com:443", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com", "https");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HostWithPort_NoHostHeader()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "contoso.com:443", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", null, "https");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Fact]
        public async Task Match_Port_NoHostHeader_InferHttpsPort()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "*:443", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", null, "https");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_NoMetadata_MatchesAnyHost()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello");

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_EmptyHostList_MatchesAnyHost()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_WildcardHost_MatchesAnyHost()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "*", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_WildcardHostAndWildcardPort_MatchesAnyHost()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", hosts: new string[] { "*:*", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "contoso.com");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
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

        internal static HttpContext CreateContext(
            string path,
            string host,
            string scheme = null)
        {
            var httpContext = new DefaultHttpContext();
            if (host != null)
            {
                httpContext.Request.Host = new HostString(host);
            }
            httpContext.Request.Path = path;
            httpContext.Request.Scheme = scheme;

            return httpContext;
        }

        internal RouteEndpoint CreateEndpoint(
            string template,
            object defaults = null,
            object constraints = null,
            int order = 0,
            string[] hosts = null)
        {
            var metadata = new List<object>();
            if (hosts != null)
            {
                metadata.Add(new HostAttribute(hosts ?? Array.Empty<string>()));
            }

            if (HasDynamicMetadata)
            {
                metadata.Add(new DynamicEndpointMetadata());
            }

            var displayName = "endpoint: " + template + " " + string.Join(", ", hosts ?? new[] { "*:*" });
            return new RouteEndpoint(
                TestConstants.EmptyRequestDelegate,
                RoutePatternFactory.Parse(template, defaults, constraints),
                order,
                new EndpointMetadataCollection(metadata),
                displayName);
        }

        internal (Matcher matcher, RouteEndpoint endpoint) CreateMatcher(string template)
        {
            var endpoint = CreateEndpoint(template);
            return (CreateMatcher(endpoint), endpoint);
        }

        private class DynamicEndpointMetadata : IDynamicEndpointMetadata
        {
            public bool IsDynamic => true;
        }
    }
}
