// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public abstract class MatcherConformanceTest
    {
        internal abstract Matcher CreateMatcher(MatcherEndpoint endpoint);

        [Fact]
        public virtual async Task Match_SingleLiteralSegment_Success()
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher("/simple");
            var (httpContext, feature) = CreateContext("/simple");

            // Act
            await matcher.MatchAsync(httpContext, feature);

            // Assert
            DispatcherAssert.AssertMatch(feature, endpoint);
        }

        [Theory]
        [InlineData("simple")]
        [InlineData("/simple")]
        [InlineData("~/simple")]
        public virtual async Task Match_Sanitizies_TemplatePrefix(string template)
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher(template);
            var (httpContext, feature) = CreateContext("/simple");

            // Act
            await matcher.MatchAsync(httpContext, feature);

            // Assert
            DispatcherAssert.AssertMatch(feature, endpoint);
        }

        internal static (HttpContext httpContext, IEndpointFeature feature) CreateContext(string path)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "TEST";
            httpContext.Request.Path = path;
            httpContext.RequestServices = CreateServices();

            var feature = new EndpointFeature();
            httpContext.Features.Set<IEndpointFeature>(feature);

            return (httpContext, feature);
        }

        // The older routing implementations retrieve services when they first execute.
        internal static IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            return services.BuildServiceProvider();
        }

        internal static MatcherEndpoint CreateEndpoint(string template)
        {
            return new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                template,
                null,
                0,
                EndpointMetadataCollection.Empty, 
                "endpoint: " + template,
                address: null);
        }

        internal (Matcher matcher, MatcherEndpoint endpoint) CreateMatcher(string template)
        {
            var endpoint = CreateEndpoint(template);
            return (CreateMatcher(endpoint), endpoint);
        }
    }
}
