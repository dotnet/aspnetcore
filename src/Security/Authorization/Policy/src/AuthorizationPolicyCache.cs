// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Authorization.Policy;

internal sealed class AuthorizationPolicyCache : IDisposable
{
    // Caches AuthorizationPolicy instances
    private readonly DataSourceDependentCache<ConcurrentDictionary<Endpoint, AuthorizationPolicy>> _policyCache;

    public AuthorizationPolicyCache(EndpointDataSource dataSource)
    {
        // We cache AuthorizationPolicy instances per-Endpoint for performance, but we want to wipe out
        // that cache if the endpoints change so that we don't allow unbounded memory growth.
        _policyCache = new DataSourceDependentCache<ConcurrentDictionary<Endpoint, AuthorizationPolicy>>(dataSource, (_) =>
        {
            // We don't eagerly fill this cache because there's no real reason to.
            return new ConcurrentDictionary<Endpoint, AuthorizationPolicy>();
        });
        _policyCache.EnsureInitialized();
    }

    public AuthorizationPolicy? Lookup(Endpoint endpoint)
    {
        _policyCache.Value!.TryGetValue(endpoint, out var policy);
        return policy;
    }

    public void Store(Endpoint endpoint, AuthorizationPolicy policy)
    {
        _policyCache.Value![endpoint] = policy;
    }

    public void Dispose()
    {
        _policyCache.Dispose();
    }
}
