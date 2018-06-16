// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public abstract partial class MatcherConformanceTest
    {
        internal abstract Matcher CreateMatcher(params MatcherEndpoint[] endpoints);

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

        internal static MatcherEndpoint CreateEndpoint(
            string template, 
            object defaults = null,
            int? order = null)
        {
            return new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                template,
                defaults,
                order ?? 0,
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
