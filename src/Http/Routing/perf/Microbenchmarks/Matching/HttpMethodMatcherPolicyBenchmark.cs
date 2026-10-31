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

    [Params(3, 5, 11)]
    public int DestinationCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        for (int i = 0; i < DestinationCount; i++)
        {
            _edges.Add(new PolicyJumpTableEdge(new HttpMethodMatcherPolicy.EdgeKey(TestHttpMethods[i], false), i + 1));
        }
    }

    [Benchmark]
    public PolicyJumpTable BuildJumpTable()
    {
        return _jumpTableBuilder.BuildJumpTable(1, _edges);
    }
}
