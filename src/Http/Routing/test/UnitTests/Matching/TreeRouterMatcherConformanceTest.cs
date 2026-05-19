// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

public class TreeRouterMatcherConformanceTest : FullFeaturedMatcherConformanceTest
{
    // TreeRouter doesn't support non-inline default values.
    [Fact]
    public override Task Match_NonInlineDefaultValues()
    {
        return Task.CompletedTask;
    }

    // TreeRouter doesn't support non-inline default values.
    [Fact]
    public override Task Match_ExtraDefaultValues()
    {
        return Task.CompletedTask;
    }

    // https://github.com/dotnet/aspnetcore/issues/18677
    //
    [Theory]
    [InlineData("/middleware", 1)]
    [InlineData("/middleware/test", 1)]
    [InlineData("/middleware/test1/test2", 1)]
    [InlineData("/bill/boga", 0)]
    public async Task Match_Regression_1867(string path, int endpointIndex)
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

        var matcher = CreateMatcher(endpoints);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, expected, ignoreValues: true);
    }

    internal override Matcher CreateMatcher(params RouteEndpoint[] endpoints)
    {
        var builder = new TreeRouterMatcherBuilder();
        for (var i = 0; i < endpoints.Length; i++)
        {
            builder.AddEndpoint(endpoints[i]);
        }
        return builder.Build();
    }
}
