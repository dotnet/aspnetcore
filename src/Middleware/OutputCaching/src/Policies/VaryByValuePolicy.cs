// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every provided value.
/// </summary>
internal sealed class VaryByValuePolicy : IOutputCachePolicy
{
    private readonly Action<HttpContext, CacheVaryByRules>? _varyBy;
    private readonly Func<HttpContext, CacheVaryByRules, CancellationToken, ValueTask>? _varyByAsync;

    /// <summary>
    /// Creates a policy that doesn't vary the cached content based on values.
    /// </summary>
    public VaryByValuePolicy()
    {
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified value.
    /// </summary>
    public VaryByValuePolicy(Func<HttpContext, string> varyBy)
    {
        _varyBy = (context, rules) => rules.VaryByPrefix += varyBy(context);
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified value.
    /// </summary>
    public VaryByValuePolicy(Func<HttpContext, CancellationToken, ValueTask<string>> varyBy)
    {
        _varyByAsync = async (context, rules, token) => rules.VaryByPrefix += await varyBy(context, token);
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified value.
    /// </summary>
    public VaryByValuePolicy(Func<HttpContext, KeyValuePair<string, string>> varyBy)
    {
        _varyBy = (context, rules) =>
        {
            var result = varyBy(context);
            rules.VaryByCustom?.TryAdd(result.Key, result.Value);
        };
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified value.
    /// </summary>
    public VaryByValuePolicy(Func<HttpContext, CancellationToken, ValueTask<KeyValuePair<string, string>>> varyBy)
    {
        _varyBy = async (context, rules) =>
        {
            var result = await varyBy(context, context.RequestAborted);
            rules.VaryByCustom?.TryAdd(result.Key, result.Value);
        };
    }

    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        _varyBy?.Invoke(context.HttpContext, context.CacheVaryByRules);

        return _varyByAsync?.Invoke(context.HttpContext, context.CacheVaryByRules, context.HttpContext.RequestAborted) ?? ValueTask.CompletedTask;
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
