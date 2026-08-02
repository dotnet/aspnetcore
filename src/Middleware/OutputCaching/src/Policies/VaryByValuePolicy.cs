// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every provided value.
/// </summary>
internal sealed class VaryByValuePolicy : IOutputCachePolicy
{
    private readonly Func<HttpContext, CacheVaryByRules, CancellationToken, ValueTask> _varyByAsync;

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified value.
    /// </summary>
    public VaryByValuePolicy(Func<HttpContext, CancellationToken, ValueTask<KeyValuePair<string, string>>> varyBy)
    {
        _varyByAsync = async (context, rules, cancellationToken) =>
        {
            var result = await varyBy(context, cancellationToken);
            rules.VaryByValues[result.Key] = result.Value;
        };
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
