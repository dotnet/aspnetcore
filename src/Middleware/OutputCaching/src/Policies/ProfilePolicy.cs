// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that prevents caching
/// </summary>
public class ProfilePolicy : IOutputCachingPolicy
{
    private readonly string _profileName;

    public ProfilePolicy(string profileName)
    {
        _profileName = profileName;
    }

    public Task OnServeResponseAsync(IOutputCachingContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.OnServeResponseAsync(context);
    }

    public Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.OnServeFromCacheAsync(context);
    }

    public Task OnRequestAsync(IOutputCachingContext context)
    {
        var policy = GetProfilePolicy(context);

        if (policy == null)
        {
            return Task.CompletedTask;
        }

        return policy.OnRequestAsync(context); ;
    }

    internal IOutputCachingPolicy GetProfilePolicy(IOutputCachingContext context)
    {
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<OutputCachingOptions>>();

        return options.Value.Profiles.TryGetValue(_profileName, out var cacheProfile)
            ? cacheProfile
            : null;
    }
}
