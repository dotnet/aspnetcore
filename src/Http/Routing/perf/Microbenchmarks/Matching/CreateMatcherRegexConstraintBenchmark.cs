// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matching;

public class CreateMatcherRegexConstraintBenchmark : EndpointRoutingBenchmarkBase
{
    [Params(true, false)]
    public bool RegexSame { get; set; }

    private const int EndpointCount = 1_000;

    [GlobalSetup]
    public void Setup()
    {
        Endpoints = new RouteEndpoint[EndpointCount];
        for (var i = 0; i < Endpoints.Length; i++)
        {
            Endpoints[i] = RegexSame
                ? CreateEndpoint("/plaintext" + i + "/{param:regex(^\\d{{7}}|(SI[[PG]]|JPA|DEM)\\d{{4}})}")
                : CreateEndpoint("/plaintext" + i + "/{param:regex(^" + i + "\\d{{7}}|(SI[[PG]]|JPA|DEM)\\d{{4}})}");
        }

    }

    [Benchmark]
    public void Build()
    {
        var builder = CreateDfaMatcherBuilder();
        for (var i = 0; i < Endpoints.Length; i++)
        {
            builder.AddEndpoint(Endpoints[i]);
        }

        builder.Build();
    }
}
