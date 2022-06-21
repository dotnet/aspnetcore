// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that prevents the response from being cached.
/// </summary>
internal class NoStorePolicy : IOutputCachePolicy
{
    public static NoStorePolicy Instance = new();

    private NoStorePolicy()
    {
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context)
    {
        context.AllowCacheStorage = false;

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context)
    {
        return ValueTask.CompletedTask;
    }
}
