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
using static Microsoft.AspNetCore.Routing.Matching.HttpMethodMatcherPolicy;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // End-to-end tests for the HTTP method matching functionality
    public abstract class HttpMethodMatcherPolicyIntegrationTestBase
    {
        protected abstract bool HasDynamicMetadata { get; }

        [Fact]
        public async Task Match_HttpMethod()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", httpMethods: new string[] { "GET", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "GET");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HttpMethod_CORS()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", httpMethods: new string[] { "GET", }, acceptCorsPreflight: true);

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "GET");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HttpMethod_CORS_Preflight()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", httpMethods: new string[] { "GET", }, acceptCorsPreflight: true);

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "GET", corsPreflight: true);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }


        [Fact] // Nothing here supports OPTIONS, so it goes to a 405.
        public async Task NotMatch_HttpMethod_CORS_Preflight()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", httpMethods: new string[] { "GET", }, acceptCorsPreflight: false);

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "GET", corsPreflight: true);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            Assert.NotSame(endpoint, httpContext.GetEndpoint());
            Assert.Same(HttpMethodMatcherPolicy.Http405EndpointDisplayName, httpContext.GetEndpoint().DisplayName);
        }

        [Fact]
        public async Task Match_HttpMethod_CaseInsensitive()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", httpMethods: new string[] { "GeT", });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "GET");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_HttpMethod_CaseInsensitive_CORS_Preflight()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", httpMethods: new string[] { "GeT", }, acceptCorsPreflight: true);

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "GET", corsPreflight: true);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_NoMetadata_MatchesAnyHttpMethod()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello");

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "GET");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_NoMetadata_MatchesAnyHttpMethod_CORS_Preflight()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", acceptCorsPreflight: true);

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "GET", corsPreflight: true);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact] // This matches because the endpoint accepts OPTIONS
        public async Task Match_NoMetadata_MatchesAnyHttpMethod_CORS_Preflight_DoesNotSupportPreflight()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", acceptCorsPreflight: false);

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "GET", corsPreflight: true);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public async Task Match_EmptyMethodList_MatchesAnyHttpMethod()
        {
            // Arrange
            var endpoint = CreateEndpoint("/hello", httpMethods: new string[] { });

            var matcher = CreateMatcher(endpoint);
            var httpContext = CreateContext("/hello", "GET");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact] // When all of the candidates handles specific verbs, use a 405 endpoint
        public async Task NotMatch_HttpMethod_Returns405Endpoint()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("/hello", httpMethods: new string[] { "GET", "PUT" });
            var endpoint2 = CreateEndpoint("/hello", httpMethods: new string[] { "DELETE" });

            var matcher = CreateMatcher(endpoint1, endpoint2);
            var httpContext = CreateContext("/hello", "POST");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            Assert.NotSame(endpoint1, httpContext.GetEndpoint());
            Assert.NotSame(endpoint2, httpContext.GetEndpoint());

            Assert.Same(HttpMethodMatcherPolicy.Http405EndpointDisplayName, httpContext.GetEndpoint().DisplayName);

            // Invoke the endpoint
            await httpContext.GetEndpoint().RequestDelegate(httpContext);
            Assert.Equal(405, httpContext.Response.StatusCode);
            Assert.Equal("DELETE, GET, PUT", httpContext.Response.Headers["Allow"]);
        }

        [Fact]
        public async Task NotMatch_HttpMethod_CORS_DoesNotReturn405()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("/hello", httpMethods: new string[] { "GET", "PUT" }, acceptCorsPreflight: true);
            var endpoint2 = CreateEndpoint("/hello", httpMethods: new string[] { "DELETE" });

            var matcher = CreateMatcher(endpoint1, endpoint2);
            var httpContext = CreateContext("/hello", "POST", corsPreflight: true);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Fact] // When one of the candidates handles all verbs, dont use a 405 endpoint
        public async Task NotMatch_HttpMethod_WithAllMethodEndpoint_DoesNotReturn405()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("/{x:int}", httpMethods: new string[] { });
            var endpoint2 = CreateEndpoint("/hello", httpMethods: new string[] { "DELETE" });

            var matcher = CreateMatcher(endpoint1, endpoint2);
            var httpContext = CreateContext("/hello", "POST");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Fact]
        public async Task Match_EndpointWithHttpMethodPreferred()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("/hello", httpMethods: new string[] { "GET", });
            var endpoint2 = CreateEndpoint("/bar");

            var matcher = CreateMatcher(endpoint1, endpoint2);
            var httpContext = CreateContext("/hello", "GET");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint1);
        }

        [Fact]
        public async Task Match_EndpointWithHttpMethodPreferred_EmptyList()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("/hello", httpMethods: new string[] { "GET", });
            var endpoint2 = CreateEndpoint("/bar", httpMethods: new string[] { });

            var matcher = CreateMatcher(endpoint1, endpoint2);
            var httpContext = CreateContext("/hello", "GET");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint1);
        }

        [Fact] // The non-http-method-specific endpoint is part of the same candidate set
        public async Task Match_EndpointWithHttpMethodPreferred_FallsBackToNonSpecific()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("/{x}", httpMethods: new string[] { "GET", });
            var endpoint2 = CreateEndpoint("/{x}", httpMethods: new string[] { });

            var matcher = CreateMatcher(endpoint1, endpoint2);
            var httpContext = CreateContext("/hello", "POST");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint2, ignoreValues: true);
        }

        [Fact] // See https://github.com/dotnet/aspnetcore/issues/6415
        public async Task NotMatch_HttpMethod_Returns405Endpoint_ReExecute()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("/hello", httpMethods: new string[] { "GET", "PUT" });
            var endpoint2 = CreateEndpoint("/hello", httpMethods: new string[] { "DELETE" });

            var matcher = CreateMatcher(endpoint1, endpoint2);
            var httpContext = CreateContext("/hello", "POST");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            Assert.NotSame(endpoint1, httpContext.GetEndpoint());
            Assert.NotSame(endpoint2, httpContext.GetEndpoint());

            Assert.Same(HttpMethodMatcherPolicy.Http405EndpointDisplayName, httpContext.GetEndpoint().DisplayName);

            // Invoke the endpoint
            await httpContext.GetEndpoint().RequestDelegate(httpContext);
            Assert.Equal(405, httpContext.Response.StatusCode);
            Assert.Equal("DELETE, GET, PUT", httpContext.Response.Headers["Allow"]);

            // Invoke the endpoint again to verify headers not duplicated
            await httpContext.GetEndpoint().RequestDelegate(httpContext);
            Assert.Equal(405, httpContext.Response.StatusCode);
            Assert.Equal("DELETE, GET, PUT", httpContext.Response.Headers["Allow"]);
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
            string httpMethod,
            bool corsPreflight = false)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = corsPreflight ? PreflightHttpMethod : httpMethod;
            httpContext.Request.Path = path;

            if (corsPreflight)
            {
                httpContext.Request.Headers[OriginHeader] = "example.com";
                httpContext.Request.Headers[AccessControlRequestMethod] = httpMethod;
            }

            return httpContext;
        }

        internal RouteEndpoint CreateEndpoint(
            string template,
            object defaults = null,
            object constraints = null,
            int order = 0,
            string[] httpMethods = null,
            bool acceptCorsPreflight = false)
        {
            var metadata = new List<object>();
            if (httpMethods != null)
            {
                metadata.Add(new HttpMethodMetadata(httpMethods ?? Array.Empty<string>(), acceptCorsPreflight));
            }

            if (HasDynamicMetadata)
            {
                metadata.Add(new DynamicEndpointMetadata());
            }

            var displayName = "endpoint: " + template + " " + string.Join(", ", httpMethods ?? new[] { "(any)" });
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
