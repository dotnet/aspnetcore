// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.LinkGeneration;

public class SingleRouteWithNoParametersBenchmark : EndpointRoutingBenchmarkBase
{
    private TreeRouter _treeRouter;
    private LinkGenerator _linkGenerator;
    private (HttpContext HttpContext, RouteValueDictionary AmbientValues) _requestContext;

    [GlobalSetup]
    public void Setup()
    {
        var template = "Products/Details";
        var defaults = new { controller = "Products", action = "Details" };
        var requiredValues = new { controller = "Products", action = "Details" };

        // Endpoint routing related
        SetupEndpoints(CreateEndpoint(template, defaults, requiredValues: requiredValues));
        var services = CreateServices();
        _linkGenerator = services.GetRequiredService<LinkGenerator>();

        // Attribute routing related
        var treeRouteBuilder = services.GetRequiredService<TreeRouteBuilder>();
        CreateOutboundRouteEntry(treeRouteBuilder, Endpoints[0]);
        _treeRouter = treeRouteBuilder.Build();

        _requestContext = CreateCurrentRequestContext();
    }

    [Benchmark(Baseline = true)]
    public void TreeRouter()
    {
        var virtualPathData = _treeRouter.GetVirtualPath(new VirtualPathContext(
            _requestContext.HttpContext,
            ambientValues: _requestContext.AmbientValues,
            values: new RouteValueDictionary(
                new
                {
                    controller = "Products",
                    action = "Details",
                })));

        AssertUrl("/Products/Details", virtualPathData?.VirtualPath);
    }

    [Benchmark]
    public void EndpointRouting()
    {
        var actualUrl = _linkGenerator.GetPathByRouteValues(
            _requestContext.HttpContext,
            routeName: null,
            values: new
            {
                controller = "Products",
                action = "Details",
            });

        AssertUrl("/Products/Details", actualUrl);
    }
}
