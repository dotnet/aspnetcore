// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that prevents the response from being served from cached.
/// </summary>
internal class NoLookupPolicy : IOutputCachePolicy
{
    public static NoLookupPolicy Instance = new();

    private NoLookupPolicy()
    {
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context)
    {
        context.AllowCacheLookup = false;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }
}
