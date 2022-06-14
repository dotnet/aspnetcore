// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

internal class OutputCachingFeature : IOutputCachingFeature
{
    public OutputCachingFeature(OutputCachingContext context)
    {
        Context = context;
    }

    public OutputCachingContext Context { get; }

    public List<IOutputCachingPolicy> Policies { get; } = new();
}
