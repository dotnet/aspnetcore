// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching.Policies;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Provides helper methods to create custom policies.
/// </summary>
public sealed class OutputCachePolicyBuilder : IOutputCachingPolicy
{
    private IOutputCachingPolicy? _builtPolicy;
    private List<IOutputCachingPolicy> Policies { get; } = new();
    private List<Func<IOutputCachingContext, Task<bool>>> Requirements { get; } = new();

    /// <summary>
    /// Creates a new instance of <see cref="OutputCachePolicyBuilder"/> with a <see cref="DefaultOutputCachePolicy"/> policy which allows unauthenticated GET, HEAD, 200 responses without cookies to be cached.
    /// </summary>
    /// <remarks>
    /// The default policy doesn't cache any request by default. To enable caching use <see cref="Enable"/> or invoke <c>CacheOutput()</c> on an endpoint.
    /// </remarks>
    public OutputCachePolicyBuilder()
    {
        _builtPolicy = null;
        Policies.Add(DefaultOutputCachePolicy.Instance);
    }

    /// <summary>
    /// Adds a policy instance.
    /// </summary>
    public OutputCachePolicyBuilder Add(IOutputCachingPolicy policy)
    {
        _builtPolicy = null;
        Policies.Add(policy);
        return this;
    }

    /// <summary>
    /// Enables caching.
    /// </summary>
    public OutputCachePolicyBuilder Enable()
    {
        _builtPolicy = null;
        Policies.Add(EnableCachingPolicy.Enabled);
        return this;
    }

    /// <summary>
    /// Disables caching.
    /// </summary>
    /// <remarks>
    /// This is the default.
    /// </remarks>
    public OutputCachePolicyBuilder Disable()
    {
        _builtPolicy = null;
        Policies.Add(EnableCachingPolicy.Disabled);
        return this;
    }

    /// <summary>
    /// Adds a requirement to the current policy.
    /// </summary>
    /// <param name="predicate">The predicate applied to the policy.</param>
    public OutputCachePolicyBuilder WithCondition(Func<IOutputCachingContext, Task<bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _builtPolicy = null;
        Requirements.Add(predicate);
        return this;
    }

    /// <summary>
    /// Adds a requirement to the current policy based on the request path.
    /// </summary>
    /// <param name="pathBase">The base path to limit the policy to.</param>
    public OutputCachePolicyBuilder WithPath(PathString pathBase)
    {
        ArgumentNullException.ThrowIfNull(pathBase);

        _builtPolicy = null;
        Requirements.Add(context =>
        {
            var match = context.HttpContext.Request.Path.StartsWithSegments(pathBase);
            return Task.FromResult(match);
        });
        return this;
    }

    /// <summary>
    /// Adds a requirement to the current policy based on the request path.
    /// </summary>
    /// <param name="pathBases">The base paths to limit the policy to.</param>
    public OutputCachePolicyBuilder WithPath(params PathString[] pathBases)
    {
        ArgumentNullException.ThrowIfNull(pathBases);

        _builtPolicy = null;
        Requirements.Add(context =>
        {
            var match = pathBases.Any(x => context.HttpContext.Request.Path.StartsWithSegments(x));
            return Task.FromResult(match);
        });
        return this;
    }

    /// <summary>
    /// Adds a requirement to the current policy based on the request method.
    /// </summary>
    /// <param name="method">The method to limit the policy to.</param>
    public OutputCachePolicyBuilder WithMethod(string method)
    {
        ArgumentNullException.ThrowIfNull(method);

        _builtPolicy = null;
        Requirements.Add(context =>
        {
            var upperMethod = method.ToUpperInvariant();
            var match = context.HttpContext.Request.Method.ToUpperInvariant() == upperMethod;
            return Task.FromResult(match);
        });
        return this;
    }

    /// <summary>
    /// Adds a requirement to the current policy based on the request method.
    /// </summary>
    /// <param name="methods">The methods to limit the policy to.</param>
    public OutputCachePolicyBuilder WithMethod(params string[] methods)
    {
        ArgumentNullException.ThrowIfNull(methods);

        _builtPolicy = null;
        Requirements.Add(context =>
        {
            var upperMethods = methods.Select(m => m.ToUpperInvariant()).ToArray();
            var match = methods.Any(m => context.HttpContext.Request.Method.ToUpperInvariant() == m);
            return Task.FromResult(match);
        });
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
        Policies.Add(new VaryByQueryPolicy(queryKeys));
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
        Policies.Add(new VaryByHeaderPolicy(headers));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom values.
    /// </summary>
    /// <param name="varyBy">The value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<Task<string>> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        _builtPolicy = null;
        Policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom key/value.
    /// </summary>
    /// <param name="varyBy">The key/value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<Task<(string, string)>> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        _builtPolicy = null;
        Policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom values.
    /// </summary>
    /// <param name="varyBy">The value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<string> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        _builtPolicy = null;
        Policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    /// <summary>
    /// Adds a policy to vary the cached responses by custom key/value.
    /// </summary>
    /// <param name="varyBy">The key/value to vary the cached responses by.</param>
    public OutputCachePolicyBuilder VaryByValue(Func<(string, string)> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy);

        _builtPolicy = null;
        Policies.Add(new VaryByValuePolicy(varyBy));
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
        Policies.Add(new ProfilePolicy(profileName));
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
        Policies.Add(new TagsPolicy(tags));
        return this;
    }

    /// <summary>
    /// Adds a policy to change the cached response expiration.
    /// </summary>
    /// <param name="expiration">The expiration of the cached reponse.</param>
    public OutputCachePolicyBuilder Expire(TimeSpan expiration)
    {
        _builtPolicy = null;
        Policies.Add(new ExpirationPolicy(expiration));
        return this;
    }

    /// <summary>
    /// Adds a policy to change the request locking strategy.
    /// </summary>
    /// <param name="lockResponse">Whether the request should be locked.</param>
    public OutputCachePolicyBuilder Lock(bool lockResponse = true)
    {
        _builtPolicy = null;
        Policies.Add(lockResponse ? LockingPolicy.Enabled : LockingPolicy.Disabled);
        return this;
    }

    /// <summary>
    /// Clears the current policies.
    /// </summary>
    public OutputCachePolicyBuilder Clear()
    {
        _builtPolicy = null;
        Requirements.Clear();
        Policies.Clear();
        return this;
    }

    /// <summary>
    /// Adds a policy to prevent the response from being cached.
    /// </summary>
    public OutputCachePolicyBuilder NoStore()
    {
        _builtPolicy = null;
        Policies.Add(NoStorePolicy.Instance);
        return this;
    }

    /// <summary>
    /// Builds a new <see cref="IOutputCachingPolicy"/> from the definitions
    /// in this instance.
    /// </summary>
    /// <returns>
    /// A new <see cref="IOutputCachingPolicy"/> built from the definitions in this instance.
    /// </returns>
    public IOutputCachingPolicy Build()
    {
        if (_builtPolicy != null)
        {
            return _builtPolicy;
        }

        var policies = new CompositePolicy(Policies.ToArray());

        if (Requirements.Any())
        {
            return new PredicatePolicy(async c =>
            {
                foreach (var r in Requirements)
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

    Task IOutputCachingPolicy.OnRequestAsync(IOutputCachingContext context)
    {
        return Build().OnRequestAsync(context);
    }

    Task IOutputCachingPolicy.OnServeFromCacheAsync(IOutputCachingContext context)
    {
        return Build().OnServeFromCacheAsync(context);
    }

    Task IOutputCachingPolicy.OnServeResponseAsync(IOutputCachingContext context)
    {
        return Build().OnServeResponseAsync(context);
    }
}
