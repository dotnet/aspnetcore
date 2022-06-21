// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that changes the locking behavior.
/// </summary>
internal sealed class LockingPolicy : IOutputCachePolicy
{
    private readonly bool _lockResponse;

    private LockingPolicy(bool lockResponse)
    {
        _lockResponse = lockResponse;
    }

    /// <summary>
    /// A policy that enables locking.
    /// </summary>
    public static readonly LockingPolicy Enabled = new(true);

    /// <summary>
    /// A policy that disabled locking/
    /// </summary>
    public static readonly LockingPolicy Disabled = new(false);

    /// <inheritdoc /> 
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context)
    {
        context.AllowLocking = _lockResponse;

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc /> 
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc /> 
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context)
    {
        return ValueTask.CompletedTask;
    }
}
