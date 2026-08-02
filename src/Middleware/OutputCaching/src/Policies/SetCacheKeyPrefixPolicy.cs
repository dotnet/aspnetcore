// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that sets the cache key prefix using the specified value.
/// </summary>
internal sealed class SetCacheKeyPrefixPolicy : IOutputCachePolicy
{
    private readonly Func<HttpContext, CacheVaryByRules, CancellationToken, ValueTask> _varyByAsync;

    /// <summary>
    /// Creates a policy that varies the cache key using the specified value.
    /// </summary>
    public SetCacheKeyPrefixPolicy(Func<HttpContext, CancellationToken, ValueTask<string>> varyBy)
    {
        _varyByAsync = async (context, rules, cancellationToken) => rules.CacheKeyPrefix = await varyBy(context, cancellationToken);
    }

    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return _varyByAsync.Invoke(context.HttpContext, context.CacheVaryByRules, cancellationToken);
    }

    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
