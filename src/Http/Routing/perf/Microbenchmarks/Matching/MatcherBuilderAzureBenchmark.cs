// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Matching;

// Generated from https://github.com/APIs-guru/openapi-directory
// Use https://editor2.swagger.io/ to convert from yaml to json-
public class MatcherBuilderAzureBenchmark : MatcherAzureBenchmarkBase
{
    private IServiceProvider _services;

    [GlobalSetup]
    public void Setup()
    {
        SetupEndpoints();

        _services = CreateServices();
    }

    [Benchmark]
    public void Dfa()
    {
        var builder = _services.GetRequiredService<DfaMatcherBuilder>();
        SetupMatcher(builder);
    }
}
