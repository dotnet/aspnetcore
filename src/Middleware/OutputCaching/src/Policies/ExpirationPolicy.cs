// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that defines a custom expiration duration.
/// </summary>
internal sealed class ExpirationPolicy : IOutputCachePolicy
{
    private readonly TimeSpan _expiration;

    /// <summary>
    /// Creates a new <see cref="ExpirationPolicy"/> instance.
    /// </summary>
    /// <param name="expiration">The expiration duration.</param>
    public ExpirationPolicy(TimeSpan expiration)
    {
        _expiration = expiration;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        context.ResponseExpirationTimeSpan = _expiration;

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
