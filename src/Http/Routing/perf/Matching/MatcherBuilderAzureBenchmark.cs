// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Matching
{
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
}