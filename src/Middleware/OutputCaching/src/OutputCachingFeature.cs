// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// Default implementation for <see cref="IOutputCachingFeature" />
public class OutputCachingFeature : IOutputCachingFeature
{
    /// <summary>
    /// Creates a new <see cref="OutputCachingFeature"/> instance.
    /// </summary>
    /// <param name="context"></param>
    public OutputCachingFeature(IOutputCachingContext context)
    {
        Context = context;
    }

    /// <inheritdoc />
    public IOutputCachingContext Context { get; }

    /// <inheritdoc />
    public List<IOutputCachingPolicy> Policies { get; } = new();
}
