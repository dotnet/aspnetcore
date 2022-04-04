// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that defines a custom expiration timespan.
/// </summary>
public class LockingPolicy : IOutputCachingPolicy
{
    private readonly bool _lockResponse;

    public LockingPolicy(bool lockResponse)
    {
        _lockResponse = lockResponse;
    }

    public Task OnRequestAsync(IOutputCachingContext context)
    {
        context.AllowLocking = _lockResponse;

        return Task.CompletedTask;
    }

    public Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    public Task OnServeResponseAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
