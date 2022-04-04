// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that prevents caching
/// </summary>
public class NoStorePolicy : IOutputCachingPolicy
{
    public Task OnServeResponseAsync(IOutputCachingContext context)
    {
        context.IsResponseCacheable = false;

        return Task.CompletedTask;
    }

    public Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    public Task OnRequestAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
