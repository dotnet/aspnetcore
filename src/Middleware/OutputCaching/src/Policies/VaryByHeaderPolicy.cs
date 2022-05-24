// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every value of the provided headers.
/// </summary>
public sealed class VaryByHeaderPolicy : IOutputCachingPolicy
{
    private StringValues _headers { get; set; }

    /// <summary>
    /// Creates a policy that doesn't vary the cached content based on headers.
    /// </summary>
    public VaryByHeaderPolicy()
    {
    }

    /// <summary>
    /// Creates a policy that varies the cached content based on the specified header.
    /// </summary>
    public VaryByHeaderPolicy(string header)
    {
        _headers = header;
    }

    /// <summary>
    /// Creates a policy that varies the cached content based on the specified query string keys.
    /// </summary>
    public VaryByHeaderPolicy(params string[] headers)
    {
        _headers = headers;
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnRequestAsync(IOutputCachingContext context)
    {
        // No vary by header?
        if (_headers.Count == 0)
        {
            context.CachedVaryByRules.Headers = _headers;
            return Task.CompletedTask;
        }

        context.CachedVaryByRules.Headers = StringValues.Concat(context.CachedVaryByRules.Headers, _headers);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeFromCacheAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeResponseAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
