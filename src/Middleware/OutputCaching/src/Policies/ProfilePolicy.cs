// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy represented by a named profile.
/// </summary>
public sealed class ProfilePolicy : IOutputCachingPolicy
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
    Task IOutputCachingPolicy.OnServeResponseAsync(IOutputCachingContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.OnServeResponseAsync(context);
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeFromCacheAsync(IOutputCachingContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.OnServeFromCacheAsync(context);
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnRequestAsync(IOutputCachingContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.OnRequestAsync(context); ;
    }

    internal IOutputCachingPolicy? GetProfilePolicy(IOutputCachingContext context)
    {
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<OutputCachingOptions>>();

        return options.Value.Policies.TryGetValue(_profileName, out var cacheProfile)
            ? cacheProfile
            : null;
    }
}
