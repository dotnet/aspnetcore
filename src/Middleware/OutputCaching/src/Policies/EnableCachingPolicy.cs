// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that enables caching
/// </summary>
public sealed class EnableCachingPolicy : IOutputCachingPolicy
{
    /// <summary>
    /// Default instance of <see cref="EnableCachingPolicy"/>.
    /// </summary>
    public static readonly EnableCachingPolicy Instance = new();

    /// <inheritdoc />
    public Task OnRequestAsync(IOutputCachingContext context)
    {
        context.EnableOutputCaching = true;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnServeResponseAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
