// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that defines a custom expiration timespan.
/// </summary>
public class ExpirationPolicy : IOutputCachingPolicy
{
    private readonly TimeSpan _expiration;

    public ExpirationPolicy(TimeSpan expiration)
    {
        _expiration = expiration;
    }

    public Task OnRequestAsync(IOutputCachingContext context)
    {
        context.ResponseExpirationTimeSpan = _expiration;

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
