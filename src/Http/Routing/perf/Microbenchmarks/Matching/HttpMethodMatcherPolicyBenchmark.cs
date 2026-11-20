// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

public class HttpMethodMatcherPolicyBenchmark
{
    private static string[] TestHttpMethods = ["*", HttpMethods.Get, HttpMethods.Connect, HttpMethods.Delete, HttpMethods.Head, HttpMethods.Options, HttpMethods.Patch, HttpMethods.Put, HttpMethods.Post, HttpMethods.Trace, "MERGE"];
    private HttpMethodMatcherPolicy _jumpTableBuilder = new();
    private List<PolicyJumpTableEdge> _edges = new();
    private List<Endpoint> _endpoints = new();

    [Params(3, 5, 11)]
    public int DestinationCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        for (int i = 0; i < DestinationCount; i++)
        {
            _edges.Add(new PolicyJumpTableEdge(new HttpMethodMatcherPolicy.EdgeKey(TestHttpMethods[i], false), i + 1));
        }

        // Endpoints for GetEdges: one endpoint per HTTP method, one that accepts CORS preflight requests
        // and one without HTTP method metadata.
        for (int i = 1; i < DestinationCount; i++)
        {
            _endpoints.Add(CreateEndpoint(new HttpMethodMetadata(new[] { TestHttpMethods[i] })));
        }
        _endpoints.Add(CreateEndpoint(new HttpMethodMetadata(new[] { HttpMethods.Get }, acceptCorsPreflight: true)));
        _endpoints.Add(CreateEndpoint());
    }

    [Benchmark]
    public PolicyJumpTable BuildJumpTable()
    {
        return _jumpTableBuilder.BuildJumpTable(1, _edges);
    }

    [Benchmark]
    public IReadOnlyList<PolicyNodeEdge> GetEdges()
    {
        return _jumpTableBuilder.GetEdges(_endpoints);
    }

    private static Endpoint CreateEndpoint(params object[] metadata)
    {
        return new Endpoint(requestDelegate: null, new EndpointMetadataCollection(metadata), displayName: null);
    }
}
