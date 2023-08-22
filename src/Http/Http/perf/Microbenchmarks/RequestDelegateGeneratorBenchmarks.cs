// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Generators.Tests;

namespace Microsoft.AspNetCore.Http.Microbenchmarks;

public class RequestDelegateGeneratorBenchmarks : RequestDelegateCreationTestBase
{
    protected override bool IsGeneratorEnabled => true;

    [Params(10, 100, 1000, 10000)]
    public int EndpointCount { get; set; }

    private string _source;

    [GlobalSetup]
    public void Setup()
    {
        _source = "";
        for (var i = 0; i < EndpointCount; i++)
        {
            _source += $"""app.MapGet("/route{i}", (int? id) => "Hello World!");""";
        }
    }

    [Benchmark]
    public async Task CreateRequestDelegate()
    {
        await RunGeneratorAsync(_source);
    }
}
