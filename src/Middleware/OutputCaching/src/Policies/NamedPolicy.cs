// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A named policy.
/// </summary>
internal sealed class NamedPolicy : IOutputCachePolicy
{
    private readonly string _policyName;
    private IOptions<OutputCacheOptions>? _options;
    private readonly object _synLock = new();

    /// <summary>
    /// Create a new <see cref="NamedPolicy"/> instance.
    /// </summary>
    /// <param name="policyName">The name of the profile.</param>
    public NamedPolicy(string policyName)
    {
        _policyName = policyName;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return ValueTask.CompletedTask;
        }

        return policy.ServeResponseAsync(context, cancellationToken);
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return ValueTask.CompletedTask;
        }

        return policy.ServeFromCacheAsync(context, cancellationToken);
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return ValueTask.CompletedTask;
        }

        return policy.CacheRequestAsync(context, cancellationToken);
    }

    internal IOutputCachePolicy? GetProfilePolicy(OutputCacheContext context)
    {
        if (_options == null)
        {
            lock (_synLock)
            {
                _options ??= context.HttpContext.RequestServices.GetRequiredService<IOptions<OutputCacheOptions>>();
            }
        }

        var policies = _options!.Value.NamedPolicies;

        return policies != null && policies.TryGetValue(_policyName, out var cacheProfile)
            ? cacheProfile
            : null;
    }
}
