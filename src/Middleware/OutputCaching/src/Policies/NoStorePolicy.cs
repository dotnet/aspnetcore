// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that prevents the response from being cached.
/// </summary>
public class NoStorePolicy : IOutputCachingPolicy
{
    /// <inheritdoc />
    public Task OnServeResponseAsync(IOutputCachingContext context)
    {
        context.IsResponseCacheable = false;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnRequestAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
