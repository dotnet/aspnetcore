// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// Default implementation for <see cref="IOutputCachingFeature" />
public class OutputCachingFeature : IOutputCachingFeature
{
    public OutputCachingFeature(IOutputCachingContext context)
    {
        Context = context;
    }

    public IOutputCachingContext Context { get; }

    public List<IOutputCachingPolicy> Policies { get; } = new();
}
