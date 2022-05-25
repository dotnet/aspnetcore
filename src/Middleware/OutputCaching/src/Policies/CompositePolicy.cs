// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching.Policies;

/// <summary>
/// A composite policy.
/// </summary>
internal sealed class CompositePolicy : IOutputCachingPolicy
{
    private readonly IOutputCachingPolicy[] _policies;

    /// <summary>
    /// Creates a new instance of <see cref="CompositePolicy"/>
    /// </summary>
    /// <param name="policies">The policies to include.</param>
    public CompositePolicy(params IOutputCachingPolicy[] policies!!)
    {
        _policies = policies;
    }

    /// <inheritdoc/>
    async Task IOutputCachingPolicy.OnRequestAsync(IOutputCachingContext context)
    {
        foreach (var policy in _policies)
        {
            await policy.OnRequestAsync(context);
        }
    }

    /// <inheritdoc/>
    async Task IOutputCachingPolicy.OnServeFromCacheAsync(IOutputCachingContext context)
    {
        foreach (var policy in _policies)
        {
            await policy.OnServeFromCacheAsync(context);
        }
    }

    /// <inheritdoc/>
    async Task IOutputCachingPolicy.OnServeResponseAsync(IOutputCachingContext context)
    {
        foreach (var policy in _policies)
        {
            await policy.OnServeResponseAsync(context);
        }
    }
}
