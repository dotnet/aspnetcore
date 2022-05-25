// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching.Policies;

internal sealed class PoliciesMetadata : IPoliciesMetadata
{
    public PoliciesMetadata(IOutputCachingPolicy policy)
    {
        Policy = policy;
    }

    public IOutputCachingPolicy Policy { get; }
}
