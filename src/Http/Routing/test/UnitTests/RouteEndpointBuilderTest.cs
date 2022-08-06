// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing;

public class RouteEndpointBuilderTest
{
    [Fact]
    public void Build_AllValuesSet_EndpointCreated()
    {
        const int defaultOrder = 0;
        var metadata = new object();
        RequestDelegate requestDelegate = (d) => null;

        var builder = new RouteEndpointBuilder(requestDelegate, RoutePatternFactory.Parse("/"), defaultOrder)
        {
            DisplayName = "Display name!",
            Metadata = { metadata }
        };

        var endpoint = Assert.IsType<RouteEndpoint>(builder.Build());
        Assert.Equal("Display name!", endpoint.DisplayName);
        Assert.Equal(defaultOrder, endpoint.Order);
        Assert.Equal(requestDelegate, endpoint.RequestDelegate);
        Assert.Equal("/", endpoint.RoutePattern.RawText);
        Assert.Equal(metadata, Assert.Single(endpoint.Metadata));
    }

    [Fact]
    public async void Build_DoesNot_RunFilters()
    {
        var endpointFilterCallCount = 0;
        var invocationFilterCallCount = 0;
        var invocationCallCount = 0;

        const int defaultOrder = 0;
        RequestDelegate requestDelegate = (d) =>
        {
            invocationCallCount++;
            return Task.CompletedTask;
        };

        var builder = new RouteEndpointBuilder(requestDelegate, RoutePatternFactory.Parse("/"), defaultOrder);

        builder.EndpointFilterFactories = new List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>();
        builder.EndpointFilterFactories.Add((endopintContext, next) =>
        {
            endpointFilterCallCount++;

            return invocationContext =>
            {
                invocationFilterCallCount++;

                return next(invocationContext);
            };
        });

        var endpoint = Assert.IsType<RouteEndpoint>(builder.Build());

        await endpoint.RequestDelegate(new DefaultHttpContext());

        Assert.Equal(0, endpointFilterCallCount);
        Assert.Equal(0, invocationFilterCallCount);
        Assert.Equal(1, invocationCallCount);
    }
}
