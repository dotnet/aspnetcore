// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Represents a cache for an AuthorizationPolicy instance
/// </summary>
public class AuthorizationPolicyCache
{
    /// <summary>
    /// The cached policy.
    /// </summary>
    public AuthorizationPolicy? Policy { get; set; }
}
