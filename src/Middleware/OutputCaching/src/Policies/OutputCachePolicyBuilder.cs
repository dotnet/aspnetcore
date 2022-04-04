// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching.Policies;

namespace Microsoft.AspNetCore.OutputCaching;

public class OutputCachePolicyBuilder
{
    private List<IOutputCachingPolicy> Policies { get; } = new();
    private List<Func<IOutputCachingContext, Task<bool>>> Requirements { get; } = new();

    public OutputCachePolicyBuilder When(Func<IOutputCachingContext, Task<bool>> predicate)
    {
        Requirements.Add(predicate);
        return this;
    }

    public OutputCachePolicyBuilder Path(PathString pathBase)
    {
        ArgumentNullException.ThrowIfNull(pathBase, nameof(pathBase));

        Requirements.Add(context =>
        {
            var match = context.HttpContext.Request.Path.StartsWithSegments(pathBase);
            return Task.FromResult(match);
        });
        return this;
    }

    public OutputCachePolicyBuilder Path(params PathString[] pathBases)
    {
        ArgumentNullException.ThrowIfNull(pathBases, nameof(pathBases));

        Requirements.Add(context =>
        {
            var match = pathBases.Any(x => context.HttpContext.Request.Path.StartsWithSegments(x));
            return Task.FromResult(match);
        });
        return this;
    }

    public OutputCachePolicyBuilder Method(string method)
    {
        ArgumentNullException.ThrowIfNull(method, nameof(method));

        Requirements.Add(context =>
        {
            var upperMethod = method.ToUpperInvariant();
            var match = context.HttpContext.Request.Method.ToUpperInvariant() == upperMethod;
            return Task.FromResult(match);
        });
        return this;
    }

    public OutputCachePolicyBuilder Method(params string[] methods)
    {
        ArgumentNullException.ThrowIfNull(methods, nameof(methods));

        Requirements.Add(context =>
        {
            var upperMethods = methods.Select(m => m.ToUpperInvariant()).ToArray();
            var match = methods.Any(m => context.HttpContext.Request.Method.ToUpperInvariant() == m);
            return Task.FromResult(match);
        });
        return this;
    }

    public OutputCachePolicyBuilder VaryByQuery(params string[] queryKeys)
    {
        ArgumentNullException.ThrowIfNull(queryKeys, nameof(queryKeys));

        Policies.Add(new VaryByQueryPolicy(queryKeys));
        return this;
    }

    public OutputCachePolicyBuilder VaryByValue(Func<Task<string>> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy, nameof(varyBy));

        Policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    public OutputCachePolicyBuilder VaryByValue(Func<Task<(string, string)>> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy, nameof(varyBy));

        Policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    public OutputCachePolicyBuilder VaryByValue(Func<string> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy, nameof(varyBy));

        Policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    public OutputCachePolicyBuilder VaryByValue(Func<(string, string)> varyBy)
    {
        ArgumentNullException.ThrowIfNull(varyBy, nameof(varyBy));

        Policies.Add(new VaryByValuePolicy(varyBy));
        return this;
    }

    public OutputCachePolicyBuilder Profile(string profileName)
    {
        ArgumentNullException.ThrowIfNull(profileName, nameof(profileName));

        Policies.Add(new ProfilePolicy(profileName));

        return this;
    }

    public OutputCachePolicyBuilder Tag(params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(tags, nameof(tags));

        Policies.Add(new TagsPolicy(tags));
        return this;
    }

    public OutputCachePolicyBuilder Expires(TimeSpan expiration)
    {
        Policies.Add(new ExpirationPolicy(expiration));
        return this;
    }

    public OutputCachePolicyBuilder Lock(bool lockResponse = true)
    {
        Policies.Add(new LockingPolicy(lockResponse));
        return this;
    }

    public OutputCachePolicyBuilder Clear()
    {
        Requirements.Clear();
        Policies.Clear();
        return this;
    }

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
