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
    private readonly Func<HttpContext, CachedVaryByRules, Task>? _varyByAsync;

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
    public VaryByValuePolicy(Func<HttpContext, Task<string>> varyBy)
    {
        _varyByAsync = async (context, rules) => rules.VaryByPrefix += await varyBy(context);
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified value.
    /// </summary>
    public VaryByValuePolicy(Func<HttpContext, (string, string)> varyBy)
    {
        _varyBy = (context, rules) =>
        {
            var result = varyBy(context);
            rules.VaryByCustom?.TryAdd(result.Item1, result.Item2);
        };
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified value.
    /// </summary>
    public VaryByValuePolicy(Func<HttpContext, Task<(string, string)>> varyBy)
    {
        _varyBy = async (context, rules) =>
        {
            var result = await varyBy(context);
            rules.VaryByCustom?.TryAdd(result.Item1, result.Item2);
        };
    }

    /// <inheritdoc/>
    Task IOutputCachePolicy.OnRequestAsync(OutputCacheContext context)
    {
        _varyBy?.Invoke(context.HttpContext, context.CachedVaryByRules);

        return _varyByAsync?.Invoke(context.HttpContext, context.CachedVaryByRules) ?? Task.CompletedTask;
    }

    /// <inheritdoc/>
    Task IOutputCachePolicy.OnServeFromCacheAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    Task IOutputCachePolicy.OnServeResponseAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }
}
