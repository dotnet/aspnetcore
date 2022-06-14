// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy that defines custom tags on the cache entry.
/// </summary>
internal sealed class TagsPolicy : IOutputCachingPolicy
{
    private readonly string[] _tags;

    /// <summary>
    /// Creates a new <see cref="TagsPolicy"/> instance.
    /// </summary>
    /// <param name="tags">The tags.</param>
    public TagsPolicy(params string[] tags)
    {
        _tags = tags;
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnRequestAsync(OutputCachingContext context)
    {
        foreach (var tag in _tags)
        {
            context.Tags.Add(tag);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeFromCacheAsync(OutputCachingContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeResponseAsync(OutputCachingContext context)
    {
        return Task.CompletedTask;
    }
}
