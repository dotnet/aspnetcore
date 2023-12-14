// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every value of the provided query string keys.
/// It also disables the default behavior which is to vary on all query string keys.
/// </summary>
internal sealed class VaryByQueryPolicy : IOutputCachePolicy
{
    private readonly StringValues _queryKeys;

    private VaryByQueryPolicy()
    {
    }

    public VaryByQueryPolicy(string queryKey, params string[] queryKeys)
    {
        ArgumentNullException.ThrowIfNull(queryKey);

        _queryKeys = queryKey;

        if (queryKeys != null && queryKeys.Length > 0)
        {
            _queryKeys = StringValues.Concat(_queryKeys, queryKeys);
        }
    }

    public VaryByQueryPolicy(string[] queryKeys)
    {
        ArgumentNullException.ThrowIfNull(queryKeys);

        _queryKeys = queryKeys;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        // No vary by query?
        if (_queryKeys.Count == 0)
        {
            context.CacheVaryByRules.QueryKeys = _queryKeys;
            return ValueTask.CompletedTask;
        }

        // If the current key is "*" (default) replace it
        if (context.CacheVaryByRules.QueryKeys.Count == 1 && string.Equals(context.CacheVaryByRules.QueryKeys[0], "*", StringComparison.Ordinal))
        {
            context.CacheVaryByRules.QueryKeys = _queryKeys;
            return ValueTask.CompletedTask;
        }

        context.CacheVaryByRules.QueryKeys = StringValues.Concat(context.CacheVaryByRules.QueryKeys, _queryKeys);

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
