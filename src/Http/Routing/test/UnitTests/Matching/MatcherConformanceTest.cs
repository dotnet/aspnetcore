// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Matching;

public abstract partial class MatcherConformanceTest
{
    internal abstract Matcher CreateMatcher(params RouteEndpoint[] endpoints);

    internal static HttpContext CreateContext(string path)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "TEST";
        httpContext.Request.Path = path;
        httpContext.RequestServices = CreateServices();
        return httpContext;
    }

    // The older routing implementations retrieve services when they first execute.
    internal static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    internal static RouteEndpoint CreateEndpoint(
        string template,
        object defaults = null,
        object constraints = null,
        int? order = null)
    {
        return new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse(template, defaults, constraints),
            order ?? 0,
            EndpointMetadataCollection.Empty,
            "endpoint: " + template);
    }

    internal (Matcher matcher, RouteEndpoint endpoint) CreateMatcher(string template)
    {
        var endpoint = CreateEndpoint(template);
        return (CreateMatcher(endpoint), endpoint);
    }
}
