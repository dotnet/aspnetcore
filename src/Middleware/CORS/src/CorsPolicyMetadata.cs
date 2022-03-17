// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Microsoft.AspNetCore.Cors;

/// <summary>
/// Metadata that provides a CORS policy.
/// </summary>
public class CorsPolicyMetadata : ICorsPolicyMetadata
{
    /// <summary>
    /// Creates a new instance of <see cref="CorsPolicyMetadata"/> using the specified policy.
    /// </summary>
    /// <param name="policy">The policy which needs to be applied.</param>
    public CorsPolicyMetadata(CorsPolicy policy)
    {
        Policy = policy;
    }

    /// <summary>
    /// The policy which needs to be applied.
    /// </summary>
    public CorsPolicy Policy { get; }
}
