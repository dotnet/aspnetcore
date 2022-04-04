// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every provided value.
/// </summary>
public class VaryByValuePolicy : IOutputCachingPolicy
{
    private readonly Action<CachedVaryByRules> _varyBy;
    private readonly Func<CachedVaryByRules, Task> _varyByAsync;

    /// <summary>
    /// Creates a policy that doesn't vary the cached content based on values.
    /// </summary>
    public VaryByValuePolicy()
    {
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified value.
    /// </summary>
    public VaryByValuePolicy(Func<string> varyBy)
    {
        _varyBy = (c) => c.VaryByPrefix = c.VaryByPrefix + varyBy();
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified value.
    /// </summary>
    public VaryByValuePolicy(Func<Task<string>> varyBy)
    {
        _varyByAsync = async (c) => c.VaryByPrefix = c.VaryByPrefix + await varyBy();
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified value.
    /// </summary>
    public VaryByValuePolicy(Func<(string, string)> varyBy)
    {
        _varyBy = (c) =>
        {
            var result = varyBy();
            c.VaryByCustom.TryAdd(result.Item1, result.Item2);
        };
    }

    /// <summary>
    /// Creates a policy that vary the cached content based on the specified value.
    /// </summary>
    public VaryByValuePolicy(Func<Task<(string, string)>> varyBy)
    {
        _varyBy = async (c) =>
        {
            var result = await varyBy();
            c.VaryByCustom.TryAdd(result.Item1, result.Item2);
        };
    }

    public Task OnRequestAsync(IOutputCachingContext context)
    {
        _varyBy?.Invoke(context.CachedVaryByRules);

        return _varyByAsync?.Invoke(context.CachedVaryByRules) ?? Task.CompletedTask;
    }

    public Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    public Task OnServeResponseAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
