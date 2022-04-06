// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Represents policies metadata for an endpoint.
/// </summary>
public interface IPoliciesMetadata
{
    /// <summary>
    /// Gets the policies.
    /// </summary>
    IReadOnlyList<IOutputCachingPolicy> Policies { get; }
}
