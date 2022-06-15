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
public sealed class OutputCachePolicyBuilder : IOutputCachePolicy
{
    private const DynamicallyAccessedMemberTypes ActivatorAccessibility = DynamicallyAccessedMemberTypes.PublicConstructors;

    private IOutputCachePolicy? _builtPolicy;
    private readonly List<IOutputCachePolicy> _policies = new();
    private List<Func<OutputCacheContext, Task<bool>>>? _requirements;

    /// <summary>
    /// Creates a new <see cref="OutputCachePolicyBuilder"/> instance.
    /// </summary>
    public OutputCachePolicyBuilder()
    {
        _builtPolicy = null;
        _policies.Add(DefaultOutputCachePolicy.Instance);
    }

    /// <summary>
    /// Adds a policy instance.
    /// </summary>
    public OutputCachePolicyBuilder AddPolicy(IOutputCachePolicy policy)
    {
        _builtPolicy = null;
        _policies.Add(policy);
        return this;
    }

    /// <summary>
    /// Adds a requirement to the current policy.
    /// </summary>
    /// <param name="predicate">The predicate applied to the policy.</param>
    public OutputCachePolicyBuilder With(Func<OutputCacheContext, Task<bool>> predicate)
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
        _requirements.Add(c => Task.FromResult(predicate(c)));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by query strings.
    /// </summary>
    /// <param name="queryKeys">The query keys to vary the cached responses by.</param>
    /// <remarks>
    /// By default all query keys vary the cache entries. However when specific query keys are specified only these are then taken into account.
    /// </remarks>
    public OutputCachePolicyBuilder VaryByQuery(params string[] queryKeys)
    {
        ArgumentNullException.ThrowIfNull(queryKeys);

        _builtPolicy = null;
        _policies.Add(new VaryByQueryPolicy(queryKeys));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by header.
    /// </summary>
    /// <param name="headers">The headers to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByHeader(params string[] headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        _builtPolicy = null;
        _policies.Add(new VaryByHeaderPolicy(headers));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom values.
    /// </summary>
    /// <param name="varyBy">The value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<HttpContext, Task<string>> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        _builtPolicy = null;
        _policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom key/value.
    /// </summary>
    /// <param name="varyBy">The key/value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<HttpContext, Task<(string, string)>> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        _builtPolicy = null;
        _policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom values.
    /// </summary>
    /// <param name="varyBy">The value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<HttpContext, string> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        _builtPolicy = null;
        _policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom key/value.
    /// </summary>
    /// <param name="varyBy">The key/value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<HttpContext, (string, string)> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        _builtPolicy = null;
        _policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    /// <summary>
    /// Adds a named policy.
    /// </summary>
    /// <param name="profileName">The name of the policy to add.</param>
    public OutputCachePolicyBuilder Policy(string profileName)
    {
        ArgumentNullException.ThrowIfNull(profileName);

        _builtPolicy = null;
        _policies.Add(new ProfilePolicy(profileName));
        return this;
    }

    /// <summary>
    /// Adds a policy to tag the cached response.
    /// </summary>
    /// <param name="tags">The tags to add to the cached reponse.</param>
    public OutputCachePolicyBuilder Tag(params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        _builtPolicy = null;
        _policies.Add(new TagsPolicy(tags));
        return this;
    }

    /// <summary>
    /// Adds a policy to change the cached response expiration.
    /// </summary>
    /// <param name="expiration">The expiration of the cached reponse.</param>
    public OutputCachePolicyBuilder Expire(TimeSpan expiration)
    {
        _builtPolicy = null;
        _policies.Add(new ExpirationPolicy(expiration));
        return this;
    }

    /// <summary>
    /// Adds a policy to change the request locking strategy.
    /// </summary>
    /// <param name="lockResponse">Whether the request should be locked.</param>
    public OutputCachePolicyBuilder AllowLocking(bool lockResponse = true)
    {
        _builtPolicy = null;
        _policies.Add(lockResponse ? LockingPolicy.Enabled : LockingPolicy.Disabled);
        return this;
    }

    /// <summary>
    /// Clears the current policies.
    /// </summary>
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
        _builtPolicy = null;
        _policies.Clear();
        _policies.Add(EnableCachePolicy.Disabled);
        return this;
    }

    internal IOutputCachePolicy Build()
    {
        if (_builtPolicy != null)
        {
            return _builtPolicy;
        }

        var policies = new CompositePolicy(_policies.ToArray());

        // If the policy was built with requirements, wrap it
        if (_requirements != null && _requirements.Any())
        {
            return new PredicatePolicy(async c =>
            {
                foreach (var r in _requirements)
                {
                    if (!await r(c))
                    {
                        return false;
                    }
                }

                return true;
            }, policies);
        }

        return _builtPolicy = policies;
    }

    /// <summary>
    /// Adds a dynamically resolved policy.
    /// </summary>
    /// <param name="policyType">The type of policy to add</param>
    public OutputCachePolicyBuilder AddPolicy([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type policyType)
    {
        AddPolicy(new TypedPolicy(policyType));
        return this;
    }

    /// <summary>
    /// Adds a dynamically resolved policy.
    /// </summary>
    /// <typeparam name="T">The policy type.</typeparam>
    public OutputCachePolicyBuilder AddPolicy<[DynamicallyAccessedMembers(ActivatorAccessibility)] T>() where T : IOutputCachePolicy
    {
        return AddPolicy(typeof(T));
    }

    Task IOutputCachePolicy.OnRequestAsync(OutputCacheContext context)
    {
        return Build().OnRequestAsync(context);
    }

    Task IOutputCachePolicy.OnServeFromCacheAsync(OutputCacheContext context)
    {
        return Build().OnServeFromCacheAsync(context);
    }

    Task IOutputCachePolicy.OnServeResponseAsync(OutputCacheContext context)
    {
        return Build().OnServeResponseAsync(context);
    }
}
