// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every value of the provided headers.
/// </summary>
internal sealed class VaryByHeaderPolicy : IOutputCachePolicy
{
    private readonly StringValues _headerNames;

    /// <summary>
    /// Creates a policy that doesn't vary the cached content based on headers.
    /// </summary>
    public VaryByHeaderPolicy()
    {
    }

    /// <summary>
    /// Creates a policy that varies the cached content based on the specified header name.
    /// </summary>
    public VaryByHeaderPolicy(string header)
    {
        ArgumentNullException.ThrowIfNull(header);

        _headerNames = header;
    }

    /// <summary>
    /// Creates a policy that varies the cached content based on the specified header names.
    /// </summary>
    public VaryByHeaderPolicy(params string[] headerNames)
    {
        ArgumentNullException.ThrowIfNull(headerNames);

        _headerNames = headerNames;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        // No vary by header?
        if (_headerNames.Count == 0)
        {
            context.CacheVaryByRules.HeaderNames = _headerNames;
            return ValueTask.CompletedTask;
        }

        context.CacheVaryByRules.HeaderNames = StringValues.Concat(context.CacheVaryByRules.HeaderNames, _headerNames);

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
