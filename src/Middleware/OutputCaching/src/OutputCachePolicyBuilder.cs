// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching.Policies;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Provides helper methods to create custom policies.
/// </summary>
public sealed class OutputCachePolicyBuilder
{
    private const DynamicallyAccessedMemberTypes ActivatorAccessibility = DynamicallyAccessedMemberTypes.PublicConstructors;

    private IOutputCachePolicy? _builtPolicy;
    private readonly List<IOutputCachePolicy> _policies = new();
    private List<Func<OutputCacheContext, CancellationToken, ValueTask<bool>>>? _requirements;

    internal OutputCachePolicyBuilder() : this(false)
    {
    }

    internal OutputCachePolicyBuilder(bool excludeDefaultPolicy)
    {
        _builtPolicy = null;
        if (!excludeDefaultPolicy)
        {
            _policies.Add(DefaultPolicy.Instance);
        }
    }

    internal OutputCachePolicyBuilder AddPolicy(IOutputCachePolicy policy)
    {
        _builtPolicy = null;
        _policies.Add(policy);
        return this;
    }

    /// <summary>
    /// Adds a dynamically resolved policy.
    /// </summary>
    /// <param name="policyType">The type of policy to add</param>
    public OutputCachePolicyBuilder AddPolicy([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type policyType)
    {
        return AddPolicy(new TypedPolicy(policyType));
    }

    /// <summary>
    /// Adds a dynamically resolved policy.
    /// </summary>
    /// <typeparam name="T">The policy type.</typeparam>
    public OutputCachePolicyBuilder AddPolicy<[DynamicallyAccessedMembers(ActivatorAccessibility)] T>() where T : IOutputCachePolicy
    {
        return AddPolicy(typeof(T));
    }

    /// <summary>
    /// Adds a requirement to the current policy.
    /// </summary>
    /// <param name="predicate">The predicate applied to the policy.</param>
    public OutputCachePolicyBuilder With(Func<OutputCacheContext, CancellationToken, ValueTask<bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _builtPolicy = null;
        _requirements ??= new();
        _requirements.Add(predicate);
        return this;
    }

    /// <summary>
    /// Adds a requirement to the current policy.
    /// </summary>
    /// <param name="predicate">The predicate applied to the policy.</param>
    public OutputCachePolicyBuilder With(Func<OutputCacheContext, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _builtPolicy = null;
        _requirements ??= new();
        _requirements.Add((c, t) => ValueTask.FromResult(predicate(c)));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by query strings.
    /// </summary>
    /// <param name="queryKey">The query key to vary the cached responses by.</param>
    /// <param name="queryKeys">The extra query keys to vary the cached responses by.</param>
    /// <remarks>
    /// By default all query keys vary the cache entries. However when specific query keys are specified only these are then taken into account.
    /// </remarks>
    public OutputCachePolicyBuilder SetVaryByQuery(string queryKey, params string[] queryKeys)
    {
        ArgumentNullException.ThrowIfNull(queryKey);

        return AddPolicy(new VaryByQueryPolicy(queryKey, queryKeys));
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by query strings.
    /// </summary>
    /// <param name="queryKeys">The query keys to vary the cached responses by.</param>
    /// <remarks>
    /// By default all query keys vary the cache entries. However when specific query keys are specified only these are then taken into account.
    /// </remarks>
    public OutputCachePolicyBuilder SetVaryByQuery(string[] queryKeys)
    {
        ArgumentNullException.ThrowIfNull(queryKeys);

        return AddPolicy(new VaryByQueryPolicy(queryKeys));
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by header.
    /// </summary>
    /// <param name="headerName">The header name to vary the cached responses by.</param>
    /// <param name="headerNames">Additional header names to vary the cached responses by.</param>
    public OutputCachePolicyBuilder SetVaryByHeader(string headerName, params string[] headerNames)
    {
        ArgumentNullException.ThrowIfNull(headerName);

        return AddPolicy(new VaryByHeaderPolicy(headerName, headerNames));
    }
    /// <summary>
    /// Adds a policy to vary the cached responses by header.
    /// </summary>
    /// <param name="headerNames">The header names to vary the cached responses by.</param>
    public OutputCachePolicyBuilder SetVaryByHeader(string[] headerNames)
    {
        ArgumentNullException.ThrowIfNull(headerNames);

        return AddPolicy(new VaryByHeaderPolicy(headerNames));
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by route value.
    /// </summary>
    /// <param name="routeValueName">The route value name to vary the cached responses by.</param>
    /// <param name="routeValueNames">The extra route value names to vary the cached responses by.</param>
    public OutputCachePolicyBuilder SetVaryByRouteValue(string routeValueName, params string[] routeValueNames)
    {
        ArgumentNullException.ThrowIfNull(routeValueName);

        return AddPolicy(new VaryByRouteValuePolicy(routeValueName, routeValueNames));
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by route value.
    /// </summary>
    /// <param name="routeValueNames">The route value names to vary the cached responses by.</param>
    public OutputCachePolicyBuilder SetVaryByRouteValue(string[] routeValueNames)
    {
        ArgumentNullException.ThrowIfNull(routeValueNames);

        return AddPolicy(new VaryByRouteValuePolicy(routeValueNames));
    }

    /// <summary>
    /// Adds a policy that varies the cache key using the specified value.
    /// </summary>
    /// <param name="keyPrefix">The value to vary the cache key by.</param>
    public OutputCachePolicyBuilder SetCacheKeyPrefix(string keyPrefix)
    {
        ArgumentNullException.ThrowIfNull(keyPrefix);

        ValueTask<string> varyByKeyFunc(HttpContext context, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(keyPrefix);
        }

        return AddPolicy(new SetCacheKeyPrefixPolicy(varyByKeyFunc));
    }

    /// <summary>
    /// Adds a policy that varies the cache key using the specified value.
    /// </summary>
    /// <param name="keyPrefix">The value to vary the cache key by.</param>
    public OutputCachePolicyBuilder SetCacheKeyPrefix(Func<HttpContext, string> keyPrefix)
    {
        ArgumentNullException.ThrowIfNull(keyPrefix);

        ValueTask<string> varyByKeyFunc(HttpContext context, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(keyPrefix(context));
        }

        return AddPolicy(new SetCacheKeyPrefixPolicy(varyByKeyFunc));
    }

    /// <summary>
    /// Adds a policy that varies the cache key using the specified value.
    /// </summary>
    /// <param name="keyPrefix">The value to vary the cache key by.</param>
    public OutputCachePolicyBuilder SetCacheKeyPrefix(Func<HttpContext, CancellationToken, ValueTask<string>> keyPrefix)
    {
        ArgumentNullException.ThrowIfNull(keyPrefix);

        return AddPolicy(new SetCacheKeyPrefixPolicy(keyPrefix));
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom key/value.
    /// </summary>
    /// <param name="key">The key to vary the cached responses by.</param>
    /// <param name="value">The value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        ValueTask<KeyValuePair<string, string>> varyByFunc(HttpContext context, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new KeyValuePair<string, string>(key, value));
        }

        return AddPolicy(new VaryByValuePolicy(varyByFunc));
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom key/value.
    /// </summary>
    /// <param name="varyBy">The key/value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<HttpContext, KeyValuePair<string, string>> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        ValueTask<KeyValuePair<string, string>> varyByFunc(HttpContext context, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(varyBy(context));
        }

        return AddPolicy(new VaryByValuePolicy(varyByFunc));
    }

    /// <summary>
    /// Adds a policy that vary the cached content based on the specified value.
    /// </summary>
    /// <param name="varyBy">The key/value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<HttpContext, CancellationToken, ValueTask<KeyValuePair<string, string>>> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        return AddPolicy(new VaryByValuePolicy(varyBy));
    }

    /// <summary>
    /// Adds a policy to tag the cached response.
    /// </summary>
    /// <param name="tags">The tags to add to the cached reponse.</param>
    public OutputCachePolicyBuilder Tag(params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        return AddPolicy(new TagsPolicy(tags));
    }

    /// <summary>
    /// Adds a policy to change the cached response expiration.
    /// </summary>
    /// <param name="expiration">The expiration of the cached reponse.</param>
    public OutputCachePolicyBuilder Expire(TimeSpan expiration)
    {
        return AddPolicy(new ExpirationPolicy(expiration));
    }

    /// <summary>
    /// Adds a policy to change the request locking strategy.
    /// </summary>
    /// <param name="enabled">Whether the request should be locked.</param>
    /// <remarks>When the default policy is used, locking is enabled by default.</remarks>
    public OutputCachePolicyBuilder SetLocking(bool enabled) => AddPolicy(enabled ? LockingPolicy.Enabled : LockingPolicy.Disabled);

    /// <summary>
    /// Clears the policies and adds one preventing any caching logic to happen.
    /// </summary>
    /// <remarks>
    /// The cache key will never be computed.
    /// </remarks>
    public OutputCachePolicyBuilder NoCache()
    {
        _policies.Clear();
        return AddPolicy(EnableCachePolicy.Disabled);
    }

    /// <summary>
    /// Enables caching for the current request if not already enabled.
    /// </summary>
    public OutputCachePolicyBuilder Cache()
    {
        // If no custom policy is added, the DefaultPolicy is already "enabled".
        if (_policies.Count != 1 || _policies[0] != DefaultPolicy.Instance)
        {
            AddPolicy(EnableCachePolicy.Enabled);
        }

        return this;
    }

    /// <summary>
    /// Adds a policy setting whether to vary by the Host header ot not.
    /// </summary>
    public OutputCachePolicyBuilder SetVaryByHost(bool enabled)
    {
        return AddPolicy(enabled ? VaryByHostPolicy.Enabled : VaryByHostPolicy.Disabled);
    }

    /// <summary>
    /// Creates the <see cref="IOutputCachePolicy"/>.
    /// </summary>
    /// <returns>The<see cref="IOutputCachePolicy"/> instance.</returns>
    internal IOutputCachePolicy Build()
    {
        if (_builtPolicy != null)
        {
            return _builtPolicy;
        }

        var policies = _policies.Count switch
        {
            0 => EmptyPolicy.Instance,
            1 => _policies[0],
            _ => new CompositePolicy(_policies.ToArray()),
        };

        // If the policy was built with requirements, wrap it
        if (_requirements != null && _requirements.Any())
        {
            policies = new PredicatePolicy(async c =>
            {
                foreach (var r in _requirements)
                {
                    if (!await r(c, c.HttpContext.RequestAborted))
                    {
                        return false;
                    }
                }

                return true;
            }, policies);
        }

        return _builtPolicy = policies;
    }
}
