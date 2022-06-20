// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy represented by a named profile.
/// </summary>
internal sealed class ProfilePolicy : IOutputCachePolicy
{
    private readonly string _profileName;

    /// <summary>
    /// Create a new <see cref="ProfilePolicy"/> instance.
    /// </summary>
    /// <param name="profileName">The name of the profile.</param>
    public ProfilePolicy(string profileName)
    {
        _profileName = profileName;
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.ServeResponseAsync(context);
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.ServeFromCacheAsync(context);
    }

    /// <inheritdoc />
    Task IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.CacheRequestAsync(context); ;
    }

    internal IOutputCachePolicy? GetProfilePolicy(OutputCacheContext context)
    {
        var policies = context.Options.NamedPolicies;

        return policies != null && policies.TryGetValue(_profileName, out var cacheProfile)
            ? cacheProfile
            : null;
    }
}
