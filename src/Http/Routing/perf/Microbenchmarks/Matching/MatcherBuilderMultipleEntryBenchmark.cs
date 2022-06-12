// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matching;

public partial class MatcherBuilderMultipleEntryBenchmark : EndpointRoutingBenchmarkBase
{
    private IServiceProvider _services;
    private List<MatcherPolicy> _policies;
    private ILoggerFactory _loggerFactory;
    private DefaultEndpointSelector _selector;
    private DefaultParameterPolicyFactory _parameterPolicyFactory;

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

        // Define an unordered mixture of policies that implement INodeBuilderPolicy,
        // IEndpointComparerPolicy and/or IEndpointSelectorPolicy
        _policies = new List<MatcherPolicy>()
                {
                    CreateNodeBuilderPolicy(4),
                    CreateUberPolicy(2),
                    CreateNodeBuilderPolicy(3),
                    CreateEndpointComparerPolicy(5),
                    CreateNodeBuilderPolicy(1),
                    CreateEndpointSelectorPolicy(9),
                    CreateEndpointComparerPolicy(7),
                    CreateNodeBuilderPolicy(6),
                    CreateEndpointSelectorPolicy(10),
                    CreateUberPolicy(12),
                    CreateEndpointComparerPolicy(11)
                };
        _loggerFactory = NullLoggerFactory.Instance;
        _selector = new DefaultEndpointSelector();
        _parameterPolicyFactory = new DefaultParameterPolicyFactory(Options.Create(new RouteOptions()), new TestServiceProvider());

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

    [Benchmark]
    public void Constructor_Policies()
    {
        new DfaMatcherBuilder(_loggerFactory, _parameterPolicyFactory, _selector, _policies);
    }

    private static MatcherPolicy CreateNodeBuilderPolicy(int order)
    {
        return new TestNodeBuilderPolicy(order);
    }
    private static MatcherPolicy CreateEndpointComparerPolicy(int order)
    {
        return new TestEndpointComparerPolicy(order);
    }

    private static MatcherPolicy CreateEndpointSelectorPolicy(int order)
    {
        return new TestEndpointSelectorPolicy(order);
    }

    private static MatcherPolicy CreateUberPolicy(int order)
    {
        return new TestUberPolicy(order);
    }

    private sealed class TestUberPolicy : TestMatcherPolicyBase, INodeBuilderPolicy, IEndpointComparerPolicy
    {
        public TestUberPolicy(int order) : base(order)
        {
        }

        public IComparer<Endpoint> Comparer => new TestEndpointComparer();

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            return false;
        }

        public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class TestNodeBuilderPolicy : TestMatcherPolicyBase, INodeBuilderPolicy
    {
        public TestNodeBuilderPolicy(int order) : base(order)
        {
        }

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            return false;
        }

        public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class TestEndpointComparerPolicy : TestMatcherPolicyBase, IEndpointComparerPolicy
    {
        public TestEndpointComparerPolicy(int order) : base(order)
        {
        }

        public IComparer<Endpoint> Comparer => new TestEndpointComparer();

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            return false;
        }

        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class TestEndpointSelectorPolicy : TestMatcherPolicyBase, IEndpointSelectorPolicy
    {
        public TestEndpointSelectorPolicy(int order) : base(order)
        {
        }

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            return false;
        }

        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
        {
            throw new NotImplementedException();
        }
    }

    private abstract class TestMatcherPolicyBase : MatcherPolicy
    {
        private readonly int _order;

        protected TestMatcherPolicyBase(int order)
        {
            _order = order;
        }

        public override int Order { get { return _order; } }
    }

    private sealed class TestEndpointComparer : IComparer<Endpoint>
    {
        public int Compare(Endpoint x, Endpoint y)
        {
            return 0;
        }
    }
}
