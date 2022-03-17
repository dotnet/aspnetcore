// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

public class BarebonesMatcherConformanceTest : MatcherConformanceTest
{
    // Route values not supported
    [Fact]
    public override Task Match_SingleParameter()
    {
        return Task.CompletedTask;
    }

    // Route values not supported
    [Fact]
    public override Task Match_SingleParameter_TrailingSlash()
    {
        return Task.CompletedTask;
    }

    // Route values not supported
    [Fact]
    public override Task Match_SingleParameter_WeirdNames()
    {
        return Task.CompletedTask;
    }

    // Route values not supported
    [Theory]
    [InlineData(null, null, null, null)]
    public override Task Match_MultipleParameters(string template, string path, string[] keys, string[] values)
    {
        GC.KeepAlive(new object[] { template, path, keys, values });
        return Task.CompletedTask;
    }

    // Route constraints not supported
    [Fact]
    public override Task Match_Constraint()
    {
        return Task.CompletedTask;
    }

    internal override Matcher CreateMatcher(params RouteEndpoint[] endpoints)
    {
        var builder = new BarebonesMatcherBuilder();
        for (var i = 0; i < endpoints.Length; i++)
        {
            builder.AddEndpoint(endpoints[i]);
        }
        return builder.Build();
    }
}
