// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A named policy.
/// </summary>
internal sealed class NamedPolicy : IOutputCachePolicy
{
    private readonly string _policyName;
    /// <summary>
    /// Create a new <see cref="NamedPolicy"/> instance.
    /// </summary>
    /// <param name="policyName">The name of the profile.</param>
    public NamedPolicy(string policyName)
    {
        _policyName = policyName;
    }

    /// <inheritdoc />
    async ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var policy = await GetProfilePolicy(context);

        if (policy == null)
        {
            return;
        }

        await policy.ServeResponseAsync(context, cancellationToken);
    }

    /// <inheritdoc />
    async ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var policy = await GetProfilePolicy(context);

        if (policy == null)
        {
            return;
        }

        await policy.ServeFromCacheAsync(context, cancellationToken);
    }

    /// <inheritdoc />
    async ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var policy = await GetProfilePolicy(context);

        if (policy == null)
        {
            return;
        }

        await policy.CacheRequestAsync(context, cancellationToken);
    }

    internal ValueTask<IOutputCachePolicy?> GetProfilePolicy(OutputCacheContext context)
    {
        var provider = context.HttpContext.RequestServices.GetRequiredService<IOutputCachePolicyProvider>();
        if (provider == null)
        {
            return ValueTask.FromResult<IOutputCachePolicy?>(null);
        }

        return provider.GetPolicyAsync(_policyName);
    }
}
