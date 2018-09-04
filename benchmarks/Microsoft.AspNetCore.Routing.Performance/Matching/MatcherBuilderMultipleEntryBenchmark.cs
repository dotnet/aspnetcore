// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public partial class MatcherBuilderMultipleEntryBenchmark : EndpointRoutingBenchmarkBase
    {
        private IServiceProvider _services;

        [GlobalSetup]
        public void Setup()
        {
            Endpoints = new RouteEndpoint[10];
            Endpoints[0] = CreateEndpoint("/product", "GET");
            Endpoints[1] = CreateEndpoint("/product/{id}", "GET");

            Endpoints[2] = CreateEndpoint("/account", "GET");
            Endpoints[3] = CreateEndpoint("/account/{id}");
            Endpoints[4] = CreateEndpoint("/account/{id}", "POST");
            Endpoints[5] = CreateEndpoint("/account/{id}", "UPDATE");

            Endpoints[6] = CreateEndpoint("/v2/account", "GET");
            Endpoints[7] = CreateEndpoint("/v2/account/{id}");
            Endpoints[8] = CreateEndpoint("/v2/account/{id}", "POST");
            Endpoints[9] = CreateEndpoint("/v2/account/{id}", "UPDATE");

            _services = CreateServices();
        }

        private Matcher SetupMatcher(MatcherBuilder builder)
        {
            for (int i = 0; i < Endpoints.Length; i++)
            {
                builder.AddEndpoint(Endpoints[i]);
            }
            return builder.Build();
        }

        [Benchmark]
        public void Dfa()
        {
            var builder = _services.GetRequiredService<DfaMatcherBuilder>();
            SetupMatcher(builder);
        }
    }
}