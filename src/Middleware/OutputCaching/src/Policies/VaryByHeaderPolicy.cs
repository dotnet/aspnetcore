// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every value of the provided headers.
/// </summary>
internal sealed class VaryByHeaderPolicy : IOutputCachePolicy
{
    private readonly StringValues _headers;

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
        ArgumentNullException.ThrowIfNull(header);

        _headers = header;
    }

    /// <summary>
    /// Creates a policy that varies the cached content based on the specified query string keys.
    /// </summary>
    public VaryByHeaderPolicy(params string[] headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        _headers = headers;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        // No vary by header?
        if (_headers.Count == 0)
        {
            context.CacheVaryByRules.Headers = _headers;
            return ValueTask.CompletedTask;
        }

        context.CacheVaryByRules.Headers = StringValues.Concat(context.CacheVaryByRules.Headers, _headers);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
