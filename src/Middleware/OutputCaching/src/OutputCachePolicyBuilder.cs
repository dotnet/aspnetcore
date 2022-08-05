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
    private List<Func<OutputCacheContext, CancellationToken, Task<bool>>>? _requirements;

    /// <summary>
    /// Creates a new <see cref="OutputCachePolicyBuilder"/> instance.
    /// </summary>
    public OutputCachePolicyBuilder()
    {
        _builtPolicy = null;
        _policies.Add(DefaultPolicy.Instance);
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
    public OutputCachePolicyBuilder With(Func<OutputCacheContext, CancellationToken, Task<bool>> predicate)
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
        _requirements.Add((c, t) => Task.FromResult(predicate(c)));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by query strings.
    /// </summary>
    /// <param name="queryKeys">The query keys to vary the cached responses by. Leave empty to ignore all query strings.</param>
    /// <remarks>
    /// By default all query keys vary the cache entries. However when specific query keys are specified only these are then taken into account.
    /// </remarks>
    public OutputCachePolicyBuilder VaryByQuery(params string[] queryKeys)
    {
        ArgumentNullException.ThrowIfNull(queryKeys);

        return AddPolicy(new VaryByQueryPolicy(queryKeys));
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by header.
    /// </summary>
    /// <param name="headers">The headers to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByHeader(params string[] headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        return AddPolicy(new VaryByHeaderPolicy(headers));
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom values.
    /// </summary>
    /// <param name="varyBy">The value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<HttpContext, CancellationToken, ValueTask<string>> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        return AddPolicy(new VaryByValuePolicy(varyBy));
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom key/value.
    /// </summary>
    /// <param name="varyBy">The key/value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<HttpContext, CancellationToken, ValueTask<KeyValuePair<string, string>>> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        return AddPolicy(new VaryByValuePolicy(varyBy));
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom values.
    /// </summary>
    /// <param name="varyBy">The value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<HttpContext, string> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        return AddPolicy(new VaryByValuePolicy(varyBy));
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom key/value.
    /// </summary>
    /// <param name="varyBy">The key/value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<HttpContext, KeyValuePair<string, string>> varyBy)
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
    /// <param name="lockResponse">Whether the request should be locked.</param>
    public OutputCachePolicyBuilder AllowLocking(bool lockResponse = true)
    {
        return AddPolicy(lockResponse ? LockingPolicy.Enabled : LockingPolicy.Disabled);
    }

    /// <summary>
    /// Clears the current policies.
    /// </summary>
    /// <remarks>It also removed the default cache policy.</remarks>
    public OutputCachePolicyBuilder Clear()
    {
        _builtPolicy = null;
        if (_requirements != null)
        {
            _requirements.Clear();
        }
        _policies.Clear();
        return this;
    }

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
    /// Creates the <see cref="IOutputCachePolicy"/>.
    /// </summary>
    /// <returns>The<see cref="IOutputCachePolicy"/> instance.</returns>
    internal IOutputCachePolicy Build()
    {
        if (_builtPolicy != null)
        {
            return _builtPolicy;
        }

        var policies = _policies.Count == 1
            ? _policies[0]
            : new CompositePolicy(_policies.ToArray())
            ;

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
