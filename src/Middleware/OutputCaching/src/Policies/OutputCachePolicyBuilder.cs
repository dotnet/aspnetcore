// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching.Policies;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Provides helper methods to create custom policies.
/// </summary>
public class OutputCachePolicyBuilder
{
    private List<IOutputCachingPolicy> Policies { get; } = new();
    private List<Func<IOutputCachingContext, Task<bool>>> Requirements { get; } = new();

    /// <summary>
    /// Gets an <see cref="OutputCachePolicyBuilder"/> initialized with a <see cref="DefaultOutputCachePolicy"/> instance.
    /// </summary>
    public static OutputCachePolicyBuilder Default
    {
        get
        {
            var builder = new OutputCachePolicyBuilder();
            builder.Policies.Add(new DefaultOutputCachePolicy());
            return builder;
        }
    }

    /// <summary>
    /// Gets an empty <see cref="OutputCachePolicyBuilder"/>.
    /// </summary>
    public static OutputCachePolicyBuilder Empty
    {
        get
        {
            return new OutputCachePolicyBuilder();
        }
    }

    /// <summary>
    /// Enables caching.
    /// </summary>
    public OutputCachePolicyBuilder Enable()
    {
        Policies.Add(new EnableCachingPolicy());
        return this;
    }

    /// <summary>
    /// Adds a requirement to the current policy.
    /// </summary>
    /// <param name="predicate">The predicate applied to the policy.</param>
    public OutputCachePolicyBuilder When(Func<IOutputCachingContext, Task<bool>> predicate)
    {
        Requirements.Add(predicate);
        return this;
    }

    /// <summary>
    /// Adds a requirement to the current policy based on the request path.
    /// </summary>
    /// <param name="pathBase">The base path to limit the policy to.</param>
    public OutputCachePolicyBuilder Path(PathString pathBase)
    {
        ArgumentNullException.ThrowIfNull(pathBase);

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
    public OutputCachePolicyBuilder Path(params PathString[] pathBases)
    {
        ArgumentNullException.ThrowIfNull(pathBases);

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
    public OutputCachePolicyBuilder Method(string method)
    {
        ArgumentNullException.ThrowIfNull(method);

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
    public OutputCachePolicyBuilder Method(params string[] methods)
    {
        ArgumentNullException.ThrowIfNull(methods);

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
    public OutputCachePolicyBuilder VaryByQuery(params string[] queryKeys)
    {
        ArgumentNullException.ThrowIfNull(queryKeys);

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

        Policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    /// <summary>
    /// Adds a named policy.
    /// </summary>
    /// <param name="profileName">The name of the policy to add.</param>
    public OutputCachePolicyBuilder Profile(string profileName)
    {
        ArgumentNullException.ThrowIfNull(profileName);

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

        Policies.Add(new TagsPolicy(tags));
        return this;
    }

    /// <summary>
    /// Adds a policy to change the cached response expiration.
    /// </summary>
    /// <param name="expiration">The expiration of the cached reponse.</param>
    public OutputCachePolicyBuilder Expires(TimeSpan expiration)
    {
        Policies.Add(new ExpirationPolicy(expiration));
        return this;
    }

    /// <summary>
    /// Adds a policy to change the request locking strategy.
    /// </summary>
    /// <param name="lockResponse">Whether the request should be locked.</param>
    public OutputCachePolicyBuilder Lock(bool lockResponse = true)
    {
        Policies.Add(lockResponse ? LockingPolicy.Enabled : LockingPolicy.Disabled);
        return this;
    }

    /// <summary>
    /// Clears the current policies.
    /// </summary>
    public OutputCachePolicyBuilder Clear()
    {
        Requirements.Clear();
        Policies.Clear();
        return this;
    }

    /// <summary>
    /// Adds a policy to prevent the response from being cached.
    /// </summary>
    public OutputCachePolicyBuilder NoStore()
    {
        Policies.Add(new NoStorePolicy());
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

        return policies;
    }
}
