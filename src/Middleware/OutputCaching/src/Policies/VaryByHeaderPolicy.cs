// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every value of the provided headers.
/// </summary>
internal sealed class VaryByHeaderPolicy : IOutputCachePolicy
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
    Task IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context)
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
    Task IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }
}
