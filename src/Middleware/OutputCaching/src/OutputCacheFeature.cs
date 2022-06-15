// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OutputCaching.Policies;

namespace Microsoft.AspNetCore.OutputCaching;

internal class OutputCacheFeature : IOutputCacheFeature
{
    public OutputCacheFeature(OutputCacheContext context)
    {
        Context = context;
    }

    public OutputCacheContext Context { get; }

    public PoliciesCollection Policies { get; } = new();
}
