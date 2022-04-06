// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that changes the locking behavior.
/// </summary>
public class LockingPolicy : IOutputCachingPolicy
{
    private readonly bool _lockResponse;

    /// <summary>
    /// Creates a new instance of <see cref="LockingPolicy"/>.
    /// </summary>
    /// <param name="lockResponse">Whether to lock responses or not.</param>
    public LockingPolicy(bool lockResponse)
    {
        _lockResponse = lockResponse;
    }

    /// <inheritdoc /> 
    public Task OnRequestAsync(IOutputCachingContext context)
    {
        context.AllowLocking = _lockResponse;

        return Task.CompletedTask;
    }

    /// <inheritdoc /> 
    public Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc /> 
    public Task OnServeResponseAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
