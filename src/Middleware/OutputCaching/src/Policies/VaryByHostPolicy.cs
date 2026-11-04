// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every value of the HOST header.
/// </summary>
internal sealed class VaryByHostPolicy : IOutputCachePolicy
{
    public static readonly VaryByHostPolicy Enabled = new(true);
    public static readonly VaryByHostPolicy Disabled = new(false);

    private readonly bool _varyByHost;

    /// <summary>
    /// Creates a policy that can vary the cached content based on the HOST header.
    /// </summary>
    private VaryByHostPolicy(bool varyByHost)
    {
        _varyByHost = varyByHost;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        context.CacheVaryByRules.VaryByHost = _varyByHost;

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
