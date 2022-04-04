// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that defines custom tags on the cache entry.
/// </summary>
public class TagsPolicy : IOutputCachingPolicy
{
    private readonly string[] _tags;

    public TagsPolicy(params string[] tags)
    {
        _tags = tags;
    }

    public Task OnRequestAsync(IOutputCachingContext context)
    {
        foreach (var tag in _tags)
        {
            context.Tags.Add(tag);
        }

        return Task.CompletedTask;
    }

    public Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    public Task OnServeResponseAsync(IOutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
