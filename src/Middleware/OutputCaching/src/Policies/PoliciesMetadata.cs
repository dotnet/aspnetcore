// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching.Policies;

internal sealed class PoliciesMetadata : IPoliciesMetadata
{
    private readonly List<IOutputCachingPolicy> _policies = new();

    public IReadOnlyList<IOutputCachingPolicy> Policies => _policies;

    public void Add(IOutputCachingPolicy policy) => _policies.Add(policy);
}
