// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that enables caching
/// </summary>
internal sealed class EnableCachingPolicy : IOutputCachingPolicy
{
    public static readonly EnableCachingPolicy Enabled = new();
    public static readonly EnableCachingPolicy Disabled = new();

    private EnableCachingPolicy()
    {
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnRequestAsync(OutputCachingContext context)
    {
        context.EnableOutputCaching = this == Enabled;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeResponseAsync(OutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeFromCacheAsync(OutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
