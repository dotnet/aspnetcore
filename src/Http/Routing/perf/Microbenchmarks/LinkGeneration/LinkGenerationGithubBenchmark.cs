// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.LinkGeneration;

public partial class LinkGenerationGithubBenchmark
{
    private LinkGenerator _linkGenerator;
    private TreeRouter _treeRouter;
    private (HttpContext HttpContext, RouteValueDictionary AmbientValues) _requestContext;
    private RouteValueDictionary _lookUpValues;

    [GlobalSetup]
    public void Setup()
    {
        SetupEndpoints();

        var services = CreateServices();
        _linkGenerator = services.GetRequiredService<LinkGenerator>();

        // Attribute routing related
        var treeRouteBuilder = services.GetRequiredService<TreeRouteBuilder>();
        foreach (var endpoint in Endpoints)
        {
            CreateOutboundRouteEntry(treeRouteBuilder, endpoint);
        }
        _treeRouter = treeRouteBuilder.Build();

        _requestContext = CreateCurrentRequestContext();

        // Get the endpoint to test and pre-populate the lookup values with the defaults
        // (as they are dynamically generated) and update with other required parameter values.
        // /repos/{owner}/{repo}/issues/comments/{commentId}
        var endpointToTest = Endpoints[176];
        _lookUpValues = new RouteValueDictionary(endpointToTest.RoutePattern.Defaults);
        _lookUpValues["owner"] = "aspnet";
        _lookUpValues["repo"] = "routing";
        _lookUpValues["commentId"] = "20202";
    }

    [Benchmark(Baseline = true)]
    public void Baseline()
    {
        var url = $"/repos/{_lookUpValues["owner"]}/{_lookUpValues["repo"]}/issues/comments/{_lookUpValues["commentId"]}";
        AssertUrl("/repos/aspnet/routing/issues/comments/20202", url);
    }

    [Benchmark]
    public void TreeRouter()
    {
        var virtualPathData = _treeRouter.GetVirtualPath(new VirtualPathContext(
            _requestContext.HttpContext,
            ambientValues: _requestContext.AmbientValues,
            values: new RouteValueDictionary(_lookUpValues)));

        AssertUrl("/repos/aspnet/routing/issues/comments/20202", virtualPathData?.VirtualPath);
    }

    [Benchmark]
    public void EndpointRouting()
    {
        var actualUrl = _linkGenerator.GetPathByRouteValues(
            _requestContext.HttpContext,
            routeName: null,
            values: _lookUpValues);

        AssertUrl("/repos/aspnet/routing/issues/comments/20202", actualUrl);
    }
}
