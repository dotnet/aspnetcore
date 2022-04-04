// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// An implementation of this interface can update how the current request is cached.
/// </summary>
public interface IOutputCachingPolicy
{
    Task OnRequestAsync(IOutputCachingContext context);
    Task OnServeFromCacheAsync(IOutputCachingContext context);
    Task OnServeResponseAsync(IOutputCachingContext context);
}
