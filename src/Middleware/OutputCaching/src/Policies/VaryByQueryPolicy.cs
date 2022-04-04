// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every value of the provided query string keys.
/// It also disables the default behavior which is to vary on all query string keys.
/// </summary>
public class VaryByQueryPolicy : IOutputCachingPolicy
{
    private StringValues _queryKeys { get; set; }

    /// <summary>
    /// Creates a policy that doesn't vary the cached content based on query string.
    /// </summary>
    public VaryByQueryPolicy()
    {
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified query string key.
    /// </summary>
    public VaryByQueryPolicy(string queryKey)
    {
        _queryKeys = queryKey;
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified query string keys.
    /// </summary>
    public VaryByQueryPolicy(params string[] queryKeys)
    {
        _queryKeys = queryKeys;
    }

    public Task OnRequestAsync(IOutputCachingContext context)
    {
        // No vary by query?
        if (_queryKeys.Count == 0)
        {
            context.CachedVaryByRules.QueryKeys = _queryKeys;
            return Task.CompletedTask;
        }

        // If the current key is "*" (default) replace it
        if (context.CachedVaryByRules.QueryKeys.Count == 1 && string.Equals(context.CachedVaryByRules.QueryKeys[0], "*", StringComparison.Ordinal))
        {
            context.CachedVaryByRules.QueryKeys = _queryKeys;
            return Task.CompletedTask;
        }

        context.CachedVaryByRules.QueryKeys = StringValues.Concat(context.CachedVaryByRules.QueryKeys, _queryKeys);

        return Task.CompletedTask;
    }

    public Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    public Task OnServeResponseAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
