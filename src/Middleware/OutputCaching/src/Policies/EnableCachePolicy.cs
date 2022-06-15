// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that enables caching
/// </summary>
internal sealed class EnableCachePolicy : IOutputCachePolicy
{
    public static readonly EnableCachePolicy Enabled = new();
    public static readonly EnableCachePolicy Disabled = new();

    private EnableCachePolicy()
    {
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.OnRequestAsync(OutputCacheContext context)
    {
        context.EnableOutputCaching = this == Enabled;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.OnServeResponseAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.OnServeFromCacheAsync(OutputCacheContext context)
    {
        return Task.CompletedTask;
    }
}
