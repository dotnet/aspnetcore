// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that enables caching
/// </summary>
public class EnableCachingPolicy : IOutputCachingPolicy
{
    public static EnableCachingPolicy Instance = new EnableCachingPolicy();

    public Task OnRequestAsync(IOutputCachingContext context)
    {
        context.EnableOutputCaching = true;

        return Task.CompletedTask;
    }

    public Task OnServeResponseAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    public Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
