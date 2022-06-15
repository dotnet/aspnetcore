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
    Task IOutputCachePolicy.OnServeResponseAsync(OutputCacheContext context)
    {
        context.AllowCacheStorage = false;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.OnServeFromCacheAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.OnRequestAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }
}
