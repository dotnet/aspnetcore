// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Matching;

public class DfaMatcherConformanceTest : FullFeaturedMatcherConformanceTest
{
    // See the comments in the base class. DfaMatcher fixes a long-standing bug
    // with catchall parameters and empty segments.
    public override async Task Quirks_CatchAllParameter(string template, string path, string[] keys, string[] values)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, endpoint, keys, values);
    }

    // https://github.com/dotnet/aspnetcore/issues/18677
    [Theory]
    [InlineData("/middleware", 1)]
    [InlineData("/middleware/test", 1)]
    [InlineData("/middleware/test1/test2", 1)]
    [InlineData("/bill/boga", 0)]
    public async Task Match_Regression_1867_CorrectBehavior(string path, int endpointIndex)
    {
        var endpoints = new RouteEndpoint[]
        {
                EndpointFactory.CreateRouteEndpoint(
                    "{firstName}/{lastName}",
                    order: 0,
                    defaults: new { controller = "TestRoute", action = "Index", }),

                EndpointFactory.CreateRouteEndpoint(
                    "middleware/{**_}",
                    order: 0),
        };

        var expected = endpoints[endpointIndex];

        var matcher = CreateMatcherCore(endpoints);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, expected, ignoreValues: true);
    }

    internal override Matcher CreateMatcher(params RouteEndpoint[] endpoints)
    {
        return CreateMatcherCore(endpoints);
    }

    internal Matcher CreateMatcherCore(params RouteEndpoint[] endpoints)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddOptions()
            .AddRouting()
            .BuildServiceProvider();

        var builder = services.GetRequiredService<DfaMatcherBuilder>();

        for (var i = 0; i < endpoints.Length; i++)
        {
            builder.AddEndpoint(endpoints[i]);
        }
        return builder.Build();
    }
}
