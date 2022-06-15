// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching.Policies;

/// <summary>
/// A composite policy.
/// </summary>
internal sealed class CompositePolicy : IOutputCachePolicy
{
    private readonly IOutputCachePolicy[] _policies;

    /// <summary>
    /// Creates a new instance of <see cref="CompositePolicy"/>
    /// </summary>
    /// <param name="policies">The policies to include.</param>
    public CompositePolicy(params IOutputCachePolicy[] policies!!)
    {
        _policies = policies;
    }

    /// <inheritdoc/>
    async Task IOutputCachePolicy.OnRequestAsync(OutputCacheContext context)
    {
        foreach (var policy in _policies)
        {
            await policy.OnRequestAsync(context);
        }
    }

    /// <inheritdoc/>
    async Task IOutputCachePolicy.OnServeFromCacheAsync(OutputCacheContext context)
    {
        foreach (var policy in _policies)
        {
            await policy.OnServeFromCacheAsync(context);
        }
    }

    /// <inheritdoc/>
    async Task IOutputCachePolicy.OnServeResponseAsync(OutputCacheContext context)
    {
        foreach (var policy in _policies)
        {
            await policy.OnServeResponseAsync(context);
        }
    }
}
