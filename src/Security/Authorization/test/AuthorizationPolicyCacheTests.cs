// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Authorization.Test;

public class AuthorizationPolicyCacheTests
{
    [Fact]
    public void WithoutEndpointDataSource_LookupReturnsNullAndStoreIsNoOp()
    {
        var cache = new AuthorizationPolicyCache();
        var endpoint = CreateEndpoint();
        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

        cache.Store(endpoint, policy);

        Assert.Null(cache.Lookup(endpoint));
        cache.Dispose();
    }

    [Fact]
    public void Lookup_ReturnsStoredPolicyInstance()
    {
        var endpoint = CreateEndpoint();
        using var cache = new AuthorizationPolicyCache(new TestEndpointDataSource(endpoint));
        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

        cache.Store(endpoint, policy);

        Assert.Same(policy, cache.Lookup(endpoint));
    }

    [Fact]
    public void Cache_IsWipedWhenEndpointDataSourceChanges()
    {
        var endpoint = CreateEndpoint();
        var dataSource = new TestEndpointDataSource(endpoint);
        using var cache = new AuthorizationPolicyCache(dataSource);
        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        cache.Store(endpoint, policy);

        dataSource.TriggerChange();

        Assert.Null(cache.Lookup(endpoint));
    }

    private static Endpoint CreateEndpoint()
        => new Endpoint(context => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test endpoint");

    private sealed class TestEndpointDataSource : EndpointDataSource
    {
        private CancellationTokenSource _cts = new();
        private CancellationChangeToken _changeToken;

        public TestEndpointDataSource(params Endpoint[] endpoints)
        {
            Endpoints = endpoints;
            _changeToken = new CancellationChangeToken(_cts.Token);
        }

        public override IReadOnlyList<Endpoint> Endpoints { get; }

        public override IChangeToken GetChangeToken() => _changeToken;

        public void TriggerChange()
        {
            var previous = _cts;
            _cts = new CancellationTokenSource();
            _changeToken = new CancellationChangeToken(_cts.Token);
            previous.Cancel();
        }
    }
}
