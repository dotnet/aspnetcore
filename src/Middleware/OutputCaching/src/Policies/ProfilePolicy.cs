// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy represented by a named profile.
/// </summary>
internal sealed class ProfilePolicy : IOutputCachingPolicy
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
    Task IOutputCachingPolicy.OnServeResponseAsync(OutputCachingContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.OnServeResponseAsync(context);
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeFromCacheAsync(OutputCachingContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.OnServeFromCacheAsync(context);
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnRequestAsync(OutputCachingContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.OnRequestAsync(context); ;
    }

    internal IOutputCachingPolicy? GetProfilePolicy(OutputCachingContext context)
    {
        var policies = context.Options.NamedPolicies;

        return policies != null && policies.TryGetValue(_profileName, out var cacheProfile)
            ? cacheProfile
            : null;
    }
}
