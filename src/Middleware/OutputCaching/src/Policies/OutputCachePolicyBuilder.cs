// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Provides helper methods to create custom policies.
/// </summary>
public sealed class OutputCachePolicyBuilder : IOutputCachingPolicy
{
    private const DynamicallyAccessedMemberTypes ActivatorAccessibility = DynamicallyAccessedMemberTypes.PublicConstructors;

    private IOutputCachingPolicy? _builtPolicy;
    private readonly List<IOutputCachingPolicy> _policies = new();
    private readonly OutputCachingOptions _options;
    private List<Func<OutputCachingContext, Task<bool>>>? _requirements;

    internal OutputCachePolicyBuilder(OutputCachingOptions options)
    {
        _builtPolicy = null;
        _policies.Add(DefaultOutputCachePolicy.Instance);
        _options = options;
    }

    /// <summary>
    /// Adds a policy instance.
    /// </summary>
    public OutputCachePolicyBuilder Add(IOutputCachingPolicy policy)
    {
        _builtPolicy = null;
        _policies.Add(policy);
        return this;
    }

    /// <summary>
    /// Adds a requirement to the current policy.
    /// </summary>
    /// <param name="predicate">The predicate applied to the policy.</param>
    public OutputCachePolicyBuilder WithCondition(Func<OutputCachingContext, Task<bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _builtPolicy = null;
        _requirements ??= new();
        _requirements.Add(predicate);
        return this;
    }

    /// <summary>
    /// Adds a requirement to the current policy based on the request path.
    /// </summary>
    /// <param name="pathBase">The base path to limit the policy to.</param>
    public OutputCachePolicyBuilder WithPathBase(PathString pathBase)
    {
        ArgumentNullException.ThrowIfNull(pathBase);

        _builtPolicy = null;
        _requirements ??= new();
        _requirements.Add(context =>
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
    public OutputCachePolicyBuilder WithPathBase(params PathString[] pathBases)
    {
        ArgumentNullException.ThrowIfNull(pathBases);

        _builtPolicy = null;
        _requirements ??= new();
        _requirements.Add(context =>
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
        _requirements ??= new();
        _requirements.Add(context =>
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
        _requirements ??= new();
        _requirements.Add(context =>
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
    public OutputCachePolicyBuilder VaryByValue(Func<Task<string>> varyBy)
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
    public OutputCachePolicyBuilder VaryByValue(Func<Task<(string, string)>> varyBy)
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
    public OutputCachePolicyBuilder VaryByValue(Func<string> varyBy)
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
    public OutputCachePolicyBuilder VaryByValue(Func<(string, string)> varyBy)
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
    public OutputCachePolicyBuilder Lock(bool lockResponse = true)
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
    /// Adds a policy to prevent the response from being cached automatically.
    /// </summary>
    /// <remarks>
    /// The cache key will still be computed for lookups.
    /// </remarks>
    public OutputCachePolicyBuilder NoStore()
    {
        _builtPolicy = null;
        _policies.Add(NoStorePolicy.Instance);
        return this;
    }

    /// <summary>
    /// Adds a policy to prevent the response from being served from cached automatically.
    /// </summary>
    /// <remarks>
    /// The cache key will still be computed for storage.
    /// </remarks>
    public OutputCachePolicyBuilder NoLookup()
    {
        _builtPolicy = null;
        _policies.Add(NoLookupPolicy.Instance);
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
        _policies.Add(EnableCachingPolicy.Disabled);
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
    /// Defines a <see cref="IOutputCachingPolicy"/> which can be referenced by name.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="policyType">The type of policy to add</param>
    public void Add([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type policyType)
    {
        if (ActivatorUtilities.GetServiceOrCreateInstance(_options.ApplicationServices, policyType) is not IOutputCachingPolicy policy)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Policy_InvalidType));
        }

        Add(policy);
    }

    /// <summary>
    /// Defines a <see cref="IOutputCachingPolicy"/> which can be referenced by name.
    /// </summary>
    /// <typeparam name="T">The policy type.</typeparam>
    public void Add<[DynamicallyAccessedMembers(ActivatorAccessibility)] T>() where T : IOutputCachingPolicy
    {
        Add(typeof(T));
    }

    Task IOutputCachingPolicy.OnRequestAsync(OutputCachingContext context)
    {
        return Build().OnRequestAsync(context);
    }

    Task IOutputCachingPolicy.OnServeFromCacheAsync(OutputCachingContext context)
    {
        return Build().OnServeFromCacheAsync(context);
    }

    Task IOutputCachingPolicy.OnServeResponseAsync(OutputCachingContext context)
    {
        return Build().OnServeResponseAsync(context);
    }
}
