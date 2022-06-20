// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every provided value.
/// </summary>
internal sealed class VaryByValuePolicy : IOutputCachePolicy
{
    private readonly Action<HttpContext, CachedVaryByRules>? _varyBy;
    private readonly Func<HttpContext, CachedVaryByRules, CancellationToken, Task>? _varyByAsync;

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
    public VaryByValuePolicy(Func<HttpContext, CancellationToken, Task<string>> varyBy)
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
    public VaryByValuePolicy(Func<HttpContext, CancellationToken, Task<KeyValuePair<string, string>>> varyBy)
    {
        _varyBy = async (context, rules) =>
        {
            var result = await varyBy(context, context.RequestAborted);
            rules.VaryByCustom?.TryAdd(result.Key, result.Value);
        };
    }

    /// <inheritdoc/>
    Task IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context)
    {
        _varyBy?.Invoke(context.HttpContext, context.CachedVaryByRules);

        return _varyByAsync?.Invoke(context.HttpContext, context.CachedVaryByRules, context.HttpContext.RequestAborted) ?? Task.CompletedTask;
    }

    /// <inheritdoc/>
    Task IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    Task IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }
}
