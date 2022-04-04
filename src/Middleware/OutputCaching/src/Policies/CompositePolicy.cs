// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching.Policies;

public class CompositePolicy : IOutputCachingPolicy
{
    private readonly IOutputCachingPolicy[] _policies;

    public CompositePolicy(params IOutputCachingPolicy[] policies!!)
    {
        _policies = policies;
    }

    public async Task OnRequestAsync(IOutputCachingContext context)
    {
        foreach (var policy in _policies)
        {
            await policy.OnRequestAsync(context);
        }
    }

    public async Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        foreach (var policy in _policies)
        {
            await policy.OnServeFromCacheAsync(context);
        }
    }

    public async Task OnServeResponseAsync(IOutputCachingContext context)
    {
        foreach (var policy in _policies)
        {
            await policy.OnServeResponseAsync(context);
        }
    }
}
