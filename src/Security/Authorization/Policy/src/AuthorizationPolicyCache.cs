// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Authorization.Policy;

internal sealed class AuthorizationPolicyCache : IDisposable
{
    // Caches AuthorizationPolicy instances. This is null when there is no EndpointDataSource
    // registered (e.g. AddAuthorization() called without AddRouting()), in which case the
    // cache no-ops since there are no endpoints to key policies on.
    private readonly DataSourceDependentCache<ConcurrentDictionary<Endpoint, AuthorizationPolicy>>? _policyCache;

    // EndpointDataSource is only registered when routing is added. We optionally resolve it so
    // that authorization works in hosts that don't auto-register routing (raw WebHostBuilder,
    // generic Host, Worker SDK). See https://github.com/dotnet/aspnetcore/issues/53332.
    public AuthorizationPolicyCache(EndpointDataSource? dataSource = null)
    {
        if (dataSource is null)
        {
            return;
        }

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
        if (_policyCache is null)
        {
            return null;
        }

        _policyCache.Value!.TryGetValue(endpoint, out var policy);
        return policy;
    }

    public void Store(Endpoint endpoint, AuthorizationPolicy policy)
    {
        if (_policyCache is null)
        {
            return;
        }

        _policyCache.Value![endpoint] = policy;
    }

    public void Dispose()
    {
        _policyCache?.Dispose();
    }
}
